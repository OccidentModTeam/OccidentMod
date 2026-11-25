namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义法师的职业标识、本地化索引及显示顺序。
///
/// Defines the Mage class identifier, localization key,
/// and display order.
/// </summary>

public sealed class MageDefinition : ClassDefinition
{
    public override string Id => "Mage";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Mage.DisplayName";

    public override int SortOrder => 40;
}