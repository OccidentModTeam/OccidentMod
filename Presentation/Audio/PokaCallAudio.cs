using Microsoft.Xna.Framework;
using VesperMod.Integration.TModLoader.Configs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace VesperMod.Presentation.Audio;

internal static class PokaCallAudio
{
    private static readonly SoundStyle Call = new("VesperMod/Assets/Sounds/Mounts/PokaCall")
    {
        MaxInstances = 1,
    };

    internal static void Play(int source, Vector2 position, float pitch)
    {
        var config = ModContent.GetInstance<MultiplayerAudioConfig>();
        float volume;
        if (Main.netMode == NetmodeID.SinglePlayer)
            volume = config.SinglePlayerMountVolume;
        else if (source == Main.myPlayer)
            volume = config.MultiplayerOwnMountVolume;
        else
            volume = config.SyncMountSounds ? config.MultiplayerMountVolume : 0f;

        if (float.IsFinite(volume) && volume > 0f)
            SoundEngine.PlaySound(Call with { Pitch = pitch, Volume = MathHelper.Clamp(volume, 0f, 1f) }, position);
    }
}
