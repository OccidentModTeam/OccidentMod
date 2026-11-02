using System.Collections.Generic;
using Occident.Common.Classes;
using Occident.Content.Items.Weapons.Summon;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Occident.Common.Players;

public sealed class ClassStartingItemsPlayer : ModPlayer
{
    public override void ModifyStartingInventory(IReadOnlyDictionary<string, List<Item>> itemsByMod,
        bool mediumCoreDeath)
    {
        // Creation runs after the class-selection step. Replace only the vanilla
        // starting sword; tools and additions from other mods keep their slots.
        if (mediumCoreDeath || !itemsByMod.TryGetValue("Terraria", out List<Item> vanillaItems)) return;
        int weaponType = ClassStartingWeapons.GetItemType(Player.GetModPlayer<ClassPlayer>().CurrentClass);
        if (weaponType == 0) return;
        for (int i = 0; i < vanillaItems.Count; i++) {
            if (vanillaItems[i].type != ItemID.CopperShortsword) continue;
            vanillaItems[i] = new Item(weaponType);
            break;
        }
    }
}
