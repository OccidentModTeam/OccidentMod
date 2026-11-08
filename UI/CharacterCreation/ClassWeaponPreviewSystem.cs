using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OccidentMod.Content.Items.Weapons.Summon;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.UI.CharacterCreation;

/// <summary>
///     在角色建立介面中繪製職業武器動作與召喚物預覽。
///     Renders class weapon animations and summon previews
///     in the character creation interface.
/// </summary>
/// <remarks>
///     使用獨立的展示角色，不執行實際攻擊，
///     也不會修改正在建立的玩家角色。
///     Uses an isolated display player without performing
///     actual attacks or modifying the character being created.
/// </remarks>
[Autoload(Side = ModSide.Client)]
public sealed class ClassWeaponPreviewSystem : ModSystem
{
    private const int AnimationCycle = 360;
    private const int WeaponDisplayEnd = 54;
    private const int WeaponAnimationStart = 18;
    private const int MushroomSpawnPhase = 48;

    private const string MushroomTexturePath =
        "OccidentMod/Assets/Sprites/Projectiles/Minions/baby_mushroom";

    private static readonly FieldInfo PlayerField =
        GetCharacterField("_player");

    private static readonly FieldInfo ScaleField =
        GetCharacterField("_characterScale");

    private static readonly FieldInfo AnimatedField =
        GetCharacterField("_animated");

    private static readonly FieldInfo BackgroundField =
        GetCharacterField("_drawsBackPanel");

    private static readonly FieldInfo PanelTextureField =
        GetCharacterField("_texture");

    private ConditionalWeakTable<UICharacter, PreviewState> states =
        new();

    /// <summary>
    ///     註冊原版角色預覽的繪製攔截。
    ///     Registers the vanilla character preview draw hook.
    /// </summary>
    public override void Load()
    {
        On_UICharacter.DrawSelf += DrawCharacter;
    }

    /// <summary>
    ///     移除繪製攔截並釋放預覽狀態。
    ///     Removes the draw hook and releases preview states.
    /// </summary>
    public override void Unload()
    {
        On_UICharacter.DrawSelf -= DrawCharacter;

        states = new ConditionalWeakTable<UICharacter, PreviewState>();
    }

    /// <summary>
    ///     在符合條件時使用獨立角色繪製武器預覽。
    ///     Draws a weapon preview using an isolated player
    ///     when the preview is enabled.
    /// </summary>
    private void DrawCharacter(
        On_UICharacter.orig_DrawSelf orig,
        UICharacter character,
        SpriteBatch spriteBatch)
    {
        var sourcePlayer =
            (Player)PlayerField.GetValue(character);

        var animated =
            (bool)AnimatedField.GetValue(character);

        if (!animated ||
            !CharacterCreationStepsSystem.TryGetWeaponPreview(
                sourcePlayer,
                out var weaponType))
        {
            states.Remove(character);

            orig(character, spriteBatch);
            return;
        }

        var state = states.GetValue(
            character,
            _ => new PreviewState());

        double now = Main.GlobalTimeWrappedHourly;

        UpdatePreviewWeapon(state, weaponType, now);

        var phase = GetAnimationPhase(state, now);

        var scale =
            (float)ScaleField.GetValue(character);

        var dimensions = character.GetDimensions();
        var center = dimensions.Center();

        DrawBackground(character, spriteBatch, dimensions.Position());

        var previewPlayer = state.DisplayPlayer;

        CopyAppearance(sourcePlayer, previewPlayer);
        PreparePreviewPlayer(previewPlayer, state, phase, weaponType);

        var position = center - new Vector2(
            previewPlayer.width / 2,
            previewPlayer.height / 2);

        DrawPreviewPlayer(
            previewPlayer,
            position,
            scale,
            phase,
            weaponType);

        DrawSummonPreview(
            state,
            spriteBatch,
            previewPlayer,
            position,
            scale,
            phase,
            weaponType);
    }

    /// <summary>
    ///     當武器變更或時間循環重設時初始化預覽。
    ///     Initializes the preview when the weapon changes
    ///     or the global time cycle resets.
    /// </summary>
    private static void UpdatePreviewWeapon(
        PreviewState state,
        int weaponType,
        double now)
    {
        if (state.WeaponType == weaponType &&
            now >= state.StartTime)
            return;

        state.WeaponType = weaponType;
        state.StartTime = now;

        state.DisplayPlayer.inventory[0] = new Item(weaponType);
    }

