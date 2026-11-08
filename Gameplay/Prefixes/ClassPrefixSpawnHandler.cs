using OccidentMod.Content.Items.Global;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     處理世界物品生成時的職業詞綴。
///     Handles class prefixes for world-spawned items.
/// </summary>
/// <remarks>
///     只負責辨識生成來源並轉交共用取得處理器。
///     不包含獨立的抽取演算法。
///     Resolves world spawn sources and delegates to
///     the shared acquisition handler.
///     Contains no separate rolling algorithm.
/// </remarks>
public sealed class ClassPrefixSpawnHandler : GlobalItem
{
    /// <summary>
    ///     世界物品生成時觸發。
    ///     Called when an item spawns in the world.
    /// </summary>
    public override void OnSpawn(
        Item item,
        IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!ClassPrefixGlobalItem.CanHaveClassPrefix(item))
            return;

        var acquisitionSource =
            ResolveSource(source);

        if (acquisitionSource == ClassPrefixSource.Unknown)
            return;

        ClassPrefixAcquisitionHandler.TryProcess(
            item,
            acquisitionSource,
            Main.rand
        );
    }

    /// <summary>
    ///     辨識世界物品的取得來源。
    ///     Resolves the acquisition source of a world item.
    /// </summary>
    private static ClassPrefixSource ResolveSource(
        IEntitySource source)
    {
        if (source is EntitySource_Loot lootSource)
        {
            if (lootSource.Entity is NPC)
                return ClassPrefixSource.EnemyDrop;

            return ClassPrefixSource.OtherLoot;
        }

        return ClassPrefixSource.Unknown;
    }
}