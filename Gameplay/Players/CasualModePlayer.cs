using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Gameplay.Players;

/// <summary>
///     保存休閒模式選擇，並讓經典角色死亡時保留金錢。
///     Stores casual mode settings and preserves coins on death for classic characters.
/// </summary>
public sealed class CasualModePlayer : ModPlayer
{
    /// <summary>
    ///     取得或設定休閒模式是否啟用。
    ///     Gets or sets whether casual mode is enabled.
    /// </summary>

    public bool Enabled { get; internal set; }

    /// <summary>
    ///     判斷休閒模式是否對目前角色生效。
    ///     Determines whether casual mode is active for the current character.
    /// </summary>

    public bool IsActive => Enabled && Player.difficulty == 0;

    /// <summary>
    ///     初始化休閒模式狀態。
    ///     Initializes the casual mode state.
    /// </summary>
    public override void Initialize()
    {
        Enabled = false;
    }

    /// <summary>
    ///     註冊玩家死亡掉錢事件。
    ///     Registers the player coin-drop hook.
    /// </summary>
    public override void Load()
    {
        On_Player.DropCoins += KeepCoins;
    }

    /// <summary>
    ///     解除玩家死亡掉錢事件。
    ///     Unregisters the player coin-drop hook.
    /// </summary>
    public override void Unload()
    {
        On_Player.DropCoins -= KeepCoins;
    }

    /// <summary>
    ///     儲存休閒模式設定。
    ///     Saves the casual mode setting.
    /// </summary>
    public override void SaveData(TagCompound tag)
    {
        if (IsActive)
            tag["CasualMode"] = true;
    }

    /// <summary>
    ///     載入休閒模式設定。
    ///     Loads the casual mode setting.
    /// </summary>
    public override void LoadData(TagCompound tag)
    {
        Enabled = tag.GetBool("CasualMode");
    }

    /// <summary>
    ///     休閒模式生效時阻止死亡掉錢。
    ///     Prevents coin loss on death while casual mode is active.
    /// </summary>
    private static long KeepCoins(
        On_Player.orig_DropCoins orig,
        Player player)
    {
        if (!player.TryGetModPlayer<CasualModePlayer>(out var mode) ||
            !mode.IsActive)
            return orig(player);

        // 僅略過金錢掉落，不修改死亡與重生機制。
        // Skip coin loss without modifying death or respawn behavior.
        player.lostCoins = 0;
        player.lostCoinString = Main.ValueToCoins(0);

        return 0;
    }
}