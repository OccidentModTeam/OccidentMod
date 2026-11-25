using OccidentMod.Content.Mounts;
using Terraria;
using Terraria.ModLoader;

namespace OccidentMod.Content.Buffs;

public sealed class PokaBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.mount.SetMount(ModContent.MountType<PokaMount>(), player);
        player.buffTime[buffIndex] = 10;
    }
}
