using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace VesperMod.Presentation.Rendering;

internal enum BipedMotion
{
    Walk,
    Stomp,
    NeckStrike,
    SummonCall,
}

internal readonly record struct BipedPose(
    Vector2 Body, Vector2 NearAnkle, Vector2 FarAnkle, float NearFootAngle,
    float ShoulderAngle, float NeckAngle, float HeadAngle, float TailAngle, float FarFootAngle = 0f);

internal static class ArticulatedBipedRig
{
    internal const string AssetPath = "VesperMod/Assets/Sprites/NPCs/Enemies/ArticulatedBiped/";
    internal const float Scale = 2f;
    internal const int StrikeHitStart = 31;
    internal const int StrikeHitEnd = 50;
    internal const int StrikeEnd = 84;
    internal const int SummonCallTicks = 180;

    private const int StompLiftTicks = 42;
    private const int StompDropTicks = 7;
    private const int StompRecoveryTicks = 32;
    private const float GroundY = 78f;
    private const float AnkleY = GroundY - 5f;
    private static readonly Vector2 GroundAnchor = new(46f, GroundY);
    private static readonly Vector2 NeckRoot = new(39f, 28f);
    private static readonly Vector2 NeckJoint = new(25f, 26f);
    private static readonly Vector2 HeadJoint = new(14f, 16f);
    private static readonly Vector2 HeadCenter = new(14.5f, 10f);
    private static readonly Vector2 TailRoot = new(53.5f, 26.5f);

    private readonly record struct StompKey(int Frame, float X, float Y, float AnkleX, float Lift, float FootAngle, float Tail);
    private readonly record struct StrikeKey(int Frame, float X, float Y, float Shoulder, float Neck, float Head, float Tail);
    private readonly record struct CallKey(float Frame, float Head, float Tail);
    private readonly record struct NeckPose(Vector2 Root, Vector2 Middle, Vector2 Head);

    private static readonly StompKey[] StompKeys =
    [
        new(0, 0f, 0f, 41f, 0f, 0f, 0f),
        new(4, 2.5f, 3.5f, 41f, 0f, 0f, 2f),
        new(9, 2f, -1f, 36f, 21f, 23f, -3f),
        new(12, 2f, -2f, 35f, 26f, 28f, -5f),
        new(14, 1.5f, 0f, 37f, 20f, 18f, -4f),
        new(15, 0.5f, 3f, 39f, 10f, 4f, -2f),
        new(16, -1f, 7f, 41f, 0f, 0f, 4f),
        new(18, -0.5f, 5.5f, 41f, 0f, 0f, 5f),
        new(22, 0f, 1f, 41f, 0f, 0f, -1.5f),
        new(26, 0f, 0f, 41f, 0f, 0f, 0f),
    ];

    private static readonly StrikeKey[] StrikeKeys =
    [
        new(0, 0f, 0f, 0f, 0f, 0f, 0f),
        new(6, 2f, -1f, -16f, -23f, -10f, 2f),
        new(8, 1f, -1f, -15f, -26f, -14f, 4f),
        new(10, -2f, 3f, 44f, 10f, 5f, 5f),
        new(12, -4f, 10f, 86f, 68f, 42f, -2f),
        new(14, -5f, 12f, 101f, 116f, 83f, -7f),
        new(16, -5f, 11f, 103f, 119f, 77f, -5f),
        new(20, -3f, 8f, 72f, 87f, 43f, -1f),
        new(24, 0f, 2f, 18f, 23f, 8f, 2f),
        new(28, 0f, 0f, 0f, 0f, 0f, 0f),
    ];

    private static readonly CallKey[] CallKeys =
    [
        new(0f, 0f, 0f),
        new(3f, 9f, 0f),
        new(9f, 44f, -1.3f),
        new(11f, 42f, -3.4f),
        new(18f, -43f, 0.8f),
        new(20f, -41f, 3.8f),
        new(24f, 16f, 0.2f),
        new(27f, -12f, -1.5f),
        new(30f, 8f, 0.9f),
        new(32.5f, -5f, -0.5f),
        new(35f, 3f, 0.3f),
        new(38f, 0f, 0f),
    ];

    private static Asset<Texture2D> bodyTexture, headTexture, tailTexture, neckFrontTexture, neckRearTexture;
    private static Asset<Texture2D> legTexture, kneeTexture, footTexture;

    internal static void LoadTextures()
    {
        bodyTexture = ModContent.Request<Texture2D>(AssetPath + "Body");
        headTexture = ModContent.Request<Texture2D>(AssetPath + "Head");
        tailTexture = ModContent.Request<Texture2D>(AssetPath + "Tail");
        neckFrontTexture = ModContent.Request<Texture2D>(AssetPath + "NeckFront");
        neckRearTexture = ModContent.Request<Texture2D>(AssetPath + "NeckRear");
        legTexture = ModContent.Request<Texture2D>(AssetPath + "LegBone");
        kneeTexture = ModContent.Request<Texture2D>(AssetPath + "Knee");
        footTexture = ModContent.Request<Texture2D>(AssetPath + "Foot");
    }

