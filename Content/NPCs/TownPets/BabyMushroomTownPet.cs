using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.Tiles.Furniture;

namespace Occident.Content.NPCs.TownPets;

[AutoloadHead]
public sealed class BabyMushroomTownPet : ModNPC
{
    public override string Texture => "Occident/Assets/Sprites/Town Pets/city_baby_mushroom";

    // Original frame durations converted from milliseconds to Terraria's 60-Hz ticks.
    internal static readonly int[] FrameTicks = {
        13, 8, 5, 5, 5, 5, 4, 4, 5, 4, 4, 4, 4, 3, 5, 7, 13, 5, 5,
        5, 5, 4, 4, 4, 5, 4, 4, 4, 3, 4, 7, 10, 15, 6, 5, 5, 4, 4,
        4, 4, 4, 4, 4, 4, 6, 4, 9, 2, 2, 4, 3, 4, 6, 51, 3, 3, 11
    };
    private int animationTick;
    private int idleDelay;
    private Point lastHome = new(-1, -1);
    private int housingCheckTimer;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 57;
        NPCID.Sets.IsTownPet[Type] = true;
        NPCID.Sets.ExtraFramesCount[Type] = 0;
        NPCID.Sets.AttackFrameCount[Type] = 0;
        NPCID.Sets.ActsLikeTownNPC[Type] = true;
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.TownCat);
        NPC.townNPC = true;
        NPC.friendly = true;
        NPC.aiStyle = NPCAIStyleID.Passive;
        NPC.width = 22;
        NPC.height = 22;
        NPC.damage = 0;
        AnimationType = -1;
    }

    // FullName already includes the localized species name. A second given name
    // would produce "菇寶寶菇寶寶" in Chinese arrival messages.
    public override List<string> SetNPCNameList() => new() { string.Empty };
    public override bool UsesPartyHat() => false;
    public override bool CanGoToStatue(bool toKingStatue) => false;
    public override bool CanChat() => false;
    public override bool CheckActive() => false;

    public override bool CanTownNPCSpawn(int numTownNPCs) => MushroomBottleEntity.AnyPlaced();

    public override bool CheckConditions(int left, int right, int top, int bottom)
        => MushroomBottleEntity.TestedRoomContainsBottle();

    public override bool PreAI()
    {
        if (!string.IsNullOrEmpty(NPC.GivenName)) {
            NPC.GivenName = string.Empty;
            if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
        }
        if (Main.netMode != NetmodeID.MultiplayerClient) {
            if (++housingCheckTimer >= 120) {
                housingCheckTimer = 0;
                if (!MushroomBottleEntity.AnyPlaced()) {
                    // Leave without death, loot or gore when the last placed bottle is removed.
                    NPC.active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    return false;
                }
                // homeTileY is the floor; flood-fill from inside the room above it.
                if (!NPC.homeless && (!WorldGen.InWorld(NPC.homeTileX, NPC.homeTileY - 2) ||
                    !WorldGen.StartRoomCheck(NPC.homeTileX, NPC.homeTileY - 2) || !WorldGen.RoomNeeds(Type) ||
                    !MushroomBottleEntity.TestedRoomContainsBottle())) {
                    NPC.homeless = true;
                    lastHome = new Point(-1, -1);
                    animationTick = 0;
                    NPC.netUpdate = true;
                }
            }
            Point home = new(NPC.homeTileX, NPC.homeTileY);
            if (!NPC.homeless && home != lastHome && WorldGen.InWorld(home.X, home.Y)) {
                NPC.Bottom = new Vector2(home.X * 16 + 8, home.Y * 16);
                NPC.velocity = Vector2.Zero;
                lastHome = home;
                NPC.netUpdate = true;
            }
            if (idleDelay <= 0) idleDelay = Main.rand.Next(60 * 60, 120 * 60 + 1);
            if (!NPC.homeless && NPC.velocity.Y == 0f && animationTick == 0 && --idleDelay == 0) {
                animationTick = 1;
                NPC.netUpdate = true;
            }
        }
        if (NPC.homeless) {
            // Only unhoused pets wander. Terraria handles looking for another valid room.
            animationTick = 0;
            return true;
        }
        NPC.velocity.X = 0f;
        if (animationTick > 0) {
            animationTick++;
            if (animationTick > TotalAnimationTicks) {
                animationTick = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
            }
        }
        return false;
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(animationTick);
    public override void ReceiveExtraAI(BinaryReader reader) => animationTick = reader.ReadInt32();

    private static int TotalAnimationTicks
    {
        get {
            int total = 0;
            foreach (int ticks in FrameTicks) total += ticks;
            return total;
        }
    }

    public override void FindFrame(int frameHeight)
    {
        int frame = 0;
        int elapsed = animationTick - 1;
        if (elapsed >= 0) {
            while (frame < FrameTicks.Length - 1 && elapsed >= FrameTicks[frame])
                elapsed -= FrameTicks[frame++];
        }
        NPC.frame = new Rectangle(0, frame * frameHeight, 80, frameHeight);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // The bottom mushroom occupies x28..49, y72..93 in each 80x100 frame.
        // Anchor the visible feet to the collision box, leaving room above for the stack.
        spriteBatch.Draw(TextureAssets.Npc[Type].Value, NPC.Bottom - screenPos + new Vector2(0, NPC.gfxOffY),
            NPC.frame, drawColor, NPC.rotation, new Vector2(39, 94), NPC.scale, SpriteEffects.None, 0f);
        return false;
    }
}
