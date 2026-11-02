using System;
using Occident.Common.Worlds;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.ModLoader;

namespace Occident.Common.UI.WorldCreation;

[Autoload(Side = ModSide.Client)]
public sealed class WorldListOptionsTagSystem : ModSystem
{
    public override void Load()
    {
        On_AWorldListItem.GetDifficulty += GetDifficulty;
        IL_UIWorldListItem.DrawSelf += ShowExtraLarge;
    }
    public override void Unload()
    {
        On_AWorldListItem.GetDifficulty -= GetDifficulty;
        IL_UIWorldListItem.DrawSelf -= ShowExtraLarge;
    }

    private static WorldOptions Read(WorldFileData data) =>
        data.TryGetHeaderData<WorldOptionsSystem>(out var header) ? WorldOptions.Read(header) : default;

    private static void GetDifficulty(On_AWorldListItem.orig_GetDifficulty orig, AWorldListItem item,
        out string text, out Color color)
    {
        orig(item, out text, out color);
        var difficulty = Read(item.Data).Difficulty;
        if (difficulty == OccidentWorldDifficulty.None)
            return;
        text = WorldCreationChoices.Text(difficulty.ToString());
        color = new Color(215, 177, 112);
    }

    private static void ShowExtraLarge(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchCallvirt<WorldFileData>("get_WorldSizeName")))
            throw new InvalidOperationException("Cannot find world size label in UIWorldListItem.DrawSelf.");
        cursor.Remove();
        // 只替換列表的顯示名稱，不改動原版 WorldFileData 或實際尺寸。
        cursor.EmitDelegate<Func<WorldFileData, string>>(data => Read(data).ExtraLarge
            ? WorldCreationChoices.Text("ExtraLarge") : data.WorldSizeName);
    }
}
