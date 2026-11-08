#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森背包測試傳送介面。
///     Inventory teleport interface for developer testing.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamTestTeleportInterface : ModSystem
{
    private TeleportButtonState? buttonState;
    private UserInterface? userInterface;

    /// <summary>
    ///     初始化背包傳送按鈕。
    ///     Initialize the inventory teleport button.
    /// </summary>
    public override void Load()
    {
        buttonState = new TeleportButtonState(() => SporeDreamTeleportService.Request(Mod));

        buttonState.Activate();

        userInterface = new UserInterface();
    }

    /// <summary>
    ///     清除介面參照。
    ///     Release interface references.
    /// </summary>
    public override void Unload()
    {
        userInterface?.SetState(null);

        userInterface = null;
        buttonState = null;
    }

    /// <summary>
    ///     只在玩家開啟背包時啟用測試按鈕。
    ///     Enable the button only while inventory is open.
    /// </summary>
    public override void UpdateUI(GameTime gameTime)
    {
        if (userInterface is null ||
            buttonState is null)
            return;

        var visible =
            !Main.gameMenu &&
            Main.playerInventory &&
            Main.LocalPlayer.active &&
            !Main.LocalPlayer.dead;

        UIState? target =
            visible ? buttonState : null;

        if (!ReferenceEquals(
                userInterface.CurrentState,
                target))
            userInterface.SetState(target);

        if (visible)
            userInterface.Update(gameTime);
    }

    /// <summary>
    ///     將測試按鈕加入原版背包介面上方。
    ///     Draw the button above the vanilla inventory layer.
    /// </summary>
    public override void ModifyInterfaceLayers(
        List<GameInterfaceLayer> layers)
    {
        var index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");

        if (index < 0)
            return;

        layers.Insert(
            index,
            new LegacyGameInterfaceLayer(
                "OccidentMod: 孢夢之森測試傳送",
                DrawInterface,
                InterfaceScaleType.UI));
    }

    /// <summary>
    ///     繪製測試傳送按鈕。
    ///     Draw the test teleport interface.
    /// </summary>
    private bool DrawInterface()
    {
        if (userInterface?.CurrentState is not null)
            userInterface.Draw(
                Main.spriteBatch,
                new GameTime());

        return true;
    }

    /// <summary>
    ///     背包內的孢夢傳送按鈕。
    ///     Inventory button for forest teleportation.
    /// </summary>
    private sealed class TeleportButtonState : UIState
    {
        private readonly Action onTeleport;

        private UITextPanel<string>? button;

        internal TeleportButtonState(
            Action onTeleport)
        {
            this.onTeleport = onTeleport;
        }

        public override void OnInitialize()
        {
            // 放在背包物品格右側。
            // Place the button beside inventory slots.
            button = new UITextPanel<string>(
                Language.GetTextValue("Mods.OccidentMod.TestTeleport.Button"),
                0.8f);

            button.Width.Set(120f, 0f);
            button.Height.Set(36f, 0f);

            button.Left.Set(570f, 0f);
            button.Top.Set(285f, 0f);

            button.BackgroundColor =
                new Color(43, 67, 57, 220);

            button.BorderColor =
                new Color(91, 139, 112);

            button.OnMouseOver += (_, _) =>
            {
                if (button is not null)
                    button.BackgroundColor =
                        new Color(62, 101, 79, 240);
            };

            button.OnMouseOut += (_, _) =>
            {
                if (button is not null)
                    button.BackgroundColor =
                        new Color(43, 67, 57, 220);
            };

            button.OnLeftClick += (_, _) => { onTeleport(); };

            Append(button);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (button?.IsMouseHovering == true)
                // 防止滑鼠點擊同時使用背包物品。
                // Prevent inventory interaction while hovering.
                Main.LocalPlayer.mouseInterface = true;
        }
    }
}