    /// <summary>
    ///     計算目前的武器預覽動畫影格。
    ///     Calculates the current weapon preview animation phase.
    /// </summary>
    private static int GetAnimationPhase(
        PreviewState state,
        double now)
    {
        return (int)((now - state.StartTime) * 60d)
               % AnimationCycle;
    }

    /// <summary>
    ///     繪製原版角色預覽背景。
    ///     Draws the vanilla character preview background.
    /// </summary>
    private static void DrawBackground(
        UICharacter character,
        SpriteBatch spriteBatch,
        Vector2 position)
    {
        var drawsBackground =
            (bool)BackgroundField.GetValue(character);

        if (!drawsBackground)
            return;

        var panel = (Asset<Texture2D>)
            PanelTextureField.GetValue(character);

        spriteBatch.Draw(
            panel.Value,
            position,
            Color.White);
    }

    /// <summary>
    ///     複製角色外觀，不複製裝備或武器動作。
    ///     Copies player appearance without copying
    ///     equipment or weapon actions.
    /// </summary>
    private static void CopyAppearance(
        Player source,
        Player target)
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

    /// <summary>
    ///     準備展示角色的武器及使用動畫狀態。
    ///     Prepares the display player's weapon
    ///     and item-use animation state.
    /// </summary>
    private static void PreparePreviewPlayer(
        Player preview,
        PreviewState state,
        int phase,
        int weaponType)
    {
        var item = preview.inventory[0];

        preview.selectedItem = 0;

        var showWeapon = phase < WeaponDisplayEnd;

        // 原版繪製系統會讀取快取的手持物。
        // 選單預覽不會執行正常玩家更新，因此必須手動指定。
        //
        // The vanilla renderer reads the cached visualized item.
        // Character creation does not run normal player updates.

        preview.lastVisualizedSelectedItem =
            showWeapon ? item : state.HiddenItem;

        preview.position = Vector2.Zero;

        preview.velocity = new Vector2(
            preview.direction,
            0f);

        preview.itemAnimationMax =
            Math.Max(1, item.useAnimation);

        var progress =
            phase >= WeaponAnimationStart &&
            phase < WeaponDisplayEnd
                ? (phase - WeaponAnimationStart) /
                  (float)(WeaponDisplayEnd - WeaponAnimationStart)
                : 0f;

        preview.itemAnimation = showWeapon
            ? Math.Max(
                1,
                (int)(preview.itemAnimationMax * (1f - progress)))
            : 0;

        preview.itemTime = preview.itemAnimation;

        preview.compositeFrontArm.enabled = false;
        preview.compositeBackArm.enabled = false;

        Main.instance.LoadItem(weaponType);
    }

    /// <summary>
    ///     繪製展示角色的武器姿勢與步行影格。
    ///     Draws the display player's weapon pose
    ///     and walking animation frames.
    /// </summary>
    private static void DrawPreviewPlayer(
        Player preview,
        Vector2 position,
        float scale,
        int phase,
        int weaponType)
    {
        var item = preview.inventory[0];

        var showWeapon = phase < WeaponDisplayEnd;

        using (new Main.CurrentPlayerOverride(preview))
        {
            // 只計算武器使用姿勢。
            // 不執行 ItemCheck 或 Shoot，避免真的發動攻擊。
            //
            // Only calculate the item-use pose.
            // Never invoke ItemCheck or Shoot.

            if (showWeapon)
                preview.ItemCheck_ApplyUseStyle(
                    0f,
                    item,
                    preview.GetItemDrawFrame(weaponType));

            preview.PlayerFrame();

            var walkingFrame =
                ((int)(Main.GlobalTimeWrappedHourly / 0.07f)
                    % 14 + 6) * 56;

            preview.legFrame.Y = walkingFrame;
            preview.headFrame.Y = walkingFrame;

            // 直接繪製隔離角色，保留原版手持物圖層。
            // Draw the isolated player directly to preserve
            // vanilla held-item rendering layers.

            Main.PlayerRenderer.DrawPlayer(
                Main.Camera,
                preview,
                position + Main.screenPosition,
                0f,
                Vector2.Zero,
                0f,
                scale);
        }
    }

