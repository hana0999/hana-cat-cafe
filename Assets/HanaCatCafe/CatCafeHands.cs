using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        bool directHeld,carryingTray,mealPlaced;
        Vector3 directPoint;
        float stirTravel;
        Vector2 lastStir;
        Vector3[] icingPositions=new Vector3[80];
        bool[] icingEdited=new bool[80];
        Vector3 placedTrayStart,placedCakeStart,placedCupStart;
        Quaternion placedRotation;
        float placedAt;
        bool DirectAvailable=>roomStation==2 && phase==Phase.Making && step==Step.Cream && !RoomInputBlocked && !tablewareOpen && !utilityOpen && !carryingTray;
        bool DirectMix=>PreparationActive && preparation.stage==CafePreparation.Stage.Mix;
        bool DirectWorking=>directHeld && (!DirectMix || Time.time<stirTravel);
        bool CanPutMeal=>carryingTray && CafeHandsRules.NearTable(view.transform.position,cat.position) && RoomPointVisible(cat.position+Vector3.up*1.7f);
        void ClearIcing(){icingPositions=new Vector3[80];icingEdited=new bool[80];}
        bool FindDirectPoint(out Vector3 point)
        {
            point=Vector3.zero;
            if(!DirectAvailable || pointerGui.y<160 || pointerGui.y>535 || pointerGui.x<170 || pointerGui.x>900)return false;
            if(PreparationActive && (preparation.stage==CafePreparation.Stage.Set||preparation.stage==CafePreparation.Stage.Unmold))return false;
            if(BakingActive && baking.stage!=CafeBaking.Stage.Pour)return false;
            float height=PreparationActive?.28f:BakingActive?.23f:dessertKind==0?.60f+creamAmount*.48f:CafeRecipes.Surface(dessertKind,false);
            if(!PointerOnPlane(cake.position.y+height,out Vector3 hit))return false;
            Vector3 local=hit-cake.position;
            Vector2 flat=new Vector2(local.x,local.z);
            float radius=PreparationActive?.28f:BakingActive?.35f:dessertKind==CafeRecipes.Pudding?.20f:dessertKind==0?.28f:.36f;
            if(!CafeHandsRules.TrySurface(flat,radius,dessertKind==CafeRecipes.Donut&&!PreparationActive,out flat))return false;
            point=new Vector3(flat.x,height,flat.y);return true;
        }
        void HandleDirectTools()
        {
            int id=GUIUtility.GetControlID("Cafe hands".GetHashCode(),FocusType.Passive);
            var e=Event.current;
            bool hit=FindDirectPoint(out Vector3 point);
            if(e.type==EventType.MouseDown&&e.button==0&&GUIUtility.hotControl==0&&hit)
            {GUIUtility.hotControl=id;directHeld=true;directPoint=point;lastStir=new Vector2(point.x,point.z);stirTravel=0;e.Use();}
            if(GUIUtility.hotControl!=id)return;
            if(e.type==EventType.MouseDrag)
            {
                directHeld=hit;
                if(hit)
                {
                    Vector2 next=new Vector2(point.x,point.z);
                    if((next-lastStir).sqrMagnitude>.0001f)stirTravel=Time.time+.16f;
                    lastStir=next;directPoint=point;
                }
                e.Use();
            }
            if(e.type==EventType.MouseUp&&e.button==0){directHeld=false;GUIUtility.hotControl=0;e.Use();}
        }
        void RecordDirectIcing(int before,int after)
        {
            if(!directHeld || PreparationActive || BakingActive)return;
            if(dessertKind!=CafeRecipes.Cupcake&&dessertKind!=CafeRecipes.Donut&&dessertKind!=CafeRecipes.LayerCake)return;
            for(int i=before;i<after && i<creamPieces.Count && i<icingEdited.Length;i++)
            {
                Vector3 point=directPoint;
                // Cupcake keeps its rising layers, while the hand controls horizontal placement.
                point.y=creamPieces[i].localPosition.y;
                icingPositions[i]=point;icingEdited[i]=true;
            }
        }
        void ApplyIcingPositions()
        {
            for(int i=0;i<creamPieces.Count&&i<icingEdited.Length;i++)
                if(icingEdited[i])creamPieces[i].localPosition=icingPositions[i];
        }
        void LiftMeal()
        {
            if(creativeMode||phase!=Phase.Making||step!=Step.Ready||!drink.Ready||carryingTray)return;
            ResetHold();GoStation(0);carryingTray=true;mealPlaced=false;utilityOpen=false;tablewareOpen=false;
            walkPosition=new Vector3(.575f,2.45f,-3.3f);roomLook=new Vector2(0,10);UpdateRoomCamera(true);sounds?.Play(0);
        }
        void ReturnMeal()
        {
            if(!carryingTray)return;
            carryingTray=false;ResetHold();RestoreMealTransforms();UpdateServiceTray();
        }
        void RestoreMealTransforms()
        {
            serviceTray.localScale=cake.localScale=drinkView.Root.localScale=Vector3.one;
            serviceTray.rotation=cake.rotation=drinkView.Root.rotation=Quaternion.identity;
        }
        void PutMeal()
        {
            if(!CanPutMeal)return;
            if(!CafeProgress.Matches(selectedFlavor,selectedTopping,wantedFlavor,wantedTopping))
            {ReturnMeal();GoStation(2);puzzledUntil=Time.time+2;return;}
            placedTrayStart=serviceTray.position;placedCakeStart=cake.position;placedCupStart=drinkView.Root.position;
            placedRotation=serviceTray.rotation;placedAt=Time.time;mealPlaced=true;carryingTray=false;Serve();
        }
        bool UpdateHandMeal()
        {
            if(!carryingTray&&!mealPlaced)return false;
            if(carryingTray)
            {
                Quaternion yaw=Quaternion.Euler(0,view.transform.eulerAngles.y,0);
                Vector3 center=view.transform.position+yaw*new Vector3(0,-.92f,1.65f);
                serviceTray.position=center;cake.position=center+yaw*new Vector3(.40f,.04f,.025f);
                drinkView.Root.position=center+yaw*new Vector3(-.40f,.04f,-.025f);
                serviceTray.rotation=cake.rotation=drinkView.Root.rotation=yaw;
                serviceTray.localScale=cake.localScale=drinkView.Root.localScale=Vector3.one*.48f;
            }
            else
            {
                float t=reduceMotion?1:Mathf.SmoothStep(0,1,(Time.time-placedAt)/.65f);
                serviceTray.position=Vector3.Lerp(placedTrayStart,new Vector3(-1.65f,1.445f,-.6f),t);
                cake.position=Vector3.Lerp(placedCakeStart,new Vector3(-.825f,1.52f,-.55f),t);
                drinkView.Root.position=Vector3.Lerp(placedCupStart,new Vector3(-2.475f,1.52f,-.65f),t);
                serviceTray.rotation=cake.rotation=drinkView.Root.rotation=Quaternion.Slerp(placedRotation,Quaternion.identity,t);
                serviceTray.localScale=cake.localScale=drinkView.Root.localScale=Vector3.one*Mathf.Lerp(.48f,1,t);
            }
            return true;
        }
        void DrawHandMeal()
        {
            if(!carryingTray)return;
            if(IconButton(new Rect(1090,675,70,65),undoIcon,"放回工作檯")){ReturnMeal();GoStation(2);return;}
            if(CanPutMeal && IconButton(new Rect(540,640,120,85),serveIcon,"放下餐點"))PutMeal();
            if(showLearningLabels)GUI.Label(new Rect(300,740,600,30),"WASD 端盤走動・右鍵環視・靠近貓咪放下・Esc 放回工作檯",labelStyle);
        }
    }
}
