using System.Collections.Generic;
using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        sealed class DisplayPastry { public Transform root;public int kind; }
        sealed class CareSpot
        {
            public Transform root,cushion,water,yarn;
            public float waterLevel;
        }
        readonly List<DisplayPastry> displays=new List<DisplayPastry>();
        readonly List<CareSpot> careSpots=new List<CareSpot>();
        readonly List<Light> pendantLights=new List<Light>();
        CafeShopSettings shop;
        bool displayOpen,careOpen,weatherOpen;
        int displayKind,careIndex;
        Vector3 displayFocus,careFocus;
        Texture2D sunIcon,rainIcon,moonIcon,cushionIcon,waterIcon,yarnIcon;
        Light sunlight;
        void InitShop()
        {
            shop=CafeShopSettings.Load();
            pressIcon=MakeIcon(37,0);mixIcon=MakeIcon(38,0);unmoldIcon=MakeIcon(39,0);
            sunIcon=MakeIcon(40,0);rainIcon=MakeIcon(41,0);moonIcon=MakeIcon(42,0);
            cushionIcon=MakeIcon(43,0);waterIcon=MakeIcon(44,0);yarnIcon=MakeIcon(45,0);
            foreach(var light in world.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)sunlight=light;
            foreach(var t in world.GetComponentsInChildren<Transform>())if(t.name=="Pendant")
            {
                var go=new GameObject("Warm pendant glow");go.transform.SetParent(t,false);go.transform.localPosition=Vector3.down*.3f;
                var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=4;light.shadows=LightShadows.None;light.color=CafeShapes.Hex("#ffd4a6");pendantLights.Add(light);
            }
            for(int i=0;i<2;i++)
            {
                var spot=new CareSpot();spot.root=art.Group(world,"Cat care corner",new Vector3(i==0?-4.3f:4.3f,0,-6.2f));
                spot.cushion=art.Ball(spot.root,"Cat cushion",new Vector3(0,.1f,-.65f),new Vector3(.95f,.18f,.7f),CafeShapes.Hex(i==0?"#d6b1bd":"#b6c6ae"));
                Color bowl=CafeShapes.Hex("#d7c7b8");
                art.Disc(spot.root,"Water bowl base",new Vector3(0,.05f,0),.25f,.07f,bowl);
                for(int j=0;j<24;j++)
                {
                    float a=j*Mathf.PI*2/24;
                    art.Ball(spot.root,"Water bowl rim",new Vector3(Mathf.Cos(a)*.23f,.10f,Mathf.Sin(a)*.23f),new Vector3(.06f,.10f,.06f),bowl);
                }
                spot.water=art.Disc(spot.root,"Fresh water",new Vector3(0,.08f,0),.20f,.014f,CafeShapes.Hex("#b8cdd0"));
                spot.yarn=art.Ball(spot.root,"Yarn ball",new Vector3(0,.12f,.65f),Vector3.one*.22f,CafeShapes.Hex("#c5a7bd"));
                for(int k=0;k<4;k++)
                {
                    var thread=art.Disc(spot.yarn,"Yarn thread",Vector3.zero,.50f,.015f,CafeShapes.Hex("#dfcbd8"));thread.localRotation=Quaternion.Euler(k*42,25,25);
                }
                spot.yarn.gameObject.SetActive(false);spot.water.gameObject.SetActive(false);careSpots.Add(spot);
            }
            ApplyShopSettings();
        }
        void ApplyShopSettings()
        {
            foreach(var drop in rain)drop.gameObject.SetActive(shop.rain);
            Color sky=CafeShapes.Hex(shop.dusk?(shop.rain?"#afa8bc":"#d6b4ad"):(shop.rain?"#c8d7d6":"#dbe7df"));
            foreach(var r in world.GetComponentsInChildren<Renderer>())
                if(r.name=="Window glass"||r.name=="Window"||r.name=="Door glass")r.sharedMaterial=art.Material(sky);
            if(sunlight!=null){sunlight.color=CafeShapes.Hex(shop.dusk?"#f4c3a2":"#fff1db");sunlight.intensity=shop.dusk?.70f:shop.rain?.95f:1.1f;}
            RenderSettings.ambientLight=CafeShapes.Hex(shop.dusk?"#aaa0b1":"#bdb4aa");view.backgroundColor=shop.dusk?CafeShapes.Hex("#d9c5bd"):wallColor;
            foreach(var light in pendantLights)light.intensity=shop.dusk?.65f:.08f;
            careSpots[0].cushion.gameObject.SetActive(shop.leftCushion);careSpots[1].cushion.gameObject.SetActive(shop.rightCushion);
        }
        void MakeDisplayPastry(Transform stand,int kind)
        {
            var pastry=art.Group(stand,"Display dessert",new Vector3(0,.2f,0));
            Color baseColor=flavors[kind%3],cream=CafeShapes.Hex("#f1e4ce");
            if(kind==0)
            {
                art.Disc(pastry,"Cupcake base",new Vector3(0,.1f,0),.19f,.2f,baseColor);
                for(int i=0;i<3;i++)art.Ball(pastry,"Cream swirl",new Vector3(0,.23f+i*.055f,0),new Vector3(.3f-i*.065f,.12f,.3f-i*.065f),cream);
            }
            else if(kind==1)
            {
                art.Disc(pastry,"Tart crust",new Vector3(0,.05f,0),.25f,.1f,CafeShapes.Hex("#d0ac7b"));
                art.Disc(pastry,"Tart filling",new Vector3(0,.105f,0),.21f,.025f,cream);
                for(int j=0;j<6;j++){float a=j*Mathf.PI/3;art.Ball(pastry,"Tart berry",new Vector3(Mathf.Cos(a)*.15f,.14f,Mathf.Sin(a)*.15f),Vector3.one*.07f,CafeShapes.Hex("#b77988"));}
            }
            else if(kind==2)
            {
                for(int j=0;j<10;j++)art.Disc(pastry,"Pudding",new Vector3(0,.04f+j*.025f,0),.20f-j*.006f,.03f,cream);
                art.Disc(pastry,"Caramel",new Vector3(0,.28f,0),.15f,.015f,CafeShapes.Hex("#b98558"));
            }
            else if(kind==3)
            {
                for(int j=0;j<3;j++)art.Disc(pastry,"Pancake",new Vector3(0,.05f+j*.07f,0),.23f,.06f,CafeShapes.Hex("#d4ae7f"));
                art.Box(pastry,"Butter",new Vector3(0,.24f,0),new Vector3(.10f,.05f,.10f),CafeShapes.Hex("#f0d599"));
            }
            else if(kind==CafeRecipes.Donut)
            {
                for(int j=0;j<24;j++){float a=j*Mathf.PI/12;art.Ball(pastry,"Donut",new Vector3(Mathf.Cos(a)*.16f,.10f,Mathf.Sin(a)*.16f),new Vector3(.13f,.12f,.13f),baseColor);}
            }
            else if(kind==CafeRecipes.LayerCake)
            {
                art.Disc(pastry,"Lower sponge",Vector3.up*.07f,.23f,.12f,baseColor);
                art.Disc(pastry,"Sandwich",Vector3.up*.15f,.23f,.04f,cream);
                art.Disc(pastry,"Upper sponge",Vector3.up*.23f,.23f,.12f,baseColor);
                art.Disc(pastry,"Icing",Vector3.up*.30f,.23f,.03f,cream);
            }
            else
            {
                art.Box(pastry,"Waffle",new Vector3(0,.06f,0),new Vector3(.43f,.1f,.43f),CafeShapes.Hex("#c9a373"));
                for(int j=-2;j<=2;j++)for(int k=-2;k<=2;k++)art.Box(pastry,"Grid",new Vector3(j*.08f,.12f,k*.08f),new Vector3(.05f,.02f,.05f),CafeShapes.Hex("#ad875d"));
            }
            displays.Add(new DisplayPastry{root=pastry,kind=kind});
        }
        void DrawShopEntrances()
        {
            if(utilityOpen && IconButton(new Rect(55,170,80,65),shop.rain?rainIcon:sunIcon,"窗景與燈光")){ResetHold();weatherOpen=true;}
            if(utilityOpen && IconButton(new Rect(55,250,80,65),waterIcon,"照顧住店貓"))OpenCare(view.transform.position.x>0?1:0);
            if(roomStation!=0)return;
            foreach(var d in displays)
                if((view.transform.position-d.root.position).sqrMagnitude<28&&WorldButton(d.root.position+Vector3.up*.45f,dessertIcons[d.kind,0],"查看甜點"))
                {ResetHold();displayKind=d.kind;displayFocus=d.root.position;displayOpen=true;roomStation=4;break;}
            for(int i=0;i<careSpots.Count;i++)
                if((view.transform.position-careSpots[i].root.position).sqrMagnitude<20&&WorldButton(careSpots[i].root.position+Vector3.up*.65f,waterIcon,"照顧貓咪"))OpenCare(i);
            if(WorldButton(new Vector3(-7.5f,2,-5.8f),shop.rain?rainIcon:sunIcon,"窗景")){ResetHold();weatherOpen=true;}
        }
        void DrawDisplay()
        {
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){displayOpen=false;GoStation(0);Event.current.Use();return;}
            GUI.Box(new Rect(330,625,540,130),GUIContent.none,panelStyle);
            if(IconButton(new Rect(1090,35,65,55),closeIcon,"返回店內")){displayOpen=false;GoStation(0);return;}
            GUI.DrawTexture(new Rect(355,650,80,80),dessertIcons[displayKind,0]);
            if(IconButton(new Rect(480,650,130,74),heartIcon,"自由製作")){displayOpen=false;ChooseCreative(displayKind);}
            GUI.enabled=!creativeMode&&phase!=Phase.Served&&displayKind==dessertKind;
            if(IconButton(new Rect(665,650,130,74),serveIcon,"接續目前訂單")){displayOpen=false;GoStation(2);}
            GUI.enabled=true;
            if(showLearningLabels)GUI.Label(new Rect(320,580,560,35),CafeRecipes.Names[displayKind]+"：愛心自由製作；餐罩接續相同甜點訂單",labelStyle);
        }
        void DrawWeather()
        {
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){weatherOpen=false;ResetHold();Event.current.Use();return;}
            GUI.Box(new Rect(300,230,600,330),GUIContent.none,panelStyle);
            if(IconButton(new Rect(810,245,60,48),closeIcon,"完成")){weatherOpen=false;ResetHold();}
            if(IconButton(new Rect(365,335,120,100),sunIcon,"晴天",!shop.rain)){shop.rain=false;ApplyShopSettings();shop.Save();}
            if(IconButton(new Rect(535,335,120,100),rainIcon,"雨天",shop.rain)){shop.rain=true;ApplyShopSettings();shop.Save();}
            if(IconButton(new Rect(705,335,120,100),shop.dusk?moonIcon:sunIcon,"午後／傍晚",shop.dusk)){shop.dusk=!shop.dusk;ApplyShopSettings();shop.Save();}
            if(showLearningLabels)GUI.Label(new Rect(330,485,540,40),"晴／雨與午後／傍晚可獨立搭配，設定會保存",labelStyle);
        }
        void OpenCare(int index)
        {
            ResetHold();careIndex=index;careFocus=careSpots[index].root.position;careOpen=true;roomStation=5;
        }
        void DrawCare()
        {
            if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){careOpen=false;GoStation(0);Event.current.Use();return;}
            GUI.Box(new Rect(325,625,550,130),GUIContent.none,panelStyle);
            if(IconButton(new Rect(1090,35,65,55),closeIcon,"返回店內")){careOpen=false;GoStation(0);return;}
            var spot=careSpots[careIndex];
            if(IconButton(new Rect(370,650,120,75),cushionIcon,"放好坐墊",spot.cushion.gameObject.activeSelf))
            {
                if(careIndex==0)shop.leftCushion=true;else shop.rightCushion=true;
                ApplyShopSettings();shop.Save();InviteHouseCat(0);
            }
            if(IconButton(new Rect(540,650,120,75),waterIcon,"添水")){spot.waterLevel=1;spot.water.gameObject.SetActive(true);InviteHouseCat(1);sounds.Play(0);}
            if(IconButton(new Rect(710,650,120,75),yarnIcon,"毛線球")){spot.yarn.gameObject.SetActive(true);InviteHouseCat(2);}
            if(showLearningLabels)GUI.Label(new Rect(325,580,550,35),"坐墊休息・添水・毛線球；貓咪會走過來",labelStyle);
        }
        void InviteHouseCat(int action)
        {
            HouseCat selected=null;float nearest=float.MaxValue;
            Vector3 target=careFocus+new Vector3(0,0,action==0?-.65f:action==1?-.32f:.35f);
            foreach(var cat in houseCats)
            {float distance=(cat.root.position-target).sqrMagnitude;if(distance<nearest){nearest=distance;selected=cat;}}
            if(selected==null)return;
            selected.root.localScale=Vector3.one*.48f;selected.head.localPosition=selected.headRest;selected.head.localRotation=Quaternion.identity;var paw=selected.root.Find("Paw");if(paw!=null)paw.localPosition=new Vector3(-.38f,1.48f,-.65f);
            selected.careSpot=careIndex;selected.careAction=action;selected.careTarget=target;selected.careRemaining=8;selected.rest=0;
        }
        bool UpdateHouseCare(HouseCat resident,float clock)
        {
            if(resident.careRemaining<=0)return false;
            var spot=careSpots[resident.careSpot];
            Vector3 delta=resident.careTarget-resident.root.position;
            if(delta.sqrMagnitude>.015f)
            {
                resident.root.position=reduceMotion?resident.careTarget:Vector3.MoveTowards(resident.root.position,resident.careTarget,Time.deltaTime*.65f);
                var flat=new Vector3(delta.x,0,delta.z);if(flat.sqrMagnitude>.001f)resident.root.rotation=Quaternion.LookRotation(-flat);
                return true;
            }
            resident.careRemaining-=Time.deltaTime;
            resident.root.rotation=Quaternion.Euler(0,180,0);
            if(resident.careAction==0)
            {
                resident.root.localScale=new Vector3(.5f,.40f,.5f);resident.root.position=resident.careTarget+Vector3.up*.08f;
                resident.head.localRotation=Quaternion.Euler(16,0,-10);
            }
            else if(resident.careAction==1)
            {
                resident.root.localScale=new Vector3(.48f,.32f,.48f);resident.head.localPosition=resident.headRest+new Vector3(0,-.6f,-.1f);
                resident.head.localRotation=Quaternion.Euler(24,0,0);
                spot.waterLevel=Mathf.Max(0,spot.waterLevel-Time.deltaTime*.05f);
                spot.water.localPosition=new Vector3(0,.06f+spot.waterLevel*.03f,0);
                spot.water.gameObject.SetActive(spot.waterLevel>0);
            }
            else
            {
                resident.head.localRotation=Quaternion.Euler(5,reduceMotion?0:Mathf.Sin(clock*3)*15,0);
                spot.yarn.localPosition=new Vector3(reduceMotion?0:Mathf.Sin(clock*3)*.16f,.12f,.65f);
                var paw=resident.root.Find("Paw");if(paw!=null)paw.localPosition=new Vector3(-.38f,1.48f+(reduceMotion?0:Mathf.Max(0,Mathf.Sin(clock*4))*.18f),-.65f);
            }
            if(resident.careRemaining<=0)
            {
                resident.root.localScale=Vector3.one*.48f;resident.head.localPosition=resident.headRest;resident.head.localRotation=Quaternion.identity;resident.rest=3;
                var paw=resident.root.Find("Paw");if(paw!=null)paw.localPosition=new Vector3(-.38f,1.48f,-.65f);
            }
            return true;
        }
    }
}