    /// <summary>
    ///     繪製特殊召喚武器的召喚物預覽。
    ///     Draws summon previews for supported summon weapons.
    /// </summary>
    private static void DrawSummonPreview(
        PreviewState state,
        SpriteBatch spriteBatch,
        Player preview,
        Vector2 position,
        float scale,
        int phase,
        int weaponType)
    {
        if (weaponType !=
            ModContent.ItemType<AncientAgaricStaff>())
            return;

        // 固定角色腳底中央作為召喚物的地面基準點。
        // Anchor the summon to the player's feet.

        var feetPosition = position + new Vector2(
            preview.width * 0.5f,
            preview.height);

        state.DrawMushroom(
            spriteBatch,
            feetPosition,
            scale,
            preview.direction,
            phase);
    }

    /// <summary>
    ///     取得 UICharacter 的原版私有欄位。
    ///     Retrieves a private field from vanilla UICharacter.
    /// </summary>
    private static FieldInfo GetCharacterField(string name)
    {
        return typeof(UICharacter).GetField(
                   name,
                   BindingFlags.NonPublic | BindingFlags.Instance)
               ?? throw new MissingFieldException(
                   typeof(UICharacter).FullName,
                   name);
    }

    /// <summary>
    ///     保存單一角色預覽元件的獨立展示狀態。
    ///     Stores isolated preview state for one UICharacter element.
    /// </summary>
    private sealed class PreviewState
    {
        private const int MushroomFrameCount = 8;

        public readonly Player DisplayPlayer = new();

        public readonly Item HiddenItem = new();

        private readonly int[] feet =
            new int[MushroomFrameCount];

        public double StartTime;

        public int WeaponType;
        private int cellHeight;

        private int cellWidth;

        private Texture2D mushroom;

        /// <summary>
        ///     繪製蘑菇召喚物的跳躍動畫與雲霧效果。
        ///     Draws the mushroom summon animation
        ///     with hopping and cloud effects.
        /// </summary>
        public void DrawMushroom(
            SpriteBatch spriteBatch,
            Vector2 feetPosition,
            float scale,
            int direction,
            int phase)
        {
            if (phase < MushroomSpawnPhase)
                return;

            if (!EnsureMushroomTexture())
                return;

            var age = phase - MushroomSpawnPhase;

            var hopTick =
                age >= 22 && age < 202
                    ? (age - 22) % 45
                    : -1;

            var hop = CalculateHop(hopTick);

            var frame = GetMushroomFrame(hopTick);

            var alpha =
                MathHelper.Clamp(age / 8f, 0f, 1f) *
                MathHelper.Clamp(
                    (AnimationCycle - phase) / 24f,
                    0f,
                    1f);

            var ground = feetPosition + new Vector2(
                direction * 39f * scale,
                0f);

            var position = ground + new Vector2(
                direction * MathF.Sin(age / 45f) * 3f,
                -hop) * scale;

            DrawMushroomSprite(
                spriteBatch,
                position,
                scale,
                direction,
                frame,
                alpha);

            if (age < 36)
                DrawCloudParticles(
                    spriteBatch,
                    ground,
                    scale,
                    age);
        }

        /// <summary>
        ///     載入並驗證蘑菇召喚物的動畫素材。
        ///     Loads and validates the mushroom animation texture.
        /// </summary>
        private bool EnsureMushroomTexture()
        {
            if (mushroom != null)
                return true;

            mushroom = ModContent.Request<Texture2D>(
                MushroomTexturePath,
                AssetRequestMode.ImmediateLoad).Value;

            cellWidth = mushroom.Width;
            cellHeight = mushroom.Height / MushroomFrameCount;

            var validSize =
                (cellWidth == 20 && cellHeight == 20) ||
                (cellWidth == 32 && cellHeight == 36);

            if (!validSize)
            {
                mushroom = null;
                return false;
            }

            CalculateFrameFeet();

            return true;
        }

