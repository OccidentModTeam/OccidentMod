using System;

namespace OccidentMod.Gameplay.Attributes;

/// <summary>
///     管理角色的自由属性配点。
///     Manages free attribute point allocation.
/// </summary>
/// <remarks>
///     此类负责配点验证与剩余点数管理。
///     不负责角色升级、职业加成或网络同步。
///     Handles allocation validation and available points.
///     Does not handle leveling, class bonuses or networking.
/// </remarks>
public sealed class AttributeAllocation
{
    /// <summary>
    ///     创建属性配点管理器。
    ///     Creates an attribute allocation manager.
    /// </summary>
    public AttributeAllocation(int initialPoints = 0)
    {
        AvailablePoints = Math.Max(0, initialPoints);
    }

    /// <summary>
    ///     当前可分配的属性点数。
    ///     Currently available attribute points.
    /// </summary>

    public int AvailablePoints { get; private set; }

    /// <summary>
    ///     增加可分配属性点数。
    ///     Grants additional attribute points.
    /// </summary>
    public void GrantPoints(int amount)
    {
        if (amount <= 0)
            return;

        AvailablePoints = (int)Math.Min(
            (long)AvailablePoints + amount,
            int.MaxValue
        );
    }

    /// <summary>
    ///     检查是否能够分配指定点数。
    ///     Checks whether the requested points can be allocated.
    /// </summary>
    public bool CanAllocate(
        AttributeType type,
        int amount)
    {
        return Enum.IsDefined(typeof(AttributeType), type)
               && amount > 0
               && amount <= AvailablePoints;
    }

    /// <summary>
    ///     将点数分配到指定属性。
    ///     Allocates points to an attribute.
    /// </summary>
    /// <remarks>
    ///     配点成功时扣除可用点数。
    ///     Deducts available points after successful allocation.
    /// </remarks>
    public bool TryAllocate(
        AttributeData attributes,
        AttributeType type,
        int amount)
    {
        if (attributes is null || !CanAllocate(type, amount))
            return false;

        // 防止属性数值溢出。
        // Prevents attribute value overflow.
        if (attributes.Get(type) > int.MaxValue - amount)
            return false;

        attributes.Add(type, amount);
        AvailablePoints -= amount;

        return true;
    }

    /// <summary>
    ///     恢复可分配点数。
    ///     Restores available points.
    /// </summary>
    /// <remarks>
    ///     仅供未来的洗点或数据恢复系统调用。
    ///     Intended for future respec or data recovery systems.
    /// </remarks>
    public void RestorePoints(int amount)
    {
        GrantPoints(amount);
    }

    /// <summary>
    ///     设置剩余可分配点数。
    ///     Sets the remaining available points.
    /// </summary>
    /// <remarks>
    ///     主要供存档读取与初始化使用。
    ///     Primarily used for loading and initialization.
    /// </remarks>
    public void SetAvailablePoints(int amount)
    {
        AvailablePoints = Math.Max(0, amount);
    }
}