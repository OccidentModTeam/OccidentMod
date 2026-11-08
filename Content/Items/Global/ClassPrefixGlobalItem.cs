using System;
using System.Collections.Generic;
using System.IO;
using OccidentMod.Gameplay.Classes;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Content.Items.Global;

/// <summary>
///     管理每件物品獨立持有的職業詞綴。
///     Manages class prefixes stored on individual items.
/// </summary>
/// <remarks>
///     負責詞綴資料、複製、儲存、讀取、網路序列化與顯示。
///     不負責自然生成機率、抽取規則或鍛造石邏輯。
///     Handles prefix data, cloning, persistence, network
///     serialization and display.
///     Does not handle natural roll chances, rolling rules
///     or forging stone behavior.
/// </remarks>
public sealed class ClassPrefixGlobalItem : GlobalItem
{
    /// <summary>
    ///     職業詞綴的儲存鍵名。
    ///     Save key for the class prefix.
    /// </summary>
    private const string SaveKey = "ClassPrefix";

    /// <summary>
    ///     目前物品的職業詞綴。
    ///     Class prefix assigned to the current item.
    /// </summary>
    private ClassType classPrefix = ClassType.None;

    /// <summary>
    ///     每件物品持有獨立的 GlobalItem 實例。
    ///     Each item has its own GlobalItem instance.
    /// </summary>

    public override bool InstancePerEntity => true;

    /// <summary>
    ///     取得目前的職業詞綴。
    ///     Gets the current class prefix.
    /// </summary>

    public ClassType ClassPrefix => classPrefix;

    /// <summary>
    ///     判斷目前是否持有有效的職業詞綴。
    ///     Determines whether a valid class prefix is assigned.
    /// </summary>

    public bool HasClassPrefix => ClassCatalog.IsValid(classPrefix);

    /// <summary>
    ///     判斷物品是否可以承載職業詞綴。
    ///     Determines whether an item can carry a class prefix.
    /// </summary>
    public static bool CanHaveClassPrefix(Item item)
    {
        if (item is null || item.IsAir || item.damage <= 0)
            return false;

        var damageClass = item.DamageType;

        return damageClass.CountsAsClass(DamageClass.Melee)
               || damageClass.CountsAsClass(DamageClass.Ranged)
               || damageClass.CountsAsClass(DamageClass.Magic)
               || damageClass.CountsAsClass(DamageClass.Summon);
    }

    /// <summary>
    ///     取得物品上的職業詞綴元件。
    ///     Gets the class prefix component attached to an item.
    /// </summary>
    public static ClassPrefixGlobalItem Get(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.GetGlobalItem<ClassPrefixGlobalItem>();
    }

    /// <summary>
    ///     嘗試為物品設定職業詞綴。
    ///     Attempts to assign a class prefix to an item.
    /// </summary>
    public bool TrySetClassPrefix(
        Item item,
        ClassType classType)
    {
        if (!CanHaveClassPrefix(item))
            return false;

        if (!ClassCatalog.IsValid(classType))
            return false;

        classPrefix = classType;

        return true;
    }

    /// <summary>
    ///     清除目前的職業詞綴。
    ///     Clears the current class prefix.
    /// </summary>
    public void ClearClassPrefix()
    {
        classPrefix = ClassType.None;
    }

    /// <summary>
    ///     複製物品時保留職業詞綴。
    ///     Preserves the class prefix when cloning an item.
    /// </summary>
    public override GlobalItem Clone(
        Item item,
        Item itemClone)
    {
        var clone =
            (ClassPrefixGlobalItem)base.Clone(item, itemClone);

        clone.classPrefix = classPrefix;

        return clone;
    }

    /// <summary>
    ///     將有效的職業詞綴寫入存檔。
    ///     Saves a valid class prefix.
    /// </summary>
    public override void SaveData(
        Item item,
        TagCompound tag)
    {
        if (!HasClassPrefix)
            return;

        tag[SaveKey] = (int)classPrefix;
    }

    /// <summary>
    ///     從存檔讀取並驗證職業詞綴。
    ///     Loads and validates a saved class prefix.
    /// </summary>
    public override void LoadData(
        Item item,
        TagCompound tag)
    {
        classPrefix = ClassType.None;

        if (!tag.ContainsKey(SaveKey))
            return;

        var loadedClass =
            (ClassType)tag.GetInt(SaveKey);

        if (!CanHaveClassPrefix(item))
            return;

        if (!ClassCatalog.IsValid(loadedClass))
            return;

        classPrefix = loadedClass;
    }

    /// <summary>
    ///     將職業詞綴寫入物品網路資料。
    ///     Writes the class prefix to item network data.
    /// </summary>
    public override void NetSend(
        Item item,
        BinaryWriter writer)
    {
        writer.Write((byte)classPrefix);
    }

    /// <summary>
    ///     從物品網路資料讀取職業詞綴。
    ///     Reads the class prefix from item network data.
    /// </summary>
    public override void NetReceive(
        Item item,
        BinaryReader reader)
    {
        var receivedClass =
            (ClassType)reader.ReadByte();

        classPrefix = ClassType.None;

        if (!CanHaveClassPrefix(item))
            return;

        if (!ClassCatalog.IsValid(receivedClass))
            return;

        classPrefix = receivedClass;
    }

    /// <summary>
    ///     在物品名稱前顯示本地化的職業詞綴。
    ///     Displays the localized class prefix before the item name.
    /// </summary>
    public override void ModifyTooltips(
        Item item,
        List<TooltipLine> tooltips)
    {
        if (!HasClassPrefix)
            return;

        var localizationKey =
            $"Mods.OccidentMod.Classes.{classPrefix}";

        var className =
            Language.GetTextValue(localizationKey);

        // 語言鍵不存在時，避免顯示原始鍵名。
        // Avoid displaying raw keys when localization is missing.
        if (!Language.Exists(localizationKey))
            return;

        foreach (var line in tooltips)
        {
            if (line.Mod != "Terraria" || line.Name != "ItemName")
                continue;

            line.Text = $"{className} {line.Text}";

            break;
        }
    }
}