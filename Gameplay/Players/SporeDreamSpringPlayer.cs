using System;
using Microsoft.Xna.Framework;
using OccidentMod.World.SporeDreamForest;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Gameplay.Players;

/// <summary>
///     孢夢之森菌蓋彈跳玩家系統。
///     Spore Dream Forest mushroom spring player.
/// </summary>
public sealed class SporeDreamSpringPlayer : ModPlayer
{
    private int cooldown;

    private Point heldRoot;

    private int heldTicks;
    private bool holding;
    private bool landing;
    private Point landingRoot;

    // 僅由玩家所屬客戶端計算角色移動。
    // Only the owning client controls player movement.
    private bool Owner =>
        Main.netMode != NetmodeID.Server &&
        Player.whoAmI == Main.myPlayer;

    // 玩家必須處於允許彈跳的狀態。
    // Player must be eligible for spring movement.
    private bool Eligible =>
        Player.active &&
        !Player.dead &&
        !Player.ghost &&
        Player.gravDir == 1 &&
        !Player.mount.Active &&
        Player.grapCount == 0 &&
        !Player.pulley;

    public override void Initialize()
    {
        Clear();
    }

    public override void OnEnterWorld()
    {
        Clear();
    }

    public override void UpdateDead()
    {
        Clear();
    }

    /// <summary>
    ///     清除所有彈跳狀態。
    ///     Reset all spring movement states.
    /// </summary>
    private void Clear()
    {
        holding = false;
        landing = false;

        heldRoot = default;
        landingRoot = default;

        heldTicks = 0;
        cooldown = 0;
    }

    /// <summary>
    ///     重置跌落傷害與跳躍狀態。
    ///     Reset fall damage tracking and jump state.
    /// </summary>
    private void ResetFall()
    {
        Player.fallStart =
            (int)(Player.position.Y / 16f);

        Player.fallStart2 =
            (int)(Player.position.Y / 16f);

        Player.jump = 0;
    }

    /// <summary>
    ///     將玩家移動資訊同步至伺服器。
    ///     Synchronize player movement to the server.
    /// </summary>
    private void SendMovement()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        NetMessage.SendData(
            MessageID.PlayerControls,
            -1,
            -1,
            null,
            Player.whoAmI
        );
    }

    /// <summary>
    ///     在原版角色位移前攔截菌蓋接觸。
    ///     Detect mushroom cap landing before vanilla movement.
    /// </summary>
    public override void PreUpdateMovement()
    {
        if (!Owner)
            return;

        landing = false;

        if (cooldown > 0)
            cooldown--;

        // 不符合條件或按住向下時取消彈跳。
        // Cancel spring movement when ineligible.
        if (!Eligible || Player.controlDown)
        {
            holding = false;
            return;
        }

        var forest =
            SporeDreamCurvedSystem.Instance;

        // 玩家正處於菌蓋壓縮階段。
        // Player is currently compressing a mushroom cap.
        if (holding)
        {
            if (!forest.TrySpringSurface(
                    heldRoot,
                    Player.position.X,
                    Player.width,
                    out _,
                    out _))
            {
                holding = false;
                return;
            }

            heldTicks++;

            // 壓縮完成後給予向上的彈跳速度。
            // Launch upward after the compression interval.
            if (heldTicks >= SporeDreamSpringMath.PressTicks)
            {
                holding = false;

                cooldown =
                    SporeDreamSpringMath.CooldownTicks;

                Player.velocity.Y =
                    -SporeDreamSpringMath.LaunchSpeed;

                ResetFall();
                SendMovement();

                return;
            }

            Player.velocity.Y = 0f;

            ResetFall();
            return;
        }

        // 冷卻期間或非下落狀態不進行落地檢查。
        // Skip landing checks during cooldown or upward motion.
        if (cooldown > 0 || Player.velocity.Y <= 0f)
            return;

        var before = Player.Bottom.Y;

        // 預測下一幀的接觸位置。
        // Predict the next frame's contact position.
        if (forest.TrySpringLanding(
                Player.position.X + Player.velocity.X,
                Player.width,
                before,
                before + Player.velocity.Y,
                Player.velocity.Y,
                out landingRoot,
                out var top))
        {
            // 避免高速下落穿過菌蓋。
            // Prevent fast falling through the mushroom cap.
            Player.velocity.Y = Math.Max(
                0f,
                top - before
            );

            landing = true;

            ResetFall();
        }
    }

    /// <summary>
    ///     位移後固定玩家與菌蓋的接觸高度。
    ///     Align the player with the cap after movement.
    /// </summary>
    public override void PostUpdate()
    {
        if (!Owner || !Eligible)
            return;

        var forest =
            SporeDreamCurvedSystem.Instance;

        var root = holding
            ? heldRoot
            : landingRoot;

        if (!holding && !landing)
            return;

        if (!forest.TrySpringSurface(
                root,
                Player.position.X,
                Player.width,
                out var top,
                out var depression))
        {
            holding = false;
            landing = false;
            return;
        }

        var target = top + depression;

        // 避免強行移動玩家穿過實心地形。
        // Prevent forced movement through solid terrain.
        if (Math.Abs(Player.Bottom.Y - target) >
            (holding ? 48f : 5f) ||
            Collision.SolidCollision(
                new Vector2(
                    Player.position.X,
                    target - Player.height
                ),
                Player.width,
                Player.height))
        {
            holding = false;
            landing = false;
            return;
        }

        // 玩家首次落到菌蓋時觸發壓縮動畫。
        // Start compression when the player first lands.
        if (!holding)
        {
            if (!forest.StartSpring(root))
            {
                landing = false;
                return;
            }

            heldRoot = root;
            holding = true;

            heldTicks = forest.SpringAge(root);

            forest.TrySpringSurface(
                root,
                Player.position.X,
                Player.width,
                out top,
                out depression
            );

            target = top + depression;

            SendMovement();
        }

        Player.position.Y =
            target - Player.height;

        Player.velocity.Y = 0f;

        ResetFall();

        landing = false;
    }
}