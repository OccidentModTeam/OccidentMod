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

        // Classic already retains items. Skip only its coin-drop operation, leaving death and respawn intact.
        player.lostCoins = 0;
        player.lostCoinString = Main.ValueToCoins(0);
        return 0;
    }
}
