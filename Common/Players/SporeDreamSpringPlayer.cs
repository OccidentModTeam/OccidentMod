// 處理玩家踩上矮菇、壓縮與彈起的移動狀態。
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Occident.Common.Worlds;

namespace Occident.Common.Players;

// 角色移動由所屬客戶端處理；其他玩家沿用原版移動封包，菌蓋動畫另經伺服器驗證同步。
public sealed class SporeDreamSpringPlayer : ModPlayer
{
    private bool holding,landing;
    private Point heldRoot,landingRoot;
    private int heldTicks,cooldown;
    private bool Owner=>Main.netMode!=NetmodeID.Server && Player.whoAmI==Main.myPlayer;
    private bool Eligible=>Player.active && !Player.dead && !Player.ghost && Player.gravDir==1
        && !Player.mount.Active && Player.grapCount==0 && !Player.pulley;
    public override void Initialize()=>Clear();
    public override void OnEnterWorld()=>Clear();
    public override void UpdateDead()=>Clear();
    private void Clear(){holding=landing=false;heldRoot=landingRoot=default;heldTicks=cooldown=0;}
    private void ResetFall(){Player.fallStart=Player.fallStart2=(int)(Player.position.Y/16f);Player.jump=0;}
    private void SendMovement()
    {
        if(Main.netMode==NetmodeID.MultiplayerClient)NetMessage.SendData(MessageID.PlayerControls,-1,-1,null,Player.whoAmI);
    }
    // 位移前先攔截落到菌蓋的交叉路徑，壓縮完成後才給向上的初速。
    public override void PreUpdateMovement()
    {
        if(!Owner)return;
        landing=false;
        if(cooldown>0)cooldown--;
        if(!Eligible || Player.controlDown){holding=false;return;}
        var forest=SporeDreamCurvedSystem.Instance;
        if(holding) {
            if(!forest.TrySpringSurface(heldRoot,Player.position.X,Player.width,out _,out _)) {holding=false;return;}
            heldTicks++;
            if(heldTicks>=SporeDreamSpringMath.PressTicks) {
                holding=false;cooldown=SporeDreamSpringMath.CooldownTicks;
                Player.velocity.Y=-SporeDreamSpringMath.LaunchSpeed;ResetFall();SendMovement();return;
            }
            Player.velocity.Y=0;ResetFall();return;
        }
        if(cooldown>0 || Player.velocity.Y<=0)return;
        float before=Player.Bottom.Y;
        if(forest.TrySpringLanding(Player.position.X+Player.velocity.X,Player.width,before,before+Player.velocity.Y,
            Player.velocity.Y,out landingRoot,out float top)) {
            // 在原版位移前先計算是否穿越菌蓋表面，避免高速下落時直接穿過。
            Player.velocity.Y=Math.Max(0,top-before);landing=true;ResetFall();
        }
    }
    // 位移後將腳底貼到實際壓縮表面；有實心碰撞或離開菌蓋時取消狀態。
    public override void PostUpdate()
    {
        if(!Owner || !Eligible)return;
        var forest=SporeDreamCurvedSystem.Instance;
        Point root=holding?heldRoot:landingRoot;
        if(!holding && !landing)return;
        if(!forest.TrySpringSurface(root,Player.position.X,Player.width,out float top,out float depression)) {holding=false;landing=false;return;}
        float target=top+depression;
        if(Math.Abs(Player.Bottom.Y-target)>(holding?48:5)
            || Collision.SolidCollision(new Vector2(Player.position.X,target-Player.height),Player.width,Player.height)) {
            holding=landing=false;return;
        }
        if(!holding) {
            if(!forest.StartSpring(root)){landing=false;return;}
            heldRoot=root;holding=true;heldTicks=forest.SpringAge(root);
            forest.TrySpringSurface(root,Player.position.X,Player.width,out top,out depression);target=top+depression;
            SendMovement();
        }
        Player.position.Y=target-Player.height;Player.velocity.Y=0;ResetFall();landing=false;
    }
}
