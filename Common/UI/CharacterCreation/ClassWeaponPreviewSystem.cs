using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Content.Items.Weapons.Summon;

namespace Occident.Common.UI.CharacterCreation;

[Autoload(Side = ModSide.Client)]
public sealed class ClassWeaponPreviewSystem : ModSystem
{
    private static readonly FieldInfo PlayerField = Field("_player");
    private static readonly FieldInfo ScaleField = Field("_characterScale");
    private static readonly FieldInfo AnimatedField = Field("_animated");
    private static readonly FieldInfo BackgroundField = Field("_drawsBackPanel");
    private static readonly FieldInfo PanelTextureField = Field("_texture");
    private ConditionalWeakTable<UICharacter, PreviewState> states = new();

    public override void Load()
    {
        On_UICharacter.DrawSelf += DrawCharacter;
    }

    public override void Unload()
    {
        On_UICharacter.DrawSelf -= DrawCharacter;
        states = new();
    }

    private void DrawCharacter(On_UICharacter.orig_DrawSelf orig, UICharacter character, SpriteBatch spriteBatch)
    {
        Player player = (Player)PlayerField.GetValue(character);
        if (!(bool)AnimatedField.GetValue(character) ||
            !CharacterCreationStepsSystem.TryGetWeaponPreview(player, out int weaponType)) {
            states.Remove(character);
            orig(character, spriteBatch);
            return;
        }
        PreviewState state = states.GetValue(character, _ => new PreviewState());
        double now = Main.GlobalTimeWrappedHourly;
        if (state.WeaponType != weaponType || now < state.StartTime) {
            state.WeaponType = weaponType;
            state.StartTime = now;
            state.DisplayPlayer.inventory[0] = new Item(weaponType);
        }
        int phase = (int)((now - state.StartTime) * 60d) % 360;
        float scale = (float)ScaleField.GetValue(character);
        var dimensions = character.GetDimensions();
        Vector2 center = dimensions.Center();
        if ((bool)BackgroundField.GetValue(character)) {
            var panel = (Asset<Texture2D>)PanelTextureField.GetValue(character);
            spriteBatch.Draw(panel.Value, dimensions.Position(), Color.White);
        }

        Player preview = state.DisplayPlayer;
        CopyAppearance(player, preview);
        Item item = preview.inventory[0];
        preview.selectedItem = 0;
        bool showWeapon = phase < 54;
        // PlayerDrawSet.CopyBasicPlayerFields reads this cache, not HeldItem.
        // Menu previews do not run the normal player update that refreshes it.
        preview.lastVisualizedSelectedItem = showWeapon ? item : state.HiddenItem;
        preview.position = Vector2.Zero;
        preview.velocity = new Vector2(preview.direction, 0f);
        preview.itemAnimationMax = Math.Max(1, item.useAnimation);
        float progress = phase >= 18 && phase < 54 ? (phase - 18) / 36f : 0f;
        preview.itemAnimation = showWeapon
            ? Math.Max(1, (int)(preview.itemAnimationMax * (1f - progress))) : 0;
        preview.itemTime = preview.itemAnimation;
        preview.compositeFrontArm.enabled = false;
        preview.compositeBackArm.enabled = false;
        Main.instance.LoadItem(weaponType);
        Vector2 position = center - new Vector2(preview.width / 2, preview.height / 2);
        using (new Main.CurrentPlayerOverride(preview)) {
            // Calculate only the real use pose. Do not run ItemCheck or Shoot.
            if (showWeapon)
                preview.ItemCheck_ApplyUseStyle(0f, item, preview.GetItemDrawFrame(weaponType));
            preview.PlayerFrame();
            int walkingFrame = ((int)(Main.GlobalTimeWrappedHourly / 0.07f) % 14 + 6) * 56;
            preview.legFrame.Y = walkingFrame;
            preview.headFrame.Y = walkingFrame;
            // Native UICharacter normally blanks the held item. Render an isolated
            // display player directly so the native held-item layer can draw it.
            Main.PlayerRenderer.DrawPlayer(Main.Camera, preview, position + Main.screenPosition,
                0f, Vector2.Zero, 0f, scale);
        }
        if (weaponType == ModContent.ItemType<AncientAgaricStaff>()) {
            // Terraria scales preview draw data around the player's bottom center.
            // This floor stays fixed as the character scale changes.
            Vector2 feetPosition = position + new Vector2(preview.width * 0.5f, preview.height);
            state.DrawMushroom(spriteBatch, feetPosition, scale, preview.direction, phase);
        }
    }

