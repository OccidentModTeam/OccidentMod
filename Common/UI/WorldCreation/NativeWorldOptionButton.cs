using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Occident.Common.UI.WorldCreation;

// 與原版、Extra World Sizes 相同，使用 GroupOptionButton 建立世界選項。
// 參考接入方式：https://github.com/RemurinTheReal/Terraria-Extra-World-Sizes/blob/main/Common/ExtraUIWorldCreation.cs
internal sealed class NativeWorldOptionButton : GroupOptionButton<int>
{
    private const string Prefix = "Mods.OccidentMod.UI.WorldCreation.";
    private readonly Func<bool> selected;
    private readonly Asset<Texture2D> optionIcon;

    public NativeWorldOptionButton(string key, Action click, Func<bool> selected)
        : base(1, Language.GetText(Prefix + key), DescriptionFor(key), TextColorFor(key), null,
            IsTab(key) ? 0.75f : 1f, IsTab(key) ? 0.5f : 1f, IsTab(key) ? 0f : 16f)
    {
        this.selected = selected;
        string assetName = key switch
        {
            "Longentity" => "Difficulty/Icons/Longentity",
            "Elation" => "Difficulty/Icons/Elation",
            "Finality" => "Difficulty/Icons/Finality",
            "ExtraLarge" => "Size/GiantWorld",
            _ => null
        };
        if (assetName is not null)
            optionIcon = ModContent.Request<Texture2D>("OccidentMod/Assets/UI/WorldCreation/" + assetName);
        OnLeftMouseDown += (_, _) => click();
        SetCurrentOption(selected() ? 1 : 0);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        if (optionIcon is null)
            return;
        var area = GetDimensions();
        var texture = optionIcon.Value;
        // 手繪圖示以原尺寸置中於原版 32px 圖示欄，不拉伸素材。
        var position = new Vector2(area.X + 1f + (32f - texture.Width) * 0.5f,
            area.Y + (area.Height - texture.Height) * 0.5f);
        spriteBatch.Draw(texture, position, IsSelected || IsMouseHovering ? Color.White : Color.White * 0.7f);
    }

    private static bool IsTab(string key) => key is "Vanilla" or "ModTab";

    public override void Update(GameTime gameTime)
    {
        SetCurrentOption(selected() ? 1 : 0);
        base.Update(gameTime);
    }

    private static LocalizedText DescriptionFor(string key) => key switch
    {
        "ExtraLarge" => Language.GetText(Prefix + "ExtraLargeHelp"),
        "Longentity" or "Elation" or "Finality" => Language.GetText(Prefix + key + "Description"),
        _ => LocalizedText.Empty
    };

    private static Color TextColorFor(string key) => key switch
    {
        "ExtraLarge" => Color.LimeGreen,
        "Elation" => Main.mcColor,
        "Finality" => Main.hcColor,
        _ => Color.White
    };

}

