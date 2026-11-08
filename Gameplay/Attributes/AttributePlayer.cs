using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Gameplay.Attributes;

/// <summary>
///     管理玩家属性、自由配点与额外属性加成。
///     Manages player attributes, allocation and modifiers.
/// </summary>
/// <remarks>
///     永久属性与配点会保存到角色存档。
///     装备、Buff 等临时加成每次更新时重新计算。
///     Permanent attributes and allocation points are saved.
///     Temporary modifiers are recalculated every update.
/// </remarks>
public sealed class AttributePlayer : ModPlayer
{
    /// <summary>
    ///     玩家永久属性数据。
    ///     Player's permanent attribute data.
    /// </summary>

    public AttributeData Attributes { get; private set; } = new();

    /// <summary>
    ///     玩家自由配点管理器。
    ///     Player's attribute allocation manager.
    /// </summary>

    public AttributeAllocation Allocation { get; private set; } = new();

    /// <summary>
    ///     当前生效的额外属性加成。
    ///     Currently active attribute modifiers.
    /// </summary>

    public AttributeModifier Modifiers { get; private set; } = new();

    /// <summary>
    ///     初始化玩家属性系统。
    ///     Initializes the player's attribute system.
    /// </summary>
    public override void Initialize()
    {
        Attributes = new AttributeData();
        Allocation = new AttributeAllocation();
        Modifiers = new AttributeModifier();
    }

    /// <summary>
    ///     每次更新时清除临时属性加成。
    ///     Clears temporary attribute modifiers each update.
    /// </summary>
    /// <remarks>
    ///     装备和 Buff 应在对应的更新钩子中重新提供加成。
    ///     Equipment and buffs should reapply their bonuses
    ///     through their respective update hooks.
    /// </remarks>
    public override void ResetEffects()
    {
        Modifiers.Reset();
    }

    /// <summary>
    ///     获取指定属性的最终有效数值。
    ///     Gets the effective value of an attribute.
    /// </summary>
    public int GetEffectiveAttribute(AttributeType type)
    {
        return Modifiers.GetTotal(Attributes, type);
    }

    /// <summary>
    ///     尝试分配属性点数。
    ///     Attempts to allocate attribute points.
    /// </summary>
    /// <remarks>
    ///     多人游戏中的配点必须由服务器验证。
    ///     Multiplayer allocations must be validated by the server.
    /// </remarks>
    public bool TryAllocateAttribute(AttributeType type, int amount)
    {
        return Allocation.TryAllocate(Attributes, type, amount);
    }

    /// <summary>
    ///     保存永久属性与剩余配点。
    ///     Saves permanent attributes and remaining points.
    /// </summary>
    public override void SaveData(TagCompound tag)
    {
        TagCompound attributes = new()
        {
            ["Vigor"] = Attributes.Vigor,
            ["Mind"] = Attributes.Mind,
            ["Endurance"] = Attributes.Endurance,
            ["Strength"] = Attributes.Strength,
            ["Dexterity"] = Attributes.Dexterity,
            ["Intelligence"] = Attributes.Intelligence,
            ["Faith"] = Attributes.Faith,
            ["Arcane"] = Attributes.Arcane
        };

        tag["Attributes"] = attributes;
        tag["AvailableAttributePoints"] = Allocation.AvailablePoints;
    }

    /// <summary>
    ///     读取永久属性与剩余配点。
    ///     Loads permanent attributes and remaining points.
    /// </summary>
    public override void LoadData(TagCompound tag)
    {
        Attributes.Reset();
        Allocation.SetAvailablePoints(0);
        Modifiers.Reset();

        if (tag.ContainsKey("Attributes"))
        {
            var saved = tag.GetCompound("Attributes");

            LoadAttribute(saved, AttributeType.Vigor, "Vigor");
            LoadAttribute(saved, AttributeType.Mind, "Mind");
            LoadAttribute(saved, AttributeType.Endurance, "Endurance");
            LoadAttribute(saved, AttributeType.Strength, "Strength");
            LoadAttribute(saved, AttributeType.Dexterity, "Dexterity");
            LoadAttribute(saved, AttributeType.Intelligence, "Intelligence");
            LoadAttribute(saved, AttributeType.Faith, "Faith");
            LoadAttribute(saved, AttributeType.Arcane, "Arcane");
        }

        if (tag.ContainsKey("AvailableAttributePoints"))
            Allocation.SetAvailablePoints(
                tag.GetInt("AvailableAttributePoints")
            );
    }

    /// <summary>
    ///     从存档读取单项属性。
    ///     Loads one attribute from saved data.
    /// </summary>
    private void LoadAttribute(
        TagCompound saved,
        AttributeType type,
        string key)
    {
        if (!saved.ContainsKey(key))
            return;

        Attributes.Set(type, saved.GetInt(key));
    }
}