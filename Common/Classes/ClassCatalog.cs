// 集中處理職業分類、有效性與舊存檔名稱對照。
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


    // 舊角色以列舉名稱儲存職業；在此轉成現名，下次存檔寫回新名稱，無法辨識的舊代號仍保留。
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
