#nullable enable

using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace OccidentMod.Integration.TModLoader;

/// <summary>
///
/// 保存角色创建时选择的初始武器选项。
/// 仅记录二选一的选择结果。
/// 不负责决定武器类型或实际发放装备。
///
/// Stores the starting weapon option selected
/// during character creation.
/// Records the choice without determining item types
/// or granting equipment.
///
/// </summary>

public sealed class OriginWeaponChoicePlayer : ModPlayer
{
    private const string SaveKey = "OriginWeaponChoice";

    public int SelectedChoice { get; private set; } = -1;

    public override void Initialize()
    {
        SelectedChoice = -1;
    }

    public void SetChoice(int choice)
    {
        if (choice is < -1 or > 1)
            return;

        SelectedChoice = choice;
    }

    public override void SaveData(TagCompound tag)
    {
        if (SelectedChoice >= 0)
            tag[SaveKey] = SelectedChoice;
    }

    public override void LoadData(TagCompound tag)
    {
        SelectedChoice = tag.ContainsKey(SaveKey)
            ? tag.GetInt(SaveKey)
            : -1;

        if (SelectedChoice is < -1 or > 1)
            SelectedChoice = -1;
    }
}