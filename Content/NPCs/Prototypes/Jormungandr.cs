// 真菌蠕蟲視覺原型，以單一 NPC 繪製整條分節身體與附肢。
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Occident.Content.NPCs.Prototypes;

// 這是造型原型：單一無害 NPC 管理可變長度的分節繪圖鏈，不包含首領進度、掉落、地形修改或身體傷害判定。
public sealed class Jormungandr : ModNPC
{
    private const string Folder ="OccidentMod/Assets/Sprites/Prototypes/Jormungandr/";
    private static readonly string[] Names = ["Head", "Forebody", "BodyLarge", "BodyMedium", "BodySmall", "TailRoot", "TailFin", "FinNear", "FinFar", "BodyVariant"];
    private static Asset<Texture2D>[] textures;
    private Vector2[] joints;
    private float phase;
    private float pathHeight;
    private const float SwimSpeed = 3.2f;
    private const float MaxPathSlope = .42f;
    private float WaveNumber
    {
        get {
            float length = 0;
            for (int i = 1; i < joints.Length; i++) length += Spacing(i);
            return MathHelper.TwoPi / Math.Max(400f, length * 1.15f);
        }
    }
    public override string Texture => Folder + "Head";

    public override void Load()
    {
        if (Main.dedServ) return;
        textures = new Asset<Texture2D>[Names.Length];
        for (int i = 0; i < Names.Length; i++) textures[i] = ModContent.Request<Texture2D>(Folder + Names[i]);
    }
    public override void Unload() => textures = null;
    public override void SetStaticDefaults()
    {
        NPCID.Sets.NPCBestiaryDrawModifiers draw = new() { Hide = true };
        NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, draw);
    }
    public override void SetDefaults()
    {
        NPC.width = 90; NPC.height = 66;
        NPC.lifeMax = 1; NPC.damage = 0; NPC.defense = 0;
        NPC.friendly = true; NPC.dontTakeDamage = true;
        NPC.noGravity = true; NPC.noTileCollide = true;
        NPC.aiStyle = -1; NPC.knockBackResist = 0; NPC.netAlways = true;
        NPC.npcSlots = 0;
    }
    public override bool CheckActive() => false;
    public override bool? CanBeHitByItem(Player player, Item item) => false;
    public override bool? CanBeHitByProjectile(Projectile projectile) => false;

    private int Part(int i)
    {
        if (i == 0) return 0;
        if (i == 1) return 1;
        if (i == joints.Length - 1) return 6;
        if (i == joints.Length - 2) return 5;
        if (i < 4) return 2;
        if (i >= joints.Length - 6) return 4;
        return i % 3 == 0 ? 9 : 3;
    }
    private float SegmentScale(int i) => Part(i) == 4
        ? MathHelper.Lerp(1f, .68f, MathHelper.Clamp((i - (joints.Length - 6)) / 3f, 0f, 1f))
        : Part(i) == 5 ? .85f : 1f;
    private float Spacing(int i) => Part(i) switch {
        1 => 83f, 2 => 54f, 3 or 9 => 39f, 4 => 27f * SegmentScale(i), 5 => 17f, 6 => 10f, _ => 40f
    };

    private void ResetChain(Vector2 head)
    {
        pathHeight = head.Y;
        NPC.Center = head;
        PlaceOnCurve();
    }

    private void PlaceOnCurve()
    {
        float k = WaveNumber;
        float amplitude = MaxPathSlope / k;
        float theta = -phase * SwimSpeed * 60f * k;
        float headX = NPC.Center.X;
        float Height(float x) => pathHeight + amplitude * MathF.Sin(theta + k * (x - headX));
        NPC.Center = new Vector2(headX, Height(headX));
        joints[0] = NPC.Center;
        for (int i = 1; i < joints.Length; i++) {
            Vector2 previous = joints[i - 1];
            float spacing = Spacing(i);
            // 沿單調曲線解出節段間的弦長距離，避免縫隙累積、自交或急轉時節段旋轉突變。
            float low = 0f, high = spacing;
            for (int step = 0; step < 16; step++) {
                float dx = (low + high) * .5f;
                float dy = Height(previous.X + dx) - previous.Y;
                if (dx * dx + dy * dy < spacing * spacing) low = dx;
                else high = dx;
            }
            float advance = (low + high) * .5f;
            joints[i] = new Vector2(previous.X + advance, Height(previous.X + advance));
        }
        float nextHeight = pathHeight + amplitude * MathF.Sin(theta - SwimSpeed * k);
        NPC.velocity = new Vector2(-SwimSpeed, nextHeight - NPC.Center.Y);
        NPC.rotation = new Vector2(-1f, -MaxPathSlope * MathF.Cos(theta)).ToRotation();
    }

    public override void AI()
    {
        int owner = (int)NPC.ai[1];
        if (owner < 0 || owner >= Main.maxPlayers || !Main.player[owner].active) { NPC.active = false; return; }
        Player player = Main.player[owner];
        if (player.dead) { NPC.active = false; return; }
        NPC.timeLeft = 600;
        int count = Math.Clamp((int)NPC.ai[0], 14, 48);
        if (joints == null || joints.Length != count) {
            joints = new Vector2[count];
            ResetChain(NPC.Center);
        }
        phase += 1f / 60f;
        // 整體使用一道寬廣的移動曲線；波長隨身長調整並限制斜率，維持背部朝上。
        pathHeight += MathHelper.Clamp((player.Center.Y - 220f - pathHeight) * .01f, -.25f, .25f);
        PlaceOnCurve();
        float halfView = Main.dedServ ? 1100f : Main.screenWidth / Math.Max(.5f, Main.GameViewMatrix.Zoom.X) * .5f;
        // 整隻移出視窗後才重新定位，避免在畫面內急轉掉頭。
        if (joints[^1].X < player.Center.X - halfView - 180f || NPC.Distance(player.Center) > 6500f)
            ResetChain(player.Center + new Vector2(halfView + 180f, -190f));
    }

    private float Heading(int i) => i == 0 ? NPC.rotation : (joints[i - 1] - joints[i]).ToRotation();
    private void DrawPart(SpriteBatch batch, int part, Vector2 position, float rotation, float scale = 1f, float opacity = 1f, Vector2? origin = null)
    {
        Texture2D image = textures[part].Value;
        // 原圖朝左，因此旋轉角度為 PI 時才對應素材原始方向。
        bool flip = MathF.Cos(rotation) > 0;
        batch.Draw(image, position - Main.screenPosition, null, Color.White * opacity,
            rotation - MathHelper.Pi, origin ?? image.Size() * .5f, scale,
            flip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
    }
    private void DrawFin(SpriteBatch batch, bool far)
    {
        float rotation = Heading(1);
        Vector2 forward = rotation.ToRotationVector2();
        Vector2 side = new(-forward.Y, forward.X);
        float sway = MathF.Sin(phase * 1.7f) * .10f;
        int part = far ? 8 : 7;
        Texture2D image = textures[part].Value;
        bool flipped = MathF.Cos(rotation) > 0;
        Vector2 root = joints[1] + side * (far ? -12 : 16) + forward * 5;
        Vector2 origin = new(5, flipped ? image.Height - 7 : 7);
        DrawPart(batch, part, root, rotation + (far ? -.17f - sway : .18f + sway),
            far ? .76f : 1f, far ? .78f : 1f, origin);
    }
    private void DrawTailFin(SpriteBatch batch)
    {
        int rootIndex = joints.Length - 2;
        float heading = Heading(rootIndex);
        Texture2D image = textures[6].Value;
        // 魚鰭接點在原圖 (8,35)，並非圖片中心；垂直翻轉時連接點也要反射，避免轉向造成根部位移。
        bool flipped = MathF.Cos(heading) > 0;
        Vector2 origin = new(8f, flipped ? image.Height - 35f : 35f);
        Vector2 attachment = joints[rootIndex] - heading.ToRotationVector2() * 10f;
        DrawPart(batch, 6, attachment, heading, .85f, origin: origin);
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (joints == null || textures == null) return false;
        DrawFin(spriteBatch, true);
        // 由尾往頭繪製，前方甲片可遮住後一節已完成的連接處。
        DrawTailFin(spriteBatch);
        for (int i = joints.Length - 2; i >= 1; i--)
            DrawPart(spriteBatch, Part(i), joints[i], Heading(i), SegmentScale(i));
        DrawFin(spriteBatch, false);
        DrawPart(spriteBatch, 1, joints[1], Heading(1));
        DrawPart(spriteBatch, 0, joints[0], Heading(0));
        return false;
    }
}
