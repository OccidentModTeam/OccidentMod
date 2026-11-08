using System.Collections.Generic;
using OccidentMod.Gameplay.Classes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Gameplay.Players;

/// <summary>
///     根據玩家職業替換原版起始武器。
///     Replaces the vanilla starting weapon based on the player's class.
/// </summary>
public sealed class ClassStartingItemsPlayer : ModPlayer
{
    /// <summary>
    ///     在角色建立後替換原版銅短劍，保留其他起始物品。
    ///     Replaces the vanilla Copper Shortsword while preserving other starting items.
    /// </summary>
    public override void ModifyStartingInventory(
        IReadOnlyDictionary<string, List<Item>> itemsByMod,
        bool mediumCoreDeath)
    {
        // 中核角色死亡時不修改掉落物品。
        // Do not modify items dropped on mediumcore death.
        if (mediumCoreDeath ||
            !itemsByMod.TryGetValue("Terraria", out var vanillaItems))
            return;

        var weaponType = ClassStartingWeapons.GetItemType(
            Player.GetModPlayer<ClassPlayer>().CurrentClass);

        if (weaponType == 0)
            return;

        // 僅替換原版銅短劍，不影響工具與其他模組的物品。
        // Replace only the vanilla Copper Shortsword.
        for (var i = 0; i < vanillaItems.Count; i++)
        {
            if (vanillaItems[i].type != ItemID.CopperShortsword)
                continue;

            vanillaItems[i] = new Item(weaponType);
            break;
        }
    }
}