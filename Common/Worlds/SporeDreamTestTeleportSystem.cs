// 提供按鍵與背包按鈕測試傳送，伺服器檢查冷卻並尋找安全落點。
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Occident.Common.Networking;

namespace Occident.Common.Worlds;

// 明確觸發的測試傳送捷徑；傳送不會建立或修改地形。
public sealed class SporeDreamTestTeleportSystem : ModSystem
{
    private ModKeybind teleportKey;
    private readonly ulong[] nextTeleport=new ulong[255];
    private ulong nextRequest;
    private const string Prefix="Mods.OccidentMod.TestTeleport.";

    public override void Load()
    {
        if(!Main.dedServ) teleportKey=KeybindLoader.RegisterKeybind(Mod,"SporeDreamTeleport","F6");
    }
    public override void Unload() => teleportKey=null;
    public override void ClearWorld() { Array.Clear(nextTeleport);nextRequest=0; }

    public override void PostUpdateInput()
    {
        if(!Main.gameMenu && !Main.dedServ && teleportKey?.JustPressed==true
            && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest) Request();
    }

    private void Request()
    {
        if(Main.LocalPlayer.dead || !Main.LocalPlayer.active || Main.GameUpdateCount<nextRequest) return;
        nextRequest=Main.GameUpdateCount+60;
        if(!SporeDreamForestSystem.Instance.Exists) {
            Main.NewText(Language.GetTextValue(Prefix+"Missing"),230,190,150);return;
        }
        if(Main.netMode==NetmodeID.MultiplayerClient) {
            var packet=Mod.GetPacket();packet.Write(PacketRouter.SporeDreamTeleportRequest);packet.Send();
        } else TryTeleport(Main.LocalPlayer);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index=layers.FindIndex(layer=>layer.Name=="Vanilla: Mouse Text");
        if(index<0) return;
        layers.Insert(index,new LegacyGameInterfaceLayer("OccidentMod: Spore Dream Test Teleport",()=> {
            if(Main.gameMenu || !Main.playerInventory || Main.LocalPlayer.dead) return true;
            var rectangle=new Rectangle((int)(Main.screenWidth/Main.UIScale)-240,
                (int)(Main.screenHeight/Main.UIScale)-78,220,36);
            bool hover=rectangle.Contains((Main.MouseScreen/Main.UIScale).ToPoint());
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,rectangle,hover ? new Color(88,70,94) : new Color(40,35,48));
            Utils.DrawBorderString(Main.spriteBatch,Language.GetTextValue(Prefix+"Button"),
                new Vector2(rectangle.Center.X,rectangle.Center.Y-8),new Color(231,214,225),.8f,.5f);
            if(hover) {
                Main.LocalPlayer.mouseInterface=true;
                if(Main.mouseLeft && Main.mouseLeftRelease) Request();
            }
            return true;
        },InterfaceScaleType.UI));
    }

    // 從群系中央向外找有支撐、無實心重疊且無液體的落點。
    internal bool TryFindDestination(Player player,out Vector2 destination)
    {
        destination=default;
        var forest=SporeDreamForestSystem.Instance;
        if(!forest.Exists) return false;
        int center=forest.Surface.Length/2;
        for(int radius=0;radius<forest.Surface.Length;radius++) for(int side=0;side<(radius==0?1:2);side++) {
            int column=center+(side==0 ? radius : -radius);
            if(column<1 || column>=forest.Surface.Length-2) continue;
            int x=forest.Left+column,ground=forest.Surface[column];
            for(int y=Math.Max(3,ground-8);y<=Math.Min(Main.maxTilesY-3,ground+20);y++) {
                if(!WorldGen.SolidTile(x,y) || !WorldGen.SolidTile(x+1,y)) continue;
                Vector2 position=new(x*16+16-player.width*.5f,y*16-player.height);
                if(Collision.SolidCollision(position,player.width,player.height)) continue;
                bool liquid=false;
                for(int tx=(int)(position.X/16);tx<=(int)((position.X+player.width-1)/16);tx++)
                    for(int ty=(int)(position.Y/16);ty<y;ty++) if(Main.tile[tx,ty].LiquidAmount>0) liquid=true;
                if(liquid) continue;
                destination=position;return true;
            }
        }
        return false;
    }

    // 多人模式由伺服器執行；先送出目的區塊資料，再同步玩家傳送。
    internal void TryTeleport(Player player)
    {
        if(Main.netMode==NetmodeID.MultiplayerClient || !player.active || player.dead
            || player.whoAmI<0 || player.whoAmI>=Main.maxPlayers || Main.GameUpdateCount<nextTeleport[player.whoAmI]) return;
        nextTeleport[player.whoAmI]=Main.GameUpdateCount+60;
        if(!TryFindDestination(player,out var destination)) {
            if(Main.netMode==NetmodeID.SinglePlayer) Main.NewText(Language.GetTextValue(Prefix+"NoLanding"),230,190,150);
            else Terraria.Chat.ChatHelper.SendChatMessageToClient(NetworkText.FromKey(Prefix+"NoLanding"),new Color(230,190,150),player.whoAmI);
            return;
        }
        if(Main.netMode==NetmodeID.Server) RemoteClient.CheckSection(player.whoAmI,destination);
        player.Teleport(destination,TeleportationStyleID.RodOfDiscord);
        player.velocity=Vector2.Zero;player.fallStart=(int)(destination.Y/16f);
        if(Main.netMode==NetmodeID.Server)
            NetMessage.SendData(MessageID.TeleportEntity,number:0,number2:player.whoAmI,
                number3:destination.X,number4:destination.Y,number5:TeleportationStyleID.RodOfDiscord);
    }
}
