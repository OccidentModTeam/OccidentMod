#nullable enable

using System;
using System.Collections.Generic;

namespace VesperMod.Features.Character.Classes;

/// <summary>
/// 管理所有已注册的职业定义，提供职业注册与查询功能。
/// 不负责存储玩家的职业选择与执行效果。
///
/// Manages all registered class definitions and provides
/// registration and lookup functionality.
/// Does not store player selections or execute class effects.
/// </summary>

public static class ClassRegistry
{
    private static readonly Dictionary<string, ClassDefinition>
        Definitions = new(StringComparer.Ordinal);

    private static void Register(ClassDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definitions.Add(definition.Id, definition);
    }

    public static ClassDefinition? Get(string id)
    {
        return Definitions.GetValueOrDefault(id);
    }

    public static void Clear()
    {
        Definitions.Clear();
    }

    public static void RegisterAll()
    {
        foreach (var type in typeof(ClassDefinition).Assembly.GetTypes())
        {
            if(type.IsAbstract ||
                !typeof(ClassDefinition).IsAssignableFrom(type))
                continue;

            if(Activator.CreateInstance(type) is ClassDefinition definition)
                Register(definition);
        }
    }
}
