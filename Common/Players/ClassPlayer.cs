// 玩家職業的執行期與存檔狀態，區分初次選職與後續切換。
using Occident.Common.Classes;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Occident.Common.Players;

public class ClassPlayer : ModPlayer
{
    public string ClassID {get; private set; } = "";
    //未選擇前保留為空字串

    public bool hasChooseClass => !string.IsNullOrEmpty(ClassID);
    public bool HasInitialClass { get; private set; }
    public ClassType CurrentClass { get; private set; }

    public override void Initialize()
    {
        ClassID = "";
        CurrentClass = ClassType.None;
        HasInitialClass = false;
    }  
    //初始化

    // 初始職業只能確認一次；一般切換請走 EditClass，不重設初選旗標。
    public bool SelectClass(string classid)
    {
        if(HasInitialClass)
            return false;
        
        if(!EditClass(classid))
            return false;
        
        HasInitialClass = true;
        return true;
    }
    //如果選擇職業後就把職業的名稱存到該變量中

    public bool EditClass(string classid)
    {
        return ClassCatalog.TryResolveClassId(classid, out var id) && EditClass(id);
    }

    // 職業飾品欄可在裝備變更時呼叫；None 代表清除目前職業。
    // 首次選擇鎖定不阻止日後換職業，存檔與列表標籤都讀取同一份 ClassID。
    public bool EditClass(ClassType classid)
    {
        if (classid != ClassType.None && !ClassCatalog.IsValid(classid))
            return false;
        string nextClassID = classid == ClassType.None ? "" : classid.ToString();
        if (ClassID == nextClassID)
            return false;

        ClassID = nextClassID;
        CurrentClass = classid;
        return true;
    }

    internal void ApplyNetworkState(ClassType classId, bool hasInitialClass)
    {
        // 雙方皆以 None 表示未知職業時，不清掉舊代號，讓存檔與角色列表仍能使用備援顯示。
        if (CurrentClass != classId)
            EditClass(classId);
        HasInitialClass = hasInitialClass;
    }

    // 儲存的是職業識別與初選紀錄，展示配點不寫入此處。
    public override void SaveData(TagCompound tag)
    {
        tag["ClassID"] = ClassID;
        tag["HasInitialClass"] = HasInitialClass;
    }

    public override void LoadData(TagCompound tag)
    {
        ClassID = tag.ContainsKey("ClassID")
            ? tag.GetString("ClassID")
            : "";
        // 只在讀檔時解析職業；未知代號保留給角色列表的備援文字使用。
        CurrentClass = ClassCatalog.TryResolveClassId(ClassID, out var id) ? id : ClassType.None;
        if (CurrentClass != ClassType.None)
            ClassID = CurrentClass.ToString();
        // 舊存檔已有職業時，也視為完成首次選擇。
        HasInitialClass = tag.GetBool("HasInitialClass") || hasChooseClass;
    }
}
