using System.Collections.Generic;
using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        // 0: walk, 1: guest, 2: pastry, 3: drink. Work remains intact when leaving a station.
        int roomStation,guestPage,recipePage;
        Vector3 walkPosition=new Vector3(0,2.45f,-4.4f);
        Vector2 roomLook=new Vector2(0,10),lastLookPointer;
        readonly HashSet<KeyCode> walkKeys=new HashSet<KeyCode>();
        bool roomLooking;
        Texture2D roomIcon,leftLookIcon,butterIcon;
        bool photoWasOrthographic;
        float photoFieldBefore;
        bool RoomInputBlocked=>compositionOpen||photoOpen||capturingPhoto||collectionOpen||decorOpen||recipeOpen||giftOpen||toyMode||displayOpen||careOpen||weatherOpen;

        void InitRoom()
        {
            roomIcon=MakeIcon(31,0);leftLookIcon=MakeIcon(34,0);butterIcon=MakeIcon(35,0);view.orthographic=false;view.fieldOfView=55;view.nearClipPlane=.08f;
            GoStation(0);UpdateRoomCamera(true);
            Color trim=CafeShapes.Hex("#c6ad96"),cream=CafeShapes.Hex("#efe2cd");
            art.Box(world,"Back wall",new Vector3(0,2.6f,-10.4f),new Vector3(16,5.4f,.15f),wallColor);
            for(int side=-1;side<=1;side+=2)
            {
                art.Box(world,"Side wall",new Vector3(side*7.9f,2.6f,-3.9f),new Vector3(.15f,5.4f,13),wallColor);
                art.Box(world,"Side panel",new Vector3(side*7.78f,.65f,-3.9f),new Vector3(.08f,1.3f,13),CafeShapes.Hex("#c5cdb7"));
                var nook=art.Group(world,"Window seating",new Vector3(side*6.65f,0,-5.2f));
                art.Disc(nook,"Round cafe table",new Vector3(0,1.1f,0),.68f,.12f,cream);
                art.Disc(nook,"Table leg",new Vector3(0,.55f,0),.09f,1.1f,trim);
                art.Disc(nook,"Table foot",new Vector3(0,.05f,0),.38f,.08f,trim);
                art.Ball(nook,"Cushion",new Vector3(0,.55f,1.1f),new Vector3(.75f,.18f,.65f),CafeShapes.Hex(side<0?"#d6b1bd":"#b9c7b0"));
                for(int j=-1;j<=1;j+=2)art.Box(nook,"Chair leg",new Vector3(j*.25f,.25f,1.1f),new Vector3(.06f,.5f,.06f),trim);
                art.Box(nook,"Chair back",new Vector3(0,1.0f,1.4f),new Vector3(.72f,.7f,.08f),trim);
                art.Ball(nook,"Tiny table vase",new Vector3(0,1.3f,0),new Vector3(.16f,.26f,.16f),CafeShapes.Hex("#d3bdcb"));
            }
            art.Box(world,"Entry door",new Vector3(0,1.5f,-10.25f),new Vector3(1.6f,3,.09f),trim);
            art.Box(world,"Door glass",new Vector3(0,2,-10.17f),new Vector3(1.25f,1.5f,.04f),CafeShapes.Hex("#cddcda"));
            art.Ball(world,"Door knob",new Vector3(.54f,1.15f,-10.08f),Vector3.one*.10f,CafeShapes.Hex("#b89e75"));
            art.Box(world,"Welcome rug",new Vector3(0,.005f,-9.6f),new Vector3(2.2f,.025f,1),CafeShapes.Hex("#cdb5ba"));
        }
        void GoStation(int station)
        {
            if(carryingTray && station!=0)ReturnMeal();ResetHold();StopRoomInput();utilityOpen=false;tablewareOpen=false;roomStation=station;
            if(station==3 && creativeMode)roomStation=2;
            if(roomStation==2||roomStation==3)
            {
                if(phase==Phase.Greeting)AcceptOrder();
                drinkStation=roomStation==3;
            }
            roomLook=new Vector2(0,roomStation==0?10:roomStation==1?8:22);
        }
        void StopRoomInput(){walkKeys.Clear();roomLooking=false;}
        void UpdateRoomCamera(bool instant=false)
        {
            if(compositionOpen||photoOpen||capturingPhoto)return;
            if(roomStation==0&&!RoomInputBlocked&&Application.isFocused)
            {
                Vector3 move=new Vector3((walkKeys.Contains(KeyCode.D)?1:0)-(walkKeys.Contains(KeyCode.A)?1:0),0,(walkKeys.Contains(KeyCode.W)?1:0)-(walkKeys.Contains(KeyCode.S)?1:0));
                walkPosition=CafeRoomRules.ClampWalk(walkPosition+Quaternion.Euler(0,roomLook.x,0)*Vector3.ClampMagnitude(move,1)*Time.deltaTime*1.6f);
            }
            Vector3 at=roomStation==0?walkPosition:roomStation==1?new Vector3(-1.45f,2.45f,-2.65f):roomStation==2?new Vector3(1.4f,2.85f,-2.8f):new Vector3(-.25f,2.85f,-2.8f);
            Quaternion rotation=Quaternion.Euler(roomLook.y,roomLook.x,0);
            if(roomStation==4){at=displayFocus+new Vector3(0,.75f,-2.1f);rotation=Quaternion.LookRotation(displayFocus+Vector3.up*.12f-at);}
            if(roomStation==5){at=careFocus+new Vector3(0,2.45f,-2.7f);rotation=Quaternion.LookRotation(careFocus+Vector3.up*.60f-at);}
            float ease=instant||reduceMotion?1:1-Mathf.Exp(-7*Time.deltaTime);
            view.transform.position=Vector3.Lerp(view.transform.position,at,ease);
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,rotation,ease);
        }
        void HandleRoomInput()
        {
            if(RoomInputBlocked){StopRoomInput();return;}
            var e=Event.current;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape){if(carryingTray)ReturnMeal();GoStation(0);e.Use();return;}
            if(e.keyCode==KeyCode.W||e.keyCode==KeyCode.A||e.keyCode==KeyCode.S||e.keyCode==KeyCode.D)
            {
                if(e.type==EventType.KeyDown){if(roomStation!=0)GoStation(0);walkKeys.Add(e.keyCode);e.Use();}
                else if(e.type==EventType.KeyUp){walkKeys.Remove(e.keyCode);e.Use();}
            }
            int id=GUIUtility.GetControlID("Cafe first person look".GetHashCode(),FocusType.Passive);
            if(e.type==EventType.MouseDown&&e.button==1&&GUIUtility.hotControl==0)
            {holdActive=false;pipingHeld=false;drinkHeld=false;releaseHoldRequested=false;GUIUtility.hotControl=id;roomLooking=true;lastLookPointer=pointerGui;e.Use();}
            if(e.type==EventType.MouseDrag&&roomLooking&&GUIUtility.hotControl==id)
            {roomLook=CafeRoomRules.Look(roomLook,pointerGui-lastLookPointer);lastLookPointer=pointerGui;e.Use();}
            if(e.type==EventType.MouseUp&&e.button==1&&GUIUtility.hotControl==id)
            {roomLooking=false;GUIUtility.hotControl=0;e.Use();}
        }
        bool RoomPointVisible(Vector3 position)
        {
            Vector3 p=view.WorldToViewportPoint(position);
            return p.z>.1f&&p.x>0&&p.x<1&&p.y>0&&p.y<1;
        }
        bool WorldButton(Vector3 position,Texture icon,string label)
        {
            if((view.transform.position-position).sqrMagnitude>16f)return false;
            Vector3 screen=view.WorldToScreenPoint(position);
            if(screen.z<.1f)return false;
            Vector2 at=ToGui(position);
            if(at.x<160||at.x>1040||at.y<160||at.y>520)return false;
            return IconButton(new Rect(at.x-26,at.y-26,52,52),icon,label);
        }
        void DrawRoomNavigation()
        {
            if(roomStation==0)
            {
                if(WorldButton(cat.position+new Vector3(0,2.7f,0),pawIcon,"靠近貓咪"))GoStation(1);
                if(WorldButton(cake.position+Vector3.up*.8f,dessertIcons[dessertKind,selectedFlavor],"甜點檯"))GoStation(2);
                if(!creativeMode&&WorldButton(drinkView.Root.position+Vector3.up*.65f,cupIcon,"飲品檯"))GoStation(3);
            }
            if(roomStation!=0 && IconButton(new Rect(555,550,85,56),roomIcon,"回到走道"))GoStation(0);
            if(showLearningLabels&&roomStation==0)GUI.Label(new Rect(230,620,740,32),"右鍵拖曳環視・WASD 移動・點圖示靠近工作檯",labelStyle);
        }
    }
}