    internal static void UnloadTextures()
    {
        bodyTexture = headTexture = tailTexture = neckFrontTexture = neckRearTexture = null;
        legTexture = kneeTexture = footTexture = null;
    }

    internal static int StompImpactTick(int holdTicks) => StompLiftTicks + holdTicks + StompDropTicks;
    internal static int StompEndTick(int holdTicks) => StompImpactTick(holdTicks) + StompRecoveryTicks;

    internal static BipedPose GetPose(BipedMotion motion, int tick, int holdTicks, float phase = 0f, float pace = 0f)
    {
        if (motion == BipedMotion.Stomp)
        {
            int impact = StompImpactTick(holdTicks);
            float frame = tick < StompLiftTicks ? tick * (12f / StompLiftTicks)
                : tick <= impact - StompDropTicks ? 12f
                : tick <= impact ? 12f + (tick - impact + StompDropTicks) * (4f / StompDropTicks)
                : 16f + (tick - impact) * (10f / StompRecoveryTicks);
            return StompPose(frame);
        }

        if (motion == BipedMotion.NeckStrike)
            return StrikePose(tick * (28f / StrikeEnd));

        if (motion == BipedMotion.SummonCall)
            return SummonCallPose(tick * (38f / SummonCallTicks));

        float cycle = phase * MathHelper.TwoPi * 2f;
        float sway = MathF.Sin(cycle) * pace;
        Vector2 near = WalkAnkle(phase, 41f, pace, out float nearAngle);
        Vector2 far = WalkAnkle(phase + 0.5f, 48f, pace, out float farAngle);
        return new(new Vector2(0f, 1.1f * MathF.Cos(cycle) * pace), near, far,
            nearAngle, 2f * sway, 4f * sway, -2f * sway, TailAngle(2.4f * MathF.Sin(cycle - 1f) * pace), farAngle);
    }

    private static Vector2 WalkAnkle(float phase, float center, float pace, out float angle)
    {
        phase -= MathF.Floor(phase);
        if (phase < 0.58f)
        {
            angle = 0f;
            return new(center + (-6f + 12f * phase / 0.58f) * pace, AnkleY);
        }

        float swing = (phase - 0.58f) / 0.42f;
        float lift = MathF.Sin(MathHelper.Pi * swing) * pace;
        angle = 17f * lift;
        return new(center + (6f - 12f * swing) * pace, AnkleY - 9f * lift);
    }

    private static BipedPose StompPose(float frame)
    {
        int index = 1;
        while (index < StompKeys.Length - 1 && frame > StompKeys[index].Frame)
            index++;

        StompKey a = StompKeys[index - 1], b = StompKeys[index];
        float t = Smooth(a.Frame, b.Frame, frame);
        Vector2 body = new(MathHelper.Lerp(a.X, b.X, t), MathHelper.Lerp(a.Y, b.Y, t));
        float lift = MathHelper.Lerp(a.Lift, b.Lift, t);
        float neck = -0.22f * lift + Math.Max(0f, body.Y - 2f) * 1.4f;
        return new(body, new(MathHelper.Lerp(a.AnkleX, b.AnkleX, t), AnkleY - lift),
            new(48f, AnkleY), MathHelper.Lerp(a.FootAngle, b.FootAngle, t), neck * 0.35f, neck,
            neck * 0.5f, TailAngle(MathHelper.Lerp(a.Tail, b.Tail, t)));
    }

    private static BipedPose StrikePose(float frame)
    {
        int index = 1;
        while (index < StrikeKeys.Length - 1 && frame > StrikeKeys[index].Frame)
            index++;

        StrikeKey a = StrikeKeys[index - 1], b = StrikeKeys[index];
        float t = Smooth(a.Frame, b.Frame, frame);
        return new(new(MathHelper.Lerp(a.X, b.X, t), MathHelper.Lerp(a.Y, b.Y, t)), new(40f, AnkleY),
            new(51f, AnkleY), 0f, MathHelper.Lerp(a.Shoulder, b.Shoulder, t), MathHelper.Lerp(a.Neck, b.Neck, t),
            MathHelper.Lerp(a.Head, b.Head, t), TailAngle(MathHelper.Lerp(a.Tail, b.Tail, t)));
    }

    private static BipedPose SummonCallPose(float frame)
    {
        int index = 1;
        while (index < CallKeys.Length - 1 && frame > CallKeys[index].Frame)
            index++;

        CallKey a = CallKeys[index - 1], b = CallKeys[index];
        float t = Smooth(a.Frame, b.Frame, frame);
        return new(Vector2.Zero, new(40f, AnkleY), new(49f, AnkleY), 0f, 0f, 0f,
            MathHelper.Lerp(a.Head, b.Head, t), TailAngle(MathHelper.Lerp(a.Tail, b.Tail, t)));
    }

