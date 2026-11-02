using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Occident.Common.Players;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;

namespace Occident.Common.UI.CharacterCreation;

internal static class CasualCreationOptions
{
    private static readonly Color CasualColor = new(120, 185, 132);
    private const string TextPrefix = "Mods.OccidentMod.UI.CharacterCreation.";

    public static void Add(UICharacterCreation page, Player player, UIElement middle, UIElement root)
    {
        var info = Field<UIElement>(page, "_infoContainer");
        var description = Field<UIText>(page, "_difficultyDescriptionText");
        var row = info.Children.Single(child => child.Children.OfType<UIDifficultyButton>().Any());
        var vanilla = row.Children.OfType<UIDifficultyButton>().OrderBy(button => button.VAlign).ToArray();
        var mode = player.GetModPlayer<CasualModePlayer>();

        // Selection is rendered against a UI-only player, keeping the saved vanilla difficulty valid.
        var selection = new Player { difficulty = mode.IsActive ? (byte)4 : player.difficulty };
        var buttonPlayer = typeof(UIDifficultyButton).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(UIDifficultyButton).FullName, "_player");
        var refreshDescription = typeof(UICharacterCreation).GetMethod("UpdateDifficultyDescription",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(UICharacterCreation).FullName, "UpdateDifficultyDescription");

        foreach (var button in vanilla)
        {
            buttonPlayer.SetValue(button, selection);
            button.OnLeftMouseDown += (evt, element) =>
            {
                player.difficulty = selection.difficulty;
                mode.Enabled = false;
                refreshDescription.Invoke(page, new object[] { evt, element });
            };
        }

        var casual = new UIDifficultyButton(selection, Language.GetText(TextPrefix + "Casual"),
            Language.GetText(TextPrefix + "CasualDescription"), 4, CasualColor);
        casual.OnLeftMouseDown += (_, _) =>
        {
            player.difficulty = 0;
            mode.Enabled = true;
            description.SetText(Language.GetText(TextPrefix + "CasualDescription"));
        };
        row.Append(casual);

        // Preserve each native button's height: grow the row by one quarter instead of squeezing five into four.
        middle.Parent.Height.Set(middle.Parent.Height.Pixels + 28f, middle.Parent.Height.Percent);
        root.Height.Set(root.Height.Pixels + 28f, root.Height.Percent);
        middle.Height.Set(middle.Height.Pixels + 28f, middle.Height.Percent);
        var ordered = new[] { vanilla[0], casual }.Concat(vanilla.Skip(1)).ToArray();
        for (int i = 0; i < ordered.Length; i++)
        {
            ordered[i].Width.Set(0f, 1f);
            ordered[i].Height.Set(0f, 1f / ordered.Length);
            ordered[i].VAlign = i / (float)(ordered.Length - 1);
            ordered[i].SetSnapPoint("Middle", i + 1);
        }
        row.OnUpdate += _ => selection.difficulty = mode.IsActive ? (byte)4 : player.difficulty;
        if (mode.IsActive)
            description.SetText(Language.GetText(TextPrefix + "CasualDescription"));
        page.Recalculate();
    }

    private static T Field<T>(UICharacterCreation page, string name) =>
        (T)(typeof(UICharacterCreation).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(page) ?? throw new MissingFieldException(typeof(UICharacterCreation).FullName, name));
}
