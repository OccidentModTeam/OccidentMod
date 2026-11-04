// 管理自訂世界難度、尺寸選項與原版分頁的選取狀態。
using System;
using System.Collections.Generic;
using Occident.Common.Worlds;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;

namespace Occident.Common.UI.WorldCreation;

internal sealed class WorldCreationChoices(UIWorldCreation page)
{
    public WorldOptions Options;
    public UIElement DifficultyRow { get; private set; }
    private UIElement difficultyContent;
    private UIText caption;
    private bool modPage;
    private readonly List<NativeWorldOptionButton> customButtons = new();
    private UIElement[] vanillaDifficulty;
    private WorldDifficultyPreview preview;
    private (WorldOptions Options, int VanillaDifficulty, string Culture)? lastCaption;

    internal static string Text(string key) => Language.GetTextValue("Mods.OccidentMod.UI.WorldCreation." + key);

    public void AddSize(UIElement parent, float top)
    {
        var native = WorldCreationAccess.Buttons(page, true);
        for (int i = 0; i < native.Length; i++)
        {
            native[i].Width.Set(-3f, 0.25f);
            native[i].HAlign = i / 3f;
        }
        var extra = Button("ExtraLarge", () =>
        {
            WorldCreationAccess.Click(page, true, native[2]);
            Options = Options with { ExtraLarge = true };
            Refresh();
            DescribeSelection();
        }, () => Options.ExtraLarge);
        extra.Width.Set(-3f, 0.25f);
        extra.Height.Set(34f, 0f);
        extra.Top.Set(top, 0f);
        extra.HAlign = 1f;
        extra.SetSnapPoint("size", 3);
        extra.OnMouseOver += (_, _) => WorldCreationAccess.Describe(page, Text("ExtraLargeHelp"));
        extra.OnMouseOut += (_, _) => DescribeSelection();
        parent.Append(extra);
    }

    public void AddDifficulty(UIElement parent, float top)
    {
        vanillaDifficulty = WorldCreationAccess.Buttons(page, false);
        foreach (var button in vanillaDifficulty)
            button.Remove();
        DifficultyRow = new UIElement();
        DifficultyRow.SetPadding(0f);
        DifficultyRow.Width.Set(0f, 1f);
        DifficultyRow.Height.Set(80f, 0f);
        DifficultyRow.Top.Set(top, 0f);
        parent.Append(DifficultyRow);

        caption = new UIText("", 0.75f) { IgnoresMouseInteraction = true };
        caption.Top.Set(5f, 0f);
        DifficultyRow.Append(caption);
        AddTab("Vanilla", false, -200f);
        AddTab("ModTab", true, -98f);
        difficultyContent = new UIElement();
        difficultyContent.SetPadding(0f);
        difficultyContent.Top.Set(40f, 0f);
        difficultyContent.Width.Set(0f, 1f);
        difficultyContent.Height.Set(40f, 0f);
        DifficultyRow.Append(difficultyContent);
        for (int i = 1; i <= 3; i++)
        {
            var value = (OccidentWorldDifficulty)i;
            var button = Button(value.ToString(), () =>
            {
                // 真正的原版欄位保持 Master，不能把自訂 enum 傳給原版生成器。
                WorldCreationAccess.Click(page, false, vanillaDifficulty[3]);
                Options = Options with { Difficulty = value };
                Refresh();
                DescribeSelection();
            }, () => Options.Difficulty == value);
            button.Width.Set(-3f, 1f / 3f);
            button.Height.Set(34f, 0f);
            button.HAlign = (i - 1) / 2f;
            button.SetSnapPoint("difficulty", i - 1);
            button.OnMouseOver += (_, _) => WorldCreationAccess.Describe(page, Text(value + "Description"));
            button.OnMouseOut += (_, _) => DescribeSelection();
            customButtons.Add(button);
        }
        ShowPage();
    }

    private void AddTab(string text, bool isMod, float left)
    {
        var tab = Button(text, () => { modPage = isMod; ShowPage(); }, () => modPage == isMod);
        tab.Width.Set(98f, 0f);
        // 原版選取亮框內縮 7px，搭配 10px 九宮格邊角，按鈕至少需要 34px 高。
        tab.Height.Set(34f, 0f);
        tab.Left.Set(left, 1f);
        DifficultyRow.Append(tab);
    }

    private void ShowPage()
    {
        difficultyContent.RemoveAllChildren();
        if (modPage)
            foreach (var button in customButtons) difficultyContent.Append(button);
        else
            foreach (var button in vanillaDifficulty)
            {
                button.Top.Set(0f, 0f);
                difficultyContent.Append(button);
            }
        Refresh();
        DifficultyRow.Recalculate();
    }

    public void Refresh()
    {
        if (caption is null)
            return;
        preview ??= new WorldDifficultyPreview(WorldCreationAccess.Preview(page));
        preview.SetDifficulty(Options.Difficulty);
        int vanilla = WorldCreationAccess.Selected(page, false);
        var state = (Options, vanilla, Language.ActiveCulture.Name);
        if (lastCaption != state)
        {
            string difficulty = Options.Difficulty == OccidentWorldDifficulty.None
                ? Language.GetTextValue(vanilla switch
                { 1 => "UI.Expert", 2 => "UI.Master", 3 => "UI.Creative", _ => "UI.Normal" })
                : Text(Options.Difficulty.ToString());
            caption.SetText(Text("SelectedDifficulty") + difficulty);
            lastCaption = state;
        }
        if (Options.Difficulty != OccidentWorldDifficulty.None)
            WorldCreationAccess.ClearHighlight(page, false);
        // 特大使用 Large 生成，但不能同時顯示「大」也被選取。
        if (Options.ExtraLarge)
            WorldCreationAccess.ClearHighlight(page, true);
    }

    public void DescribeSelection()
    {
        string description = Options.Difficulty != OccidentWorldDifficulty.None
            ? Text(Options.Difficulty + "Description")
            : Options.ExtraLarge ? Text("ExtraLargeHelp") : "";
        WorldCreationAccess.Describe(page, description);
    }

    private static NativeWorldOptionButton Button(string text, Action click, Func<bool> selected) => new(text, click, selected);

}