    private static float Smooth(float start, float end, float value)
    {
        float t = MathHelper.Clamp((value - start) / (end - start), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static float TailAngle(float displacement) => MathHelper.ToDegrees(MathF.Atan2(-displacement, 10f));

    private static Vector2 RotateOffset(Vector2 point, Vector2 origin, float angle)
    {
        float radians = MathHelper.ToRadians(angle);
        float sine = MathF.Sin(radians), cosine = MathF.Cos(radians);
        Vector2 offset = point - origin;
        return new(cosine * offset.X + sine * offset.Y, -sine * offset.X + cosine * offset.Y);
    }

    private static NeckPose GetNeckPose(BipedPose pose)
    {
        Vector2 root = pose.Body + NeckRoot;
        Vector2 middle = root + RotateOffset(NeckJoint, NeckRoot, pose.ShoulderAngle);
        Vector2 head = middle + RotateOffset(HeadJoint, NeckJoint, pose.NeckAngle);
        return new(root, middle, head);
    }

    internal static Vector2 ToWorld(Vector2 anchor, int direction, Vector2 point)
    {
        Vector2 offset = point - GroundAnchor;
        if (direction == 1)
            offset.X = -offset.X;

        return anchor + offset * Scale;
    }

    internal static Vector2 HeadHitCenter(Vector2 anchor, int direction, BipedPose pose)
        => ToWorld(anchor, direction, GetNeckPose(pose).Head + RotateOffset(HeadCenter, HeadJoint, pose.HeadAngle));

    internal static Vector2 StompImpactPoint(Vector2 anchor, int direction, BipedPose pose)
        => ToWorld(anchor, direction, new Vector2(pose.NearAnkle.X - 3f, GroundY));

    internal static void Draw(SpriteBatch batch, Vector2 anchor, int direction, Color color, BipedPose pose)
    {
        Color farColor = new((byte)(color.R * 0.82f), (byte)(color.G * 0.82f), (byte)(color.B * 0.82f), color.A);
        DrawLeg(batch, anchor, direction, farColor, pose.Body + new Vector2(48f, 32f), pose.FarAnkle, pose.FarFootAngle);
        DrawLeg(batch, anchor, direction, color, pose.Body + new Vector2(41f, 33f), pose.NearAnkle, pose.NearFootAngle);
        NeckPose neck = GetNeckPose(pose);
        DrawPart(batch, neckRearTexture.Value, anchor, direction, color, NeckRoot, neck.Root, pose.ShoulderAngle);
        DrawPart(batch, neckFrontTexture.Value, anchor, direction, color, NeckJoint, neck.Middle, pose.NeckAngle);
        DrawPart(batch, bodyTexture.Value, anchor, direction, color, Vector2.Zero, pose.Body);
        DrawPart(batch, tailTexture.Value, anchor, direction, color, TailRoot, pose.Body + TailRoot, pose.TailAngle);
        DrawPart(batch, headTexture.Value, anchor, direction, color, HeadJoint, neck.Head, pose.HeadAngle);
    }

    private static void DrawLeg(SpriteBatch batch, Vector2 anchor, int direction, Color color,
        Vector2 hip, Vector2 ankle, float footAngle)
    {
        const float thigh = 22.3f, shin = 21.7f;
        Vector2 delta = ankle - hip;
        float actualDistance = delta.Length();
        float distance = Math.Min(actualDistance, thigh + shin);
        Vector2 axis = delta / actualDistance;
        float along = (distance * distance + thigh * thigh - shin * shin) / (2f * distance);
        float bend = MathF.Sqrt(Math.Max(0f, thigh * thigh - along * along));
        Vector2 knee = hip + axis * along + new Vector2(axis.Y, -axis.X) * bend;
        ankle = hip + axis * distance;
        DrawChain(batch, anchor, direction, color, hip, knee);
        DrawChain(batch, anchor, direction, color, knee, ankle);
        DrawPart(batch, kneeTexture.Value, anchor, direction, color, new(2.5f, 2f), knee);
        DrawPart(batch, footTexture.Value, anchor, direction, color, new(6.5f, 0.5f), ankle, footAngle);
    }

    private static void DrawChain(SpriteBatch batch, Vector2 anchor, int direction, Color color, Vector2 start, Vector2 end)
    {
        Vector2 step = (end - start) / 4f;
        float angle = MathHelper.ToDegrees(MathF.Atan2(step.X, step.Y));
        for (int i = 0; i < 4; i++)
            DrawPart(batch, legTexture.Value, anchor, direction, color, new(1.5f, 0.5f), start + step * i, angle);
    }

    private static void DrawPart(SpriteBatch batch, Texture2D texture, Vector2 anchor, int direction, Color color,
        Vector2 origin, Vector2 point, float angle = 0f)
    {
        bool flipped = direction == 1;
        if (flipped)
            origin.X = texture.Width - origin.X;

        batch.Draw(texture, ToWorld(anchor, direction, point), null, color, MathHelper.ToRadians(flipped ? angle : -angle),
            origin, Scale, flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }
}
