using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Occident.Common.Classes;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Occident.Common.UI.CharacterCreation;

// Independent second-step tabs. Add new tab contents here without modifying appearance controls.
internal sealed class ClassCreationPage
{
    private readonly List<UIColoredImageButton> buttons = new();
    private readonly List<ClassCreationCategory> contents = new();
    private int selectedTab = -1;

    public UIElement Header { get; }
    public UIElement Content { get; } = new();
    public IReadOnlyList<UIColoredImageButton> Buttons => buttons;
    public IReadOnlyList<UIDifficultyButton> ClassButtons => contents[selectedTab].Buttons;
    public int SelectedTab => selectedTab;
    public ClassCategory SelectedCategory => contents[selectedTab].Category;

    public ClassCreationPage(UIElement appearanceHeader, Player player)
    {
        Header = new UIElement
        {
            Width = appearanceHeader.Width,
            Height = appearanceHeader.Height,
            Left = appearanceHeader.Left,
            Top = appearanceHeader.Top,
            HAlign = appearanceHeader.HAlign,
            VAlign = appearanceHeader.VAlign
        };
        Header.SetPadding(0f);
        Header.PaddingTop = appearanceHeader.PaddingTop;
        Header.PaddingBottom = appearanceHeader.PaddingBottom;
        Header.PaddingLeft = appearanceHeader.PaddingLeft;
        Header.PaddingRight = appearanceHeader.PaddingRight;
        Content.SetPadding(0f);
        Content.Width.Set(0f, 1f);
        Content.Height.Set(0f, 1f);

        // Melee, ranged, magic, summon. Use modest vanilla weapons without extra effects.
        AddTab(ItemID.IronBroadsword, new ClassCreationCategory(player, ClassCategory.Melee));
        AddTab(ItemID.WoodenBow, new ClassCreationCategory(player, ClassCategory.Ranged));
        AddTab(ItemID.WandofSparking, new ClassCreationCategory(player, ClassCategory.Magic));
        AddTab(ItemID.BabyBirdStaff, new ClassCreationCategory(player, ClassCategory.Summon));

        // Match MakeCategoriesBar's separator, including its tint and position below the buttons.
        var separator = new UIHorizontalSeparator(2, true)
        {
            HAlign = 0.5f,
            VAlign = 1f,
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f,
            IgnoresMouseInteraction = true
        };
        separator.Width.Set(-20f, 1f);
        separator.Top.Set(6f, 0f);
        Header.Append(separator);
    }

    internal void AddTab(int itemType, ClassCreationCategory content)
    {
        int index = buttons.Count;
        Main.instance.LoadItem(itemType);
        var button = new UIColoredImageButton(TextureAssets.Item[itemType]);
        button.Top.Set(0f, 0f);
        button.Width.Set(44f, 0f);
        button.Height.Set(44f, 0f);
        button.SetSnapPoint("ClassCreationTab", index);
        button.OnLeftClick += (_, _) =>
        {
            SelectTab(index);
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        content.Width.Set(0f, 1f);
        content.Height.Set(0f, 1f);
        buttons.Add(button);
        contents.Add(content);
        Header.Append(button);

        for (int i = 0; i < buttons.Count; i++)
            buttons[i].Left.Set(-buttons.Count * 48f / 2f + i * 48f, 0.5f);
        if (selectedTab < 0)
            SelectTab(index);
        else
            RefreshColors();
        Header.Recalculate();
    }

    private void SelectTab(int index)
    {
        selectedTab = index;
        Content.RemoveAllChildren();
        Content.Append(contents[index]);
        RefreshColors();
        Content.Recalculate();
    }

    private void RefreshColors()
    {
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].SetSelected(i == selectedTab);
    }
}
