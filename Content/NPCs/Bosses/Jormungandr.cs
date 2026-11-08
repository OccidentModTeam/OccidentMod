#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OccidentMod.Content.NPCs.Bosses;

/// <summary>
///     耶夢加得的無害視覺測試原型。
///     Harmless visual prototype of Jormungandr.
/// </summary>
public sealed class Jormungandr : ModNPC
{
    private const string Folder =
        "OccidentMod/Assets/Sprites/NPCs/Bosses/Jormungandr/";

    private const float SwimSpeed = 3.2f;
    private const float MaxPathSlope = 0.42f;

    private static readonly string[] Names =
    {
        "Head",
        "Forebody",
        "BodyLarge",
        "BodyMedium",
        "BodySmall",
        "TailRoot",
        "TailFin",
        "FinNear",
        "FinFar",
        "BodyVariant"
    };

    private static Asset<Texture2D>[]? _textures;

    private Vector2[] joints = Array.Empty<Vector2>();
    private float pathHeight;

    private float phase;

    public override string Texture => Folder + "Head";

    private float WaveNumber
    {
        get
        {
            var length = 0f;

            for (var i = 1; i < joints.Length; i++) length += Spacing(i);

            return MathHelper.TwoPi /
                   Math.Max(400f, length * 1.15f);
        }
    }

    public override void Load()
    {
        if (Main.dedServ)
            return;

        _textures = new Asset<Texture2D>[Names.Length];

        for (var i = 0; i < Names.Length; i++)
            _textures[i] =
                ModContent.Request<Texture2D>(
                    Folder + Names[i]);
    }

    public override void Unload()
    {
        _textures = null;
    }

    public override void SetStaticDefaults()
    {
        NPCID.Sets.NPCBestiaryDrawModifiers draw = new()
        {
            Hide = true
        };

        NPCID.Sets.NPCBestiaryDrawOffset.Add(
            Type,
            draw);
    }

    public override void SetDefaults()
    {
        NPC.width = 90;
        NPC.height = 66;

        NPC.lifeMax = 1;
        NPC.damage = 0;
        NPC.defense = 0;

        NPC.friendly = true;
        NPC.dontTakeDamage = true;

        NPC.noGravity = true;
        NPC.noTileCollide = true;

        NPC.aiStyle = -1;
        NPC.knockBackResist = 0f;
        NPC.netAlways = true;
        NPC.npcSlots = 0f;
    }

    public override bool CheckActive()
    {
        return false;
    }

    public override bool? CanBeHitByItem(
        Player player,
        Item item)
    {
        return false;
    }

    public override bool? CanBeHitByProjectile(
        Projectile projectile)
    {
        return false;
    }

    /// <summary>
    ///     對應節段使用的貼圖索引。
    ///     Resolve the texture index for each segment.
    /// </summary>
    private int Part(int index)
    {
        if (index == 0)
            return 0;

        if (index == 1)
            return 1;

        if (index == joints.Length - 1)
            return 6;

        if (index == joints.Length - 2)
            return 5;

        if (index < 4)
            return 2;

        if (index >= joints.Length - 6)
            return 4;

        return index % 3 == 0 ? 9 : 3;
    }

    private float SegmentScale(int index)
    {
        return Part(index) == 4
            ? MathHelper.Lerp(
                1f,
                0.68f,
                MathHelper.Clamp(
                    (index - (joints.Length - 6)) / 3f,
                    0f,
                    1f))
            : Part(index) == 5
                ? 0.85f
                : 1f;
    }

    private float Spacing(int index)
    {
        return Part(index) switch
        {
            1 => 83f,
            2 => 54f,
            3 or 9 => 39f,
            4 => 27f * SegmentScale(index),
            5 => 17f,
            6 => 10f,
            _ => 40f
        };
    }

    /// <summary>
    ///     重新建立分節身體的位置。
    ///     Reset the body chain.
    /// </summary>
    private void ResetChain(Vector2 head)
    {
        pathHeight = head.Y;
        NPC.Center = head;

        PlaceOnCurve();
    }

    /// <summary>
    ///     讓整條身體沿平滑曲線游動。
    ///     Position the body along a smooth curve.
    /// </summary>
    private void PlaceOnCurve()
    {
        var wave = WaveNumber;
        var amplitude = MaxPathSlope / wave;

        var theta =
            -phase * SwimSpeed * 60f * wave;

        var headX = NPC.Center.X;

        float Height(float x)
        {
            return pathHeight +
                   amplitude *
                   MathF.Sin(
                       theta + wave * (x - headX));
        }

        NPC.Center = new Vector2(
            headX,
            Height(headX));

        joints[0] = NPC.Center;

        for (var i = 1; i < joints.Length; i++)
        {
            var previous = joints[i - 1];
            var spacing = Spacing(i);

            var low = 0f;
            var high = spacing;

            // 二分搜尋每節的自然間距。
            // Solve segment spacing with binary search.
            for (var step = 0; step < 16; step++)
            {
                var dx = (low + high) * 0.5f;

                var dy =
                    Height(previous.X + dx) -
                    previous.Y;

                if (dx * dx + dy * dy <
                    spacing * spacing)
                    low = dx;
                else
                    high = dx;
            }

            var advance = (low + high) * 0.5f;

            joints[i] = new Vector2(
                previous.X + advance,
                Height(previous.X + advance));
        }

        var nextHeight =
            pathHeight +
            amplitude *
            MathF.Sin(
                theta - SwimSpeed * wave);

        NPC.velocity = new Vector2(
            -SwimSpeed,
            nextHeight - NPC.Center.Y);

        NPC.rotation = new Vector2(
            -1f,
            -MaxPathSlope * MathF.Cos(theta)
        ).ToRotation();
    }

