namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义战士的职业标识、本地化索引及显示顺序。
///
/// Defines the Warrior class identifier, localization key,
/// and display order.
/// </summary>

public sealed class WarriorDefinition : ClassDefinition
{
    public override string Id => "Warrior";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Warrior.DisplayName";

    public override int SortOrder => 10;
}