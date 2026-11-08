using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     在原版角色難度選擇介面加入獨立的休閒模式。
///     Adds an independent Casual Mode option to the
///     vanilla character difficulty selection interface.
/// </summary>
/// <remarks>
///     休閒模式狀態由 CasualModePlayer 管理，
///     不會將自訂難度代號寫入玩家的原版 difficulty 欄位。
///     Casual Mode is managed by CasualModePlayer.
///     Its custom selection value is never written directly
///     into the player's vanilla difficulty field.
/// </remarks>
internal static class CasualCreationOptions
{
    private const byte CasualSelectionId = 4;
    private const byte CasualVanillaDifficulty = 0;

    private const float AdditionalHeight = 28f;

    private const string LocalizationPrefix =
        "Mods.OccidentMod.UI.CharacterCreation.";

    private static readonly Color CasualColor =
        new(120, 185, 132);

    /// <summary>
    ///     將休閒模式加入原版角色難度選擇介面。
    ///     Adds Casual Mode to the vanilla character
    ///     difficulty selection interface.
    /// </summary>
    /// <param name="page">
    ///     原版角色建立介面。
    ///     Vanilla character creation interface.
    /// </param>
    /// <param name="player">
    ///     正在建立的玩家角色。
    ///     Player currently being created.
    /// </param>
    /// <param name="middle">
    ///     原版角色建立介面的中間容器。
    ///     Middle container of the vanilla interface.
    /// </param>
    /// <param name="root">
    ///     原版角色建立介面的根容器。
    ///     Root container of the vanilla interface.
    /// </param>
    public static void Add(
        UICharacterCreation page,
        Player player,
        UIElement middle,
        UIElement root)
    {
        var info = GetField<UIElement>(
            page,
            "_infoContainer");

        var description = GetField<UIText>(
            page,
            "_difficultyDescriptionText");

        var row = FindDifficultyContainer(info);

        var vanillaButtons =
            GetVanillaDifficultyButtons(row);

        var mode =
            player.GetModPlayer<CasualModePlayer>();

        // 使用獨立玩家副本保存介面選取狀態。
        // 避免自訂難度代號污染原版玩家資料。
        //
        // Use a separate player instance for UI selection.
        // Never store custom difficulty IDs in vanilla player data.

        var selection = new Player
        {
            difficulty = mode.IsActive
                ? CasualSelectionId
                : player.difficulty
        };

        ConfigureVanillaButtons(
            page,
            player,
            selection,
            mode,
            vanillaButtons);

        var casualButton =
            CreateCasualButton(
                player,
                selection,
                mode,
                description);

        row.Append(casualButton);

        ExpandLayout(middle, root);

        ArrangeDifficultyButtons(
            vanillaButtons,
            casualButton);

        // 持續同步介面選取狀態。
        // Keep the UI-only selection state synchronized.

        row.OnUpdate += _ =>
        {
            selection.difficulty = mode.IsActive
                ? CasualSelectionId
                : player.difficulty;
        };

        if (mode.IsActive) SetCasualDescription(description);

        page.Recalculate();
    }

    /// <summary>
    ///     尋找包含原版難度按鈕的容器。
    ///     Finds the container holding vanilla difficulty buttons.
    /// </summary>
    private static UIElement FindDifficultyContainer(
        UIElement info)
    {
        return info.Children.Single(child => child.Children
            .OfType<UIDifficultyButton>()
            .Any());
    }

    /// <summary>
    ///     取得依照原始垂直位置排序的難度按鈕。
    ///     Gets vanilla difficulty buttons ordered
    ///     by their original vertical alignment.
    /// </summary>
    private static UIDifficultyButton[] GetVanillaDifficultyButtons(
        UIElement row)
    {
        return row.Children
            .OfType<UIDifficultyButton>()
            .OrderBy(button => button.VAlign)
            .ToArray();
    }

    /// <summary>
    ///     讓原版難度按鈕使用獨立選取狀態，
    ///     並在點擊時停用休閒模式。
    ///     Configures vanilla difficulty buttons to use
    ///     isolated selection state and disable Casual Mode
    ///     when a vanilla difficulty is selected.
    /// </summary>
    private static void ConfigureVanillaButtons(
        UICharacterCreation page,
        Player player,
        Player selection,
        CasualModePlayer mode,
        UIDifficultyButton[] buttons)
    {
        var buttonPlayerField =
            GetDifficultyButtonPlayerField();

        var refreshDescription =
            GetDifficultyDescriptionMethod();

        foreach (var button in buttons)
        {
            buttonPlayerField.SetValue(button, selection);

            button.OnLeftMouseDown += (evt, element) =>
            {
                player.difficulty = selection.difficulty;

                mode.Enabled = false;

                refreshDescription.Invoke(
                    page,
                    new object[] { evt, element });
            };
        }
    }

