namespace VesperMod.Features.Character.Classes.Definitions;

/// <summary>
/// 定义召唤师的职业标识、本地化索引及显示顺序。
///
/// Defines the Summoner class identifier, localization key,
/// and display order.
/// </summary>

public sealed class SummonerDefinition : ClassDefinition
{
    public override string Id => "Summoner";

    public override string DisplayNameKey =>
        "Mods.VesperMod.Classes.Summoner.DisplayName";

    public override int SortOrder => 70;
}