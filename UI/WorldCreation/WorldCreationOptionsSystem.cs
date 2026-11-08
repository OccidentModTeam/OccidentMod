using System.Linq;
using System.Runtime.CompilerServices;
using OccidentMod.World;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.UI;

namespace OccidentMod.UI.WorldCreation;

/// <summary>
///     將自訂世界選項接入原版世界建立流程。
///     Integrates custom world options into the
///     vanilla world creation workflow.
/// </summary>
/// <remarks>
///     負責原版 UI 攔截、選項同步、版面調整，
///     以及建立世界時的設定交接。
///     Handles vanilla UI hooks, option synchronization,
///     layout adjustments, and creation option handoff.
/// </remarks>
[Autoload(Side = ModSide.Client)]
public sealed class WorldCreationOptionsSystem : ModSystem
{
    private const float DifficultyRowExpansion = 40f;

    // 每個世界建立頁面擁有獨立的選項狀態。
    // Each world creation page owns independent option state.

    private ConditionalWeakTable<
        UIWorldCreation,
        WorldCreationChoices
    > pages = new();

    /// <summary>
    ///     取得指定世界建立頁面的選項管理器。
    ///     Gets the options controller for a world creation page.
    /// </summary>
    private WorldCreationChoices GetChoices(UIWorldCreation page)
    {
        return pages.GetValue(
            page,
            static currentPage =>
                new WorldCreationChoices(currentPage));
    }

    /// <summary>
    ///     註冊原版世界建立流程的攔截事件。
    ///     Registers hooks for the vanilla
    ///     world creation workflow.
    /// </summary>
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

    /// <summary>
    ///     移除所有攔截事件並清除頁面狀態快取。
    ///     Removes all hooks and clears cached page states.
    /// </summary>
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