    /// <summary>
    ///     建立休閒模式的難度選擇按鈕。
    ///     Creates the Casual Mode difficulty selection button.
    /// </summary>
    private static UIDifficultyButton CreateCasualButton(
        Player player,
        Player selection,
        CasualModePlayer mode,
        UIText description)
    {
        var button = new UIDifficultyButton(
            selection,
            Language.GetText(LocalizationPrefix + "Casual"),
            Language.GetText(LocalizationPrefix + "CasualDescription"),
            CasualSelectionId,
            CasualColor);

        button.OnLeftMouseDown += (_, _) =>
        {
            // 休閒模式使用原版軟核難度作為儲存值。
            // 自訂模式狀態由 ModPlayer 獨立保存。
            //
            // Casual Mode stores a valid vanilla difficulty.
            // Its custom state is stored separately by ModPlayer.

            player.difficulty = CasualVanillaDifficulty;

            mode.Enabled = true;

            SetCasualDescription(description);
        };

        return button;
    }

    /// <summary>
    ///     更新休閒模式的本地化說明。
    ///     Updates the localized Casual Mode description.
    /// </summary>
    private static void SetCasualDescription(
        UIText description)
    {
        description.SetText(
            Language.GetText(
                LocalizationPrefix + "CasualDescription"));
    }

    /// <summary>
    ///     增加介面高度以容納第五個難度按鈕。
    ///     Expands the interface height to fit
    ///     the fifth difficulty button.
    /// </summary>
    private static void ExpandLayout(
        UIElement middle,
        UIElement root)
    {
        var middleParent = middle.Parent;

        middleParent.Height.Set(
            middleParent.Height.Pixels + AdditionalHeight,
            middleParent.Height.Percent);

        root.Height.Set(
            root.Height.Pixels + AdditionalHeight,
            root.Height.Percent);

        middle.Height.Set(
            middle.Height.Pixels + AdditionalHeight,
            middle.Height.Percent);
    }

    /// <summary>
    ///     重新排列原版難度與休閒模式按鈕，
    ///     並保留手把導覽順序。
    ///     Rearranges vanilla difficulty buttons and Casual Mode,
    ///     preserving gamepad navigation order.
    /// </summary>
    private static void ArrangeDifficultyButtons(
        UIDifficultyButton[] vanillaButtons,
        UIDifficultyButton casualButton)
    {
        // 休閒模式放在第一個原版難度之後。
        // Place Casual Mode immediately after
        // the first vanilla difficulty option.

        var ordered =
            new[] { vanillaButtons[0], casualButton }
                .Concat(vanillaButtons.Skip(1))
                .ToArray();

        for (var i = 0; i < ordered.Length; i++)
        {
            var button = ordered[i];

            button.Width.Set(0f, 1f);

            button.Height.Set(
                0f,
                1f / ordered.Length);

            button.VAlign =
                i / (float)(ordered.Length - 1);

            button.SetSnapPoint(
                "Middle",
                i + 1);
        }
    }

    /// <summary>
    ///     取得原版難度按鈕內部的玩家欄位。
    ///     Retrieves the private player field
    ///     from vanilla UIDifficultyButton.
    /// </summary>
    private static FieldInfo GetDifficultyButtonPlayerField()
    {
        return typeof(UIDifficultyButton).GetField(
                   "_player",
                   BindingFlags.Instance | BindingFlags.NonPublic)
               ?? throw new MissingFieldException(
                   typeof(UIDifficultyButton).FullName,
                   "_player");
    }

    /// <summary>
    ///     取得原版角色建立介面的難度說明更新方法。
    ///     Retrieves the vanilla difficulty description
    ///     update method.
    /// </summary>
    private static MethodInfo GetDifficultyDescriptionMethod()
    {
        return typeof(UICharacterCreation).GetMethod(
                   "UpdateDifficultyDescription",
                   BindingFlags.Instance | BindingFlags.NonPublic)
               ?? throw new MissingMethodException(
                   typeof(UICharacterCreation).FullName,
                   "UpdateDifficultyDescription");
    }

    /// <summary>
    ///     透過反射取得原版角色建立介面的私有欄位。
    ///     Retrieves a private field from vanilla
    ///     UICharacterCreation using reflection.
    /// </summary>
    private static T GetField<T>(
        UICharacterCreation page,
        string name)
    {
        var field = typeof(UICharacterCreation).GetField(
                        name,
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(
                        typeof(UICharacterCreation).FullName,
                        name);

        return (T)field.GetValue(page);
    }
}