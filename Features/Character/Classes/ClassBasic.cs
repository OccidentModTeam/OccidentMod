#nullable enable
using Terraria.ModLoader;

namespace OccidentMod.Features.Character.Classes;

/// <summary>
/// 管理职业与原版基础伤害类型的对应关系。
/// 不负责修改武器伤害、计算伤害或限制武器使用。
///
/// Maps classes to vanilla damage types.
/// Does not modify weapon damage, calculate damage,
/// or restrict weapon usage.
/// </summary>

public static class ClassBasic
{
    public static DamageClass? GetDamageClass(string classId)
    {
        return classId switch
        {
            "Warrior" or "Rogue" => DamageClass.Melee,
            "Ranger" or "Thrower" => DamageClass.Ranged,
            "Mage" or "Bard" => DamageClass.Magic,
            "Summoner" or "Artificer" => DamageClass.Summon,

            _ => null
        };
    }
}