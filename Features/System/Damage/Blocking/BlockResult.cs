namespace OccidentMod.Features.System.Damage.Blocking;

public readonly record struct BlockResult(
    double InputDamage,
    double BlockedDamage,
    double DamageTaken,
    bool IsPerfectBlock);
