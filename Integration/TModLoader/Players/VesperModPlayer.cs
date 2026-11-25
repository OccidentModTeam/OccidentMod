using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace VesperMod.Integration.TModLoader.Players;

public class VesperModPlayer : ModPlayer
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