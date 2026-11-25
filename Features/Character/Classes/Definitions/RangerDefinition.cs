namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义射手的职业标识、本地化索引及显示顺序。
///
/// Defines the Ranger class identifier, localization key,
/// and display order.
/// </summary>

public sealed class RangerDefinition : ClassDefinition
{
    public override string Id => "Ranger";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Ranger.DisplayName";

    public override int SortOrder => 50;
}