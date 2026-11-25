using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;

namespace VesperMod.Integration.TModLoader.Configs;

public enum AudioProfile
{
    SinglePlayer,
    Multiplayer,
}

public sealed class MultiplayerAudioConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(AudioProfile.Multiplayer)]
    public AudioProfile EditingProfile { get; set; } = AudioProfile.Multiplayer;

    [Header("Music")]
    [DefaultValue(true)]
    public bool SyncBardMusic { get; set; } = true;

    [JsonIgnore]
    [ShowDespiteJsonIgnore]
    [Slider]
    [CustomModConfigItem(typeof(SyncVolumeSlider))]
    public float BardVolumeSlider
    {
        get => EditingProfile == AudioProfile.SinglePlayer ? singlePlayerBardVolume : multiplayerBardVolume;
        set
        {
            if (EditingProfile == AudioProfile.SinglePlayer)
                singlePlayerBardVolume = value;
            else
                multiplayerBardVolume = value;
        }
    }

    [Header("Sound")]
    [DefaultValue(true)]
    public bool SyncMountSounds { get; set; } = true;

    [JsonIgnore]
    [ShowDespiteJsonIgnore]
    [Slider]
    [CustomModConfigItem(typeof(SyncVolumeSlider))]
    public float MountVolumeSlider
    {
        get => EditingProfile == AudioProfile.SinglePlayer ? SinglePlayerMountVolume : MultiplayerOwnMountVolume;
        set
        {
            if (EditingProfile == AudioProfile.SinglePlayer)
                SinglePlayerMountVolume = value;
            else
                MultiplayerOwnMountVolume = value;
        }
    }

    [JsonIgnore]
    [ShowDespiteJsonIgnore]
    [Slider]
    [CustomModConfigItem(typeof(SyncVolumeSlider))]
    public float NearbyMountVolumeSlider
    {
        get => MultiplayerMountVolume;
        set => MultiplayerMountVolume = value;
    }

    [JsonProperty]
    [DefaultValue(0.35f)]
    internal float MultiplayerOwnMountVolume = 0.35f;

    [JsonProperty("BardMusicVolume", DefaultValueHandling = DefaultValueHandling.Ignore)]
    private float? LegacyBardVolume = null;

    [JsonProperty("MountSoundVolume", DefaultValueHandling = DefaultValueHandling.Ignore)]
    private float? LegacyMountVolume = null;

    [JsonProperty("SinglePlayerBardMusicVolume")]
    [DefaultValue(1f)]
    private float singlePlayerBardVolume = 1f;

    [JsonProperty("MultiplayerBardMusicVolume", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
    private float? savedMultiplayerBardVolume;

    private float multiplayerBardVolume
    {
        get => savedMultiplayerBardVolume ?? 1f;
        set => savedMultiplayerBardVolume = value;
    }

    [JsonProperty]
    [DefaultValue(0.35f)]
    internal float SinglePlayerMountVolume = 0.35f;

    [JsonProperty("MultiplayerMountSoundVolume", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
    private float? savedMultiplayerMountVolume;

    internal float MultiplayerMountVolume
    {
        get => savedMultiplayerMountVolume ?? 0.35f;
        set => savedMultiplayerMountVolume = value;
    }

    [OnDeserialized]
    private void RestoreLegacyVolumes(StreamingContext context)
    {
        savedMultiplayerBardVolume ??= LegacyBardVolume;
        savedMultiplayerMountVolume ??= LegacyMountVolume;

        LegacyBardVolume = LegacyMountVolume = null;
    }
}
