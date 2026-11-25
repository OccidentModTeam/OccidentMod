using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Integration.TModLoader.Players;

public class OccidentModPlayer : ModPlayer
{
    public string SelectedClassId { get; private set; } = "";

    public override void SaveData(TagCompound tag)
    {
        tag["SelectedClassId"] = SelectedClassId;
    }

    public override void LoadData(TagCompound tag)
    {
        SelectedClassId = tag.GetString("SelectedClassId");
    }

    public void SetClass(string classId)
    {
        SelectedClassId = classId;
    }
}