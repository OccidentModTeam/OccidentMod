namespace Occident.Common.Classes;
public static class ClassCatalog
{
    public static ClassCategory GetCategory(ClassType classId)
    {
        return classId switch
        {
            ClassType.Blademaster
                or ClassType.Goliath
                or ClassType.Rogue
                or ClassType.Knight
                => ClassCategory.Melee,

            ClassType.Priest
                or ClassType.Sage
                or ClassType.Apothecary
                or ClassType.Warlock
                => ClassCategory.Magic,

            ClassType.Skirmisher
                or ClassType.Musketeer 
                or ClassType.Archer
                => ClassCategory.Ranged,

            ClassType.Cultist
                or ClassType.Evoker
                or ClassType.Druid
                => ClassCategory.Summon,

            _ => ClassCategory.None
        };
    }


    // Old character saves store enum names as strings. Resolve them here and
    // write only the current name on subsequent saves; unknown IDs stay intact.
    public static bool TryResolveClassId(string classId, out ClassType id)
    {
        string canonicalId = classId?.ToLowerInvariant() switch
        {
            "swordmaster" => nameof(ClassType.Blademaster),
            "titanborn" => nameof(ClassType.Goliath),
            "shadow" => nameof(ClassType.Rogue),
            "monk" => nameof(ClassType.Knight),
            "cleric" => nameof(ClassType.Priest),
            "exorcist" => nameof(ClassType.Priest),
            "scholar" => nameof(ClassType.Sage),
            "caster" => nameof(ClassType.Warlock),
            "night_owl" => nameof(ClassType.Skirmisher),
            "enforcer" => nameof(ClassType.Musketeer),
            "sharpshooter" => nameof(ClassType.Archer),
            "binder" => nameof(ClassType.Evoker),
            "outrider" => nameof(ClassType.Druid),
            _ => classId
        };
        return System.Enum.TryParse(canonicalId, true, out id) && IsValid(id);
    }

    public static bool IsValid(ClassType classId)
    {
        return GetCategory(classId) != ClassCategory.None;
    }
    //檢查傳入的職業是否有效
}
