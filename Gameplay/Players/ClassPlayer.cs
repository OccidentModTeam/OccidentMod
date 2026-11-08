using OccidentMod.Gameplay.Classes;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Gameplay.Players;

/// <summary>
///     管理玩家職業的執行期狀態、首次選擇與存檔資料。
///     Manages the player's class state, initial selection, and saved data.
/// </summary>
public class ClassPlayer : ModPlayer
{
    /// <summary>
    ///     目前職業的識別字串，未選擇時為空字串。
    ///     Current class identifier, or an empty string when unselected.
    /// </summary>

    public string ClassId { get; private set; } = "";

    /// <summary>
    ///     玩家是否已經選擇職業。
    ///     Whether the player has selected a class.
    /// </summary>

    public bool HasChooseClass => !string.IsNullOrEmpty(ClassId);

    /// <summary>
    ///     是否已完成首次職業選擇。
    ///     Whether the initial class selection has been completed.
    /// </summary>

    public bool HasInitialClass { get; private set; }

    /// <summary>
    ///     目前已解析的職業類型。
    ///     Currently resolved class type.
    /// </summary>

    public ClassType CurrentClass { get; private set; }

    /// <summary>
    ///     初始化玩家職業狀態。
    ///     Initializes the player's class state.
    /// </summary>
    public override void Initialize()
    {
        ClassId = "";
        CurrentClass = ClassType.None;
        HasInitialClass = false;
    }

    /// <summary>
    ///     執行首次職業選擇，成功後鎖定首次選擇狀態。
    ///     Performs the initial class selection and locks it after success.
    /// </summary>
    public bool SelectClass(string classId)
    {
        if (HasInitialClass)
            return false;

        if (!EditClass(classId))
            return false;

        HasInitialClass = true;
        return true;
    }

    /// <summary>
    ///     根據職業識別字串切換職業。
    ///     Changes the class using its string identifier.
    /// </summary>
    public bool EditClass(string classId)
    {
        return ClassCatalog.TryResolveClassId(classId, out var id)
               && EditClass(id);
    }

    /// <summary>
    ///     切換或清除目前職業，不重設首次選擇紀錄。
    ///     Changes or clears the current class without resetting initial selection.
    /// </summary>
    public bool EditClass(ClassType classId)
    {
        if (classId != ClassType.None && !ClassCatalog.IsValid(classId))
            return false;

        var nextClassId = classId == ClassType.None
            ? ""
            : classId.ToString();

        if (ClassId == nextClassId)
            return false;

        ClassId = nextClassId;
        CurrentClass = classId;

        return true;
    }

    /// <summary>
    ///     套用網路同步後的職業狀態。
    ///     Applies class state received through network synchronization.
    /// </summary>
    internal void ApplyNetworkState(
        ClassType classId,
        bool hasInitialClass)
    {
        // 雙方皆為 None 時保留未知職業代號。
        // Preserve unknown class identifiers when both states are None.
        if (CurrentClass != classId)
            EditClass(classId);

        HasInitialClass = hasInitialClass;
    }

    /// <summary>
    ///     儲存職業識別與首次選擇紀錄。
    ///     Saves the class identifier and initial selection state.
    /// </summary>
    public override void SaveData(TagCompound tag)
    {
        tag["ClassID"] = ClassId;
        tag["HasInitialClass"] = HasInitialClass;
    }

    /// <summary>
    ///     載入職業資料並維持舊存檔相容性。
    ///     Loads class data while maintaining legacy save compatibility.
    /// </summary>
    public override void LoadData(TagCompound tag)
    {
        ClassId = tag.ContainsKey("ClassID")
            ? tag.GetString("ClassID")
            : "";

        // 未知職業代號保留原字串，供角色列表備援顯示。
        // Preserve unknown identifiers for fallback character-list display.
        CurrentClass = ClassCatalog.TryResolveClassId(
            ClassId,
            out var id)
            ? id
            : ClassType.None;

        if (CurrentClass != ClassType.None)
            ClassId = CurrentClass.ToString();

        // 舊存檔已有職業時，視為完成首次選擇。
        // Treat existing classes in legacy saves as initially selected.
        HasInitialClass = tag.GetBool("HasInitialClass") || HasChooseClass;
    }
}