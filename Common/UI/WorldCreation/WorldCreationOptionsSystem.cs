// 將自訂選項接到原版世界建立流程，確認建立時交接待生成設定。
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Occident.Common.Worlds;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.UI;

namespace Occident.Common.UI.WorldCreation;

[Autoload(Side = ModSide.Client)]
public sealed class WorldCreationOptionsSystem : ModSystem
{
    private ConditionalWeakTable<UIWorldCreation, WorldCreationChoices> pages = new();
    private WorldCreationChoices Choices(UIWorldCreation page) => pages.GetValue(page, p => new(p));

    public override void Load()
    {
        On_UIWorldCreation.AddWorldSizeOptions += AddSize;
        On_UIWorldCreation.AddWorldDifficultyOptions += AddDifficulty;
        On_UIWorldCreation.MakeInfoMenu += ExpandDifficultyRow;
        On_UIWorldCreation.ClickSizeOption += SelectVanillaSize;
        On_UIWorldCreation.ClickDifficultyOption += SelectVanillaDifficulty;
        On_UIWorldCreation.SetDefaultOptions += SetDefaults;
        On_UIWorldCreation.UpdateSliders += UpdateSelection;
        On_UIWorldCreation.FinishCreatingWorld += Finish;
        On_UIWorldCreation.ProcessSeed += ProcessSeed;
    }

    public override void Unload()
    {
        On_UIWorldCreation.AddWorldSizeOptions -= AddSize;
        On_UIWorldCreation.AddWorldDifficultyOptions -= AddDifficulty;
        On_UIWorldCreation.MakeInfoMenu -= ExpandDifficultyRow;
        On_UIWorldCreation.ClickSizeOption -= SelectVanillaSize;
        On_UIWorldCreation.ClickDifficultyOption -= SelectVanillaDifficulty;
        On_UIWorldCreation.SetDefaultOptions -= SetDefaults;
        On_UIWorldCreation.UpdateSliders -= UpdateSelection;
        On_UIWorldCreation.FinishCreatingWorld -= Finish;
        On_UIWorldCreation.ProcessSeed -= ProcessSeed;
        pages = new();
    }

    private void AddSize(On_UIWorldCreation.orig_AddWorldSizeOptions orig, UIWorldCreation page,
        UIElement parent, float top, UIElement.MouseEvent click, string group, float width)
    {
        orig(page, parent, top, click, group, width);
        Choices(page).AddSize(parent, top);
    }

    private void AddDifficulty(On_UIWorldCreation.orig_AddWorldDifficultyOptions orig, UIWorldCreation page,
        UIElement parent, float top, UIElement.MouseEvent click, string group, float width)
    {
        orig(page, parent, top, click, group, width);
        Choices(page).AddDifficulty(parent, top);
    }

    private void ExpandDifficultyRow(On_UIWorldCreation.orig_MakeInfoMenu orig, UIWorldCreation page, UIElement parent)
    {
        orig(page, parent);
        var row = Choices(page).DifficultyRow;
        // 加出分頁的 40px，連同邪惡、說明、外框及底部建立按鈕一起往下排。
        foreach (var child in row.Parent.Children.ToArray())
            if (child != row && child.Top.Percent == 0f && child.Top.Pixels > row.Top.Pixels)
                child.Top.Set(child.Top.Pixels + 40f, 0f);
        var panel = parent.Parent;
        panel.Height.Set(panel.Height.Pixels + 40f, panel.Height.Percent);
        var root = panel.Parent;
        root.Height.Set(root.Height.Pixels + 40f, root.Height.Percent);
        root.Recalculate();
    }

    private void SelectVanillaSize(On_UIWorldCreation.orig_ClickSizeOption orig, UIWorldCreation page,
        UIMouseEvent evt, UIElement element)
    {
        var choices = Choices(page);
        choices.Options = choices.Options with { ExtraLarge = false };
        orig(page, evt, element);
        choices.Refresh();
        choices.DescribeSelection();
    }

    private void SelectVanillaDifficulty(On_UIWorldCreation.orig_ClickDifficultyOption orig, UIWorldCreation page,
        UIMouseEvent evt, UIElement element)
    {
        var choices = Choices(page);
        choices.Options = choices.Options with { Difficulty = OccidentWorldDifficulty.None };
        orig(page, evt, element);
        choices.Refresh();
        choices.DescribeSelection();
    }

    private void SetDefaults(On_UIWorldCreation.orig_SetDefaultOptions orig, UIWorldCreation page)
    {
        Choices(page).Options = default;
        orig(page);
        Choices(page).Refresh();
    }

    private void UpdateSelection(On_UIWorldCreation.orig_UpdateSliders orig, UIWorldCreation page)
    {
        orig(page);
        Choices(page).Refresh();
    }

    // 只有確認建立世界時才交接 PendingCreation，避免預覽選項污染別的世界。
    private void Finish(On_UIWorldCreation.orig_FinishCreatingWorld orig, UIWorldCreation page)
    {
        WorldOptionsSystem.PendingCreation = Choices(page).Options;
        try { orig(page); }
        catch
        {
            WorldOptionsSystem.PendingCreation = null;
            throw;
        }
    }

    private void ProcessSeed(On_UIWorldCreation.orig_ProcessSeed orig, UIWorldCreation page, out string seed)
    {
        orig(page, out seed);
        // 完整種子可帶原版難度及尺寸；自訂選項仍使用已說明的 Master／Large 基底。
        WorldCreationAccess.ApplyFallback(page, Choices(page).Options);
    }
}
