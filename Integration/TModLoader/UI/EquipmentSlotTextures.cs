using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace VesperMod.Integration.TModLoader.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class EquipmentSlotTextures : ModSystem
{
    private readonly Texture2D[,] backgrounds = new Texture2D[3, 3];

    public override void PostSetupContent()
    {
        const string prefix = "Assets/UI/Inventory/Inventory_Back13_";
        string[] columns = ["Equip", "Vanity", "Dye"];
        for (int loadout = 0; loadout < backgrounds.GetLength(0); loadout++)
        {
            string suffix = loadout == 0 ? "" : $"_Loadout{loadout + 1}";
            for (int column = 0; column < columns.Length; column++)
            {
                backgrounds[loadout, column] = Mod.Assets.Request<Texture2D>(
                    prefix + columns[column] + suffix, AssetRequestMode.ImmediateLoad).Value;
            }
        }

        var draw = typeof(ItemSlot).GetMethod(nameof(ItemSlot.Draw),
            [typeof(SpriteBatch), typeof(Item[]), typeof(int), typeof(int), typeof(Vector2), typeof(Color)]);
        if (draw is null)
        {
            Mod.Logger.Warn("Equipment slot draw method was not found.");
            return;
        }

        MonoModHooks.Modify(draw, ReplaceEquipmentBackgrounds);
    }

    private void ReplaceEquipmentBackgrounds(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Each vanilla equipment branch loads Back13 and then chooses its column tint.
        var textureLoads = new Instruction[3];
        var colorCalls = new Instruction[3];
        for (int i = 0; i < textureLoads.Length; i++)
        {
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchLdsfld(typeof(TextureAssets), nameof(TextureAssets.InventoryBack13)),
                    instruction => instruction.MatchCallvirt<Asset<Texture2D>>("get_Value"),
                    instruction => instruction.MatchStloc(out _),
                    instruction => instruction.MatchLdarg(3),
                    instruction => instruction.MatchLdarg(2),
                    instruction => instruction.MatchCall<ItemSlot>(nameof(ItemSlot.GetColorByLoadout))))
            {
                Mod.Logger.Warn("Equipment slot background branches were not found.");
                return;
            }

            textureLoads[i] = cursor.Prev.Previous.Previous.Previous.Previous;
            colorCalls[i] = cursor.Prev;
        }

        for (int i = 0; i < textureLoads.Length; i++)
        {
            cursor.Goto(textureLoads[i], MoveType.After);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Func<Texture2D, int, Texture2D>>(SelectBackground);

            cursor.Goto(colorCalls[i], MoveType.After);
            cursor.EmitDelegate<Func<Color, Color>>(color => new Color(color.A, color.A, color.A, color.A));
        }
    }

    private Texture2D SelectBackground(Texture2D original, int context)
    {
        int column = context switch
        {
            ItemSlot.Context.EquipArmor or ItemSlot.Context.EquipAccessory => 0,
            ItemSlot.Context.EquipArmorVanity or ItemSlot.Context.EquipAccessoryVanity => 1,
            ItemSlot.Context.EquipDye => 2,
            _ => -1,
        };

        int loadout = Main.LocalPlayer.CurrentLoadoutIndex;
        return column >= 0 && (uint)loadout < (uint)backgrounds.GetLength(0)
            ? backgrounds[loadout, column]
            : original;
    }
}
