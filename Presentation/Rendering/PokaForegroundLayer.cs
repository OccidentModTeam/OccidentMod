using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VesperMod.Content.Mounts;
using VesperMod.Integration.TModLoader.Players;
using VesperMod.Presentation.UI;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace VesperMod.Presentation.Rendering;

[Autoload(Side = ModSide.Client)]
public sealed class PokaForegroundLayer : PlayerDrawLayer
{
    private Asset<Texture2D> idle;
    private Asset<Texture2D> walk;
    private Asset<Texture2D> crouch;
    private Asset<Texture2D> chargeWalk;
    private Asset<Texture2D> chargeBarFrame;
    private Asset<Texture2D> chargeBarFill;
    private Asset<Texture2D> chargeBarGlow;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        idle = ModContent.Request<Texture2D>(PokaMount.SpritePath + "Idle");
        walk = ModContent.Request<Texture2D>(PokaMount.SpritePath + "Walk");
        crouch = ModContent.Request<Texture2D>(PokaMount.SpritePath + "Crouch");
        chargeWalk = ModContent.Request<Texture2D>(PokaMount.SpritePath + "ChargeWalk");
        const string barPath = "VesperMod/Assets/UI/EnergyBar/ChargeBar_";
        chargeBarFrame = ModContent.Request<Texture2D>(barPath + "Frame");
        chargeBarFill = ModContent.Request<Texture2D>(barPath + "Fill");
        chargeBarGlow = ModContent.Request<Texture2D>(barPath + "FullGlow");
    }

    public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
        !drawInfo.headOnlyRender && drawInfo.shadow == 0f && PokaMount.IsMounted(drawInfo.drawPlayer);

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player rider = drawInfo.drawPlayer;
        var mount = rider.GetModPlayer<PokaRider>();
        var (animation, frame) = mount.GetPose();
        Texture2D texture = animation switch
        {
            "Crouch" => crouch.Value,
            "Walk" => walk.Value,
            "ChargeWalk" => chargeWalk.Value,
            _ => idle.Value,
        };
        var source = new Rectangle(0, frame * PokaMount.FrameHeight, PokaMount.FrameWidth, PokaMount.FrameHeight);
        Vector2 riderOffset = new(-15f * rider.direction, -28f * rider.gravDir);
        Vector2 pivot = drawInfo.Position - Main.screenPosition + new Vector2(rider.width * 0.5f, 25f);
        float lean = 0.22f * rider.direction * rider.gravDir;

        // Tilt the rider around the seat, then add Poka in front of the rider.
        for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
        {
            DrawData part = drawInfo.DrawDataCache[i];
            part.position = pivot + (part.position - pivot).RotatedBy(lean) + riderOffset;
            part.rotation += lean;
            drawInfo.DrawDataCache[i] = part;
        }

        Vector2 ground = drawInfo.Position - Main.screenPosition +
            new Vector2(rider.width * 0.5f, rider.gravDir > 0f ? rider.height : 0f);
        ground.Y += drawInfo.mountOffSet;
        ground = new Vector2((int)ground.X, (int)ground.Y);
        SpriteEffects effects = rider.direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (rider.gravDir < 0f)
            effects |= SpriteEffects.FlipVertically;

        var draw = new DrawData(texture, ground, source, drawInfo.colorMount, 0f,
            new Vector2(PokaMount.FrameWidth * 0.5f, rider.gravDir > 0f ? PokaMount.FrameHeight : 0f),
            PokaMount.SpriteScale, effects);
        draw.shader = rider.cMount;
        drawInfo.DrawDataCache.Add(draw);

        if (rider.whoAmI != Main.myPlayer || mount.ChargeProgress <= 0f)
            return;

        const float barScale = 2f;
        const int glowFrameTicks = 4;
        Texture2D frameTexture = chargeBarFrame.Value;
        Texture2D fillTexture = chargeBarFill.Value;
        // The frame is already stored at 2x, while the fill and glow use native pixels.
        Vector2 bar = ground + new Vector2(-frameTexture.Width / 2f,
            rider.gravDir > 0f ? 6f : -frameTexture.Height - 6f);
        drawInfo.DrawDataCache.Add(new DrawData(frameTexture, bar, null,
            Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None));
        Vector2 fillPosition = bar + new Vector2(6, 4) * barScale;

        Rectangle fillRect = BarTextureRegions.HorizontalFill(fillTexture, mount.ChargeProgress, 1);
        if (fillRect.Width > 0)
        {
            drawInfo.DrawDataCache.Add(new DrawData(fillTexture, fillPosition, fillRect,
                Color.White, 0f, Vector2.Zero, barScale, SpriteEffects.None));
        }

        if (fillRect.Width == fillTexture.Width)
        {
            Texture2D glow = chargeBarGlow.Value;
            int frameCount = glow.Height / fillTexture.Height;
            if (frameCount == 0)
                return;

            int glowFrame = System.Math.Max(0, mount.FullChargeElapsed - 1) / glowFrameTicks % frameCount;
            if (!BarTextureRegions.TryVerticalFrame(glow, fillTexture.Height, glowFrame, out Rectangle glowRect))
                return;
            drawInfo.DrawDataCache.Add(new DrawData(glow, fillPosition, glowRect,
                Color.White, 0f, Vector2.Zero, barScale, SpriteEffects.None));
        }

    }
}
