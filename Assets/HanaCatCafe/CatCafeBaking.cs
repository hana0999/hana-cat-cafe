using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        CafeBaking baking;
        bool batterHeld;
        Transform bakingRig,batterSurface,ironLid,batterJug;
        Renderer batterRenderer;
        Texture2D panIcon;
        bool BakingActive=>dessertKind>=CafeRecipes.Pancakes && step==Step.Cream && baking!=null && baking.stage!=CafeBaking.Stage.Done;
        void BuildBakingRig()
        {
            if(bakingRig!=null){bakingRig.gameObject.SetActive(false);Destroy(bakingRig.gameObject);}
            bakingRig=null;batterSurface=null;ironLid=null;
            if(!BakingActive)return;
            bakingRig=art.Group(cake,"Cooking station",Vector3.zero);
            Color iron=CafeShapes.Hex("#8c8281");
            art.Disc(bakingRig,"Pan",new Vector3(0,.12f,0),.60f,.11f,iron);
            art.Box(bakingRig,"Pan handle",new Vector3(.77f,.12f,0),new Vector3(.48f,.08f,.13f),CafeShapes.Hex("#c3a68c"));
            batterSurface=art.Disc(bakingRig,"Batter",new Vector3(0,.20f,0),.49f,.07f,flavors[selectedFlavor]);
            batterRenderer=batterSurface.GetComponent<Renderer>();
            if(baking.waffle)
            {
                ironLid=art.Group(bakingRig,"Waffle iron hinge",new Vector3(0,.16f,.54f));
                art.Box(ironLid,"Waffle iron lid",new Vector3(0,.03f,-.54f),new Vector3(1.15f,.10f,1.1f),iron);
                art.Ball(ironLid,"Iron handle",new Vector3(0,.13f,-.9f),new Vector3(.36f,.10f,.10f),CafeShapes.Hex("#c3a68c"));
            }
            batterJug=MakePourTool(bakingRig);batterJug.localPosition=new Vector3(0,.40f,0);
            UpdateBakingView();
        }
        void TickBaking()
        {
            if(!BakingActive)return;
            bool active=phase==Phase.Making && roomStation==2 && !RoomInputBlocked && Application.isFocused;
            var before=baking.stage;
            if(active)baking.Tick(Time.deltaTime,batterHeld);
            if(before!=baking.stage)
            {
                ResetHold();sounds?.Play(0);
                if(baking.stage==CafeBaking.Stage.Done){BuildCake();return;}
            }
            UpdateBakingView();
        }
        void UpdateBakingView()
        {
            if(!BakingActive||batterSurface==null)return;
            if(batterJug!=null)batterJug.gameObject.SetActive(batterHeld && baking.stage==CafeBaking.Stage.Pour && roomStation==2 && !RoomInputBlocked);
            if(batterJug!=null)batterJug.localPosition=directHeld?new Vector3(directPoint.x,.40f,directPoint.z):new Vector3(0,.40f,0);
            float size=baking.stage==CafeBaking.Stage.Pour?Mathf.Max(.03f,baking.amount):1;
            batterSurface.localScale=new Vector3(.98f*size,.035f,.98f*size);
            // A fixed set of materials avoids allocating one material per animation frame.
            int shade=baking.stage==CafeBaking.Stage.Pour?0:baking.stage==CafeBaking.Stage.FirstSide?Mathf.FloorToInt(baking.amount*4):4+Mathf.FloorToInt(baking.amount*4);
            batterRenderer.sharedMaterial=art.Material(Color.Lerp(flavors[selectedFlavor],CafeShapes.Hex("#c59a67"),shade/8f));
            if(!baking.waffle && baking.stage==CafeBaking.Stage.SecondSide)
            {
                float flip=Mathf.Clamp01(baking.amount*4);
                batterSurface.localPosition=new Vector3(0,.20f+(reduceMotion?0:Mathf.Sin(flip*Mathf.PI)*.5f),0);
                batterSurface.localRotation=Quaternion.Euler(0,0,reduceMotion?0:flip*180);
            }
            if(ironLid!=null)ironLid.localRotation=Quaternion.Euler(baking.stage==CafeBaking.Stage.Pour||baking.stage==CafeBaking.Stage.Turn?-105:0,0,0);
        }
        void DrawBakingTools()
        {
            if(baking.stage==CafeBaking.Stage.Pour)
                batterHeld=HoldButton(new Rect(490,650,210,74),pourIcon) || DirectWorking;
            else if(baking.stage==CafeBaking.Stage.Turn)
            {
                if(IconButton(new Rect(490,650,210,74),baking.waffle?nextIcon:panIcon,baking.waffle?"合蓋再烤":"翻面"))
                {ResetHold();baking.Turn();sounds?.Play(0);}
            }
            else GUI.DrawTexture(new Rect(565,654,70,70),panIcon);
            Color old=GUI.color;GUI.color=CafeShapes.Hex("#b6c5a7");GUI.DrawTexture(new Rect(490,739,210*baking.amount,5),Texture2D.whiteTexture);GUI.color=old;
            if(showLearningLabels)GUI.Label(new Rect(425,730,350,25),baking.stage==CafeBaking.Stage.Pour?"按住倒麵糊":baking.stage==CafeBaking.Stage.Turn?(baking.waffle?"打開檢查後，再合蓋烤香":"點一下翻面") :"慢慢烘烤；離開工作檯會暫停",labelStyle);
        }
    }
}
