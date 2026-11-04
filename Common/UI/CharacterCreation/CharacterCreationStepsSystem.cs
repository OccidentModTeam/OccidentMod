// 掛接原版建立角色流程，讓下一步、返回與手把導覽支援職業選擇。
using System.Runtime.CompilerServices;
using Occident.Common.Classes;
using Occident.Common.Players;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.Initializers;
using Terraria.ModLoader;
using Terraria.UI;

namespace Occident.Common.UI.CharacterCreation;

[Autoload(Side = ModSide.Client)]
public sealed class CharacterCreationStepsSystem : ModSystem
{
    private static ConditionalWeakTable<UICharacterCreation, CharacterCreationSteps> pages = new();

    internal static bool TryGetWeaponPreview(Player player, out int itemType)
    {
        itemType = 0;
        if (!Main.gameMenu || Main.MenuUI.CurrentState is not UICharacterCreation page ||
            !pages.TryGetValue(page, out var steps) || !steps.WeaponPreviewEnabled ||
            !ReferenceEquals(player, steps.PreviewPlayer)) return false;
        itemType = ClassStartingWeapons.GetItemType(player.GetModPlayer<ClassPlayer>().CurrentClass);
        return itemType > 0;
    }

    public override void Load()
    {
        On_UICharacterCreation.BuildPage += Build;
        On_UICharacterCreation.Click_NamingAndCreating += Continue;
        On_UICharacterCreation.Click_GoBack += Back;
        On_UICharacterCreation.TryMovingCategory += MoveCategory;
        On_UICharacterCreation.SetupGamepadPoints += SetupGamepad;
        On_UILinksInitializer.FancyExit += ExitWithGamepad;
    }

    public override void Unload()
    {
        On_UICharacterCreation.BuildPage -= Build;
        On_UICharacterCreation.Click_NamingAndCreating -= Continue;
        On_UICharacterCreation.Click_GoBack -= Back;
        On_UICharacterCreation.TryMovingCategory -= MoveCategory;
        On_UICharacterCreation.SetupGamepadPoints -= SetupGamepad;
        On_UILinksInitializer.FancyExit -= ExitWithGamepad;
        pages = new();
    }

    // 先讓原版完成頁面，再為這個頁面建立控制器；弱參考表不阻止舊頁面回收。
    private void Build(On_UICharacterCreation.orig_BuildPage orig, UICharacterCreation page)
    {
        orig(page);
        pages.Remove(page);
        pages.Add(page, new CharacterCreationSteps(page));
    }

    // 第一次下一步先開職業頁；職業頁再按下一步才交回原版命名流程。
    private void Continue(On_UICharacterCreation.orig_Click_NamingAndCreating orig,
        UICharacterCreation page, UIMouseEvent evt, UIElement element)
    {
        if (pages.TryGetValue(page, out var steps) && !steps.IsClassStep)
            steps.ShowClassStep();
        else
            orig(page, evt, element);
    }

    private void Back(On_UICharacterCreation.orig_Click_GoBack orig,
        UICharacterCreation page, UIMouseEvent evt, UIElement element)
    {
        if (pages.TryGetValue(page, out var steps) && steps.IsClassStep)
            steps.ShowAppearanceStep();
        else
            orig(page, evt, element);
    }

    private void MoveCategory(On_UICharacterCreation.orig_TryMovingCategory orig,
        UICharacterCreation page, int offset)
    {
        if (!pages.TryGetValue(page, out var steps) || !steps.IsClassStep)
            orig(page, offset);
    }

    private void SetupGamepad(On_UICharacterCreation.orig_SetupGamepadPoints orig,
        UICharacterCreation page, SpriteBatch spriteBatch)
    {
        if (pages.TryGetValue(page, out var steps) && steps.IsClassStep)
            steps.SetupGamepad();
        else
            orig(page, spriteBatch);
    }

    private void ExitWithGamepad(On_UILinksInitializer.orig_FancyExit orig)
    {
        if (Main.gameMenu && Main.MenuUI.CurrentState is UICharacterCreation page
            && pages.TryGetValue(page, out var steps) && steps.IsClassStep)
            steps.ShowAppearanceStep();
        else
            orig();
    }
}