    public override void AI()
    {
        var owner = (int)NPC.ai[1];

        if (owner < 0 ||
            owner >= Main.maxPlayers ||
            !Main.player[owner].active)
        {
            NPC.active = false;
            return;
        }

        var player = Main.player[owner];

        if (player.dead)
        {
            NPC.active = false;
            return;
        }

        NPC.timeLeft = 600;

        var count = Math.Clamp(
            (int)NPC.ai[0],
            14,
            48);

        if (joints.Length != count)
        {
            joints = new Vector2[count];
            ResetChain(NPC.Center);
        }

        phase += 1f / 60f;

        // 平滑追隨玩家高度，不突然急轉。
        // Smoothly follow the player's altitude.
        pathHeight += MathHelper.Clamp(
            (player.Center.Y -
             220f -
             pathHeight) * 0.01f,
            -0.25f,
            0.25f);

        PlaceOnCurve();

        var halfView = Main.dedServ
            ? 1100f
            : Main.screenWidth /
              Math.Max(
                  0.5f,
                  Main.GameViewMatrix.Zoom.X) *
              0.5f;

        // 全部節段離開畫面才重新定位。
        // Reposition only after the chain leaves view.
        if (joints[^1].X <
            player.Center.X -
            halfView -
            180f ||
            NPC.Distance(player.Center) > 6500f)
            ResetChain(
                player.Center +
                new Vector2(
                    halfView + 180f,
                    -190f));
    }

    private float Heading(int index)
    {
        return index == 0
            ? NPC.rotation
            : (joints[index - 1] -
               joints[index]).ToRotation();
    }

    /// <summary>
    ///     繪製單一身體或附肢。
    ///     Draw a body segment or appendage.
    /// </summary>
    private static void DrawPart(
        SpriteBatch batch,
        int part,
        Vector2 position,
        float rotation,
        float scale = 1f,
        float opacity = 1f,
        Vector2? origin = null)
    {
        if (_textures is null)
            return;

        var image =
            _textures[part].Value;

        var flip =
            MathF.Cos(rotation) > 0f;

        batch.Draw(
            image,
            position - Main.screenPosition,
            null,
            Color.White * opacity,
            rotation - MathHelper.Pi,
            origin ?? image.Size() * 0.5f,
            scale,
            flip
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None,
            0f);
    }

    private void DrawFin(
        SpriteBatch batch,
        bool far)
    {
        if (_textures is null)
            return;

        var rotation = Heading(1);

        var forward =
            rotation.ToRotationVector2();

        Vector2 side = new(
            -forward.Y,
            forward.X);

        var sway =
            MathF.Sin(phase * 1.7f) *
            0.10f;

        var part = far ? 8 : 7;

        var image =
            _textures[part].Value;

        var flipped =
            MathF.Cos(rotation) > 0f;

        var root =
            joints[1] +
            side * (far ? -12f : 16f) +
            forward * 5f;

        Vector2 origin = new(
            5f,
            flipped
                ? image.Height - 7f
                : 7f);

        DrawPart(
            batch,
            part,
            root,
            rotation +
            (far
                ? -0.17f - sway
                : 0.18f + sway),
            far ? 0.76f : 1f,
            far ? 0.78f : 1f,
            origin);
    }

    private void DrawTailFin(SpriteBatch batch)
    {
        if (_textures is null)
            return;

        var rootIndex =
            joints.Length - 2;

        var heading =
            Heading(rootIndex);

        var image =
            _textures[6].Value;

        var flipped =
            MathF.Cos(heading) > 0f;

        Vector2 origin = new(
            8f,
            flipped
                ? image.Height - 35f
                : 35f);

        var attachment =
            joints[rootIndex] -
            heading.ToRotationVector2() *
            10f;

        DrawPart(
            batch,
            6,
            attachment,
            heading,
            0.85f,
            origin: origin);
    }

    public override bool PreDraw(
        SpriteBatch spriteBatch,
        Vector2 screenPos,
        Color drawColor)
    {
        if (joints.Length < 3 ||
            _textures is null)
            return false;

        DrawFin(spriteBatch, true);
        DrawTailFin(spriteBatch);

        // 從尾往頭繪製，讓前節蓋住後節。
        // Draw back-to-front for natural overlap.
        for (var i = joints.Length - 2;
             i >= 1;
             i--)
            DrawPart(
                spriteBatch,
                Part(i),
                joints[i],
                Heading(i),
                SegmentScale(i));

        DrawFin(spriteBatch, false);

        DrawPart(
            spriteBatch,
            1,
            joints[1],
            Heading(1));

        DrawPart(
            spriteBatch,
            0,
            joints[0],
            Heading(0));

        return false;
    }
}