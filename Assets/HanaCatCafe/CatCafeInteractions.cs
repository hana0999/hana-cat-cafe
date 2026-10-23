using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        enum DragKind { None,Topping,Tray }
        DragKind dragKind;
        bool dragMoved,dragExisting;
        int draggedTopping,returnTopping=-1;
        Vector2 pointerGui,dragStart,returnStart,returnEnd,guiOffset;
        float guiScale,returnUntil,servedAt=-100;
        Vector3 toppingOffset,dragOffset,dragOrigin,lookTarget,trayOffset;
        Transform dragGhost,serviceTray;
        Texture2D targetIcon;
        bool CanTray=>!carryingTray && serviceTray!=null && RoomPointVisible(serviceTray.position) && !creativeMode && phase==Phase.Making && step==Step.Ready && drink.Ready;
        bool CanDecorate=>roomStation==2 && phase==Phase.Making && !drinkStation && (step==Step.Decoration || step==Step.Ready);

        void BuildServiceTray()
        {
            serviceTray=art.Group(world,"Serving tray",new Vector3(.575f,1.445f,-.6f));
            Color wood=CafeShapes.Hex("#d8c2a8"),trim=CafeShapes.Hex("#bda48b");
            art.Box(serviceTray,"Tray base",Vector3.zero,new Vector3(3.25f,.035f,1.70f),wood);
            for(int side=-1;side<=1;side+=2)
            {
                art.Box(serviceTray,"Tray rim",new Vector3(0,.04f,side*.82f),new Vector3(3.25f,.07f,.05f),trim);
                art.Box(serviceTray,"Side rim",new Vector3(side*1.60f,.04f,0),new Vector3(.05f,.07f,1.65f),trim);
            }
            art.Ball(serviceTray,"Front handle",new Vector3(0,.03f,-.90f),new Vector3(.72f,.09f,.17f),wood);
            serviceTray.gameObject.SetActive(false);
        }
        Vector2 ToGui(Vector3 point)
        {
            Vector3 p=view.WorldToScreenPoint(point);
            return (new Vector2(p.x,Screen.height-p.y)-guiOffset)/guiScale;
        }
        bool PointerOnPlane(float y,out Vector3 point)
        {
            Vector2 p=guiOffset+pointerGui*guiScale;
            Ray ray=view.ScreenPointToRay(new Vector3(p.x,Screen.height-p.y,0));
            var plane=new Plane(Vector3.up,new Vector3(0,y,0));
            if(plane.Raycast(ray,out float distance)){point=ray.GetPoint(distance);return true;}
            point=Vector3.zero;return false;
        }
        Vector2 GuestDropPoint()=>ToGui(cat.TransformPoint(new Vector3(0,1.85f,-.4f)));
        Rect FruitSlot(int i)=>new Rect(370+i*100,654,80,72);
        Rect TrayHandle()
        {
            Vector2 p=ToGui(serviceTray.position+new Vector3(0,.06f,-.90f));
            return new Rect(p.x-110,p.y-22,220,44);
        }
        void HandleInteractions()
        {
            int id=GUIUtility.GetControlID("CafeInteraction".GetHashCode(),FocusType.Passive);
            var e=Event.current;
            if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape && dragKind!=DragKind.None)
            {CancelInteraction();GUIUtility.hotControl=0;e.Use();return;}
            if(e.type==EventType.MouseDown && e.button==0 && GUIUtility.hotControl==0)
            {
                bool found=false;
                if(CanDecorate)
                {
                    for(int i=0;i<3;i++)if(FruitSlot(i).Contains(pointerGui))
                    {dragKind=DragKind.Topping;draggedTopping=i;dragExisting=false;found=true;break;}
                    if(!found && selectedTopping>=0 && !fruitRing)
                    {
                        Vector2 at=ToGui(decoration.TransformPoint(new Vector3(0,ToppingHeight,0)));
                        if((pointerGui-at).sqrMagnitude<34*34)
                        {dragKind=DragKind.Topping;draggedTopping=selectedTopping;dragExisting=true;found=true;}
                    }
                }
                if(!found && CanTray && TrayHandle().Contains(pointerGui)){LiftMeal();e.Use();return;}
                if(found)
                {
                    dragStart=pointerGui;dragMoved=false;GUIUtility.hotControl=id;dragOrigin=trayOffset;
                    if(dragKind==DragKind.Tray)PointerOnPlane(1.5f,out dragOffset);
                    e.Use();
                }
            }
            if(GUIUtility.hotControl!=id || dragKind==DragKind.None)return;
            if(e.type==EventType.MouseDrag)
            {
                if((pointerGui-dragStart).sqrMagnitude>64)dragMoved=true;
                if(dragMoved)MoveInteraction();e.Use();
            }
            if(e.type==EventType.MouseUp && e.button==0)
            {
                DragKind finished=dragKind;bool moved=dragMoved,existing=dragExisting;int topping=draggedTopping;
                bool near=CafeInteractionRules.NearGuest(pointerGui,GuestDropPoint());
                Vector3 placement=Vector3.zero;
                bool valid=PointerOnPlane(cake.position.y+ToppingHeight,out Vector3 point) && CafeRecipes.TryPlace(dessertKind==0 && creamAmount==0 ? 2 : dessertKind,new Vector2(point.x-cake.position.x,point.z-cake.position.z),out placement);
                Vector2 restore=existing ? ToGui(cake.TransformPoint(toppingOffset+new Vector3(0,ToppingHeight,0))) : FruitSlot(topping).center;
                dragKind=DragKind.None;GUIUtility.hotControl=0;DestroyGhost();decoration.gameObject.SetActive(true);
                if(finished==DragKind.Topping)
                {
                    if(!moved && !existing)PlaceTopping(topping,Vector3.zero);
                    else if(moved && valid)PlaceTopping(topping,placement);
                    else if(moved){returnTopping=topping;returnStart=pointerGui;returnEnd=restore;returnUntil=Time.time+.3f;}
                }
                else if(!moved || near)LiftMeal();
                e.Use();
            }
        }
        void MoveInteraction()
        {
            if(dragKind==DragKind.Topping && PointerOnPlane(cake.position.y+ToppingHeight,out Vector3 point))
            {
                if(dragGhost==null)
                {
                    dragGhost=art.Group(world,"Held topping",Vector3.zero);BuildTopping(dragGhost,draggedTopping);
                    if(dragExisting)decoration.gameObject.SetActive(false);
                }
                dragGhost.position=point-new Vector3(0,ToppingHeight,0);lookTarget=point;
            }
            else if(dragKind==DragKind.Tray && PointerOnPlane(1.5f,out Vector3 trayPoint))
            {
                Vector3 delta=dragOrigin+trayPoint-dragOffset;
                trayOffset=new Vector3(Mathf.Clamp(delta.x,-1.5f,.45f),0,Mathf.Clamp(delta.z,-.12f,.40f));
                lookTarget=serviceTray.position+Vector3.up*.4f;
            }
        }
        void PlaceTopping(int type,Vector3 offset)
        {
            fruitRing=false;selectedTopping=type;sounds?.Play(0);step=Step.Ready;toppingOffset=offset;BuildCake();
            decoration.localPosition=toppingOffset+(reduceMotion ? Vector3.zero : Vector3.up*.35f);
        }
        void DrawFruitSlot(int i)
        {
            Rect r=FruitSlot(i);Color old=GUI.backgroundColor;
            if(selectedTopping==i)GUI.backgroundColor=new Color(.80f,.63f,.66f);
            GUI.Box(r,toppingIcons[i],buttonStyle);GUI.backgroundColor=old;
            if(showLearningLabels)GUI.Label(new Rect(r.x,r.yMax,r.width,24),new[]{"櫻桃","藍莓","糖花"}[i],labelStyle);
        }
        void DrawInteractionHints()
        {
            if(CanTray && dragKind!=DragKind.Topping)
            {
                Rect r=TrayHandle();GUI.DrawTexture(new Rect(r.center.x-18,r.center.y-18,36,36),serveIcon);
                if(showLearningLabels)GUI.Label(new Rect(r.x,r.yMax,r.width,24),"拖向貓咪／點一下送餐",labelStyle);
            }
            if(dragMoved && dragKind==DragKind.Topping)
            {
                Vector2 center=ToGui(cake.position+new Vector3(0,ToppingHeight,0));
                Vector2 edge=ToGui(cake.position+new Vector3(dessertKind==1 ? .60f : .45f,ToppingHeight,0));
                float r=Mathf.Max(28,Mathf.Abs(edge.x-center.x));
                GUI.DrawTexture(new Rect(center.x-r,center.y-r*.55f,r*2,r*1.1f),targetIcon);
            }
            if(dragKind==DragKind.Tray)
            {
                Vector2 at=GuestDropPoint();Color old=GUI.color;
                GUI.color=CafeInteractionRules.NearGuest(pointerGui,at) ? Color.white : new Color(1,1,1,.4f);
                GUI.DrawTexture(new Rect(at.x-110,at.y-110,220,220),targetIcon);
                GUI.DrawTexture(new Rect(at.x-20,at.y-70,40,40),heartIcon);GUI.color=old;
            }
            if(returnTopping>=0 && Time.time<returnUntil)
            {
                float t=reduceMotion ? 1 : Mathf.Clamp01(1-(returnUntil-Time.time)/.3f);
                Vector2 at=Vector2.Lerp(returnStart,returnEnd,t*t*(3-2*t));
                GUI.DrawTexture(new Rect(at.x-25,at.y-25,50,50),toppingIcons[returnTopping]);
            }
        }
        void UpdateServiceTray()
        {
            bool ready=!creativeMode && step==Step.Ready && drink.Ready && phase!=Phase.Greeting;
            serviceTray.gameObject.SetActive(ready);
            if(ready&&UpdateHandMeal())return;
            if(!ready)
            {
                trayOffset=Vector3.zero;cake.position=new Vector3(1.4f,1.46f,-.55f);
                drinkView.Root.position=new Vector3(-.25f,1.46f,-.65f);return;
            }
            if(dragKind!=DragKind.Tray)trayOffset=Vector3.Lerp(trayOffset,phase==Phase.Served ? new Vector3(-1.25f,0,0) : Vector3.zero,reduceMotion ? 1 : 1-Mathf.Exp(-7*Time.deltaTime));
            serviceTray.position=new Vector3(.575f,1.445f,-.6f)+trayOffset;
            cake.position=new Vector3(1.4f,1.52f,-.55f)+trayOffset;
            drinkView.Root.position=new Vector3(-.25f,1.52f,-.65f)+trayOffset;
        }
        void UpdateGuestReaction()
        {
            if(reduceMotion)return;
            float elapsed=Time.time-servedAt;
            if(phase==Phase.Served && elapsed<5.5f)
            {
                float sniff=Mathf.Sin(Mathf.Clamp01(elapsed/1.15f)*Mathf.PI);
                float rub=elapsed>1.15f ? Mathf.Sin((elapsed-1.15f)*5)*Mathf.Sin(Mathf.Clamp01((elapsed-1.15f)/1.85f)*Mathf.PI) : 0;
                head.localPosition+=new Vector3(rub*.10f,-sniff*.09f,-sniff*.20f);
                head.localRotation*=Quaternion.Euler(sniff*10,0,rub*13);
                if(elapsed>1.3f&&elapsed<3.8f){float nibble=(Mathf.Sin(elapsed*10)+1)*.5f;head.localPosition+=new Vector3(0,-.10f-nibble*.025f,-.14f);head.localRotation*=Quaternion.Euler(12+nibble*3,0,0);}
                if(elapsed>=3.8f){head.localRotation*=Quaternion.Euler(-4,0,Mathf.Sin(elapsed*3)*5);leftEye.localScale=rightEye.localScale=new Vector3(.12f,.025f,.045f);}
            }
            else if(dragMoved && dragKind!=DragKind.None)
            {
                Vector3 direction=lookTarget-head.position;
                float yaw=Mathf.Clamp(Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg,-24,24);
                head.localRotation*=Quaternion.Euler(0,yaw,0);
            }
        }
        void DestroyGhost(){if(dragGhost!=null){Destroy(dragGhost.gameObject);dragGhost=null;}}
        void CancelInteraction()
        {
            dragKind=DragKind.None;dragMoved=false;DestroyGhost();
            if(decoration!=null)decoration.gameObject.SetActive(true);
        }
    }
}
