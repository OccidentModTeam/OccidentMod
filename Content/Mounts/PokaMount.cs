using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using OccidentMod.Content.Buffs;

namespace OccidentMod.Content.Mounts;

public sealed class PokaMount : ModMount
{
    internal const string SpritePath = "OccidentMod/Assets/Sprites/NPCs/Prototypes/Poka_";
    internal const int FrameWidth = 27;
    internal const int FrameHeight = 25;
    internal const int WalkFrameCount = 8;
    internal const float SpriteScale = 3f;

    public override void SetStaticDefaults()
    {
        MountData.buff = ModContent.BuffType<PokaBuff>();
        MountData.heightBoost = 18;
        MountData.runSpeed = 5.25f;
        MountData.dashSpeed = MountData.runSpeed;
        MountData.acceleration = 0.23f;
        MountData.blockExtraJumps = true;
        MountData.fallDamage = 0.5f;
        MountData.spawnDust = DustID.Cloud;
        MountData.bodyFrame = 3;
        MountData.playerHeadOffset = 6;
        MountData.totalFrames = 1;
        MountData.playerYOffsets = [8];
    }

    internal static bool IsMounted(Player player) =>
        player.mount.Active && player.mount.Type == ModContent.MountType<PokaMount>();

    public override bool UpdateFrame(Player mountedPlayer, int state, Vector2 velocity) => false;
}
