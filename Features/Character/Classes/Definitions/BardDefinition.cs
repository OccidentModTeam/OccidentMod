namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义吟游诗人的职业标识、本地化索引及显示顺序。
///
/// Defines the Bard class identifier, localization key,
/// and display order.
/// </summary>

public sealed class BardDefinition : ClassDefinition
{
    public override string Id => "Bard";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Bard.DisplayName";

    public override int SortOrder => 30;
}