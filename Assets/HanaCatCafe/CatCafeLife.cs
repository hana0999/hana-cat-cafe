using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        CafeSounds sounds;
        CafeGifts gifts;
        Texture2D soundIcon,giftIcon,toyIcon,angleIcon,ribbonIcon;
        bool giftOpen,toyMode,compositionOpen,orbitDragging;
        int cameraPreset,petZone;
        float orbitYaw,orbitPitch=32,photoZoom=1,compositionStarted,petUntil,lastPurr=-10;
        Vector3 cameraBefore;
        Quaternion rotationBefore;
        float sizeBefore;
        Vector2 orbitLast;
        Transform toy,playPaw,giftVase;
        Vector3 pawRest;

        void InitLife()
        {
            gifts=CafeGifts.Load();sounds=new CafeSounds(gameObject);
            soundIcon=MakeIcon(26,0);giftIcon=MakeIcon(27,0);toyIcon=MakeIcon(28,0);angleIcon=MakeIcon(29,0);ribbonIcon=MakeIcon(30,0);
            ApplyGifts();
        }
        void ApplyGifts()
        {
            if(gifts==null)return;
            var ribbon=cat.Find("Ribbon");
            if(ribbon!=null)foreach(var render in ribbon.GetComponentsInChildren<Renderer>())
                render.sharedMaterial=art.Material(CafeShapes.Hex(gifts.ribbon?"#bd8c9c":guest.Ribbon));
            if(giftVase!=null){giftVase.gameObject.SetActive(false);Destroy(giftVase.gameObject);giftVase=null;}
            if(gifts.flowers)
            {
                giftVase=art.Group(world,"Gift rose vase",new Vector3(2.95f,1.45f,.12f));
                art.Ball(giftVase,"Ceramic vase",new Vector3(0,.17f,0),new Vector3(.27f,.34f,.27f),CafeShapes.Hex("#d2b9c9"));
                for(int j=0;j<3;j++)
                {
                    float x=(j-1)*.12f;
                    art.Box(giftVase,"Stem",new Vector3(x,.40f,0),new Vector3(.018f,.4f,.018f),CafeShapes.Hex("#9aa885"));
                    for(int k=0;k<5;k++)
                    {
                        float a=k*Mathf.PI*2/5;
                        art.Ball(giftVase,"Petal",new Vector3(x+Mathf.Cos(a)*.065f,.62f+Mathf.Sin(a)*.065f,0),new Vector3(.13f,.13f,.09f),CafeShapes.Hex("#dcadb6"));
                    }
                }
            }
        }
        void PetAt(int zone)
        {
            petZone=zone;petUntil=Time.time+1.6f;Pet();
            if(Time.time-lastPurr>.6f){sounds.Play(1);lastPurr=Time.time;}
        }
        void BeginToy()
        {
            GoStation(1);ResetHold();toyMode=true;
            playPaw=cat.Find("Paw");if(playPaw!=null)pawRest=playPaw.localPosition;
            toy=art.Group(world,"Feather teaser",cat.position+new Vector3(.6f,2,-.9f));
            art.Box(toy,"Wand",new Vector3(.24f,.48f,0),new Vector3(.035f,.75f,.035f),CafeShapes.Hex("#bca487"));
            art.Box(toy,"String",new Vector3(.24f,.02f,0),new Vector3(.012f,.2f,.012f),CafeShapes.Hex("#ddd2bd"));
            for(int i=0;i<3;i++)
            {
                var feather=art.Ball(toy,"Soft feather",new Vector3(.18f+i*.06f,-.17f,0),new Vector3(.10f,.32f,.06f),flavors[i]);
                feather.localRotation=Quaternion.Euler(0,0,(i-1)*25);
            }
        }
        void EndToy()
        {
            toyMode=false;if(toy!=null){toy.gameObject.SetActive(false);Destroy(toy.gameObject);toy=null;}
            if(playPaw!=null)playPaw.localPosition=pawRest;
            ResetHold();
        }
        void UpdateLife()
        {
            sounds?.Flow(Application.isFocused && !giftOpen && !toyMode && !displayOpen && !careOpen && !weatherOpen && !recipeOpen && !decorOpen && !collectionOpen && (pipingHeld || drinkHeld || batterHeld || preparationHeld));
            if(petUntil>Time.time && !reduceMotion)
            {
                float t=(petUntil-Time.time)/1.6f;
                head.localRotation*=Quaternion.Euler(petZone==0?-8*t:0,0,petZone==1?16*t:0);
                if(petZone==2 && playPaw!=null)playPaw.localPosition=pawRest+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.16f);
            }
            else if(!toyMode && playPaw!=null)playPaw.localPosition=pawRest;
            if(!toyMode || toy==null)return;
            Vector2 p=guiOffset+pointerGui*guiScale;
            var ray=view.ScreenPointToRay(new Vector3(p.x,Screen.height-p.y,0));
            var plane=new Plane(Vector3.forward,new Vector3(0,0,-.4f));
            if(Application.isFocused && plane.Raycast(ray,out float distance))
            {
                Vector3 target=ray.GetPoint(distance);
                target.x=Mathf.Clamp(target.x,cat.position.x-.8f,cat.position.x+.8f);
                target.y=Mathf.Clamp(target.y,1.6f,2.8f);target.z=-.4f;
                toy.position=Vector3.Lerp(toy.position,target,reduceMotion?1:1-Mathf.Exp(-8*Time.deltaTime));
            }
            Vector3 delta=toy.position-head.position;
            head.localRotation*=Quaternion.Euler(Mathf.Clamp(-delta.y*12,-12,12),Mathf.Clamp(delta.x*25,-22,22),0);
            if(playPaw!=null && !reduceMotion)playPaw.localPosition=pawRest+new Vector3(0,.12f+Mathf.Max(0,Mathf.Sin(Time.time*4))*.15f,-.12f);
            // Play is expressive only; it never farms affection or completed orders.
        }
        void DrawLifeButtons()
        {
            if(roomStation==1 && IconButton(new Rect(55,450,80,72),toyIcon,"逗貓"))BeginToy();
            if(utilityOpen && IconButton(new Rect(55,350,80,72),giftIcon,"常客禮物")){ResetHold();giftOpen=true;}
            bool pending=false;for(int i=0;i<3;i++)pending|=gifts.Available(i,guestBook);
            if(pending && utilityOpen)GUI.DrawTexture(new Rect(114,342,25,25),heartIcon);
            if(utilityOpen && IconButton(new Rect(1120,100,52,52),soundIcon,"聲音",!sounds.Muted))sounds.SetMuted(!sounds.Muted);
            if(utilityOpen && sounds.Muted)GUI.DrawTexture(new Rect(1135,115,22,22),closeIcon);
        }
        void DrawToy()
        {
            if(IconButton(new Rect(1090,35,65,55),closeIcon,"放下逗貓棒"))EndToy();
            if(IconButton(new Rect(55,650,80,80),pawIcon,"摸摸肉球"))
            {petZone=2;PetAt(2);}
            if(showLearningLabels)GUI.Label(new Rect(260,700,680,45),"移動滑鼠，讓小貓追看羽毛；叉號返回製作",labelStyle);
        }
        void DrawGifts()
        {
            GUI.Box(new Rect(150,160,900,450),GUIContent.none,panelStyle);
            if(IconButton(new Rect(965,175,60,48),closeIcon,"關閉"))giftOpen=false;
            for(int i=0;i<3;i++)
            {
                float x=230+i*270;
                GUI.DrawTexture(new Rect(x+30,235,80,80),portraits[i]);
                Texture icon=i==0?MakeGiftRibbonIcon():i==1?decorChoices[2,1]:frameIcons[3];
                bool available=gifts.Available(i,guestBook),owned=gifts.Has(i);
                GUI.enabled=available||owned;
                if(IconButton(new Rect(x,340,145,120),owned?icon:giftIcon,new[]{"蝴蝶結","小花瓶","金色相框"}[i],owned&&(i==0?gifts.ribbon:i==1?gifts.flowers:true)))
                {
                    if(gifts.Claim(i,guestBook))sounds.Play(2);
                    else if(i==0)gifts.ribbon=!gifts.ribbon;
                    else if(i==1)gifts.flowers=!gifts.flowers;
                    gifts.Save();ApplyGifts();
                }
                GUI.enabled=true;
                if(!owned&&!available)GUI.DrawTexture(new Rect(x+55,382,35,35),lockIcon);
                for(int h=0;h<2;h++)
                {
                    Color old=GUI.color;GUI.color=guestBook.entries[i].Hearts>h?Color.white:new Color(1,1,1,.25f);
                    GUI.DrawTexture(new Rect(x+40+h*36,490,28,28),heartIcon);GUI.color=old;
                }
                if(showLearningLabels)GUI.Label(new Rect(x-35,540,220,35),owned?"已領取；點選穿戴／擺設":"兩顆心可拆禮物",labelStyle);
            }
        }
        Texture MakeGiftRibbonIcon()=>ribbonIcon;

        void OpenComposition()
        {
            if(compositionOpen||photoOpen||capturingPhoto)return;
            if(carryingTray)ReturnMeal();ResetHold();sounds.Stop();compositionOpen=true;compositionStarted=Time.time;
            photoWasOrthographic=view.orthographic;photoFieldBefore=view.fieldOfView;view.orthographic=true;
            cameraBefore=view.transform.position;rotationBefore=view.transform.rotation;sizeBefore=view.orthographicSize;
            if(pipingBag!=null)pipingBag.gameObject.SetActive(false);drinkView.Sync(drink,false);
            SavePhotoPoses();photoScene=world.GetComponentsInChildren<Renderer>();SetCameraPreset(phase==Phase.Greeting?2:0);
        }
        void SetCameraPreset(int preset)
        {
            cameraPreset=preset;PoseForPhoto();orbitYaw=0;orbitPitch=preset==1?78:preset==2?20:32;photoZoom=1;ApplyPhotoCamera();
        }
        void ApplyPhotoCamera()
        {
            FramePhotoSubjects();
        }
        void CloseComposition()
        {
            if(!compositionOpen)return;
            RestorePhotoVisibility();RestorePhotoPoses();photoPosesSaved=false;photoScene=null;compositionOpen=false;orbitDragging=false;
            photoStarted=compositionStarted;ResumePhotoClock();
            view.orthographic=photoWasOrthographic;view.fieldOfView=photoFieldBefore;
            view.transform.SetPositionAndRotation(cameraBefore,rotationBefore);view.orthographicSize=sizeBefore;ResetHold();
        }
        void DrawComposition()
        {
            if(IconButton(new Rect(1090,35,65,55),closeIcon,"返回製作")){CloseComposition();return;}
            for(int i=0;i<3;i++)
                if(IconButton(new Rect(365+i*110,660,90,72),i==0?dessertIcons[dessertKind,selectedFlavor]:i==1?angleIcon:portraits[guestIndex],new[]{"甜點特寫","俯視","貓咪合照"}[i],cameraPreset==i))SetCameraPreset(i);
            if(IconButton(new Rect(750,655,100,80),cameraIcon,"快門")){sounds.Play(0);StartCoroutine(CapturePhoto());}
            var e=Event.current;int id=GUIUtility.GetControlID("Photo orbit".GetHashCode(),FocusType.Passive);
            Rect area=new Rect(150,110,900,510);
            if(e.type==EventType.MouseDown&&e.button==0&&area.Contains(pointerGui)&&GUIUtility.hotControl==0)
            {GUIUtility.hotControl=id;orbitDragging=true;orbitLast=pointerGui;e.Use();}
            if(e.type==EventType.MouseDrag&&GUIUtility.hotControl==id)
            {
                Vector2 delta=pointerGui-orbitLast;orbitLast=pointerGui;
                orbitYaw=Mathf.Clamp(orbitYaw+delta.x*.25f,-65,65);orbitPitch=Mathf.Clamp(orbitPitch-delta.y*.18f,12,85);ApplyPhotoCamera();e.Use();
            }
            if(e.type==EventType.MouseUp&&GUIUtility.hotControl==id){orbitDragging=false;GUIUtility.hotControl=0;e.Use();}
            if(e.type==EventType.ScrollWheel&&area.Contains(pointerGui))
            {photoZoom=Mathf.Clamp(photoZoom+e.delta.y*.04f,1f,1.6f);ApplyPhotoCamera();e.Use();}
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){GUIUtility.hotControl=0;CloseComposition();e.Use();}
            if(showLearningLabels)GUI.Label(new Rect(200,745,800,35),"拖曳調整角度，滾輪縮放；快門後可選相框",labelStyle);
        }
    }
}
