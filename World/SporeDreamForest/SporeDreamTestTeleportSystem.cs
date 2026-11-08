#nullable enable

using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.World.SporeDreamForest;

/// <summary>
///     孢夢之森開發者測試傳送快捷鍵。
///     Developer teleport hotkey for Spore Dream Forest.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamTestTeleportSystem : ModSystem
{
    // F6 快捷鍵，由 tModLoader 管理。
    // F6 keybind managed by tModLoader.
    internal static ModKeybind? TeleportKeybind { get; private set; }

    /// <summary>
    ///     註冊預設 F6 快捷鍵。
    ///     Register the default F6 keybind.
    /// </summary>
    public override void Load()
    {
        TeleportKeybind = KeybindLoader.RegisterKeybind(
            Mod,
            "SporeDreamTestTeleport",
            "F6"
        );
    }

    /// <summary>
    ///     清除快捷鍵參照。
    ///     Clear the keybind reference.
    /// </summary>
    public override void Unload()
    {
        TeleportKeybind = null;
    }

    /// <summary>
    ///     進入或重設世界時清除傳送冷卻。
    ///     Reset teleport cooldowns for a new world.
    /// </summary>
    public override void ClearWorld()
    {
        SporeDreamTeleportService.Reset();
    }
}

/// <summary>
///     處理本機玩家的 F6 按鍵輸入。
///     Handles F6 input for the local player.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SporeDreamTestTeleportPlayer : ModPlayer
{
    /// <summary>
    ///     遊戲中按下 F6 時提出傳送要求。
    ///     Request teleportation when F6 is pressed.
    /// </summary>
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var keybind =
            SporeDreamTestTeleportSystem.TeleportKeybind;

        if (keybind is null || !keybind.JustPressed)
            return;

        // 只處理本機玩家。
        // Handle local player input only.
        if (Player.whoAmI != Main.myPlayer ||
            Main.gameMenu ||
            Main.netMode == NetmodeID.Server ||
            !Player.active ||
            Player.dead ||
            Player.ghost)
            return;

        SporeDreamTeleportService.Request(Mod);
    }
}