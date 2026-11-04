// 登錄標題選單主題，交由共用繪製器處理標誌與背景。
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace Occident.Common.UI.MenuBackground;

// 登錄到標題畫面底部既有的 tModLoader 選單主題切換器。
[Autoload(Side = ModSide.Client)]
public sealed class OccidentMenu : ModMenu
{
    public override string DisplayName => "OccidentMod";

    private readonly OccidentMenuBackground background = new();

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoCenter,
        ref float logoRotation, ref float logoScale, ref Color drawColor)
    {
        // 保留模組標誌；若有提供背景圖，則覆蓋原版選單景色。
        background.Draw(spriteBatch);
        // 自訂繪製器會在上方中央畫出透明標誌，因此略過預設 tModLoader 標誌。
        return false;
    }

    public override void Unload() => background.Unload();
}
