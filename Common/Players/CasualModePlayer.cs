// 保存休閒模式選擇，僅讓經典角色死亡時保留金錢。
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Occident.Common.Players;

public sealed class CasualModePlayer : ModPlayer
{
    public bool Enabled { get; internal set; }
    public bool IsActive => Enabled && Player.difficulty == 0;

    public override void Initialize() => Enabled = false;

    public override void Load() => On_Player.DropCoins += KeepCoins;
    public override void Unload() => On_Player.DropCoins -= KeepCoins;

    public override void SaveData(TagCompound tag)
    {
        if (IsActive)
            tag["CasualMode"] = true;
    }

    public override void LoadData(TagCompound tag) => Enabled = tag.GetBool("CasualMode");

    private static long KeepCoins(On_Player.orig_DropCoins orig, Player player)
    {
        if (!player.TryGetModPlayer<CasualModePlayer>(out var mode) || !mode.IsActive)
            return orig(player);

        // 經典角色本來就保留物品；只略過掉錢流程，保留死亡與重生行為。
        player.lostCoins = 0;
        player.lostCoinString = Main.ValueToCoins(0);
        return 0;
    }
}
