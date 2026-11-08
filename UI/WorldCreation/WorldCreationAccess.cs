using System;
using System.Collections.Generic;
using System.Reflection;
using OccidentMod.World;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;

namespace OccidentMod.UI.WorldCreation;

/// <summary>
///     集中存取原版世界建立介面的私有欄位與方法。
///     Provides centralized access to private fields
///     and methods of the vanilla world creation interface.
/// </summary>
/// <remarks>
///     原版世界大小與難度選項使用私有列舉，
///     因此透過反射保留原版選項、點擊回呼與預覽更新。
///     Vanilla world size and difficulty options use private enums.
///     Reflection is used to preserve vanilla option behavior,
///     click callbacks, and preview updates.
/// </remarks>
internal static class WorldCreationAccess
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private const int VanillaFallbackOption = 2;
    private const int UnselectedOption = -1;

    private static readonly Type PageType =
        typeof(UIWorldCreation);

    private static readonly FieldInfo SizeButtonsField =
        GetPageField("_sizeButtons");

    private static readonly FieldInfo DifficultyButtonsField =
        GetPageField("_difficultyButtons");

    private static readonly FieldInfo SizeField =
        GetPageField("_optionSize");

    private static readonly FieldInfo DifficultyField =
        GetPageField("_optionDifficulty");

    private static readonly FieldInfo DescriptionField =
        GetPageField("_descriptionText");

    private static readonly FieldInfo PreviewField =
        GetPageField("_previewPlate");

    private static readonly MethodInfo ClickSizeMethod =
        GetPageMethod("ClickSizeOption");

    private static readonly MethodInfo ClickDifficultyMethod =
        GetPageMethod("ClickDifficultyOption");

    // 快取不同按鈕類型的高亮清除方法與參數。
    // Cache highlight-clearing methods and arguments
    // for each button type.

    private static readonly Dictionary<
        Type,
        (MethodInfo Method, object[] Arguments)
    > ClearSetters = new();

    /// <summary>
    ///     取得原版世界大小或難度按鈕。
    ///     Gets the vanilla world size or difficulty buttons.
    /// </summary>
    /// <param name="page">
    ///     世界建立介面。
    ///     The world creation UI page.
    /// </param>
    /// <param name="size">
    ///     true 取得世界大小按鈕；false 取得難度按鈕。
    ///     true for world size buttons; false for difficulty buttons.
    /// </param>
    public static UIElement[] Buttons(
        UIWorldCreation page,
        bool size)
    {
        var field = size
            ? SizeButtonsField
            : DifficultyButtonsField;

        return (UIElement[])field.GetValue(page);
    }

    /// <summary>
    ///     讀取原版目前選取的世界大小或難度代號。
    ///     Reads the currently selected vanilla
    ///     world size or difficulty value.
    /// </summary>
    public static int Selected(
        UIWorldCreation page,
        bool size)
    {
        var field = size
            ? SizeField
            : DifficultyField;

        var value = field.GetValue(page);

        return Convert.ToInt32(value);
    }

    /// <summary>
    ///     取得原版世界建立預覽元件。
    ///     Gets the vanilla world creation preview element.
    /// </summary>
    public static UIWorldCreationPreview Preview(
        UIWorldCreation page)
    {
        return (UIWorldCreationPreview)
            PreviewField.GetValue(page);
    }

    /// <summary>
    ///     將自訂世界選項映射到有效的原版備援選項。
    ///     Maps custom world options to valid vanilla fallback values.
    /// </summary>
    /// <remarks>
    ///     自訂世界難度或超大型世界啟用時，
    ///     使用原版代號 2 作為對應的介面備援值。
    ///     Uses vanilla option value 2 as the UI fallback
    ///     when custom difficulty or extra-large world size
    ///     is enabled.
    /// </remarks>
    public static void ApplyFallback(
        UIWorldCreation page,
        WorldOptions options)
    {
        if (options.Difficulty != OccidentWorldDifficulty.None)
            SetEnumField(
                page,
                DifficultyField,
                VanillaFallbackOption);

        if (options.ExtraLarge)
            SetEnumField(
                page,
                SizeField,
                VanillaFallbackOption);
    }

    /// <summary>
    ///     呼叫原版世界大小或難度選項的點擊處理方法。
    ///     Invokes the vanilla click handler for
    ///     world size or difficulty options.
    /// </summary>
    public static void Click(
        UIWorldCreation page,
        bool size,
        UIElement button)
    {
        var method = size
            ? ClickSizeMethod
            : ClickDifficultyMethod;

        var mouseEvent = new UIMouseEvent(
            button,
            default);

        method.Invoke(
            page,
            new object[] { mouseEvent, button });
    }

    /// <summary>
    ///     清除指定選項群組中所有原版按鈕的高亮狀態。
    ///     Clears the highlighted state of all vanilla
    ///     buttons in the selected option group.
    /// </summary>
    public static void ClearHighlight(
        UIWorldCreation page,
        bool size)
    {
        foreach (var button in Buttons(page, size))
        {
            var setter = GetClearSetter(button.GetType());

            setter.Method.Invoke(
                button,
                setter.Arguments);
        }
    }

    /// <summary>
    ///     更新原版世界建立介面的選項說明文字。
    ///     Updates the option description text
    ///     in the vanilla world creation interface.
    /// </summary>
    public static void Describe(
        UIWorldCreation page,
        string text)
    {
        var description =
            (UIText)DescriptionField.GetValue(page);

        description.SetText(text);
    }

    /// <summary>
    ///     將整數選項轉換為原版私有列舉並寫入欄位。
    ///     Converts an integer option into the vanilla
    ///     private enum type and assigns it to the field.
    /// </summary>
    private static void SetEnumField(
        UIWorldCreation page,
        FieldInfo field,
        int value)
    {
        var enumValue = Enum.ToObject(
            field.FieldType,
            value);

        field.SetValue(page, enumValue);
    }

    /// <summary>
    ///     取得並快取指定按鈕類型的高亮清除方法。
    ///     Gets and caches the highlight-clearing method
    ///     for the specified button type.
    /// </summary>
    private static (
        MethodInfo Method,
        object[] Arguments
        ) GetClearSetter(Type buttonType)
    {
        if (ClearSetters.TryGetValue(
                buttonType,
                out var cached))
            return cached;

        var method =
            buttonType.GetMethod("SetCurrentOption")
            ?? throw new MissingMethodException(
                buttonType.FullName,
                "SetCurrentOption");

        var optionType =
            method.GetParameters()[0].ParameterType;

        var unselectedValue = Enum.ToObject(
            optionType,
            UnselectedOption);

        var setter = (
            Method: method,
            Arguments: new[] { unselectedValue }
        );

        ClearSetters.Add(buttonType, setter);

        return setter;
    }

    /// <summary>
    ///     取得原版世界建立介面的私有欄位。
    ///     Gets a private field from vanilla UIWorldCreation.
    /// </summary>
    private static FieldInfo GetPageField(string name)
    {
        return PageType.GetField(
                   name,
                   InstanceFlags)
               ?? throw new MissingFieldException(
                   PageType.FullName,
                   name);
    }

    /// <summary>
    ///     取得原版世界建立介面的私有方法。
    ///     Gets a private method from vanilla UIWorldCreation.
    /// </summary>
    private static MethodInfo GetPageMethod(string name)
    {
        return PageType.GetMethod(
                   name,
                   InstanceFlags)
               ?? throw new MissingMethodException(
                   PageType.FullName,
                   name);
    }
}