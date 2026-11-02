using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace Occident.Common.UI.CharacterCreation;

// Preview-only UI state. No equipment, inventory or saved character data is changed.
internal sealed class ClassPreviewToggle : UIPanel
{
    private const string Prefix = "Mods.Occident.UI.CharacterCreation.";
    private readonly Color accent;
    private readonly UIText label, status;
    private string lastCulture = "";
    public bool Enabled { get; private set; }

    public ClassPreviewToggle(string localizationKey, Color accent)
    {
        this.accent = accent;
        Width.Set(0f, 1f);
        Height.Set(46f, 0f);
        SetPadding(0f);
        label = new UIText(Language.GetText(localizationKey), 0.85f)
        {
            HAlign = 0.5f,
            TextColor = accent,
            IgnoresMouseInteraction = true
        };
        label.Top.Set(7f, 0f);
        Append(label);
        status = new UIText(Language.GetText(Prefix + "PreviewOff"), 0.65f)
        {
            HAlign = 0.5f,
            IgnoresMouseInteraction = true
        };
        status.Top.Set(27f, 0f);
        Append(status);
        OnLeftClick += (_, _) =>
        {
            Enabled = !Enabled;
            RefreshStatus();
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
    }

    public override void Update(GameTime gameTime)
    {
        if (lastCulture != Language.ActiveCulture.Name)
            RefreshStatus();
        base.Update(gameTime);
    }

    private void RefreshStatus()
    {
        lastCulture = Language.ActiveCulture.Name;
        status.SetText(Language.GetText(Prefix + (Enabled ? "PreviewOn" : "PreviewOff")));
        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        BackgroundColor = Enabled ? Color.Lerp(new Color(24, 33, 55), accent, 0.18f)
            : IsMouseHovering ? new Color(34, 46, 69) : new Color(24, 33, 55);
        BorderColor = Enabled ? accent : IsMouseHovering ? accent * 0.8f : new Color(63, 82, 125);
        label.TextColor = IsMouseHovering || Enabled ? accent : accent * 0.9f;
        status.TextColor = Enabled ? new Color(230, 233, 240) : new Color(145, 158, 180);
        base.DrawSelf(spriteBatch);
    }
}
