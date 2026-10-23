using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        int dessertKind;
        bool recipeOpen,creativeMode,fruitRing,puddingCream;
        Texture2D recipeIcon;
        Texture2D[,] dessertIcons;
        Transform jelly,mold;
        float unmoldUntil,creativeStarted;
        WorkSnapshot orderWork;
        readonly WorkSnapshot[] drafts=new WorkSnapshot[CafeRecipes.Count];
        float ToppingHeight=>creamAmount==0 && dessertKind<2 ? (dessertKind==0 ? .59f : .24f) : CafeRecipes.Surface(dessertKind,puddingCream);

        sealed class WorkSnapshot
        {
            public Vector3[] icing;public bool[] edited;
            public Phase phase;public Step step;public int kind,flavor,topping;
            public bool placed;public float placementTime;
            public float fill,served,happy,puzzled,next,arrive,welcome,unmold;
            public Vector3 position,tray,mealTray,mealCake,mealCup;public Quaternion mealRotation;
            public bool ring,cream,station,petted;
            public CafeDrink drink;public CafeBaking baking;public CafePreparation preparation;
        }
        WorkSnapshot RememberWork()=>new WorkSnapshot {
            mealTray=placedTrayStart,mealCake=placedCakeStart,mealCup=placedCupStart,mealRotation=placedRotation,placed=mealPlaced,placementTime=placedAt,icing=(Vector3[])icingPositions.Clone(),edited=(bool[])icingEdited.Clone(),phase=phase,step=step,kind=dessertKind,flavor=selectedFlavor,topping=selectedTopping,fill=creamAmount,
            position=toppingOffset,tray=trayOffset,ring=fruitRing,cream=puddingCream,station=drinkStation,petted=petted,
            preparation=preparation,baking=baking,drink=drink,served=servedAt,happy=happyUntil,puzzled=puzzledUntil,next=nextAllowed,arrive=arriveAt,welcome=welcomeUntil,unmold=unmoldUntil
        };
        void RestoreWork(WorkSnapshot data,float paused)
        {
            mealPlaced=data.placed;placedAt=data.placementTime+paused;placedTrayStart=data.mealTray;placedCakeStart=data.mealCake;placedCupStart=data.mealCup;placedRotation=data.mealRotation;
            icingPositions=data.icing!=null?(Vector3[])data.icing.Clone():new Vector3[80];icingEdited=data.edited!=null?(bool[])data.edited.Clone():new bool[80];
            preparation=data.preparation;baking=data.baking;phase=data.phase;step=data.step;dessertKind=data.kind;selectedFlavor=data.flavor;selectedTopping=data.topping;
            creamAmount=data.fill;toppingOffset=data.position;trayOffset=data.tray;fruitRing=data.ring;puddingCream=data.cream;
            drinkStation=data.station;petted=data.petted;drink=data.drink??new CafeDrink();
            servedAt=data.served+paused;happyUntil=data.happy+paused;puzzledUntil=data.puzzled+paused;
            nextAllowed=data.next+paused;arriveAt=data.arrive+paused;welcomeUntil=data.welcome+paused;unmoldUntil=data.unmold+paused;
            BuildCake();drinkView.Build(world,drink);ApplySaucer();drinkView.Root.gameObject.SetActive(!creativeMode && phase!=Phase.Greeting);
            UpdateServiceTray();
        }
        void ChooseCreative(int kind)
        {
            ResetHold();
            if(!creativeMode){orderWork=RememberWork();creativeStarted=Time.time;creativeMode=true;}
            else drafts[dessertKind]=RememberWork();
            var data=drafts[kind]??new WorkSnapshot {phase=Phase.Making,step=Step.Sponge,kind=kind,topping=-1,drink=new CafeDrink(),served=-100,unmold=-100};
            RestoreWork(data,0);drinkStation=false;recipeOpen=false;GoStation(2);
        }
        void ResumeOrders()
        {
            if(!creativeMode){recipeOpen=false;return;}
            ResetHold();drafts[dessertKind]=RememberWork();creativeMode=false;
            RestoreWork(orderWork,Time.time-creativeStarted);orderWork=null;recipeOpen=false;GoStation(phase==Phase.Making?(drinkStation?3:2):1);
        }
        void ChooseBase(int flavor)
        {
            ResetHold();ClearIcing();sounds?.Play(0);selectedFlavor=flavor;step=Step.Cream;baking=(dessertKind==CafeRecipes.Pancakes||dessertKind==CafeRecipes.Waffle)?new CafeBaking(dessertKind==CafeRecipes.Waffle):null;
            preparation=dessertKind==CafeRecipes.Tart||dessertKind==CafeRecipes.Pudding?new CafePreparation(dessertKind==CafeRecipes.Pudding):null;unmoldUntil=-100;
            BuildCake();
        }
        void DrawRecipes()
        {
            Color old=GUI.color;GUI.color=new Color(.25f,.20f,.23f,.68f);
            GUI.DrawTexture(new Rect(0,0,1200,800),Texture2D.whiteTexture);GUI.color=old;
            GUI.Box(new Rect(140,145,920,490),GUIContent.none,panelStyle);
            if(IconButton(new Rect(965,165,65,48),closeIcon,"關閉"))recipeOpen=false;
            if(IconButton(new Rect(400,165,110,65),serveIcon,"返回營業",!creativeMode))ResumeOrders();
            if(IconButton(new Rect(555,165,110,65),heartIcon,"自由創作",creativeMode))
            {ChooseCreative(dessertKind);recipeOpen=true;}
            if(IconButton(new Rect(870,175,65,48),nextIcon,"下一頁食譜"))recipePage=(recipePage+1)%((CafeRecipes.Count+2)/3);
            for(int slot=0;slot<3;slot++)
            {
                int kind=recipePage*3+slot;if(kind>=CafeRecipes.Count)break;
                float x=190+slot*285;
                GUI.Box(new Rect(x,270,250,315),GUIContent.none,panelStyle);
                if(IconButton(new Rect(x+60,285,130,120),dessertIcons[kind,0],CafeRecipes.Names[kind],dessertKind==kind))
                {
                    if(creativeMode)ChooseCreative(kind);
                    else if(kind==dessertKind){recipeOpen=false;if(phase==Phase.Greeting)AcceptOrder();GoStation(2);}
                }
                GUI.DrawTexture(new Rect(x+20,450,52,52),dessertIcons[kind,0]);
                GUI.DrawTexture(new Rect(x+75,467,20,20),nextIcon);
                GUI.DrawTexture(new Rect(x+100,450,52,52),kind>=CafeRecipes.Pudding ? pourIcon : pipingIcon);
                GUI.DrawTexture(new Rect(x+155,467,20,20),nextIcon);
                GUI.DrawTexture(new Rect(x+180,450,52,52),toppingIcons[kind%3]);
                if(showLearningLabels)GUI.Label(new Rect(x+5,530,240,40),creativeMode ? "點選開始／恢復作品" : "依訂單製作；愛心進入自由模式",labelStyle);
            }
        }
        void BuildOtherDessert()
        {
            creamPieces.Clear();visibleCream=0;jelly=null;mold=null;
            Color filling=Color.Lerp(flavors[selectedFlavor],CafeShapes.Hex("#fff0d2"),.25f);
            if(dessertKind>=CafeRecipes.Donut)BuildNewPastry(filling);
            else if(dessertKind>=CafeRecipes.Pancakes)BuildBreakfast(filling);
            else if(dessertKind==CafeRecipes.Tart)
            {
                if(step!=Step.Sponge)
                {
                    Color crust=CafeShapes.Hex("#d8b48a");
                    art.Disc(cake,"Tart shell",new Vector3(0,.13f,0),.56f,.18f,crust);
                    for(int j=0;j<36;j++)
                    {
                        float a=j*Mathf.PI*2/36;
                        art.Ball(cake,"Fluted edge",new Vector3(Mathf.Cos(a)*.52f,.23f,Mathf.Sin(a)*.52f),new Vector3(.10f,.23f,.10f),crust);
                    }
                }
                for(int j=0;j<50;j++)
                {
                    float t=(j+1)/50f;
                    var fill=art.Disc(cake,"Custard filling",new Vector3(0,.265f+t*.012f,0),.47f*t,.045f,filling);
                    fill.gameObject.SetActive(false);creamPieces.Add(fill);
                }
            }
            else
            {
                jelly=art.Group(cake,"Pudding",Vector3.zero);
                if(step!=Step.Sponge)
                {
                    for(int j=0;j<28;j++)
                    {
                        float t=j/27f;
                        art.Disc(jelly,"Silky pudding",new Vector3(0,.08f+t*.43f,0),Mathf.Lerp(.40f,.28f,t),.022f,filling);
                    }
                    mold=art.Group(cake,"Lifted mold",Vector3.zero);
                    art.Disc(mold,"Pudding mold",new Vector3(0,.32f,0),.44f,.53f,CafeShapes.Hex("#d2cecb"));
                }
                for(int j=0;j<60;j++)
                {
                    Transform part;
                    if(j<40)part=art.Disc(jelly,"Caramel cap",new Vector3(0,.53f+j*.0001f,0),.29f*(j+1)/40f,.025f,CafeShapes.Hex("#b87e58"));
                    else
                    {
                        int drip=(j-40)%5,level=(j-40)/5;
                        float a=drip*Mathf.PI*2/5;
                        part=art.Ball(jelly,"Caramel drip",new Vector3(Mathf.Cos(a)*(.30f+level*.012f),.50f-level*.045f,Mathf.Sin(a)*(.30f+level*.012f)),new Vector3(.05f,.09f,.05f),CafeShapes.Hex("#b87e58"));
                    }
                    part.gameObject.SetActive(false);creamPieces.Add(part);
                }
                if(puddingCream && step!=Step.Sponge)
                    for(int j=0;j<24;j++)
                    {
                        float t=j/23f,a=t*Mathf.PI*5;
                        art.Ball(jelly,"Cream dollop",new Vector3(Mathf.Cos(a)*.12f*(1-t),.58f+t*.18f,Mathf.Sin(a)*.12f*(1-t)),Vector3.one*Mathf.Lerp(.12f,.04f,t),CafeShapes.Hex("#fff0d8"));
                    }
            }
            pipingBag=art.Group(cake,"Filling tool",Vector3.zero);
            art.Ball(pipingBag,"Soft bag",new Vector3(.08f,.36f,0),new Vector3(.29f,.54f,.27f),CafeShapes.Hex(dessertKind>=2 ? "#d6b39d" : "#f5e5d0"));
            art.Disc(pipingBag,"Nozzle",new Vector3(0,.06f,0),.045f,.12f,CafeShapes.Hex("#b9b7af"));
            decoration=art.Group(cake,"Decoration",toppingOffset);
            if(fruitRing && dessertKind==CafeRecipes.Tart && selectedTopping>=0)
            {
                for(int j=0;j<8;j++)
                {
                    float a=j*Mathf.PI*2/8;
                    var cluster=art.Group(decoration,"Fruit ring",new Vector3(Mathf.Cos(a)*.34f,ToppingHeight*.35f,Mathf.Sin(a)*.34f));
                    cluster.localScale=Vector3.one*.65f;BuildTopping(cluster,selectedTopping);
                }
            }
            else BuildTopping(decoration,selectedTopping);
            cake.gameObject.SetActive(phase!=Phase.Greeting);UpdateCream();UpdateDessertAnimation();
        }
        void UpdateDessertAnimation()
        {
            if(dessertKind!=CafeRecipes.Pudding)return;
            if(mold!=null)
            {
                float t=Mathf.Clamp01(1-(unmoldUntil-Time.time)/.85f);
                mold.gameObject.SetActive(Time.time<unmoldUntil && !reduceMotion);
                mold.localPosition=Vector3.up*(t*.8f);
                mold.localScale=Vector3.one*Mathf.Lerp(1,.05f,t*t);
            }
            if(jelly!=null)
            {
                float after=Time.time-unmoldUntil;
                float wobble=!reduceMotion && after>0 && after<2 ? Mathf.Sin(after*18)*Mathf.Exp(-after*2.4f)*.055f : 0;
                jelly.localScale=new Vector3(1+wobble,1-wobble,1+wobble);
            }
        }
        void ToggleDessertExtra()
        {
            ResetHold();toppingOffset=Vector3.zero;
            if(dessertKind==CafeRecipes.Tart)fruitRing=!fruitRing;else puddingCream=!puddingCream;
            BuildCake();
        }
    }
}
