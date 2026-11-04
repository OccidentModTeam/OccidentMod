// 建立單一職業分類的清單，處理選取與捲動顯示。
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

// 沿用原版難度按鈕的圖樣與文字尺寸，組成職業名稱清單。
internal sealed class ClassCreationCategory : UIElement
{
    private const string PromptKey = "Mods.OccidentMod.UI.CharacterCreation.ChooseClass";
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

        // 內容區透明，讓說明直接呈現在主面板上。
        var details = new UIElement();
        details.SetPadding(0f);
        details.Left.Set(4f, 0.4f);
        details.Width.Set(-4f, 0.6f);
        // 原版分隔線已預留上方 3 像素與下方 14 像素；只將頂端內縮 11 像素，維持底緣位置。
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
        // 保留捲動控制與滾輪輸入，但不繪製捲軸。
        scrollbar = new UIScrollbar();
        descriptionList.SetScrollbar(scrollbar);
        description = new UIText(Language.GetText(PromptKey), 1f)
        {
            IsWrapped = true,
            // 中文字形可能超出一般基線，需預留上緣以免被清單裁切。
            PaddingTop = 8f,
            PaddingBottom = 4f
        };
        description.Width.Set(0f, 1f);
        descriptionList.Add(description);

        foreach (ClassType id in classes)
        {
            string prefix = $"Mods.OccidentMod.Classes.{id}.";
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

    // 只更新目前分類的選取標示，實際職業資料仍放在 ClassPlayer。
    private void RefreshSelection()
    {
        ClassType current = player.GetModPlayer<ClassPlayer>().CurrentClass;
        selection.difficulty = (byte)current;
        var state = (current, Language.ActiveCulture.Name);
        if (state == lastState)
            return;
        lastState = state;
        string key = ClassCatalog.GetCategory(current) == category
            ? $"Mods.OccidentMod.Classes.{current}.Description" : PromptKey;
        description.SetText(Language.GetText(key));
        descriptionList.Recalculate();
        scrollbar.ViewPosition = 0f;
    }
}
