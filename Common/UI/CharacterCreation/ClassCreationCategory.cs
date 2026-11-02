using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Occident.Common.Classes;
using Occident.Common.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Occident.Common.UI.CharacterCreation;

// Native difficulty-button artwork and text sizing, used as a blue class-name list.
internal sealed class ClassCreationCategory : UIElement
{
    private const string PromptKey = "Mods.Occident.UI.CharacterCreation.ChooseClass";
    private readonly Player player;
    private readonly ClassCategory category;
    private readonly Player selection = new();
    private readonly List<UIDifficultyButton> buttons = new();
    private readonly UIText description;
    private readonly UIList descriptionList;
    private readonly UIScrollbar scrollbar;
    private (ClassType Class, string Culture) lastState;

    public IReadOnlyList<UIDifficultyButton> Buttons => buttons;
    public ClassCategory Category => category;

    public ClassCreationCategory(Player player, ClassCategory category)
    {
        this.player = player;
        this.category = category;
        SetPadding(10f);
        PaddingTop = 0f;
        PaddingBottom = 0f;

        var classes = Enum.GetValues<ClassType>()
            .Where(id => ClassCatalog.GetCategory(id) == category).ToArray();

        var list = new UIElement();
        list.SetPadding(0f);
        list.Width.Set(-4f, 0.4f);
        list.Height.Set(Math.Max(0f, classes.Length * 32f - 4f), 0f);
        list.VAlign = 0.5f;
        Append(list);

        // Transparent content area: let the description sit directly on the main panel.
        var details = new UIElement();
        details.SetPadding(0f);
        details.Left.Set(4f, 0.4f);
        details.Width.Set(-4f, 0.6f);
        // The native separator already leaves 3px above this region and 14px below it.
        // Inset only the top by 11px, keeping the existing bottom edge in place.
        details.Top.Set(11f, 0f);
        details.Height.Set(-11f, 1f);
        Append(details);

        descriptionList = new UIList();
        descriptionList.Left.Set(8f, 0f);
        descriptionList.Top.Set(8f, 0f);
        descriptionList.Width.Set(-16f, 1f);
        descriptionList.Height.Set(-16f, 1f);
        descriptionList.SetPadding(0f);
        details.Append(descriptionList);
        // Retain the scroll position/controller for wheel input without drawing a scrollbar.
        scrollbar = new UIScrollbar();
        descriptionList.SetScrollbar(scrollbar);
        description = new UIText(Language.GetText(PromptKey), 1f)
        {
            IsWrapped = true,
            // Chinese glyphs extend above the normal baseline. Keep them inside the list's clip region.
            PaddingTop = 8f,
            PaddingBottom = 4f
        };
        description.Width.Set(0f, 1f);
        descriptionList.Add(description);

        foreach (ClassType id in classes)
        {
            string prefix = $"Mods.Occident.Classes.{id}.";
            var button = new ClassNameButton(selection, Language.GetText(prefix + "DisplayName"),
                Language.GetText(prefix + "Description"), (byte)id);
            button.Width.Set(0f, 1f);
            button.Height.Set(28f, 0f);
            button.Top.Set(buttons.Count * 32f, 0f);
            button.SetSnapPoint("ClassCreationChoice", buttons.Count);
            button.OnLeftMouseDown += (_, _) =>
            {
                var state = player.GetModPlayer<ClassPlayer>();
                state.EditClass(state.CurrentClass == id ? ClassType.None : id);
                RefreshSelection();
            };
            buttons.Add(button);
            list.Append(button);
        }
        RefreshSelection();
    }

    public override void Update(GameTime gameTime)
    {
        RefreshSelection();
        base.Update(gameTime);
    }

    private void RefreshSelection()
    {
        ClassType current = player.GetModPlayer<ClassPlayer>().CurrentClass;
        selection.difficulty = (byte)current;
        var state = (current, Language.ActiveCulture.Name);
        if (state == lastState)
            return;
        lastState = state;
        string key = ClassCatalog.GetCategory(current) == category
            ? $"Mods.Occident.Classes.{current}.Description" : PromptKey;
        description.SetText(Language.GetText(key));
        descriptionList.Recalculate();
        scrollbar.ViewPosition = 0f;
    }
}
