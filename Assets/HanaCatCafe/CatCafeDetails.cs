using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        bool utilityOpen,tablewareOpen;
        int tablewareStyle=-1,tablewareTrim=-1;
        void EnsureTableware()
        {
            if(tablewareStyle>=0)return;
            tablewareStyle=Mathf.Clamp(PlayerPrefs.GetInt("Hana.CatCafe.Plate.v1",0),0,2);
            tablewareTrim=Mathf.Clamp(PlayerPrefs.GetInt("Hana.CatCafe.Trim.v1",0),0,2);
        }
        void BuildTableware(Transform parent,float radius)
        {
            EnsureTableware();
            Color ceramic=CafeShapes.Hex(new[]{"#c4d0bd","#dfbdc6","#c4cddd"}[tablewareStyle]);
            art.Disc(parent,"Plate",Vector3.zero,radius,.06f,ceramic);
            if(tablewareTrim==0)return;
            Color trim=CafeShapes.Hex(tablewareTrim==1?"#f6e8d1":"#c3a276");
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48;
                art.Ball(parent,"Plate trim",new Vector3(Mathf.Cos(a)*(radius-.045f),.035f,Mathf.Sin(a)*(radius-.045f)),new Vector3(.065f,.016f,.065f),trim);
            }
            if(tablewareTrim==2)
                for(int j=0;j<8;j++)
                {
                    float a=j*Mathf.PI/4;Vector3 center=new Vector3(Mathf.Cos(a)*(radius-.13f),.035f,Mathf.Sin(a)*(radius-.13f));
                    for(int k=0;k<5;k++){float b=k*Mathf.PI*2/5;art.Ball(parent,"Painted petal",center+new Vector3(Mathf.Cos(b)*.025f,0,Mathf.Sin(b)*.025f),new Vector3(.035f,.009f,.035f),CafeShapes.Hex("#bf8f9f"));}
                }
        }
        void ApplyTableware()
        {
            PlayerPrefs.SetInt("Hana.CatCafe.Plate.v1",tablewareStyle);PlayerPrefs.SetInt("Hana.CatCafe.Trim.v1",tablewareTrim);PlayerPrefs.Save();
            BuildCake();ApplySaucer();
        }
        void ApplySaucer()
        {
            if(drinkView.Root==null)return;
            EnsureTableware();drinkView.SetCeramic(CafeShapes.Hex(new[]{"#c4d0bd","#dfbdc6","#c4cddd"}[tablewareStyle]));
            var old=drinkView.Root.Find("Matching saucer");if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
            var original=drinkView.Root.Find("Milk saucer");if(original!=null)original.gameObject.SetActive(false);
            BuildTableware(art.Group(drinkView.Root,"Matching saucer",Vector3.zero),.64f);
        }
        void DrawQuietMenu()
        {
            if(IconButton(new Rect(1100,675,70,70),utilityOpen?closeIcon:decorIcon,"小店選單",utilityOpen))
            {ResetHold();utilityOpen=!utilityOpen;tablewareOpen=false;}
            if(roomStation==2||roomStation==3)
            {
                if(IconButton(new Rect(980,570,70,58),decorIcon,"杯盤搭配",tablewareOpen)){ResetHold();tablewareOpen=!tablewareOpen;}
            }
            else tablewareOpen=false;
            if(!tablewareOpen)return;
            GUI.Box(new Rect(910,250,275,300),GUIContent.none,panelStyle);
            for(int i=0;i<3;i++)
            {
                if(IconButton(new Rect(930+i*80,285,65,65),cupChoices[i],"杯盤色系",tablewareStyle==i)){ResetHold();tablewareStyle=i;ApplyTableware();}
                if(IconButton(new Rect(930+i*80,400,65,65),i==0?cupIcon:i==1?targetIcon:toppingIcons[2],"素面／蕾絲邊／花朵金邊",tablewareTrim==i)){ResetHold();tablewareTrim=i;ApplyTableware();}
            }
        }
        Transform MakePourTool(Transform parent)
        {
            var tool=art.Group(parent,"Pouring jug and stream",Vector3.zero);
            var jug=art.Ball(tool,"Tilted jug",new Vector3(.17f,.35f,0),new Vector3(.30f,.35f,.28f),CafeShapes.Hex("#ddc2c8"));
            jug.localRotation=Quaternion.Euler(0,0,35);
            art.Disc(tool,"Pouring stream",new Vector3(0,.10f,0),.025f,.30f,CafeShapes.Hex("#efddbc"));return tool;
        }
        Vector3 FillToolPosition(float t)
        {
            if(dessertKind==CafeRecipes.Donut){float a=t*Mathf.PI*2;return new Vector3(Mathf.Cos(a)*.34f,.38f,Mathf.Sin(a)*.34f);}
            float angle=t*Mathf.PI*5,radius=dessertKind==CafeRecipes.Pudding?.20f:.35f;
            return new Vector3(Mathf.Cos(angle)*radius*t,CafeRecipes.Surface(dessertKind,false)+.06f,Mathf.Sin(angle)*radius*t);
        }
        void BuildNewPastry(Color filling)
        {
            if(dessertKind==CafeRecipes.Donut)
            {
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI*2/48;Vector3 point=new Vector3(Mathf.Cos(a)*.34f,.21f,Mathf.Sin(a)*.34f);
                    if(step!=Step.Sponge)art.Ball(cake,"Soft donut",point,new Vector3(.30f,.28f,.30f),CafeShapes.Hex("#d7ae80"));
                    var icing=art.Ball(cake,"Donut glaze",point+Vector3.up*.12f,new Vector3(.29f,.06f,.29f),filling);
                    icing.gameObject.SetActive(false);creamPieces.Add(icing);
                }
            }
            else
            {
                if(step!=Step.Sponge)
                {
                    art.Disc(cake,"Lower sponge",new Vector3(0,.20f,0),.46f,.27f,flavors[selectedFlavor]);
                    art.Disc(cake,"Cream sandwich",new Vector3(0,.36f,0),.45f,.08f,CafeShapes.Hex("#fff0db"));
                    art.Disc(cake,"Upper sponge",new Vector3(0,.52f,0),.46f,.25f,flavors[selectedFlavor]);
                    for(int j=0;j<10;j++){float a=j*Mathf.PI/5;art.Ball(cake,"Berry filling",new Vector3(Mathf.Cos(a)*.43f,.36f,Mathf.Sin(a)*.43f),new Vector3(.09f,.07f,.09f),CafeShapes.Hex("#b98595"));}
                }
                for(int i=0;i<50;i++)
                {
                    float t=(i+1)/50f,a=t*Mathf.PI*5;
                    var icing=art.Ball(cake,"Piped cake frosting",new Vector3(Mathf.Cos(a)*.35f*t,.70f,Mathf.Sin(a)*.35f*t),new Vector3(.16f,.09f,.16f),filling);
                    icing.gameObject.SetActive(false);creamPieces.Add(icing);
                }
            }
        }
    }
}
