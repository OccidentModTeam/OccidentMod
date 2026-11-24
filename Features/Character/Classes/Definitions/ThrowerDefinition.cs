namespace OccidentMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义镖客的职业标识、本地化索引及显示顺序。
///
/// Defines the Thrower class identifier, localization key,
/// and display order.
/// </summary>

public sealed class ThrowerDefinition : ClassDefinition
{
    public override string Id => "Thrower";

    public override string DisplayNameKey =>
        "Mods.OccidentMod.Classes.Thrower.DisplayName";

    public override int SortOrder => 60;
}