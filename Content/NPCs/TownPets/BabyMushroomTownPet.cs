#nullable enable

// 菇寶寶城鎮寵物的住房、離開條件、動畫與腳底對齊。
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Tiles.Furniture;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.NPCs.TownPets;

[AutoloadHead]
public sealed class BabyMushroomTownPet : ModNPC
{
    // 原動畫每一影格的時長，單位為遊戲更新刻數。
    // Original frame durations in game ticks.
    internal static readonly int[] FrameTicks =
    {
        13, 8, 5, 5, 5, 5, 4, 4, 5, 4,
        4, 4, 4, 3, 5, 7, 13, 5, 5,
        5, 5, 4, 4, 4, 5, 4, 4, 4,
        3, 4, 7, 10, 15, 6, 5, 5, 4,
        4, 4, 4, 4, 4, 4, 4, 6, 4,
        9, 2, 2, 4, 3, 4, 6, 51,
        3, 3, 11
    };

    private int animationTick;
    private int housingCheckTimer;
    private int idleDelay;
    private Point lastHome = new(-1, -1);

    public override string Texture =>
        "OccidentMod/Assets/Sprites/NPCs/TownPets/city_baby_mushroom";

    private static int TotalAnimationTicks
    {
        get
        {
            var total = 0;

            foreach (var ticks in FrameTicks)
                total += ticks;

            return total;
        }
    }

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 57;

        NPCID.Sets.IsTownPet[Type] = true;
        NPCID.Sets.ExtraFramesCount[Type] = 0;
        NPCID.Sets.AttackFrameCount[Type] = 0;
        NPCID.Sets.ActsLikeTownNPC[Type] = true;
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.TownCat);

        NPC.townNPC = true;
        NPC.friendly = true;
        NPC.aiStyle = NPCAIStyleID.Passive;
        NPC.width = 22;
        NPC.height = 22;
        NPC.damage = 0;

        AnimationType = -1;
    }

    // 不設定個體名稱，避免入住訊息重複顯示物種名稱。
    // Avoid duplicated species names in arrival messages.
    public override List<string> SetNPCNameList()
    {
        return new List<string> { string.Empty };
    }

    public override bool UsesPartyHat()
    {
        return false;
    }

    public override bool CanGoToStatue(bool toKingStatue)
    {
        return false;
    }

    public override bool CanChat()
    {
        return false;
    }

    public override bool CheckActive()
    {
        return false;
    }

    public override bool CanTownNPCSpawn(int numTownNpCs)
    {
        return MushroomBottleEntity.AnyPlaced();
    }

    public override bool CheckConditions(
        int left,
        int right,
        int top,
        int bottom)
    {
        return MushroomBottleEntity.TestedRoomContainsBottle();
    }

    public override bool PreAI()
    {
        if (!string.IsNullOrEmpty(NPC.GivenName))
        {
            NPC.GivenName = string.Empty;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                NPC.netUpdate = true;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (++housingCheckTimer >= 120)
            {
                housingCheckTimer = 0;

                if (!MushroomBottleEntity.AnyPlaced())
                {
                    // 最後一個瓶子移除時直接離開。
                    // Leave without death or item drops.
                    NPC.active = false;

                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(
                            MessageID.SyncNPC,
                            number: NPC.whoAmI);

                    return false;
                }

                // 房間檢查從地板上方開始。
                // Check the room above its floor.
                if (!NPC.homeless &&
                    (!WorldGen.InWorld(
                         NPC.homeTileX,
                         NPC.homeTileY - 2) ||
                     !WorldGen.StartRoomCheck(
                         NPC.homeTileX,
                         NPC.homeTileY - 2) ||
                     !WorldGen.RoomNeeds(Type) ||
                     !MushroomBottleEntity
                         .TestedRoomContainsBottle()))
                {
                    NPC.homeless = true;
                    lastHome = new Point(-1, -1);
                    animationTick = 0;
                    NPC.netUpdate = true;
                }
            }

            Point home = new(
                NPC.homeTileX,
                NPC.homeTileY);

            if (!NPC.homeless &&
                home != lastHome &&
                WorldGen.InWorld(home.X, home.Y))
            {
                NPC.Bottom = new Vector2(
                    home.X * 16 + 8,
                    home.Y * 16);

                NPC.velocity = Vector2.Zero;
                lastHome = home;
                NPC.netUpdate = true;
            }

            if (idleDelay <= 0)
                idleDelay = Main.rand.Next(
                    60 * 60,
                    120 * 60 + 1);

            if (!NPC.homeless &&
                NPC.velocity.Y == 0f &&
                animationTick == 0 &&
                --idleDelay == 0)
            {
                animationTick = 1;
                NPC.netUpdate = true;
            }
        }

        if (NPC.homeless)
        {
            // 無房時交給原版城鎮 NPC 行為處理。
            // Use vanilla behavior while homeless.
            animationTick = 0;
            return true;
        }

        NPC.velocity.X = 0f;

        if (animationTick > 0)
        {
            animationTick++;

            if (animationTick > TotalAnimationTicks)
            {
                animationTick = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.netUpdate = true;
            }
        }

        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(animationTick);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        animationTick = reader.ReadInt32();
    }

    // 根據每一影格的獨立時長播放動畫。
    // Use variable duration for each animation frame.
    public override void FindFrame(int frameHeight)
    {
        var frame = 0;
        var elapsed = animationTick - 1;

        if (elapsed >= 0)
            while (frame < FrameTicks.Length - 1 &&
                   elapsed >= FrameTicks[frame])
                elapsed -= FrameTicks[frame++];

        NPC.frame = new Rectangle(
            0,
            frame * frameHeight,
            80,
            frameHeight);
    }

    public override bool PreDraw(
        SpriteBatch spriteBatch,
        Vector2 screenPos,
        Color drawColor)
    {
        // 對齊精靈圖中蘑菇的可見腳底。
        // Align the visible feet with the collision box.
        spriteBatch.Draw(
            TextureAssets.Npc[Type].Value,
            NPC.Bottom -
            screenPos +
            new Vector2(0, NPC.gfxOffY),
            NPC.frame,
            drawColor,
            NPC.rotation,
            new Vector2(39, 94),
            NPC.scale,
            SpriteEffects.None,
            0f);

        return false;
    }
}