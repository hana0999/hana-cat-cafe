using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hana.CatCafe
{
    [DisallowMultipleComponent]
    public sealed partial class CatCafePrototype : MonoBehaviour
    {
        [Header("Look / 美術設定")]
        public Color wallColor = new Color(0.94f, 0.88f, 0.82f);
        public Color counterColor = new Color(0.80f, 0.64f, 0.65f);
        [Header("Accessibility / 可選教學標籤")]
        public bool showLearningLabels = false;
        public bool reduceMotion = false;
        enum Phase { Greeting, Making, Served }
        Phase phase;
        bool capturingPhoto, photoOpen;
        float photoStarted;
        Texture2D photoSource, photoPreview;
        Texture2D cameraIcon, saveIcon;
        Texture2D[] frameIcons;
        int photoStyle, photoStatus;

        CafeDecor cafeDecor;
        CafeDecorView decorView;
        Renderer counterRenderer;
        bool decorOpen;
        Texture2D decorIcon;
        Texture2D[,] decorChoices;

        CafeGuestBook guestBook;
        CafeGuestDefinition guest;
        int guestIndex;
        bool collectionOpen;
        Texture2D albumIcon, closeIcon, lockIcon;
        Texture2D[] portraits;
        Transform welcomeHeart;
        float welcomeUntil;

        CafeDrink drink;
        CafeDrinkView drinkView;
        bool drinkStation, drinkHeld, holdActive, releaseHoldRequested;
        Texture2D cupIcon, pourIcon, foamIcon;
        Texture2D[] cupChoices;

        enum Step { Sponge, Cream, Decoration, Ready }
        Step step;
        float creamAmount;
        bool pipingHeld;
        readonly List<Transform> creamPieces = new List<Transform>();
        Transform pipingBag, decoration;
        Texture2D pipingIcon, undoIcon;
        int visibleCream;
        CafeProgress progress;
        CafeShapes art;
        Transform world, cat, head, tail, cake, leftEye, rightEye;
        Camera view;
        readonly List<Texture2D> textures = new List<Texture2D>();
        Texture2D[] cakeIcons, toppingIcons;
        Texture2D pawIcon, serveIcon, nextIcon, heartIcon;
        GUIStyle buttonStyle, panelStyle, labelStyle;
        Color[] flavors;
        int selectedFlavor, selectedTopping = -1, wantedFlavor, wantedTopping;
        bool petted;
        float happyUntil, puzzledUntil, nextAllowed, arriveAt;
        Vector3 headRest;
        const float DesignWidth = 1200, DesignHeight = 800;

        void Start()
        {
            art = new CafeShapes(); drinkView = new CafeDrinkView(art); progress = CafeProgress.Load(); guestBook=CafeGuestBook.Load();
            cafeDecor=CafeDecor.Load(progress.served);decorView=new CafeDecorView(art);
            flavors = new[] { CafeShapes.Hex("#ebd5aa"), CafeShapes.Hex("#dfb1bb"), CafeShapes.Hex("#ac8874") };
            world = art.Group(transform, "Generated Cafe", Vector3.zero);
            BuildRoom();BuildServiceTray();decorView.Apply(world,counterRenderer,counterColor,cafeDecor); BuildIcons(); NewVisit();InitLife();InitRoom();InitAtmosphere();InitShop();
        }
        void BuildRoom()
        {
            art.Box(world, "Floor", new Vector3(0,-0.12f,0), new Vector3(16,.2f,24), CafeShapes.Hex("#e4d5bc"));
            art.Box(world, "Wall", new Vector3(0,2.6f,2.6f), new Vector3(16,5.4f,.15f), wallColor);
            art.Box(world, "Wall panel", new Vector3(0,.65f,2.48f), new Vector3(16,1.3f,.08f), CafeShapes.Hex("#c5cdb7"));
            counterRenderer=art.Box(world, "Counter", new Vector3(0,.65f,-.5f), new Vector3(7,1.3f,1.8f), counterColor).GetComponent<Renderer>();
            art.Box(world, "Counter top", new Vector3(0,1.35f,-.5f), new Vector3(7.2f,.16f,2), CafeShapes.Hex("#f3e7d4"));
            for (int i=0;i<13;i++) art.Ball(world,"Scalloped trim",new Vector3(-3.2f+i*.53f,1.19f,-1.42f),new Vector3(.55f,.16f,.045f),CafeShapes.Hex("#f1dfcb"));
            art.Box(world,"Window frame",new Vector3(-2.9f,2.9f,2.35f),new Vector3(2.5f,2.7f,.15f),CafeShapes.Hex("#d2b99d"));
            art.Box(world,"Window",new Vector3(-2.9f,2.9f,2.24f),new Vector3(2.25f,2.45f,.06f),CafeShapes.Hex("#cddcda"));
            art.Box(world,"Window cross",new Vector3(-2.9f,2.9f,2.16f),new Vector3(.08f,2.45f,.07f),CafeShapes.Hex("#f8ebd6"));
            art.Box(world,"Window bar",new Vector3(-2.9f,2.9f,2.16f),new Vector3(2.25f,.08f,.07f),CafeShapes.Hex("#f8ebd6"));
            art.Box(world,"Shelf",new Vector3(2.8f,2.7f,2.1f),new Vector3(2.2f,.12f,.5f),CafeShapes.Hex("#bc9d84"));
            for(int i=0;i<3;i++) art.Disc(world,"Ceramic jar",new Vector3(2.1f+i*.65f,2.98f,2.08f),.20f,.43f,flavors[i]);
            var cameraObject = new GameObject("Cafe Camera"); cameraObject.transform.SetParent(world);
            view = cameraObject.AddComponent<Camera>(); view.orthographic = true; view.orthographicSize = 3.65f;
            view.transform.position = new Vector3(0,4.7f,-9); view.transform.LookAt(new Vector3(0,1.65f,0));
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = wallColor;
            cameraObject.AddComponent<AudioListener>();
            var lamp = new GameObject("Soft window light"); lamp.transform.SetParent(world);
            var light = lamp.AddComponent<Light>(); light.type=LightType.Directional; light.color=CafeShapes.Hex("#fff1db");light.intensity=1.1f;
            lamp.transform.rotation=Quaternion.Euler(45,-30,0); light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=CafeShapes.Hex("#bdb4aa");
        }
        void NewVisit()
        {
            if(toyMode)EndToy();
            carryingTray=false;mealPlaced=false;ClearIcing();if(serviceTray!=null&&cake!=null&&drinkView.Root!=null)RestoreMealTransforms();guestSettled=false;preparation=null;baking=null;batterHeld=false;dessertKind=CafeRecipes.ForOrder(progress.served);fruitRing=false;puddingCream=false;unmoldUntil=-100;
            toppingOffset=Vector3.zero;trayOffset=Vector3.zero;servedAt=-100;returnTopping=-1;
            phase=Phase.Greeting; step=Step.Sponge; creamAmount=0; pipingHeld=false; petted=false; selectedFlavor=0;selectedTopping=-1;
            guestIndex=guestBook.Next(progress.served);guest=CafeGuests.All[guestIndex];
            guestBook.Meet(guestIndex);guestBook.Save();collectionOpen=false;decorOpen=false;
            wantedFlavor=guest.Flavor;wantedTopping=guest.Topping;
            welcomeUntil=guestBook.entries[guestIndex].Hearts>=2 ? Time.time+3 : 0;
            happyUntil=puzzledUntil=0;arriveAt=Time.time;
            drink=new CafeDrink();drinkStation=false;ResetHold();
            BuildCat();sounds?.Play(2);playPaw=cat.Find("Paw");if(playPaw!=null)pawRest=playPaw.localPosition;petUntil=0;ApplyGifts(); BuildCake();drinkView.Build(world,drink);ApplySaucer();drinkView.Root.gameObject.SetActive(false);if(roomIcon!=null)GoStation(0);
        }
        void BuildCat()
        {
            if(cat!=null){cat.gameObject.SetActive(false);Destroy(cat.gameObject);}
            cat=art.Group(world,guest.Id+" guest",new Vector3(-1.45f,0,.8f));
            Color fur=CafeShapes.Hex(guest.Fur),cream=CafeShapes.Hex("#f3e4cf"),ink=CafeShapes.Hex("#5c4e4a");
            art.Ball(cat,"Body",new Vector3(0,1.1f,0),new Vector3(1.12f,1.5f,.8f),fur);
            head=art.Group(cat,"Head",new Vector3(0,2.05f,-.16f));headRest=head.localPosition;
            art.Ball(head,"Face",Vector3.zero,new Vector3(1.4f,1.15f,1),fur);
            for(int side=-1;side<=1;side+=2)
            {
                var ear=art.Ball(head,"Ear",new Vector3(side*.47f,.49f,.01f),new Vector3(.39f,.65f,.3f),fur);ear.localRotation=Quaternion.Euler(0,0,-side*24);
                var inner=art.Ball(head,"Inner ear",new Vector3(side*.47f,.5f,-.13f),new Vector3(.22f,.38f,.045f),CafeShapes.Hex("#d7a8a5"));inner.localRotation=ear.localRotation;
                art.Ball(head,"Muzzle",new Vector3(side*.14f,-.24f,-.48f),new Vector3(.32f,.23f,.13f),cream);
                var eye=art.Ball(head,"Eye",new Vector3(side*.27f,.02f,-.46f),new Vector3(.075f,.12f,.045f),ink);
                if(side<0)leftEye=eye;else rightEye=eye;
                art.Ball(head,"Blush",new Vector3(side*.44f,-.17f,-.42f),new Vector3(.19f,.07f,.03f),CafeShapes.Hex("#d2a19b"));
                art.Ball(cat,"Paw",new Vector3(side*.38f,1.48f,-.65f),new Vector3(.32f,.19f,.4f),cream);
                for(int j=0;j<2;j++){var whisker=art.Box(head,"Whisker",new Vector3(side*.53f,-.22f-j*.07f,-.43f),new Vector3(.25f,.014f,.014f),ink);whisker.localRotation=Quaternion.Euler(0,0,side*(j==0?12:-8));}
            }
            art.Ball(head,"Nose",new Vector3(0,-.18f,-.57f),new Vector3(.10f,.07f,.05f),CafeShapes.Hex("#b78585"));
            for(int j=-1;j<=1;j++)art.Ball(head,"Forehead stripe",new Vector3(j*.15f,.32f,-.42f),new Vector3(.07f,.19f,.035f),CafeShapes.Hex(guest.Stripe));
            if(guestIndex==2)
            {
                // Cloud's soft white chest distinguishes the third guest.
                art.Ball(cat,"White bib",new Vector3(0,1.35f,-.42f),new Vector3(.54f,.62f,.06f),cream);
            }
            if(guestIndex==3)
            {
                art.Ball(head,"Calico ear patch",new Vector3(-.46f,.43f,-.17f),new Vector3(.29f,.32f,.08f),CafeShapes.Hex("#766572"));
                art.Ball(head,"Calico cheek patch",new Vector3(.48f,.13f,-.38f),new Vector3(.35f,.40f,.10f),CafeShapes.Hex("#c79b74"));
                art.Ball(cat,"Calico chest patch",new Vector3(.25f,1.1f,-.38f),new Vector3(.40f,.52f,.09f),CafeShapes.Hex("#a88978"));
            }
            if(guestIndex==5)art.Ball(cat,"Tuxedo bib",new Vector3(0,1.3f,-.43f),new Vector3(.60f,.75f,.09f),cream);
            if(guestIndex==6)art.Ball(head,"Mocha mask",new Vector3(0,-.18f,-.49f),new Vector3(.48f,.26f,.06f),CafeShapes.Hex(guest.Stripe));
            if(guestIndex==4)art.Ball(cat,"Cream white bib",new Vector3(0,1.35f,-.42f),new Vector3(.60f,.60f,.08f),CafeShapes.Hex("#fff0dc"));
            for(int side=-1;side<=1;side+=2)art.Ball(cat,"Back paw",new Vector3(side*.28f,.18f,-.08f),new Vector3(.35f,.36f,.45f),cream);
            var ribbon=art.Group(cat,"Ribbon",new Vector3(0,1.58f,-.54f));
            Color ribbonColor=CafeShapes.Hex(guest.Ribbon);
            for(int side=-1;side<=1;side+=2)
            {
                var wing=art.Ball(ribbon,"Bow loop",new Vector3(side*.12f,0,0),new Vector3(.23f,.15f,.07f),ribbonColor);
                wing.localRotation=Quaternion.Euler(0,0,-side*18);
            }
            art.Ball(ribbon,"Knot",Vector3.zero,Vector3.one*.10f,ribbonColor);
            welcomeHeart=art.Group(cat,"Familiar welcome",new Vector3(0,3,-.2f));
            for(int side=-1;side<=1;side+=2)
            {
                var half=art.Ball(welcomeHeart,"Heart half",new Vector3(side*.06f,0,0),new Vector3(.16f,.24f,.07f),CafeShapes.Hex("#d2a0b0"));
                half.localRotation=Quaternion.Euler(0,0,-side*35);
            }
            welcomeHeart.gameObject.SetActive(false);
            tail=art.Group(cat,"Tail pivot",new Vector3(.55f,.6f,.1f));
            for(int j=0;j<8;j++)art.Ball(tail,"Tail",new Vector3(.10f*j,.06f*j,0),new Vector3(.22f,.22f,.22f),fur);
        }
        void BuildCake()
        {
            if(cake!=null){cake.gameObject.SetActive(false);Destroy(cake.gameObject);}
            cake=art.Group(world,"Your cupcake",new Vector3(1.4f,1.46f,-.55f));
            BuildTableware(cake,.75f);
            if(PreparationActive)
            {
                creamPieces.Clear();visibleCream=0;pipingBag=null;decoration=art.Group(cake,"Decoration",Vector3.zero);
                BuildPreparationRig();cake.gameObject.SetActive(phase!=Phase.Greeting);return;
            }
            if(BakingActive)
            {
                creamPieces.Clear();visibleCream=0;pipingBag=null;
                decoration=art.Group(cake,"Decoration",Vector3.zero);BuildBakingRig();
                cake.gameObject.SetActive(phase!=Phase.Greeting);return;
            }
            if(dessertKind!=CafeRecipes.Cupcake){BuildOtherDessert();return;}
            jelly=null;mold=null;
            if(step!=Step.Sponge)
            {
            art.Disc(cake,"Sponge",new Vector3(0,.29f,0),.40f,.5f,flavors[selectedFlavor]);
            for(int j=0;j<30;j++){float a=j*Mathf.PI*2/30;art.Ball(cake,"Paper pleat",new Vector3(Mathf.Cos(a)*.40f,.24f,Mathf.Sin(a)*.40f),new Vector3(.038f,.38f,.038f),CafeShapes.Hex("#dfc5cb"));}
            }
            creamPieces.Clear();
            for(int j=0;j<70;j++)
            {
                float t=j/69f,a=t*Mathf.PI*6;
                var piece=art.Ball(cake,"Cream swirl",new Vector3(Mathf.Cos(a)*.28f*(1-t),.57f+t*.48f,Mathf.Sin(a)*.28f*(1-t)),Vector3.one*Mathf.Lerp(.21f,.055f,t),flavors[selectedFlavor]);
                piece.gameObject.SetActive(false);creamPieces.Add(piece);
            }
            visibleCream=0;
            pipingBag=art.Group(cake,"Piping bag",Vector3.zero);
            art.Ball(pipingBag,"Soft bag",new Vector3(.08f,.36f,0),new Vector3(.29f,.54f,.27f),CafeShapes.Hex("#f5e5d0"));
            art.Disc(pipingBag,"Metal nozzle",new Vector3(0,.06f,0),.045f,.12f,CafeShapes.Hex("#b9b7af"));
            pipingBag.localRotation=Quaternion.Euler(0,0,-12);
            decoration=art.Group(cake,"Decoration",Vector3.zero);
            BuildTopping(decoration,selectedTopping);
            decoration.localPosition=toppingOffset;
            cake.gameObject.SetActive(phase!=Phase.Greeting);
            UpdateCream();
        }
        void BuildTopping(Transform parent,int type)
        {
            parent=art.Group(parent,"Topping height",new Vector3(dessertKind==CafeRecipes.Donut?.32f:0,ToppingHeight-1.10f,0));
            if(type==0){art.Ball(parent,"Cherry",new Vector3(0,1.13f,0),Vector3.one*.19f,CafeShapes.Hex("#ba727e"));var stem=art.Box(parent,"Cherry stem",new Vector3(.025f,1.27f,0),new Vector3(.018f,.16f,.018f),CafeShapes.Hex("#80916f"));stem.localRotation=Quaternion.Euler(0,0,-18);}
            if(type==1)for(int j=0;j<3;j++)art.Ball(parent,"Blueberry",new Vector3((j-1)*.13f,1.06f,j%2*.11f),Vector3.one*.16f,CafeShapes.Hex("#8794b2"));
            if(type==2)for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;art.Ball(parent,"Sugar flower",new Vector3(Mathf.Cos(a)*.1f,1.08f,Mathf.Sin(a)*.1f),new Vector3(.16f,.06f,.16f),CafeShapes.Hex("#d5bfda"));}
        }
        void AcceptOrder(){if(phase!=Phase.Greeting)return;phase=Phase.Making;sounds?.Play(0);BuildCake();drinkView.Root.gameObject.SetActive(true);if(roomIcon!=null&&roomStation==0)GoStation(2);}
        void Pet()
        {
            happyUntil=Time.time+2;
            if(creativeMode)return;
            if(!petted){petted=true;progress.affection++;progress.Save();guestBook.entries[guestIndex].AddBond(1);guestBook.Save();}
        }
        void Serve()
        {
            if(!mealPlaced || creativeMode || phase!=Phase.Making || step!=Step.Ready || !drink.Ready)return;
            if(!CafeProgress.Matches(selectedFlavor,selectedTopping,wantedFlavor,wantedTopping)){puzzledUntil=Time.time+2;return;}
            ResetHold();sounds?.Play(2);phase=Phase.Served;servedAt=Time.time;happyUntil=Time.time+5;nextAllowed=Time.time+5.5f;
            progress.served++;progress.affection+=2;progress.Save();
            guestBook.entries[guestIndex].AddBond(2);
            guestBook.entries[guestIndex].orders=Mathf.Min(100000,guestBook.entries[guestIndex].orders+1);guestBook.Save();GoStation(1);
        }
        void Update()
        {
            if(cat==null || capturingPhoto || photoOpen || compositionOpen){sounds?.Flow(false);if(compositionOpen&&!photoOpen&&!capturingPhoto&&Mathf.Abs(view.aspect-lastPhotoAspect)>.001f)ApplyPhotoCamera();return;}
            float time=Time.time;
            UpdateGuestArrival();
            bool happy=time<happyUntil || time<welcomeUntil,puzzled=time<puzzledUntil;
            bool sleepy=guestIndex==1 && !happy && !puzzled;
            welcomeHeart.gameObject.SetActive(time<welcomeUntil);
            welcomeHeart.localPosition=new Vector3(0,3+(reduceMotion ? 0 : Mathf.Sin(time*2)*.05f),-.2f);
            head.localPosition=headRest+Vector3.up*(reduceMotion?0:Mathf.Sin(time*(happy ? guest.BobSpeed*2 : guest.BobSpeed))*(happy ? .045f : .012f));
            head.localRotation=Quaternion.Euler(0,0,puzzled ? 12 : happy ? -5 : guestIndex==2 ? 5 : 0);
            float blink=happy ? .035f : (!reduceMotion&&time%4<.13f ? .018f : sleepy ? .06f : .12f);
            leftEye.localScale=rightEye.localScale=new Vector3(happy ? .13f : .075f,blink,.045f);
            tail.localRotation=Quaternion.Euler(0,reduceMotion?0:Mathf.Sin(time*(happy ? guest.TailSpeed*2 : guest.TailSpeed))*guest.TailRange,0);

            if(phase==Phase.Making && !collectionOpen && !decorOpen && !recipeOpen && !giftOpen && !toyMode && !drinkStation && step==Step.Cream && !BakingActive && !PreparationActive && pipingHeld && Application.isFocused)
            {
                int previousPieces=Mathf.FloorToInt(creamAmount*creamPieces.Count);
                if(Time.time>=unmoldUntil || reduceMotion)creamAmount=Mathf.Clamp01(creamAmount+Time.deltaTime/CafeRecipes.PourSeconds(dessertKind));
                RecordDirectIcing(previousPieces,Mathf.FloorToInt(creamAmount*creamPieces.Count));
                if(creamAmount>=1){step=Step.Decoration;ResetHold();}
            }
            if(phase==Phase.Making && !collectionOpen && !decorOpen && !recipeOpen && !giftOpen && !toyMode && drinkStation)
            {
                var previous=drink.Current;
                drink.Tick(Time.deltaTime,drinkHeld && Application.isFocused);
                if(previous!=drink.Current)ResetHold();
            }
            drinkView.Sync(drink,drinkHeld && drinkStation && phase==Phase.Making);
            UpdateServiceTray();UpdateGuestReaction();UpdateDessertAnimation();UpdateLife();TickBaking();TickPreparation();UpdateAtmosphere();
            UpdateCream();
            if(decoration!=null)decoration.localPosition=Vector3.Lerp(decoration.localPosition,toppingOffset,reduceMotion ? 1 : 1-Mathf.Exp(-10*Time.deltaTime));
            if(dragKind!=DragKind.None)return;
            UpdateRoomCamera();if(carryingTray)UpdateHandMeal();
        }
        void OnGUI()
        {
            if(progress==null || capturingPhoto)return;
            if(releaseHoldRequested){GUIUtility.hotControl=0;releaseHoldRequested=false;}
            float scale=Mathf.Min(Screen.width/DesignWidth,Screen.height/DesignHeight);
            var old=GUI.matrix;float ox=(Screen.width-DesignWidth*scale)/2,oy=(Screen.height-DesignHeight*scale)/2;
            guiScale=scale;guiOffset=new Vector2(ox,oy);pointerGui=(Event.current.mousePosition-guiOffset)/scale;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,Vector3.one*scale);
            if(buttonStyle==null){buttonStyle=new GUIStyle(GUI.skin.button);buttonStyle.padding=new RectOffset(8,8,8,8);panelStyle=new GUIStyle(GUI.skin.box);labelStyle=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=16};}
            GUI.backgroundColor=new Color(1,.94f,.85f,.95f);
            if(photoOpen)
            {
                DrawPhoto();GUI.matrix=old;GUI.backgroundColor=Color.white;return;
            }
            if(collectionOpen)
            {
                DrawCollection();GUI.matrix=old;GUI.backgroundColor=Color.white;return;
            }
            if(decorOpen)
            {
                DrawDecor();GUI.matrix=old;GUI.backgroundColor=Color.white;return;
            }
            if(recipeOpen)
            {
                DrawRecipes();GUI.matrix=old;GUI.backgroundColor=Color.white;return;
            }
            if(compositionOpen){DrawComposition();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            if(giftOpen){DrawGifts();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            if(toyMode){DrawToy();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            if(displayOpen){DrawDisplay();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            if(careOpen){DrawCare();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            if(weatherOpen){DrawWeather();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            HandleRoomInput();
            if(carryingTray){DrawHandMeal();GUI.matrix=old;GUI.backgroundColor=Color.white;return;}
            HandleDirectTools();HandleInteractions();
            // Picture-only order card. Clicking it or the cat accepts the order.
            if(creativeMode){GUI.DrawTexture(new Rect(545,45,60,60),heartIcon);GUI.DrawTexture(new Rect(615,40,75,75),dessertIcons[dessertKind,selectedFlavor]);}
            if(!creativeMode && phase!=Phase.Served && (roomStation!=0 || (view.transform.position-cat.position).sqrMagnitude<16))
            {
                GUI.Box(new Rect(390,35,440,120),GUIContent.none,panelStyle);
                if(GUI.Button(new Rect(400,48,100,94),dessertIcons[dessertKind,wantedFlavor],buttonStyle))AcceptOrder();
                GUI.DrawTexture(new Rect(515,65,64,64),toppingIcons[wantedTopping]);
                GUI.DrawTexture(new Rect(620,60,76,76),cupIcon);
                if(step==Step.Ready)GUI.DrawTexture(new Rect(475,123,18,18),heartIcon);
                if(drink.Ready)GUI.DrawTexture(new Rect(680,123,18,18),heartIcon);
                if(phase==Phase.Greeting && IconButton(new Rect(740,65,64,64),nextIcon,"接單"))AcceptOrder();
            }
            // Project the cat's position rather than relying on a fixed screen-space hotspot.
            Vector3 at=view.WorldToScreenPoint(cat.TransformPoint(new Vector3(0,2,0)));
            Rect catRect=new Rect((at.x-ox)/scale-90,(Screen.height-at.y-oy)/scale-70,180,150);
            if(roomStation==1 && at.z>0 && dragKind==DragKind.None && GUI.Button(catRect,GUIContent.none,GUIStyle.none)){if(phase==Phase.Greeting)AcceptOrder();else PetAt(pointerGui.y<catRect.y+50?0:1);}
            Vector2 pawAt=ToGui(cat.TransformPoint(new Vector3(-.38f,1.48f,-.65f)));
            if(roomStation==1 && RoomPointVisible(cat.position) && dragKind==DragKind.None && GUI.Button(new Rect(pawAt.x-24,pawAt.y-18,48,36),GUIContent.none,GUIStyle.none))PetAt(2);
            for(int i=0;i<5;i++){Color before=GUI.color;GUI.color=i<guestBook.entries[guestIndex].Hearts?Color.white:new Color(1,1,1,.25f);GUI.DrawTexture(new Rect(35+i*35,30,28,28),heartIcon);GUI.color=before;}
            GUI.DrawTexture(new Rect(35,65,52,52),portraits[guestIndex]);
            // Guest portraits replace ambiguous global unlock stamps.
            if(utilityOpen) for(int i=0;i<CafeGuests.All.Length;i++){Color before=GUI.color;GUI.color=CafeGuests.Unlocked(i,progress.served)?Color.white:new Color(1,1,1,.20f);GUI.DrawTexture(new Rect(790+i*48,25,42,42),portraits[i]);GUI.color=before;}
            if(phase==Phase.Making && (roomStation==2 || roomStation==3))
            {
                GUI.Box(new Rect(240,640,720,115),GUIContent.none,panelStyle);
                if(drinkStation)DrawDrinkTools();
                else
                {
                // Only the tools for the current step are shown.
                if(step==Step.Sponge)
                {
                    for(int i=0;i<3;i++)if(IconButton(new Rect(390+i*140,654,90,72),dessertIcons[dessertKind,i],new[]{"放香草胚","放草莓胚","放可可胚"}[i]))
                    {ChooseBase(i);}
                }
                else if(step==Step.Cream && PreparationActive)DrawPreparationTools();
                else if(step==Step.Cream && BakingActive)DrawBakingTools();
                else if(step==Step.Cream)
                {
                    Rect hold=new Rect(490,650,210,74);
                    GUI.enabled=Time.time>=unmoldUntil || reduceMotion;
                    pipingHeld=HoldButton(hold,dessertKind>=CafeRecipes.Pudding ? pourIcon : pipingIcon) || DirectWorking;GUI.enabled=true;
                    if(creativeMode && IconButton(new Rect(740,654,80,72),nextIcon,"略過奶油／淋醬")){ResetHold();ClearIcing();creamAmount=0;step=Step.Decoration;BuildCake();}
                    if(showLearningLabels)GUI.Label(new Rect(490,727,210,25),dessertKind==0 ? "按住擠奶油" : dessertKind==1 ? "按住填卡士達" : dessertKind==2 ? "按住淋焦糖" : "按住淋楓糖",labelStyle);
                    Color color=GUI.color;
                    GUI.color=new Color(.75f,.81f,.70f);
                    GUI.DrawTexture(new Rect(490,739,210*creamAmount,5),Texture2D.whiteTexture);GUI.color=color;
                }
                else
                {
                    for(int i=0;i<3;i++)DrawFruitSlot(i);
                    if(dessertKind!=CafeRecipes.Cupcake && dessertKind<CafeRecipes.Donut)
                    {
                        GUI.enabled=dessertKind>=CafeRecipes.Pudding || selectedTopping>=0;
                        if(IconButton(new Rect(672,654,56,72),dessertKind==CafeRecipes.Tart ? targetIcon : dessertKind>=3 ? butterIcon : pipingIcon,dessertKind==1 ? "水果花圈" : dessertKind==2 ? "加奶油" : "加奶油塊",dessertKind==1 ? fruitRing : puddingCream))ToggleDessertExtra();
                        GUI.enabled=true;
                    }
                    GUI.enabled=creativeMode || step==Step.Ready;
                    if(IconButton(new Rect(740,654,118,72),creativeMode ? cameraIcon : drink.Ready ? serveIcon : cupIcon,creativeMode ? "拍作品" : drink.Ready ? "送套餐" : "製作飲品"))
                    {if(creativeMode)OpenComposition();else if(drink.Ready)LiftMeal();else SwitchStation(true);}
                    GUI.enabled=true;
                }
                if(step!=Step.Sponge && IconButton(new Rect(265,654,72,72),undoIcon,"退一步"))UndoStep();
                for(int i=0;i<4;i++)
                {
                    Color color=GUI.color;GUI.color=i<=(int)step ? new Color(.75f,.57f,.61f) : new Color(.8f,.8f,.8f,.3f);
                    GUI.DrawTexture(new Rect(555+i*25,610,12,6),Texture2D.whiteTexture);GUI.color=color;
                }
                }
            }
            if(phase==Phase.Served)
            {
                GUI.DrawTexture(new Rect(560,55,80,80),heartIcon);
                GUI.enabled=Time.time>=nextAllowed;
                if(IconButton(new Rect(540,660,120,80),nextIcon,"下一位")){NewVisit();}
                GUI.enabled=true;
            }
            if(utilityOpen && IconButton(new Rect(55,550,80,72),recipeIcon,"食譜／自由創作")){ResetHold();recipeOpen=true;}
            if(photoStatus<0)GUI.DrawTexture(new Rect(905,635,28,28),closeIcon);
            if(IconButton(new Rect(880,675,78,70),cameraIcon,"拍照"))OpenComposition();
            if(utilityOpen && IconButton(new Rect(1010,370,78,70),decorIcon,"佈置咖啡館")){ResetHold();decorOpen=true;}
            if(utilityOpen && IconButton(new Rect(1010,470,78,70),albumIcon,"貓咪圖鑑")){ResetHold();collectionOpen=true;}
            if(roomStation==1 && IconButton(new Rect(55,650,80,80),pawIcon,"摸摸頭"))PetAt(0);
            if(Time.time<puzzledUntil){GUI.color=new Color(1,.72f,.72f);GUI.DrawTexture(new Rect(845,68,48,48),nextIcon);GUI.color=Color.white;}
            DrawRoomNavigation();DrawResidentPets();DrawShopEntrances();
            DrawLifeButtons();
            if(showLearningLabels)DrawInteractionHints();
            DrawQuietMenu();
            GUI.matrix=old;GUI.backgroundColor=Color.white;
        }
        void UpdateCream()
        {
            ApplyIcingPositions();
            int count=Mathf.FloorToInt(creamAmount*creamPieces.Count);
            if(count!=visibleCream)
            {
                for(int i=0;i<creamPieces.Count;i++)creamPieces[i].gameObject.SetActive(i<count);
                visibleCream=count;
            }
            if(pipingBag==null)return;
            pipingBag.gameObject.SetActive(phase==Phase.Making && !RoomInputBlocked && roomStation==2 && step==Step.Cream && pipingHeld);
            float t=creamAmount,a=t*Mathf.PI*6;
            pipingBag.localRotation=Quaternion.Euler(0,0,pipingHeld?Mathf.Clamp((pointerGui.x-595)/12,-16,16):-12);
            pipingBag.localPosition=directHeld ? new Vector3(directPoint.x,dessertKind==0?.60f+creamAmount*.48f:directPoint.y,directPoint.z) : dessertKind==0 ? new Vector3(Mathf.Cos(a)*.28f*(1-t),.60f+t*.48f,Mathf.Sin(a)*.28f*(1-t)) : FillToolPosition(t);
        }
        void UndoStep()
        {
            ResetHold();
            if(PreparationActive){if(preparation.amount==0&&(preparation.stage==CafePreparation.Stage.Press||preparation.stage==CafePreparation.Stage.Mix)){preparation=null;step=Step.Sponge;}else preparation.Undo(dessertKind==CafeRecipes.Pudding);BuildCake();return;}
            if(BakingActive){if(baking.stage==CafeBaking.Stage.Pour&&baking.amount==0){baking=null;step=Step.Sponge;}else baking.Undo();BuildCake();return;}
            if(step==Step.Ready){selectedTopping=-1;fruitRing=false;toppingOffset=Vector3.zero;step=Step.Decoration;}
            else if(step==Step.Decoration){ClearIcing();creamAmount=0;puddingCream=false;step=Step.Cream;}
            else if(step==Step.Cream){ClearIcing();creamAmount=0;puddingCream=false;unmoldUntil=-100;baking=null;preparation=null;step=Step.Sponge;}
            BuildCake();
        }
        IEnumerator CapturePhoto()
        {
            if(capturingPhoto || photoOpen)yield break;
            ResetHold();capturingPhoto=true;photoStarted=Time.time;photoStatus=0;
            if(pipingBag!=null)pipingBag.gameObject.SetActive(false);
            drinkView.Sync(drink,false);
            // A complete new frame is rendered with OnGUI hidden before capture.
            yield return null;
            yield return new WaitForEndOfFrame();
            try
            {
                if(photoSource!=null)Destroy(photoSource);
                photoSource=ScreenCapture.CaptureScreenshotAsTexture();
                if(photoSource==null)throw new System.InvalidOperationException("No screenshot was captured.");
                Reframe(photoStyle);photoOpen=true;
            }
            catch(System.Exception error)
            {
                photoStatus=-1;Debug.LogError("Cafe photo capture failed: "+error.Message);
                ResumePhotoClock();
            }
            finally{capturingPhoto=false;}
        }
        void Reframe(int style)
        {
            var next=CafePhoto.Frame(photoSource,style);
            if(photoPreview!=null)Destroy(photoPreview);
            photoPreview=next;photoStyle=style;photoStatus=0;
        }
        void ResumePhotoClock()
        {
            if(compositionOpen)return;
            float elapsed=Time.time-photoStarted;
            atmosphereClockShift+=elapsed;petUntil+=elapsed;happyUntil+=elapsed;puzzledUntil+=elapsed;nextAllowed+=elapsed;arriveAt+=elapsed;welcomeUntil+=elapsed;servedAt+=elapsed;unmoldUntil+=elapsed;placedAt+=elapsed;
        }
        void ClosePhoto()
        {
            ResumePhotoClock();photoOpen=false;ResetHold();
            if(photoSource!=null)Destroy(photoSource);photoSource=null;
            if(photoPreview!=null)Destroy(photoPreview);photoPreview=null;
        }
        void DrawPhoto()
        {
            Color old=GUI.color;GUI.color=CafeShapes.Hex("#e2d8cc");
            GUI.DrawTexture(new Rect(0,0,1200,800),Texture2D.whiteTexture);GUI.color=old;
            if(photoPreview!=null)GUI.DrawTexture(new Rect(150,35,900,550),photoPreview,ScaleMode.ScaleToFit);
            if(IconButton(new Rect(1090,35,60,55),closeIcon,"返回小店"))ClosePhoto();
            for(int i=0;i<(gifts.Has(2)?4:3);i++)if(IconButton(new Rect(260+i*110,620,95,80),frameIcons[i],new[]{"奶油明信片","玫瑰花邊","鼠尾草格紋","常客金色花框"}[i],photoStyle==i))
            {
                try{Reframe(i);}catch(System.Exception error){photoStatus=-1;Debug.LogError("Cafe frame failed: "+error.Message);}
            }
            if(IconButton(new Rect(760,620,110,80),saveIcon,"儲存 PNG"))
            {
                photoStatus=0;
                try
                {
                    string path=CafePhoto.Save(photoPreview);
                    if(path!=null){photoStatus=1;Debug.Log("Cafe photo saved: "+path);}
                }
                catch(System.Exception error){photoStatus=-1;Debug.LogError("Cafe photo save failed: "+error.Message);}
            }
            if(photoStatus!=0)GUI.DrawTexture(new Rect(910,638,45,45),photoStatus==1 ? heartIcon : closeIcon);
            if(showLearningLabels && photoStatus!=0)
                GUI.Label(new Rect(150,740,900,35),photoStatus==1 ? "已儲存；檔案位置見 Console" : "未能完成，請查看 Console 後重試",labelStyle);
        }
        void DrawDecor()
        {
            // Bottom drawer keeps the room visible while editing; no clicks reach the game behind it.
            GUI.Box(new Rect(175,485,850,295),GUIContent.none,panelStyle);
            GUI.DrawTexture(new Rect(200,500,42,42),decorIcon);
            if(IconButton(new Rect(950,500,48,42),closeIcon,"完成佈置")){decorOpen=false;ResetHold();}
            string[] labels={"櫃檯配色","桌墊","花瓶"};
            for(int category=0;category<3;category++)
            {
                float x=205+category*265;
                if(showLearningLabels)GUI.Label(new Rect(x,540,245,22),labels[category],labelStyle);
                for(int option=0;option<3;option++)
                {
                    Rect r=new Rect(x+option*80,570,70,76);
                    int required=CafeDecor.Required(category,option);
                    bool unlocked=progress.served>=required;
                    GUI.enabled=unlocked;
                    if(IconButton(r,decorChoices[category,option],"",cafeDecor.Selected(category)==option))
                    {
                        if(cafeDecor.Choose(category,option,progress.served))
                        {decorView.Apply(world,counterRenderer,counterColor,cafeDecor);cafeDecor.Save();}
                    }
                    GUI.enabled=true;
                    if(!unlocked)
                    {
                        GUI.DrawTexture(new Rect(r.x+22,r.y+26,28,28),lockIcon);
                        for(int n=0;n<required;n++)
                        {
                            Color old=GUI.color;GUI.color=n<progress.served ? Color.white : new Color(1,1,1,.25f);
                            GUI.DrawTexture(new Rect(r.x+9+(n%3)*18,666+(n/3)*20,16,16),pawIcon);GUI.color=old;
                        }
                    }
                    else if(cafeDecor.Selected(category)==option)GUI.DrawTexture(new Rect(r.x+23,665,24,24),heartIcon);
                }
            }
            if(showLearningLabels)GUI.Label(new Rect(240,735,720,24),"點選立即套用並保存；肉球代表所需完成套餐數",labelStyle);
        }
        void DrawCollection()
        {
            Color before=GUI.color;GUI.color=new Color(.25f,.20f,.23f,.68f);
            GUI.DrawTexture(new Rect(0,0,DesignWidth,DesignHeight),Texture2D.whiteTexture);GUI.color=before;
            GUI.backgroundColor=new Color(1,.95f,.87f);
            GUI.Box(new Rect(145,150,910,490),GUIContent.none,panelStyle);
            GUI.DrawTexture(new Rect(175,170,44,44),albumIcon);
            if(IconButton(new Rect(980,170,48,48),closeIcon,"關閉")){collectionOpen=false;ResetHold();}
            if(IconButton(new Rect(880,170,65,48),nextIcon,"下一頁常客"))guestPage=(guestPage+1)%((CafeGuests.All.Length+2)/3);
            for(int slot=0;slot<3;slot++)
            {
                int i=guestPage*3+slot;if(i>=CafeGuests.All.Length)break;
                float x=185+slot*285;bool unlocked=CafeGuests.Unlocked(i,progress.served);
                bool met=guestBook.entries[i].met;
                GUI.Box(new Rect(x,235,260,350),GUIContent.none,panelStyle);
                before=GUI.color;GUI.color=unlocked ? Color.white : new Color(.50f,.47f,.48f,.65f);
                GUI.DrawTexture(new Rect(x+65,255,130,130),portraits[i]);GUI.color=before;
                if(unlocked && met)
                {
                    for(int h=0;h<5;h++)
                    {
                        before=GUI.color;GUI.color=h<guestBook.entries[i].Hearts ? Color.white : new Color(1,1,1,.22f);
                        GUI.DrawTexture(new Rect(x+46+h*34,405,26,26),heartIcon);GUI.color=before;
                    }
                    GUI.DrawTexture(new Rect(x+45,455,74,74),cakeIcons[CafeGuests.All[i].Flavor]);
                    GUI.DrawTexture(new Rect(x+145,463,60,60),toppingIcons[CafeGuests.All[i].Topping]);
                    if(showLearningLabels)GUI.Label(new Rect(x+10,548,240,24),CafeGuests.All[i].Name,labelStyle);
                }
                else
                {
                    GUI.DrawTexture(new Rect(x+105,405,50,50),unlocked ? nextIcon : lockIcon);
                    int needed=CafeGuests.All[i].UnlockOrders;
                    for(int n=0;n<needed;n++)
                    {
                        before=GUI.color;GUI.color=n<progress.served ? Color.white : new Color(1,1,1,.22f);
                        GUI.DrawTexture(new Rect(x+40+(n%6)*32,475+(n/6)*32,24,24),pawIcon);GUI.color=before;
                    }
                    if(showLearningLabels)GUI.Label(new Rect(x+10,548,240,24),unlocked ? "即將來訪" : "完成套餐解鎖",labelStyle);
                }
            }
        }
        void SwitchStation(bool milk){GoStation(milk?3:2);}
        void ResetHold(){directHeld=false;stirTravel=0;if(prepTool!=null)prepTool.gameObject.SetActive(false);if(prepSpoon!=null)prepSpoon.gameObject.SetActive(false);if(batterJug!=null)batterJug.gameObject.SetActive(false);preparationHeld=false;batterHeld=false;StopRoomInput();CancelInteraction();holdActive=false;pipingHeld=false;drinkHeld=false;releaseHoldRequested=true;}
        void OnApplicationFocus(bool focused){if(!focused){ResetHold();orbitDragging=false;sounds?.Stop();}}
        void OnDisable(){sounds?.Stop();if(toyMode)EndToy();ResetHold();if(capturingPhoto || photoOpen){StopAllCoroutines();capturingPhoto=false;ClosePhoto();}CloseComposition();}
        bool HoldButton(Rect rect, Texture icon)
        {
            if(!GUI.enabled){GUI.Box(rect,icon,buttonStyle);return false;}
            int id=GUIUtility.GetControlID(FocusType.Passive,rect);
            var e=Event.current;
            if(e.type==EventType.Repaint)buttonStyle.Draw(rect,new GUIContent(icon),rect.Contains(pointerGui),holdActive,false,false);
            switch(e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if(e.button==0 && rect.Contains(pointerGui)){GUIUtility.hotControl=id;holdActive=true;e.Use();}
                    break;
                case EventType.MouseDrag:
                    if(GUIUtility.hotControl==id){holdActive=rect.Contains(pointerGui);e.Use();}
                    break;
                case EventType.MouseUp:
                    if(GUIUtility.hotControl==id){GUIUtility.hotControl=0;holdActive=false;e.Use();}
                    break;
            }
            return holdActive && GUIUtility.hotControl==id;
        }
        void DrawDrinkTools()
        {
            if(drink.Current==CafeDrink.Stage.Cup)
            {
                for(int i=0;i<3;i++)if(IconButton(new Rect(420+i*110,570,90,52),cakeIcons[i],new[]{"香草牛奶","草莓牛奶","可可"}[i],drink.Flavor==i))drink.ChooseFlavor(i);
                for(int i=0;i<3;i++)if(IconButton(new Rect(390+i*140,654,90,72),cupChoices[i],"放杯子"))
                {ResetHold();drink.ChooseCup(i);tablewareStyle=i;PlayerPrefs.SetInt("Hana.CatCafe.Plate.v1",i);PlayerPrefs.Save();BuildCake();drinkView.Build(world,drink);ApplySaucer();}
            }
            else if(!drink.Ready)
            {
                bool milk=drink.Current==CafeDrink.Stage.Pour;
                drinkHeld=HoldButton(new Rect(490,650,210,74),milk ? pourIcon : foamIcon);
                if(showLearningLabels)GUI.Label(new Rect(490,727,210,25),milk ? "按住倒香草牛奶" : "按住加奶泡",labelStyle);
                var old=GUI.color;GUI.color=new Color(.75f,.81f,.70f);
                GUI.DrawTexture(new Rect(490,739,210*(milk ? drink.Fill : drink.Foam),5),Texture2D.whiteTexture);GUI.color=old;
            }
            else
            {
                GUI.DrawTexture(new Rect(460,660,64,64),heartIcon);
                if(IconButton(new Rect(580,654,180,72),step==Step.Ready ? serveIcon : dessertIcons[dessertKind,selectedFlavor],step==Step.Ready ? "送套餐" : "繼續做甜點"))
                {if(step==Step.Ready)LiftMeal();else SwitchStation(false);}
            }
            if(drink.Current!=CafeDrink.Stage.Cup && IconButton(new Rect(265,654,72,72),undoIcon,"退一步"))
            {ResetHold();drink.Undo();drinkView.Build(world,drink);ApplySaucer();}
            for(int i=0;i<4;i++)
            {
                Color old=GUI.color;GUI.color=i<=(int)drink.Current ? new Color(.75f,.57f,.61f) : new Color(.8f,.8f,.8f,.3f);
                GUI.DrawTexture(new Rect(555+i*25,610,12,6),Texture2D.whiteTexture);GUI.color=old;
            }
        }
        bool IconButton(Rect r,Texture icon,string label,bool selected=false)
        {
            Color before=GUI.backgroundColor;if(selected)GUI.backgroundColor=new Color(.80f,.63f,.66f);
            bool clicked=GUI.Button(r,icon,buttonStyle);GUI.backgroundColor=before;
            if(showLearningLabels)GUI.Label(new Rect(r.x,r.yMax,r.width,24),label,labelStyle);
            return clicked;
        }
        void BuildIcons()
        {
            targetIcon=MakeIcon(22,0);cameraIcon=MakeIcon(19,0);saveIcon=MakeIcon(20,0);
            frameIcons=new Texture2D[4];for(int i=0;i<4;i++)frameIcons[i]=MakeIcon(21,i);
            decorIcon=MakeIcon(15,0);decorChoices=new Texture2D[3,3];
            for(int category=0;category<3;category++)for(int option=0;option<3;option++)decorChoices[category,option]=MakeIcon(16+category,option);
            portraits=new Texture2D[CafeGuests.All.Length];for(int i=0;i<portraits.Length;i++)portraits[i]=MakeIcon(11,i);
            albumIcon=MakeIcon(12,0);closeIcon=MakeIcon(13,0);lockIcon=MakeIcon(14,0);
            cupChoices=new Texture2D[3];for(int i=0;i<3;i++)cupChoices[i]=MakeIcon(8,i);
            cupIcon=MakeIcon(8,0);pourIcon=MakeIcon(9,0);foamIcon=MakeIcon(10,0);
            cakeIcons=new Texture2D[3];toppingIcons=new Texture2D[3];
            for(int i=0;i<3;i++){cakeIcons[i]=MakeIcon(0,i);toppingIcons[i]=MakeIcon(1,i);}
            pipingIcon=MakeIcon(6,0);undoIcon=MakeIcon(7,0);pawIcon=MakeIcon(2,0);serveIcon=MakeIcon(3,0);nextIcon=MakeIcon(4,0);heartIcon=MakeIcon(5,0);
            recipeIcon=MakeIcon(25,0);dessertIcons=new Texture2D[CafeRecipes.Count,3];
            for(int i=0;i<3;i++){dessertIcons[0,i]=cakeIcons[i];dessertIcons[1,i]=MakeIcon(23,i);dessertIcons[2,i]=MakeIcon(24,i);dessertIcons[3,i]=MakeIcon(32,i);dessertIcons[4,i]=MakeIcon(33,i);dessertIcons[5,i]=MakeIcon(46,i);dessertIcons[6,i]=MakeIcon(47,i);}
        }
        Texture2D MakeIcon(int kind,int variant)
        {
            var t=new Texture2D(96,96,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Bilinear;
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            {
                float u=(x-48)/48f,v=(y-48)/48f;Color c=Color.clear;
                if(kind==26){if(Mathf.Abs(u+.35f)<.16f&&Mathf.Abs(v)<.2f || u>-.25f&&u<.12f&&Mathf.Abs(v)<u+.4f)c=counterColor;float r=Mathf.Sqrt((u-.05f)*(u-.05f)+v*v);if(u>.2f&&Mathf.Abs(r-.5f)<.05f)c=counterColor;}
                if(kind==27){if(Mathf.Abs(u)<.58f&&v>-.55f&&v<.28f)c=CafeShapes.Hex("#d6b3bb");if(Mathf.Abs(u)<.08f&&v>-.55f&&v<.4f || Mathf.Abs(v-.12f)<.065f&&Mathf.Abs(u)<.62f)c=CafeShapes.Hex("#f6ead4");if(Ellipse(u-.2f,v-.48f,.24f,.15f)||Ellipse(u+.2f,v-.48f,.24f,.15f))c=counterColor;}
                if(kind==28){if(Mathf.Abs(u-.25f)<.035f&&v>-.1f&&v<.7f)c=CafeShapes.Hex("#ac9279");if(Mathf.Abs(u-.25f)<.015f&&v>-.35f&&v<0)c=counterColor;if(Ellipse(u-.1f,v+.45f,.22f,.30f)||Ellipse(u-.35f,v+.4f,.12f,.25f))c=CafeShapes.Hex("#c9b7ce");}
                if(kind==29){float r=Mathf.Sqrt(u*u+v*v);if(r>.43f&&r<.53f)c=counterColor;if(Mathf.Abs(u)<.08f&&v>-.25f&&v<.35f || v>-.4f&&v<-.1f&&Mathf.Abs(u)<v+.4f)c=CafeShapes.Hex("#a6b598");}
                if(kind==30){if(Ellipse(u-.3f,v,.3f,.25f)||Ellipse(u+.3f,v,.3f,.25f))c=CafeShapes.Hex("#bd8c9c");if(Ellipse(u,v,.12f,.14f))c=CafeShapes.Hex("#e4cba7");}
                if(kind==31){if(Mathf.Abs(u)<.5f&&v>-.6f&&v<.1f)c=CafeShapes.Hex("#c6ad96");if(v>.1f&&v<.65f&&Mathf.Abs(u)<.7f-v)c=counterColor;if(Mathf.Abs(u)<.15f&&v>-.6f&&v<-.1f)c=CafeShapes.Hex("#f3e7d4");}
                if(kind==46 && Ellipse(u,v,.65f,.42f) && !Ellipse(u,v,.23f,.15f))c=flavors[variant];
                if(kind==47 && Mathf.Abs(u)<.55f && v>-.50f && v<.50f)c=Mathf.Abs(v)<.09f?CafeShapes.Hex("#fff0db"):flavors[variant];
                if(kind==32){for(int k=0;k<3;k++)if(Ellipse(u,v+.42f-k*.24f,.62f,.18f))c=Color.Lerp(flavors[variant],CafeShapes.Hex("#d1a273"),.35f);if(Ellipse(u,v-.22f,.45f,.12f))c=CafeShapes.Hex("#bb8558");if(Mathf.Abs(u)<.14f&&v>.19f&&v<.38f)c=CafeShapes.Hex("#f4d99c");}
                if(kind==33){if(Mathf.Abs(u)<.62f&&Mathf.Abs(v)<.55f)c=Color.Lerp(flavors[variant],CafeShapes.Hex("#d1a273"),.35f);for(int a=-2;a<=2;a++)for(int b=-2;b<=2;b++)if(Mathf.Abs(u-a*.23f)<.075f&&Mathf.Abs(v-b*.21f)<.065f)c=CafeShapes.Hex("#b58e65");}
                if(kind==34&&((Mathf.Abs(v)<.065f&&u<.65f&&u>-.5f)||(u<-.1f&&u>-.6f&&Mathf.Abs(Mathf.Abs(v)-(.6f+u))<.08f)))c=counterColor;
                if(kind==35){if(Mathf.Abs(u)<.48f&&Mathf.Abs(v)<.34f)c=CafeShapes.Hex("#edce88");if(Mathf.Abs(u)<.43f&&v>.04f&&v<.30f)c=CafeShapes.Hex("#f7dfaa");}
                if(kind==36){if(Ellipse(u+.15f,v,.46f,.30f))c=CafeShapes.Hex("#8c8281");if(u>.22f&&u<.75f&&Mathf.Abs(v)<.075f)c=CafeShapes.Hex("#c3a68c");if(Ellipse(u+.15f,v,.32f,.18f))c=CafeShapes.Hex("#d7b381");}
                if(kind==37){if(Ellipse(u,v+.3f,.62f,.18f))c=CafeShapes.Hex("#d6b483");if(Mathf.Abs(u)<.15f&&v>-.12f&&v<.65f)c=counterColor;if(v>-.18f&&v<-.06f&&Mathf.Abs(u)<.4f)c=counterColor;}
                if(kind==38){if(v<.1f&&v>-.55f&&Ellipse(u,v,.6f,.55f))c=CafeShapes.Hex("#c4b3bd");if(Mathf.Abs(u-v*.5f)<.04f&&v>-.2f&&v<.7f)c=CafeShapes.Hex("#b79b7d");}
                if(kind==39){if(v>-.4f&&v<.25f&&Mathf.Abs(u)<.4f-v*.2f)c=CafeShapes.Hex("#d1bc99");if(Mathf.Abs(u)<.045f&&v>.35f&&v<.8f)c=counterColor;if(v>.55f&&v<.8f&&Mathf.Abs(u)<.8f-v)c=counterColor;}
                if(kind==40){float rr=Mathf.Sqrt(u*u+v*v);if(rr<.30f)c=CafeShapes.Hex("#dfbd82");for(int j=0;j<8;j++){float a=j*Mathf.PI/4;if(Ellipse(u-Mathf.Cos(a)*.50f,v-Mathf.Sin(a)*.50f,.055f,.055f))c=CafeShapes.Hex("#dfbd82");}}
                if(kind==41){if(Ellipse(u,v-.2f,.58f,.25f)||Ellipse(u+.2f,v-.4f,.28f,.25f))c=CafeShapes.Hex("#b9c9ce");for(int j=-1;j<=1;j++)if(Mathf.Abs(u-j*.28f)<.035f&&v<-.12f&&v>-.55f)c=CafeShapes.Hex("#91abb5");}
                if(kind==42){if(Ellipse(u,v,.52f,.60f)&&!Ellipse(u-.26f,v-.18f,.44f,.50f))c=CafeShapes.Hex("#bba9c8");}
                if(kind==43){if(Ellipse(u,v,.65f,.4f))c=CafeShapes.Hex("#d5b4be");if(Ellipse(u,v,.08f,.06f))c=CafeShapes.Hex("#f1e2cc");}
                if(kind==44){if(v<0&&v>-.48f&&Mathf.Abs(u)<.55f+v*.3f)c=CafeShapes.Hex("#cbb8a5");if(Ellipse(u,v,.55f,.12f))c=CafeShapes.Hex("#a9c5ce");if(Ellipse(u,v-.5f,.10f,.18f))c=CafeShapes.Hex("#a9c5ce");}
                if(kind==45){if(Ellipse(u,v,.50f,.50f))c=CafeShapes.Hex("#c5a7bd");for(int j=-2;j<=2;j++)if(Ellipse(u,v,.48f,.48f)&&Mathf.Abs(v-u*.5f-j*.17f)<.025f)c=CafeShapes.Hex("#e7d6df");}
                if(kind==0){if(v>-.65f&&v<0&&Mathf.Abs(u)<.45f+v*.15f)c=CafeShapes.Hex("#c5adb7");if(Ellipse(u,v-.12f,.52f,.3f)||Ellipse(u,v-.38f,.32f,.26f))c=flavors[variant];}
                if(kind==1){if(variant==0){if(Ellipse(u,v+.08f,.34f,.37f))c=CafeShapes.Hex("#bb7484");if(v>.23f&&v<.7f&&Mathf.Abs(u-(v-.23f)*.3f)<.04f)c=CafeShapes.Hex("#839171");}if(variant==1&&(Ellipse(u+.23f,v,.27f,.27f)||Ellipse(u-.23f,v,.27f,.27f)||Ellipse(u,v-.32f,.27f,.27f)))c=CafeShapes.Hex("#8593b1");if(variant==2){for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;if(Ellipse(u-Mathf.Cos(a)*.27f,v-Mathf.Sin(a)*.27f,.24f,.24f))c=CafeShapes.Hex("#c8afd0");}if(Ellipse(u,v,.12f,.12f))c=CafeShapes.Hex("#e5c899");}}
                if(kind==2){if(Ellipse(u,v+.2f,.37f,.29f))c=counterColor;for(int j=0;j<4;j++)if(Ellipse(u-(j-1.5f)*.27f,v-.28f+Mathf.Abs(j-1.5f)*.12f,.115f,.15f))c=counterColor;}
                if(kind==3){if(Mathf.Abs(v+.35f)<.035f&&Mathf.Abs(u)<.7f)c=counterColor;if(v>-.3f&&v<.35f&&Ellipse(u,v+.3f,.62f,.65f))c=CafeShapes.Hex("#b5c3a8");if(Ellipse(u,v-.43f,.08f,.08f))c=counterColor;}
                if(kind==4&&((Mathf.Abs(v)<.065f&&u>-.65f&&u<.5f)||(u>.1f&&u<.6f&&Mathf.Abs(Mathf.Abs(v)-(.6f-u))<.08f)))c=counterColor;
                if(kind==5){float a=u*1.5f,b=v*1.5f;float q=a*a+b*b-1;if(q*q*q-a*a*b*b*b<0)c=CafeShapes.Hex("#c58e9d");}
                if(kind==6){if(v>-.3f&&v<.65f&&Mathf.Abs(u)<(v+.5f)*.40f)c=CafeShapes.Hex("#d7b5bd");if(v>-.65f&&v<=-.3f&&Mathf.Abs(u)<.07f)c=CafeShapes.Hex("#87978a");}
                if(kind==7){float radius=Mathf.Sqrt(u*u+v*v);if(radius>.42f&&radius<.53f&&(u>-.2f||v>0))c=counterColor;if(u<-.18f&&u>-.65f&&v>.1f&&v<.45f&&v<.7f+u)c=counterColor;}
                if(kind==8)
                {
                    Color ceramic=CafeShapes.Hex(new[]{"#b6c7ab","#d8afb9","#aebed0"}[variant]);
                    if(Ellipse(u-.45f,v+.05f,.26f,.30f)&&!Ellipse(u-.45f,v+.05f,.15f,.19f))c=ceramic;
                    if(v>-.5f&&v<.35f&&Mathf.Abs(u)<.43f)c=ceramic;
                    if(Ellipse(u,v-.33f,.43f,.12f))c=CafeShapes.Hex("#fff0db");
                    if(Ellipse(u,v+.58f,.62f,.055f))c=ceramic;
                }
                if(kind==9)
                {
                    if(u>-.30f&&u<.35f&&v>-.15f&&v<.60f)c=counterColor;
                    if(u>-.52f&&u<-.20f&&v>.25f&&v<.43f)c=counterColor;
                    if(Mathf.Abs(u+.47f)<.045f&&v>-.65f&&v<.25f)c=CafeShapes.Hex("#d8bb8e");
                }
                if(kind==10)
                {
                    if(Ellipse(u,v+.10f,.6f,.3f)||Ellipse(u+.28f,v-.15f,.3f,.3f)||Ellipse(u-.18f,v-.24f,.35f,.35f))c=CafeShapes.Hex("#e9d3b6");
                    if(Ellipse(u,v+.1f,.15f,.11f))c=counterColor;
                    for(int j=-1;j<=1;j++)if(Ellipse(u-j*.18f,v-.15f,.06f,.08f))c=counterColor;
                }
                if(kind==11)
                {
                    Color fur=CafeShapes.Hex(CafeGuests.All[variant].Fur), ink=CafeShapes.Hex("#51484b");
                    if(Ellipse(u,v+.08f,.65f,.52f)||Ellipse(u+.43f,v-.43f,.20f,.38f)||Ellipse(u-.43f,v-.43f,.20f,.38f))c=fur;
                    if(variant==3&&Ellipse(u+.43f,v-.43f,.15f,.27f))c=CafeShapes.Hex("#766572");
                    if(variant==3&&Ellipse(u-.4f,v-.13f,.18f,.22f))c=CafeShapes.Hex("#c79b74");
                    if(variant==5&&Ellipse(u,v+.32f,.30f,.24f))c=CafeShapes.Hex("#fff0dc");
                    if(variant==6&&Ellipse(u,v+.15f,.28f,.18f))c=CafeShapes.Hex("#9d8177");
                    if(variant==4&&Ellipse(u,v+.35f,.30f,.18f))c=CafeShapes.Hex("#fff0dc");
                    if(Ellipse(u+.24f,v,.035f,variant==1 ? .035f : .065f)||Ellipse(u-.24f,v,.035f,variant==1 ? .035f : .065f))c=ink;
                    if(Ellipse(u,v+.20f,.06f,.04f))c=CafeShapes.Hex("#bf939a");
                    if(Ellipse(u,v+.58f,.25f,.07f))c=CafeShapes.Hex(CafeGuests.All[variant].Ribbon);
                }
                if(kind==12)
                {
                    if(Mathf.Abs(u)<.65f&&Mathf.Abs(v)<.60f)c=CafeShapes.Hex("#c0c9b2");
                    if(Mathf.Abs(u+.40f)<.025f&&Mathf.Abs(v)<.60f)c=CafeShapes.Hex("#8b9c7a");
                    if(Ellipse(u-.10f,v+.13f,.19f,.16f))c=CafeShapes.Hex("#f4e4ce");
                    for(int j=-1;j<=1;j++)if(Ellipse(u-.10f-j*.18f,v-.18f,.065f,.08f))c=CafeShapes.Hex("#f4e4ce");
                }
                if(kind==13&&Mathf.Abs(u)<.5f&&Mathf.Abs(v)<.5f&&(Mathf.Abs(u-v)<.07f||Mathf.Abs(u+v)<.07f))c=counterColor;
                if(kind==14)
                {
                    if(Mathf.Abs(u)<.4f&&v>-.5f&&v<.13f)c=CafeShapes.Hex("#b6ada8");
                    if(v>.10f&&Ellipse(u,v-.2f,.28f,.32f)&&!Ellipse(u,v-.2f,.18f,.22f))c=CafeShapes.Hex("#b6ada8");
                    if(Ellipse(u,v+.14f,.045f,.08f))c=CafeShapes.Hex("#f2e5cf");
                }
                if(kind==15)
                {
                    if(Ellipse(u,v,.66f,.53f)&&!Ellipse(u-.37f,v+.10f,.19f,.22f))c=CafeShapes.Hex("#dcc9ae");
                    for(int j=0;j<3;j++)if(Ellipse(u+.35f-j*.28f,v-.24f,.10f,.10f))c=CafeShapes.Hex(new[]{"#cfa7b4","#a9bc9c","#b2bed3"}[j]);
                }
                if(kind==16)
                {
                    Color swatch=variant==0 ? counterColor : CafeShapes.Hex(variant==1 ? "#b7c5ae" : "#bcc5d7");
                    if(Mathf.Abs(u)<.65f&&v>-.5f&&v<.2f)c=swatch;
                    if(Mathf.Abs(u)<.73f&&v>=.2f&&v<.32f)c=CafeShapes.Hex("#e8d8bf");
                }
                if(kind==17&&variant>0)
                {
                    if(variant==1&&Mathf.Abs(u)<.65f&&Mathf.Abs(v)<.5f)c=((x/12+y/12)%2==0) ? CafeShapes.Hex("#d8b7be") : CafeShapes.Hex("#f1e3ce");
                    if(variant==2)
                    {
                        if(Ellipse(u,v,.55f,.46f))c=CafeShapes.Hex("#e1cdaa");
                        for(int j=0;j<12;j++){float a=j*Mathf.PI*2/12;if(Ellipse(u-Mathf.Cos(a)*.53f,v-Mathf.Sin(a)*.43f,.12f,.12f))c=CafeShapes.Hex("#e1cdaa");}
                    }
                }
                if(kind==18&&variant>0)
                {
                    if(Ellipse(u,v+.35f,.24f,.28f))c=CafeShapes.Hex(variant==1 ? "#c7b6ce" : "#b5c8a9");
                    if(Mathf.Abs(u)<.025f&&v>-.14f&&v<.48f)c=CafeShapes.Hex("#95a77f");
                    if(variant==1)
                    {
                        for(int j=0;j<7;j++){float a=j*Mathf.PI*2/7;if(Ellipse(u-Mathf.Cos(a)*.18f,v-.40f-Mathf.Sin(a)*.18f,.14f,.14f))c=CafeShapes.Hex("#e6d6b8");}
                        if(Ellipse(u,v-.40f,.10f,.10f))c=CafeShapes.Hex("#cdb076");
                    }
                    else if(Ellipse(u,v-.40f,.23f,.28f))c=CafeShapes.Hex("#d3a4b7");
                }
                if((kind==17||kind==18)&&variant==0)
                {
                    float radius=Mathf.Sqrt(u*u+v*v);
                    if((radius>.43f&&radius<.51f)||(Mathf.Abs(u-v)<.045f&&Mathf.Abs(u)<.35f))c=CafeShapes.Hex("#b9aaa2");
                }
                if(kind==19)
                {
                    if(Mathf.Abs(u)<.68f&&Mathf.Abs(v)<.4f)c=CafeShapes.Hex("#c1b5c8");
                    if(u>-.4f&&u<-.05f&&v>.4f&&v<.55f)c=CafeShapes.Hex("#c1b5c8");
                    if(Ellipse(u,v,.29f,.29f))c=CafeShapes.Hex("#f3e6cf");
                    if(Ellipse(u,v,.19f,.19f))c=CafeShapes.Hex("#8f9d94");
                }
                if(kind==20)
                {
                    if(Mathf.Abs(u)<.055f&&v>-.2f&&v<.6f)c=counterColor;
                    if(v<.15f&&v>-.3f&&Mathf.Abs(Mathf.Abs(u)-(v+.3f))<.055f)c=counterColor;
                    if(v>-.6f&&v<-.52f&&Mathf.Abs(u)<.6f)c=counterColor;
                    if(Mathf.Abs(u)>.52f&&Mathf.Abs(u)<.6f&&v>-.6f&&v<-.32f)c=counterColor;
                }
                if(kind==21)
                {
                    if(Mathf.Abs(u)<.67f&&Mathf.Abs(v)<.72f)c=CafeShapes.Hex(new[]{"#ead8b9","#d8b2c1","#b5c4a7","#ba9c68"}[variant]);
                    if(Mathf.Abs(u)<.49f&&v<.52f&&v>(variant==0 ? -.25f : -.52f))c=CafeShapes.Hex("#f7efdf");
                }
                if(kind==22)
                {
                    float radius=Mathf.Sqrt(u*u+v*v);
                    if(radius>.80f&&radius<.88f)c=CafeShapes.Hex("#a9bf98");
                    if(radius>.72f&&radius<.75f)c=CafeShapes.Hex("#e8e1c8");
                }
                if(kind==23)
                {
                    if(Ellipse(u,v+.15f,.65f,.34f))c=CafeShapes.Hex("#d1aa7b");
                    if(Ellipse(u,v-.02f,.53f,.23f))c=flavors[variant];
                    for(int j=0;j<8;j++){float a=j*Mathf.PI*2/8;if(Ellipse(u-Mathf.Cos(a)*.40f,v-.02f-Mathf.Sin(a)*.15f,.06f,.06f))c=CafeShapes.Hex("#ba7d8b");}
                }
                if(kind==24)
                {
                    if(Ellipse(u,v+.50f,.67f,.08f))c=CafeShapes.Hex("#b7c4ab");
                    if(v>-.43f&&v<.36f&&Mathf.Abs(u)<.48f-(v+.43f)*.14f)c=flavors[variant];
                    if(Ellipse(u,v-.35f,.38f,.10f))c=CafeShapes.Hex("#ae7c5e");
                }
                if(kind==25)
                {
                    if(Mathf.Abs(u)<.72f&&Mathf.Abs(v)<.55f)c=CafeShapes.Hex("#d5b0be");
                    if(Mathf.Abs(u)<.64f&&Mathf.Abs(v)<.46f)c=CafeShapes.Hex("#f1e4ce");
                    if(Mathf.Abs(u)<.025f&&Mathf.Abs(v)<.5f)c=CafeShapes.Hex("#c8a2b1");
                    if(Ellipse(u+.32f,v,.18f,.16f)||Ellipse(u-.32f,v,.18f,.16f))c=CafeShapes.Hex("#b9c6aa");
                }
                t.SetPixel(x,y,c);
            }
            t.Apply();textures.Add(t);return t;
        }
        static bool Ellipse(float x,float y,float rx,float ry)=>x*x/(rx*rx)+y*y/(ry*ry)<1;
        void OnDestroy(){sounds?.Dispose();if(photoSource!=null)Destroy(photoSource);if(photoPreview!=null)Destroy(photoPreview);decorView?.Dispose();drinkView?.Dispose();foreach(var t in textures)if(t!=null)Destroy(t);art?.Dispose();}
    }
}