        pages = new ConditionalWeakTable<UIWorldCreation, WorldCreationChoices>();
    }

    /// <summary>
    ///     在原版世界大小選項建立後加入超大型世界。
    ///     Adds the extra-large world option after
    ///     vanilla world size buttons are created.
    /// </summary>
    private void AddSize(
        On_UIWorldCreation.orig_AddWorldSizeOptions orig,
        UIWorldCreation page,
        UIElement parent,
        float top,
        UIElement.MouseEvent click,
        string group,
        float width)
    {
        orig(
            page,
            parent,
            top,
            click,
            group,
            width);

        GetChoices(page).AddSize(parent, top);
    }

    /// <summary>
    ///     在原版難度選項建立後加入模組難度分頁。
    ///     Adds mod difficulty tabs after vanilla
    ///     difficulty buttons are created.
    /// </summary>
    private void AddDifficulty(
        On_UIWorldCreation.orig_AddWorldDifficultyOptions orig,
        UIWorldCreation page,
        UIElement parent,
        float top,
        UIElement.MouseEvent click,
        string group,
        float width)
    {
        orig(
            page,
            parent,
            top,
            click,
            group,
            width);

        GetChoices(page).AddDifficulty(parent, top);
    }

    /// <summary>
    ///     擴充難度分頁高度，並向下移動後續原版元件。
    ///     Expands the difficulty row and shifts
    ///     subsequent vanilla UI elements downward.
    /// </summary>
    private void ExpandDifficultyRow(
        On_UIWorldCreation.orig_MakeInfoMenu orig,
        UIWorldCreation page,
        UIElement parent)
    {
        orig(page, parent);

        var row = GetChoices(page).DifficultyRow;

        ShiftFollowingElements(row);

        ExpandParentContainers(parent);
    }

    /// <summary>
    ///     將難度列下方的同層元件向下移動。
    ///     Moves sibling elements below the difficulty
    ///     row downward.
    /// </summary>
    private static void ShiftFollowingElements(UIElement row)
    {
        foreach (var child in row.Parent.Children.ToArray())
        {
            if (child == row)
                continue;

            if (child.Top.Percent != 0f)
                continue;

            if (child.Top.Pixels <= row.Top.Pixels)
                continue;

            child.Top.Set(
                child.Top.Pixels + DifficultyRowExpansion,
                0f);
        }
    }

    /// <summary>
    ///     擴充世界建立面板與外層容器高度。
    ///     Expands the world creation panel
    ///     and its outer container.
    /// </summary>
    private static void ExpandParentContainers(UIElement parent)
    {
        var panel = parent.Parent;

        panel.Height.Set(
            panel.Height.Pixels + DifficultyRowExpansion,
            panel.Height.Percent);

        var root = panel.Parent;

        root.Height.Set(
            root.Height.Pixels + DifficultyRowExpansion,
            root.Height.Percent);

        root.Recalculate();
    }

    /// <summary>
    ///     選取原版世界大小時清除超大型世界標記。
    ///     Clears the extra-large flag when
    ///     a vanilla world size is selected.
    /// </summary>
    private void SelectVanillaSize(
        On_UIWorldCreation.orig_ClickSizeOption orig,
        UIWorldCreation page,
        UIMouseEvent evt,
        UIElement element)
    {
        var choices = GetChoices(page);

        choices.Options = choices.Options with
        {
            ExtraLarge = false
        };

        orig(page, evt, element);

        choices.Refresh();
        choices.DescribeSelection();
    }

    /// <summary>
    ///     選取原版難度時清除模組自訂難度。
    ///     Clears custom difficulty when
    ///     a vanilla difficulty is selected.
    /// </summary>
    private void SelectVanillaDifficulty(
        On_UIWorldCreation.orig_ClickDifficultyOption orig,
        UIWorldCreation page,
        UIMouseEvent evt,
        UIElement element)
    {
        var choices = GetChoices(page);

        choices.Options = choices.Options with
        {
            Difficulty = OccidentWorldDifficulty.None
        };

        orig(page, evt, element);

        choices.Refresh();
        choices.DescribeSelection();
    }

    /// <summary>
    ///     重設原版選項時同步清除自訂世界設定。
    ///     Resets custom world options when
    ///     vanilla default options are restored.
    /// </summary>
    private void SetDefaults(
        On_UIWorldCreation.orig_SetDefaultOptions orig,
        UIWorldCreation page)
    {
        var choices = GetChoices(page);

        choices.Options = default;

        orig(page);

        choices.Refresh();
    }

    /// <summary>
    ///     在原版選項更新後同步自訂介面。
    ///     Synchronizes custom UI after
    ///     vanilla options are updated.
    /// </summary>
    private void UpdateSelection(
        On_UIWorldCreation.orig_UpdateSliders orig,
        UIWorldCreation page)
    {
        orig(page);

        GetChoices(page).Refresh();
    }

    /// <summary>
    ///     確認建立世界時交接自訂世界設定。
    ///     Transfers custom world options when
    ///     world creation is confirmed.
    /// </summary>
    /// <remarks>
    ///     不在預覽階段設定 PendingCreation，
    ///     避免影響其他世界的建立流程。
    ///     PendingCreation is not assigned during preview,
    ///     preventing unintended effects on other worlds.
    /// </remarks>
    private void Finish(
        On_UIWorldCreation.orig_FinishCreatingWorld orig,
        UIWorldCreation page)
    {
        WorldOptionsSystem.PendingCreation =
            GetChoices(page).Options;

        try
        {
            orig(page);
        }
        catch
        {
            // 原版建立流程發生例外時清除待建立設定。
            // Clear pending creation options if
            // the vanilla creation process throws.

            WorldOptionsSystem.PendingCreation = null;

            throw;
        }
    }

    /// <summary>
    ///     處理種子後套用自訂選項的原版備援值。
    ///     Applies vanilla fallback values for custom
    ///     options after seed processing.
    /// </summary>
    private void ProcessSeed(
        On_UIWorldCreation.orig_ProcessSeed orig,
        UIWorldCreation page,
        out string seed)
    {
        orig(page, out seed);

        // 完整種子可能包含原版難度與世界大小。
        // 自訂難度及超大型世界仍使用 Master / Large 基底。
        //
        // A full seed may contain vanilla difficulty and size.
        // Custom difficulty and extra-large worlds continue
        // using Master / Large as their vanilla bases.

        WorldCreationAccess.ApplyFallback(
            page,
            GetChoices(page).Options);
    }
}