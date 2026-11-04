// 職業起始武器的單一對照來源，供發裝與預覽共用。
using Occident.Content.Items.Weapons.Summon;
using Terraria.ModLoader;

namespace Occident.Common.Classes;

public static class ClassStartingWeapons
{
    // 建立角色與畫面預覽共用這份對照，避免預覽武器與實際起始武器不同。
    public static int GetItemType(ClassType type) => type switch {
        ClassType.Evoker => ModContent.ItemType<AncientAgaricStaff>(),
        _ => 0
    };
}
