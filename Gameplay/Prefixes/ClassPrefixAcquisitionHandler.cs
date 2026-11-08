using System;
using OccidentMod.Content.Items.Global;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace OccidentMod.Gameplay.Prefixes;

/// <summary>
///     統一處理不同取得來源的職業詞綴生成。
///     Handles class prefix generation across acquisition sources.
/// </summary>
/// <remarks>
///     取得來源只提供生成條件，不需要各自實作抽取演算法。
///     不負責鍛造石、品質階級或原版前綴重鑄。
///     Acquisition sources provide generation conditions
///     without implementing separate rolling algorithms.
///     Does not handle forging stones, quality tiers
///     or vanilla prefix reforging.
/// </remarks>
public sealed class ClassPrefixAcquisitionHandler : GlobalItem
{
    /// <summary>
    ///     物品建立完成時觸發。
    ///     Called when an item is created.
    /// </summary>
    public override void OnCreated(
        Item item,
        ItemCreationContext context)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (context is RecipeItemCreationContext)
            TryProcess(
                item,
                ClassPrefixSource.Crafting,
                Main.rand
            );
    }

    /// <summary>
    ///     依指定取得來源嘗試生成職業詞綴。
    ///     Attempts class prefix generation for an acquisition source.
    /// </summary>
    /// <remarks>
    ///     呼叫端必須避免對同一次取得重複檢定。
    ///     目前尚未持久化生成嘗試狀態。
    ///     Callers must avoid repeated checks for one acquisition.
    ///     Generation attempt state is not yet persisted.
    /// </remarks>
    public static bool TryProcess(
        Item item,
        ClassPrefixSource source,
        UnifiedRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return false;

        if (source == ClassPrefixSource.Unknown)
            return false;

        if (!ClassPrefixGlobalItem.CanHaveClassPrefix(item))
            return false;

        var data =
            ClassPrefixGlobalItem.Get(item);

        if (data.HasClassPrefix)
            return false;

        if (!ClassPrefixSourceRegistry.ShouldGenerate(
                source,
                random))
            return false;

        return ClassPrefixGeneration.TryGenerateNatural(
            item,
            random
        );
    }

    /// <summary>
    ///     處理商店購買取得的物品。
    ///     Processes an item acquired through a shop purchase.
    /// </summary>
    public static bool TryProcessShop(
        Item item,
        UnifiedRandom random)
    {
        return TryProcess(
            item,
            ClassPrefixSource.Shop,
            random
        );
    }

    /// <summary>
    ///     處理寶箱取得的物品。
    ///     Processes an item acquired from a chest.
    /// </summary>
    public static bool TryProcessChest(
        Item item,
        UnifiedRandom random)
    {
        return TryProcess(
            item,
            ClassPrefixSource.Chest,
            random
        );
    }

    /// <summary>
    ///     處理其他戰利品來源。
    ///     Processes an item from another loot source.
    /// </summary>
    public static bool TryProcessOtherLoot(
        Item item,
        UnifiedRandom random)
    {
        return TryProcess(
            item,
            ClassPrefixSource.OtherLoot,
            random
        );
    }
}