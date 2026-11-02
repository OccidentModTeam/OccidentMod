using Occident.Content.Items.Weapons.Summon;
using Terraria.ModLoader;

namespace Occident.Common.Classes;

public static class ClassStartingWeapons
{
    // Character creation and its visual preview must resolve the same weapon.
    public static int GetItemType(ClassType type) => type switch {
        ClassType.Evoker => ModContent.ItemType<AncientAgaricStaff>(),
        _ => 0
    };
}
