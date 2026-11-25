
#nullable enable

using System;

namespace OccidentMod.Features.Character.Classes;

/// <summary>
///
/// 管理玩家潜能的积累、上限及发动状态。
/// 不负责判断潜能的获取条件或执行职业能力。
///
/// Manages player potential accumulation, limits,
/// and activation state.
/// Does not determine gain conditions or execute
/// class abilities.
///
/// </summary>

public sealed class PotentialState
{
    // 当前潜能值和最大潜能值。
    // Current and maximum potential values.

    private double current;

    public float Current => (float)current;
    public float Maximum { get; }

    // 判断潜能是否已满，以及职业能力是否正在发动。
    // 发动期间禁止继续积累潜能。
    //
    // Indicates whether potential is full
    // and whether an ability is currently active.
    // Accumulation is blocked during activation.

    public bool IsFull => current >= Maximum;
    public bool IsActive { get; private set; }

    /// <summary>
    ///
    /// 初始化潜能最大值。
    /// 拒绝零、负数、NaN 或无穷大等无效数值。
    ///
    /// Initializes the maximum potential value.
    /// Rejects zero, negative numbers, NaN, and infinity.
    ///
    /// </summary>

    public PotentialState(float maximum)
    {
        if (!float.IsFinite(maximum) || maximum <= 0f)
            throw new ArgumentOutOfRangeException(nameof(maximum));

        Maximum = maximum;
    }

    /// <summary>
    ///
    /// 增加当前潜能值。
    /// 如果相加结果超过最大值，则使用最大值。
    /// 职业能力发动期间或增加量无效时不执行。
    ///
    /// Increases the current potential value.
    /// Caps the result at the maximum value.
    /// Ignores accumulation during activation
    /// or when the amount is invalid.
    ///
    /// </summary>

    public void Add(float amount)
    {
        if (IsActive || !float.IsFinite(amount) || amount <= 0f)
            return;

        current = Math.Min(current + amount, Maximum);
    }

    /// <summary>
    ///
    /// 尝试发动职业能力。
    /// 只有潜能已满且能力未发动时才允许启动。
    /// 发动成功后清空潜能，并禁止继续积累。
    ///
    /// Attempts to activate a class ability.
    /// Requires full potential and an inactive state.
    /// Clears potential and blocks accumulation on success.
    ///
    /// </summary>

    public bool TryActivate()
    {
        if (IsActive || !IsFull)
            return false;

        IsActive = true;
        Reset();

        return true;
    }

    /// <summary>
    ///
    /// 结束职业能力的发动状态。
    /// 由职业系统在能力真正结束后调用。
    /// 解除积累限制，允许重新获得潜能。
    ///
    /// Ends the class ability activation state.
    /// Called when the class-specific ability ends.
    /// Allows potential accumulation again.
    ///
    /// </summary>

    public void EndActivation()
    {
        IsActive = false;
    }

    /// <summary>
    ///
    /// 清空当前潜能值，不改变发动状态。
    /// 因此不会提前解除潜能积累限制。
    ///
    /// Clears current potential without changing
    /// the activation state or accumulation restriction.
    ///
    /// </summary>

    public void Reset()
    {
        current = 0d;
    }

    internal bool ChargeOverTime(int ticksToFull)
    {
        if (IsActive || IsFull)
            return false;

        double next = current + (double)Maximum / ticksToFull;
        // Snap only within floating-point error at the exact fill boundary.
        current = next >= Maximum * (1d - 1e-12) ? Maximum : next;
        return true;
    }

    internal bool TryBeginRelease()
    {
        if (IsActive || !IsFull)
            return false;

        IsActive = true;
        return true;
    }

    internal void AdvanceRelease(float remainingFraction)
    {
        current = (double)Maximum * remainingFraction;
        if (current == 0d)
            EndActivation();
    }

    internal bool TryApplySnapshot(float current, bool active)
    {
        if (!float.IsFinite(current) || current < 0f || current > Maximum)
            return false;

        this.current = current;
        IsActive = active;
        return true;
    }
}
