#nullable enable
namespace OccidentMod.Features.Character.Classes;

/// <summary>
/// 定义职业的基本参数与共用属性
///
/// Defines the basic parameters and shared properties of a class.
/// </summary>

public abstract class ClassDefinition
{
    public abstract string Id { get; }
    public abstract string DisplayNameKey { get; }
    public virtual int SortOrder => 0;
}