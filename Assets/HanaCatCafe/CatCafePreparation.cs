using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        CafePreparation preparation;
        bool preparationHeld;
        Transform preparationRig,prepSurface,prepSpoon,prepTool;
        Texture2D pressIcon,mixIcon,unmoldIcon;
        bool PreparationActive=>step==Step.Cream && preparation!=null && preparation.stage!=CafePreparation.Stage.Done;
        void BuildPreparationRig()
        {
            preparationRig=art.Group(cake,"Pastry preparation",Vector3.zero);prepSpoon=null;prepTool=null;
            bool tart=dessertKind==CafeRecipes.Tart;
            var st=preparation.stage;
            float radius=tart?.56f:.40f;
            Color ceramic=CafeShapes.Hex("#c5b5be");
            art.Disc(preparationRig,"Bowl bottom",new Vector3(0,.08f,0),radius,.08f,ceramic);
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32;
                art.Ball(preparationRig,"Bowl rim",new Vector3(Mathf.Cos(a)*radius,tart?.20f:.30f,Mathf.Sin(a)*radius),new Vector3(.09f,tart?.22f:.44f,.09f),ceramic);
            }
            prepSurface=art.Disc(preparationRig,tart?"Soft dough":"Custard mixture",new Vector3(0,tart?.16f:.20f,0),tart?.25f:.32f,tart?.22f:.12f,flavors[selectedFlavor]);
            if(st==CafePreparation.Stage.Mix)
            {
                prepSpoon=art.Group(preparationRig,"Mixing spoon",new Vector3(0,.4f,0));
                art.Box(prepSpoon,"Spoon handle",new Vector3(.2f,.25f,0),new Vector3(.045f,.60f,.045f),CafeShapes.Hex("#c2a284"));
                art.Ball(prepSpoon,"Spoon bowl",new Vector3(.2f,-.06f,0),new Vector3(.13f,.16f,.08f),CafeShapes.Hex("#c2a284"));
            }
            if(st==CafePreparation.Stage.Press)
            {
                prepTool=art.Group(preparationRig,"Dough press",Vector3.zero);
                art.Disc(prepTool,"Press foot",Vector3.zero,.16f,.05f,CafeShapes.Hex("#c7a481"));
                art.Disc(prepTool,"Press handle",Vector3.up*.16f,.055f,.30f,CafeShapes.Hex("#c7a481"));
            }
            else if(st==CafePreparation.Stage.Pour)prepTool=MakePourTool(preparationRig);
            UpdatePreparationView();
        }
        void UpdatePreparationView()
        {
            if(!PreparationActive||prepSurface==null)return;
            float t=preparation.amount;
            if(preparation.stage==CafePreparation.Stage.Press)
            {
                float radius=Mathf.Lerp(.25f,.51f,t),height=Mathf.Lerp(.22f,.07f,t);
                prepSurface.localScale=new Vector3(radius*2,height/2,radius*2);
                prepSurface.localPosition=new Vector3(0,.12f+height/2,0);
            }
            if(preparation.stage==CafePreparation.Stage.Pour)
            {
                float height=.04f+t*.32f;
                prepSurface.localScale=new Vector3(.64f,height/2,.64f);
                prepSurface.localPosition=new Vector3(0,.1f+height/2,0);
            }
            if(preparation.stage==CafePreparation.Stage.Set||preparation.stage==CafePreparation.Stage.Unmold)
            {prepSurface.localScale=new Vector3(.64f,.18f,.64f);prepSurface.localPosition=new Vector3(0,.28f,0);}
            if(prepTool!=null)
            {
                prepTool.gameObject.SetActive(preparationHeld && roomStation==2 && !RoomInputBlocked);
                float a=t*Mathf.PI*4;
                prepTool.localPosition=preparation.stage==CafePreparation.Stage.Press?(directHeld?new Vector3(directPoint.x,.22f,directPoint.z):new Vector3(Mathf.Cos(a)*.15f,.22f,Mathf.Sin(a)*.15f)):(directHeld?new Vector3(directPoint.x,.55f,directPoint.z):new Vector3(0,.55f,0));
            }
            if(prepSpoon!=null)prepSpoon.gameObject.SetActive(preparationHeld && roomStation==2 && !RoomInputBlocked);
            if(prepSpoon!=null){prepSpoon.localPosition=directHeld?new Vector3(directPoint.x-.2f,.4f,directPoint.z):new Vector3(0,.4f,0);prepSpoon.localRotation=Quaternion.Euler(0,directHeld||reduceMotion?0:t*720,0);}
        }
        void TickPreparation()
        {
            if(!PreparationActive)return;
            var before=preparation.stage;
            if(phase==Phase.Making&&roomStation==2&&!RoomInputBlocked&&Application.isFocused)preparation.Tick(Time.deltaTime,preparationHeld);
            if(before!=preparation.stage){ResetHold();sounds?.Play(0);BuildCake();}
            UpdatePreparationView();
        }
        void DrawPreparationTools()
        {
            var st=preparation.stage;
            if(st==CafePreparation.Stage.Unmold)
            {
                if(IconButton(new Rect(490,650,210,74),unmoldIcon,"脫模"))
                {ResetHold();preparation.Unmold();unmoldUntil=Time.time+.85f;sounds?.Play(0);BuildCake();}
            }
            else if(st==CafePreparation.Stage.Set)GUI.DrawTexture(new Rect(565,654,70,70),dessertIcons[2,selectedFlavor]);
            else preparationHeld=HoldButton(new Rect(490,650,210,74),st==CafePreparation.Stage.Press?pressIcon:st==CafePreparation.Stage.Mix?mixIcon:pourIcon) || DirectWorking;
            Color old=GUI.color;GUI.color=CafeShapes.Hex("#b6c5a7");GUI.DrawTexture(new Rect(490,739,210*preparation.amount,5),Texture2D.whiteTexture);GUI.color=old;
            if(showLearningLabels)GUI.Label(new Rect(420,727,360,25),st==CafePreparation.Stage.Press?"按住壓塔皮":st==CafePreparation.Stage.Mix?"按住攪拌布丁液":st==CafePreparation.Stage.Pour?"放開後再按住倒入模具":st==CafePreparation.Stage.Set?"等待凝固；離開工作檯會暫停":"點一下脫模",labelStyle);
        }
    }
}