        /// <summary>
        ///     計算每個動畫影格的腳底位置，避免跳動時地面偏移。
        ///     Calculates the foot position of each animation frame
        ///     to prevent ground alignment shifts.
        /// </summary>
        private void CalculateFrameFeet()
        {
            var pixels =
                new Color[mushroom.Width * mushroom.Height];

            mushroom.GetData(pixels);

            for (var frame = 0; frame < MushroomFrameCount; frame++)
            {
                feet[frame] = cellHeight;

                var found = false;

                for (var y = cellHeight - 1;
                     y >= 0 && !found;
                     y--)
                for (var x = 0; x < cellWidth; x++)
                {
                    var index =
                        (frame * cellHeight + y) *
                        cellWidth + x;

                    if (pixels[index].A == 0)
                        continue;

                    feet[frame] = y + 1;
                    found = true;
                    break;
                }
            }
        }

        /// <summary>
        ///     計算蘑菇召喚物的跳躍高度。
        ///     Calculates the mushroom summon's hopping height.
        /// </summary>
        private static float CalculateHop(int hopTick)
        {
            if (hopTick < 6 || hopTick >= 30)
                return 0f;

            return MathF.Sin(
                (hopTick - 6) / 24f * MathHelper.Pi) * 7f;
        }

        /// <summary>
        ///     根據跳躍進度選擇蘑菇動畫影格。
        ///     Selects the mushroom animation frame
        ///     based on hopping progress.
        /// </summary>
        private static int GetMushroomFrame(int hopTick)
        {
            return hopTick < 0 ? 0
                : hopTick < 6 ? 1
                : hopTick < 12 ? 2
                : hopTick < 18 ? 3
                : hopTick < 23 ? 4
                : hopTick < 30 ? 6
                : hopTick < 35 ? 5
                : 0;
        }

        /// <summary>
        ///     繪製蘑菇召喚物的當前動畫影格。
        ///     Draws the current mushroom animation frame.
        /// </summary>
        private void DrawMushroomSprite(
            SpriteBatch spriteBatch,
            Vector2 position,
            float scale,
            int direction,
            int frame,
            float alpha)
        {
            Rectangle source = new(
                0,
                frame * cellHeight,
                cellWidth,
                cellHeight);

            Vector2 origin = new(
                cellWidth / 2f,
                feet[frame]);

            var effects = direction < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            spriteBatch.Draw(
                mushroom,
                position,
                source,
                Color.White * alpha,
                0f,
                origin,
                scale,
                effects,
                0f);
        }

        /// <summary>
        ///     在召喚物出現時繪製本地雲霧粒子。
        ///     Draws local cloud particles during summon appearance.
        /// </summary>
        /// <remarks>
        ///     沿用原本的雲霧圖集，但不建立世界粒子。
        ///     Reuses the original cloud atlas without
        ///     spawning particles into the world.
        /// </remarks>
        private static void DrawCloudParticles(
            SpriteBatch spriteBatch,
            Vector2 ground,
            float scale,
            int age)
        {
            for (var i = 0; i < 14; i++)
            {
                var particleAge =
                    age - i % 4 * 1.5f;

                if (particleAge < 0f)
                    continue;

                var progress = MathHelper.Clamp(
                    particleAge / 30f,
                    0f,
                    1f);

                var angle = i * 2.399963f;

                Vector2 start = new(
                    MathF.Cos(angle) * (3f + i % 5),
                    -9f + MathF.Sin(angle) * (3f + i % 4));

                Vector2 velocity = new(
                    MathF.Cos(angle) * 0.45f,
                    -0.35f - i % 5 * 0.08f);

                var offset =
                    (start + velocity * particleAge) * scale;

                Rectangle dustFrame = new(
                    DustID.Cloud % 100 * 10,
                    DustID.Cloud / 100 * 30 + i % 3 * 10,
                    8,
                    8);

                var size =
                    (0.8f + i % 3 * 0.08f) *
                    (1f - progress * 0.65f) *
                    scale;

                spriteBatch.Draw(
                    TextureAssets.Dust.Value,
                    ground + offset,
                    dustFrame,
                    Color.White * (1f - progress),
                    i * 0.4f,
                    new Vector2(4f),
                    size,
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}