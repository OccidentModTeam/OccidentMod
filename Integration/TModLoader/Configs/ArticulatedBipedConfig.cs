using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace OccidentMod.Integration.TModLoader.Configs;

/// <summary>調整高瞰者的攻擊。預設值可在此修改，遊戲內模組設定可覆寫。</summary>
public sealed class ArticulatedBipedConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    // 震地以落腳點為中心，此值是左右各自的地面距離（格）。
    [DefaultValue(10)]
    [Range(1, 24)]
    public int StompRangeTiles { get; set; } = 10;

    [DefaultValue(36)]
    [Range(0, 500)]
    public int StompDamage { get; set; } = 36;

    [DefaultValue(48)]
    [Range(0, 500)]
    public int HeadStrikeDamage { get; set; } = 48;

    // 60 ticks 約一秒。只影響抬腳後停留的時間。
    [DefaultValue(37)]
    [Range(0, 180)]
    public int StompHoldTicks { get; set; } = 37;

    [DefaultValue(25)]
    [Range(0, 100)]
    public int StompLongHoldChancePercent { get; set; } = 25;

    [DefaultValue(32)]
    [Range(0, 180)]
    public int StompExtraHoldTicks { get; set; } = 32;

    [DefaultValue(50)]
    [Range(0, 100)]
    public int JumpReactionChancePercent { get; set; } = 50;

    internal int StompRadius => Math.Clamp(StompRangeTiles, 1, 24) * 16;
    internal int GetStompHoldTicks(int roll)
    {
        int hold = Math.Clamp(StompHoldTicks, 0, 180);
        if (roll < Math.Clamp(StompLongHoldChancePercent, 0, 100))
            hold += Math.Clamp(StompExtraHoldTicks, 0, 180);

        return hold;
    }
}
