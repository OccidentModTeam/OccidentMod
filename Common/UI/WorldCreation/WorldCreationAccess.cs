// 集中反射存取原版世界建立頁的私有欄位與回呼。
using System;
using System.Collections.Generic;
using System.Reflection;
using Occident.Common.Worlds;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;

namespace Occident.Common.UI.WorldCreation;

// 原版選項 enum 是 private，集中存取並保留原版按鈕的回呼與預覽更新。
internal static class WorldCreationAccess
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Type Page = typeof(UIWorldCreation);
    private static readonly FieldInfo SizeButtons = Field("_sizeButtons"), DifficultyButtons = Field("_difficultyButtons");
    private static readonly FieldInfo Size = Field("_optionSize"), Difficulty = Field("_optionDifficulty");
    private static readonly FieldInfo Description = Field("_descriptionText");
    private static readonly FieldInfo PreviewPlate = Field("_previewPlate");
    private static readonly MethodInfo ClickSize = Method("ClickSizeOption"), ClickDifficulty = Method("ClickDifficultyOption");
    private static readonly Dictionary<Type, (MethodInfo Method, object[] Arguments)> ClearSetters = new();

    private static FieldInfo Field(string name) => Page.GetField(name, Flags) ?? throw new MissingFieldException(Page.Name, name);
    private static MethodInfo Method(string name) => Page.GetMethod(name, Flags) ?? throw new MissingMethodException(Page.Name, name);
    public static UIElement[] Buttons(UIWorldCreation page, bool size) =>
        (UIElement[])(size ? SizeButtons : DifficultyButtons).GetValue(page);
    public static int Selected(UIWorldCreation page, bool size) => Convert.ToInt32((size ? Size : Difficulty).GetValue(page));
    public static UIWorldCreationPreview Preview(UIWorldCreation page) => (UIWorldCreationPreview)PreviewPlate.GetValue(page);
    public static void ApplyFallback(UIWorldCreation page, WorldOptions options)
    {
        if (options.Difficulty != OccidentWorldDifficulty.None)
            Difficulty.SetValue(page, Enum.ToObject(Difficulty.FieldType, 2));
        if (options.ExtraLarge)
            Size.SetValue(page, Enum.ToObject(Size.FieldType, 2));
    }
    public static void Click(UIWorldCreation page, bool size, UIElement button) =>
        (size ? ClickSize : ClickDifficulty).Invoke(page, new object[] { new UIMouseEvent(button, default), button });
    public static void ClearHighlight(UIWorldCreation page, bool size)
    {
        foreach (var button in Buttons(page, size))
        {
            Type type = button.GetType();
            if (!ClearSetters.TryGetValue(type, out var setter))
            {
                MethodInfo method = type.GetMethod("SetCurrentOption")
                    ?? throw new MissingMethodException(type.FullName, "SetCurrentOption");
                setter = (method, new[] { Enum.ToObject(method.GetParameters()[0].ParameterType, -1) });
                ClearSetters.Add(type, setter);
            }
            setter.Method.Invoke(button, setter.Arguments);
        }
    }
    public static void Describe(UIWorldCreation page, string text) => ((UIText)Description.GetValue(page)).SetText(text);
}
