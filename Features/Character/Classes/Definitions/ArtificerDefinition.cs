namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义魔术师的职业标识、本地化索引及显示顺序。
///
/// Defines the Artificer class identifier, localization key,
/// and display order.
/// </summary>

public sealed class ArtificerDefinition : ClassDefinition
{
    public override string Id => "Artificer";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Artificer.DisplayName";

    public override int SortOrder => 80;
}