    private static void CopyAppearance(Player source, Player target)
    {
        target.skinVariant = source.skinVariant;
        target.direction = source.direction;
        target.hair = source.hair;
        target.hairDye = source.hairDye;
        target.skinColor = source.skinColor;
        target.eyeColor = source.eyeColor;
        target.hairColor = source.hairColor;
        target.shirtColor = source.shirtColor;
        target.underShirtColor = source.underShirtColor;
        target.pantsColor = source.pantsColor;
        target.shoeColor = source.shoeColor;
    }

    private static FieldInfo Field(string name) => typeof(UICharacter).GetField(name,
        BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingFieldException(typeof(UICharacter).FullName, name);

    private sealed class PreviewState
    {
        public readonly Player DisplayPlayer = new Player();
        public readonly Item HiddenItem = new Item();
        public int WeaponType;
        public double StartTime;
        private Texture2D mushroom;
        private int cellWidth, cellHeight;
        private readonly int[] feet = new int[8];

        public void DrawMushroom(SpriteBatch batch, Vector2 feetPosition, float scale, int direction, int phase)
        {
            if (phase < 48) return;
            if (mushroom == null) {
                mushroom = ModContent.Request<Texture2D>("OccidentMod/Assets/Sprites/Summoned Entity/baby_mushroom",
                    AssetRequestMode.ImmediateLoad).Value;
                cellWidth = mushroom.Width;
                cellHeight = mushroom.Height / 8;
                if (!((cellWidth == 20 && cellHeight == 20) || (cellWidth == 32 && cellHeight == 36))) {
                    mushroom = null;
                    return;
                }
                Color[] pixels = new Color[mushroom.Width * mushroom.Height];
                mushroom.GetData(pixels);
                for (int f = 0; f < 8; f++) {
                    feet[f] = cellHeight;
                    bool found = false;
                    for (int y = cellHeight - 1; y >= 0 && !found; y--) {
                        for (int x = 0; x < cellWidth; x++) {
                            if (pixels[(f * cellHeight + y) * cellWidth + x].A == 0) continue;
                            feet[f] = y + 1;
                            found = true;
                            break;
                        }
                    }
                }
            }
            int age = phase - 48;
            int hopTick = age >= 22 && age < 202 ? (age - 22) % 45 : -1;
            float hop = hopTick >= 6 && hopTick < 30
                ? MathF.Sin((hopTick - 6) / 24f * MathHelper.Pi) * 7f : 0f;
            int frame = hopTick < 0 ? 0 : hopTick < 6 ? 1 : hopTick < 12 ? 2 :
                hopTick < 18 ? 3 : hopTick < 23 ? 4 : hopTick < 30 ? 6 : hopTick < 35 ? 5 : 0;
            float alpha = MathHelper.Clamp(age / 8f, 0f, 1f) * MathHelper.Clamp((360 - phase) / 24f, 0f, 1f);
            Vector2 ground = feetPosition + new Vector2(direction * 39f * scale, 0f);
            Vector2 position = ground + new Vector2(direction * MathF.Sin(age / 45f) * 3f, -hop) * scale;
            batch.Draw(mushroom, position, new Rectangle(0, frame * cellHeight, cellWidth, cellHeight),
                Color.White * alpha, 0f, new Vector2(cellWidth / 2f, feet[frame]), scale,
                direction<0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            if (age < 36) {
                // Use the same Cloud dust atlas as BabyMushroomMinion.OnSpawn.
                // UI-local particles never consume the world's Dust pool.
                for (int i = 0; i < 14; i++) {
                    float particleAge = age - (i % 4) * 1.5f;
                    if (particleAge < 0f) continue;
                    float progress = MathHelper.Clamp(particleAge / 30f, 0f, 1f);
                    float angle = i * 2.399963f;
                    Vector2 start = new(MathF.Cos(angle) * (3f + i % 5),
                        -9f + MathF.Sin(angle) * (3f + i % 4));
                    Vector2 velocity = new(MathF.Cos(angle) * 0.45f, -0.35f - (i % 5) * 0.08f);
                    Vector2 offset = (start + velocity * particleAge) * scale;
                    Rectangle dustFrame = new(DustID.Cloud % 100 * 10,
                        DustID.Cloud / 100 * 30 + i % 3 * 10, 8, 8);
                    float size = (0.8f + i % 3 * 0.08f) * (1f - progress * 0.65f) * scale;
                    batch.Draw(TextureAssets.Dust.Value, ground + offset, dustFrame,
                        Color.White * (1f - progress), i * 0.4f, new Vector2(4f), size,
                        SpriteEffects.None, 0f);
                }
            }
        }
    }
}
