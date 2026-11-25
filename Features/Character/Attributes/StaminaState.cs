using System;

namespace OccidentMod.Features.Character.Attributes;

public sealed class StaminaState
{
    internal const int DefaultMaximum = 50;

    private const int RecoveryDelay = 60;
    private const int RecoveryInterval = 3;

    public int Maximum { get; }
    public int Current { get; private set; }
    public float Fraction => (float)Current / Maximum;
    internal int TicksUntilRecovery { get; private set; }

    public StaminaState(int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        Maximum = maximum;
        Current = maximum;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > Current)
            return false;

        Current -= amount;
        if (amount > 0)
            TicksUntilRecovery = RecoveryDelay + RecoveryInterval;

        return true;
    }

    public void Restore(int amount)
    {
        if (amount > 0)
            Current += Math.Min(amount, Maximum - Current);

        if (Current == Maximum)
            TicksUntilRecovery = 0;
    }

    internal void UpdateRecovery()
    {
        if (Current == Maximum)
            return;

        if (TicksUntilRecovery > 0 && --TicksUntilRecovery > 0)
            return;

        Restore(1);
        if (Current < Maximum)
            TicksUntilRecovery = RecoveryInterval;
    }

    internal void ApplySnapshot(int current, int ticksUntilRecovery, ulong elapsedTicks = 0)
    {
        Current = Math.Clamp(current, 0, Maximum);
        TicksUntilRecovery = Current == Maximum ? 0 : Math.Clamp(ticksUntilRecovery, 0, RecoveryDelay + RecoveryInterval);
        while (elapsedTicks > 0 && Current < Maximum)
        {
            UpdateRecovery();
            elapsedTicks--;
        }
    }
}
