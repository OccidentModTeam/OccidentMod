using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;

namespace OccidentMod.Integration.TModLoader.Configs;

public sealed class SyncVolumeSlider : FloatElement
{
    private bool Enabled => Item is MultiplayerAudioConfig config && (MemberInfo.Name switch
    {
        nameof(MultiplayerAudioConfig.BardVolumeSlider) =>
            config.EditingProfile == AudioProfile.SinglePlayer || config.SyncBardMusic,
        nameof(MultiplayerAudioConfig.NearbyMountVolumeSlider) =>
            config.EditingProfile == AudioProfile.Multiplayer && config.SyncMountSounds,
        _ => true,
    });

    private const int ButtonSize = 22;
    private const int TrackGap = 3;
    private Texture2D muteIcon;

    public override void OnBind()
    {
        base.OnBind();
        muteIcon = ModContent.Request<Texture2D>("OccidentMod/Assets/UI/Config/MuteSpeaker").Value;
        TextDisplayFunction = () => $"{Label}  {Proportion:P0}";
        ColorMethod = percent => Color.Lerp(Color.Black, Enabled ? SliderColor : new Color(105, 105, 105), percent);
    }

    protected override float Proportion
    {
        get => base.Proportion;
        set
        {
            if (Enabled)
                base.Proportion = value;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        // The native slider leaves its track origin in valuePosition after drawing.
        Vector2 track = IngameOptions.valuePosition;
        Rectangle button = new((int)track.X - ButtonSize - TrackGap,
            (int)track.Y - ButtonSize / 2, ButtonSize, ButtonSize);
        bool hovered = IsMouseHovering && button.Contains(Main.mouseX, Main.mouseY);
        var iconPosition = new Vector2(
            button.Center.X - muteIcon.Width / 2,
            button.Center.Y - muteIcon.Height / 2);
        spriteBatch.Draw(muteIcon, iconPosition,
            !Enabled ? new Color(140, 140, 140) : hovered ? Color.White : new Color(220, 220, 220));

        if (Enabled && hovered && PlayerInput.Triggers.JustPressed.MouseLeft && Proportion > 0f)
            Proportion = 0f;
    }
}
