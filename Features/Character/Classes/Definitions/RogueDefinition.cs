namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义盗贼的职业标识、本地化索引及显示顺序。
///
/// Defines the Rogue class identifier, localization key,
/// and display order.
/// </summary>

public sealed class RogueDefinition : ClassDefinition
{
    public override string Id => "Rogue";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Rogue.DisplayName";

    public override int SortOrder => 20;
}