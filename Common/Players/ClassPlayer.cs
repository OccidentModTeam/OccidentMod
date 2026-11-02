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

        string previousClassID = ClassID;
        ClassID = nextClassID;
        CurrentClass = classid;
        
        OnClassChange(previousClassID, ClassID);
        return true;
    }

    private void OnClassChange(string previousClassID, string newClassID)
    {
        
    }

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
        // Parse only when loading; preserve unknown IDs for the list's fallback label.
        CurrentClass = ClassCatalog.TryResolveClassId(ClassID, out var id) ? id : ClassType.None;
        if (CurrentClass != ClassType.None)
            ClassID = CurrentClass.ToString();
        // 舊存檔已有職業時，也視為完成首次選擇。
        HasInitialClass = tag.GetBool("HasInitialClass") || hasChooseClass;
    }
}
