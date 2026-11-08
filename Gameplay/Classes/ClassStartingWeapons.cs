using OccidentMod.Content.Items.Weapons.Summon;
using Terraria.ModLoader;

namespace OccidentMod.Gameplay.Classes;

/// <summary>
///     集中管理各職業的起始武器對照，供角色建立與武器預覽共用。
///     Centralizes starting weapon mappings for character creation
///     and weapon previews.
/// </summary>
public static class ClassStartingWeapons
{
    /// <summary>
    ///     取得指定職業的起始武器物品類型。
    ///     Gets the starting weapon item type for the specified class.
    /// </summary>
    /// <remarks>
    ///     回傳 0 表示目前沒有設定專屬起始武器。
    ///     Returns 0 when no dedicated starting weapon is configured.
    /// </remarks>
    public static int GetItemType(ClassType type)
    {
        return type switch
        {
            // 喚靈術士：遠古菌杖。
            // Evoker: Ancient Agaric Staff.
            ClassType.Evoker =>
                ModContent.ItemType<AncientAgaricStaff>(),

            // 其他職業尚未配置專屬起始武器。
            // Other classes do not have dedicated starting weapons yet.
            _ => 0
        };
    }
}