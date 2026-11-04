// 角色建立完成後，依職業替換原版起始武器。
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
        // 此處在職業選擇之後執行；只替換原版銅短劍，保留工具與其他模組加入的物品。
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
