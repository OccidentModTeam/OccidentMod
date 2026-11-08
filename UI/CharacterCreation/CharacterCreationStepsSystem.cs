using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Gameplay.Classes;
using OccidentMod.Gameplay.Players;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.Initializers;
using Terraria.ModLoader;
using Terraria.UI;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     管理角色建立介面的步驟擴充與原版事件掛接。
///     Manages character creation steps and vanilla UI hooks.
/// </summary>
/// <remarks>
///     此系統只負責原版事件轉發與頁面控制器的生命週期。
///     實際的介面排版、職業選擇及資訊面板由其他類別管理。
///     This system only handles vanilla event forwarding and
///     the lifecycle of page controllers.
///     Layout, class selection, and information panels
///     are managed by separate classes.
/// </remarks>
[Autoload(Side = ModSide.Client)]
public sealed class CharacterCreationStepsSystem : ModSystem
{
    private static ConditionalWeakTable<
        UICharacterCreation,
        CharacterCreationSteps> _pages = new();

    /// <summary>
    ///     註冊角色建立介面的原版事件。
    ///     Registers vanilla character creation UI hooks.
    /// </summary>
    public override void Load()
    {
        On_UICharacterCreation.BuildPage += Build;
        On_UICharacterCreation.Click_NamingAndCreating += Continue;
        On_UICharacterCreation.Click_GoBack += Back;
        On_UICharacterCreation.TryMovingCategory += MoveCategory;
        On_UICharacterCreation.SetupGamepadPoints += SetupGamepad;
        On_UILinksInitializer.FancyExit += ExitWithGamepad;
    }

    /// <summary>
    ///     移除事件掛接並清除頁面控制器參考。
    ///     Removes hooks and clears page controller references.
    /// </summary>
    public override void Unload()
    {
        On_UICharacterCreation.BuildPage -= Build;
        On_UICharacterCreation.Click_NamingAndCreating -= Continue;
        On_UICharacterCreation.Click_GoBack -= Back;
        On_UICharacterCreation.TryMovingCategory -= MoveCategory;
        On_UICharacterCreation.SetupGamepadPoints -= SetupGamepad;
        On_UILinksInitializer.FancyExit -= ExitWithGamepad;

        _pages = new ConditionalWeakTable<UICharacterCreation, CharacterCreationSteps>();
    }

    /// <summary>
    ///     嘗試取得目前角色建立介面的控制器。
    ///     Attempts to retrieve the active character creation controller.
    /// </summary>
    private static bool TryGetActiveSteps(
        UICharacterCreation page,
        out CharacterCreationSteps steps)
    {
        steps = null;

        return Main.gameMenu &&
               _pages.TryGetValue(page, out steps);
    }

    /// <summary>
    ///     取得目前角色建立介面的武器預覽資訊。
    ///     Gets weapon preview information for the active creation page.
    /// </summary>
    internal static bool TryGetWeaponPreview(
        Player player,
        out int itemType)
    {
        itemType = 0;

        if (!Main.gameMenu ||
            Main.MenuUI.CurrentState is not UICharacterCreation page ||
            !TryGetActiveSteps(page, out var steps))
            return false;

        if (!steps.WeaponPreviewEnabled ||
            !ReferenceEquals(player, steps.PreviewPlayer))
            return false;

        var selectedClass =
            player.GetModPlayer<ClassPlayer>().CurrentClass;

        itemType = ClassStartingWeapons.GetItemType(selectedClass);

        return itemType > 0;
    }

    /// <summary>
    ///     在原版頁面建立完成後初始化職業選擇控制器。
    ///     Initializes the class selection controller after vanilla page creation.
    /// </summary>
    private static void Build(
        On_UICharacterCreation.orig_BuildPage orig,
        UICharacterCreation page)
    {
        orig(page);

        _pages.Remove(page);
        _pages.Add(page, new CharacterCreationSteps(page));
    }

    /// <summary>
    ///     攔截下一步操作，先進入職業選擇頁面。
    ///     Intercepts continuation to display class selection first.
    /// </summary>
    private static void Continue(
        On_UICharacterCreation.orig_Click_NamingAndCreating orig,
        UICharacterCreation page,
        UIMouseEvent evt,
        UIElement element)
    {
        if (TryGetActiveSteps(page, out var steps) &&
            !steps.IsClassStep)
        {
            steps.ShowClassStep();
            return;
        }

        orig(page, evt, element);
    }

    /// <summary>
    ///     攔截返回操作，優先返回角色外觀頁面。
    ///     Intercepts back navigation to restore the appearance page.
    /// </summary>
    private static void Back(
        On_UICharacterCreation.orig_Click_GoBack orig,
        UICharacterCreation page,
        UIMouseEvent evt,
        UIElement element)
    {
        if (TryGetActiveSteps(page, out var steps) &&
            steps.IsClassStep)
        {
            steps.ShowAppearanceStep();
            return;
        }

        orig(page, evt, element);
    }

    /// <summary>
    ///     職業選擇頁面不使用原版外觀分類切換。
    ///     Disables vanilla appearance category switching on the class page.
    /// </summary>
    private static void MoveCategory(
        On_UICharacterCreation.orig_TryMovingCategory orig,
        UICharacterCreation page,
        int offset)
    {
        if (TryGetActiveSteps(page, out var steps) &&
            steps.IsClassStep)
            return;

        orig(page, offset);
    }

    /// <summary>
    ///     根據目前步驟設定手把導覽。
    ///     Configures gamepad navigation for the active creation step.
    /// </summary>
    private static void SetupGamepad(
        On_UICharacterCreation.orig_SetupGamepadPoints orig,
        UICharacterCreation page,
        SpriteBatch spriteBatch)
    {
        if (TryGetActiveSteps(page, out var steps) &&
            steps.IsClassStep)
        {
            steps.SetupGamepad();
            return;
        }

        orig(page, spriteBatch);
    }

    /// <summary>
    ///     處理手把返回操作。
    ///     Handles gamepad back navigation.
    /// </summary>
    private static void ExitWithGamepad(
        On_UILinksInitializer.orig_FancyExit orig)
    {
        if (Main.gameMenu &&
            Main.MenuUI.CurrentState is UICharacterCreation page &&
            TryGetActiveSteps(page, out var steps) &&
            steps.IsClassStep)
        {
            steps.ShowAppearanceStep();
            return;
        }

        orig();
    }
}