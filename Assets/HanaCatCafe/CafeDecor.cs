using System;
using UnityEngine;

namespace Hana.CatCafe
{
    [Serializable]
    public sealed class CafeDecor
    {
        public int version=1;
        public int palette, mat, vase;
        const string Key="Hana.CatCafe.Decor.v1";
        public static int Required(int category,int option)
        {
            if(category<0 || category>2 || option<0 || option>2)return int.MaxValue;
            if(category==1 && option==2)return 3;
            if(category==2)return option*3;
            return 0;
        }
        public int Selected(int category)=>category==0 ? palette : category==1 ? mat : vase;
        public bool Choose(int category,int option,int served)
        {
            if(category<0 || category>2 || option<0 || option>2 || served<Required(category,option))return false;
            if(category==0)palette=option;else if(category==1)mat=option;else vase=option;
            return true;
        }
        public void Normalize(int served)
        {
            if(palette<0 || palette>2)palette=0;
            if(mat<0 || mat>2 || served<Required(1,mat))mat=0;
            if(vase<0 || vase>2 || served<Required(2,vase))vase=0;
        }
        public static CafeDecor Load(int served)
        {
            try
            {
                var data=JsonUtility.FromJson<CafeDecor>(PlayerPrefs.GetString(Key,"{}"));
                if(data==null || data.version!=1)data=new CafeDecor();
                data.Normalize(served);return data;
            }
            catch{return new CafeDecor();}
        }
        public void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
    }
    public sealed class CafeDecorView
    {
        readonly CafeShapes art;
        Transform root;
        public CafeDecorView(CafeShapes shapes){art=shapes;}
        public void Apply(Transform world,Renderer counter,Color original,CafeDecor data)
        {
            counter.sharedMaterial=art.Material(data.palette==0 ? original : CafeShapes.Hex(data.palette==1 ? "#b7c5ae" : "#bcc5d7"));
            if(root!=null){root.gameObject.SetActive(false);UnityEngine.Object.Destroy(root.gameObject);}
            root=art.Group(world,"Cafe decorations",Vector3.zero);
            if(data.mat==1)Gingham();
            if(data.mat==2)Lace();
            if(data.vase>0)Flowers(data.vase);
        }
        void Gingham()
        {
            Color cream=CafeShapes.Hex("#eee2ce"),pink=CafeShapes.Hex("#ddc3c5"),deep=CafeShapes.Hex("#cfb2b8");
            for(int x=0;x<16;x++)for(int z=0;z<6;z++)
            {
                Color c=x%2==0 && z%2==0 ? deep : x%2==0 || z%2==0 ? pink : cream;
                art.Box(root,"Woven square",new Vector3(-1.0f+x*.22f,1.434f,-1.05f+z*.22f),new Vector3(.22f,.006f,.22f),c);
            }
        }
        void Lace()
        {
            Color c=CafeShapes.Hex("#f1e5d2");
            foreach(float x in new[]{-.25f,1.4f})
            {
                var center=art.Disc(root,"Lace doily",new Vector3(x,1.434f,-.6f),.76f,.006f,c);
                for(int i=0;i<32;i++)
                {
                    float a=i*Mathf.PI*2/32;
                    art.Ball(root,"Lace scallop",new Vector3(x+Mathf.Cos(a)*.77f,1.434f,-.6f+Mathf.Sin(a)*.77f),new Vector3(.15f,.008f,.15f),c);
                }
            }
        }
        void Flowers(int style)
        {
            var vase=art.Group(root,"Flower vase",new Vector3(-2.75f,1.43f,-.35f));
            Color ceramic=CafeShapes.Hex(style==1 ? "#d5c5d8" : "#c0ceba");
            art.Ball(vase,"Rounded ceramic",new Vector3(0,.20f,0),new Vector3(.36f,.40f,.34f),ceramic);
            art.Disc(vase,"Vase neck",new Vector3(0,.40f,0),.095f,.16f,ceramic);
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*.17f,h=.67f+(i%2)*.12f;
                var stem=art.Disc(vase,"Stem",new Vector3(x*.5f,(h+.40f)/2,0),.012f,h-.40f,CafeShapes.Hex("#8c9e7e"));
                stem.localRotation=Quaternion.Euler(0,0,-(i-1)*24);
                var leaf=art.Ball(vase,"Leaf",new Vector3(x*.6f+.06f,.52f,.02f),new Vector3(.17f,.055f,.08f),CafeShapes.Hex("#a9ba96"));
                leaf.localRotation=Quaternion.Euler(0,0,25);
                if(style==1)
                {
                    for(int j=0;j<7;j++)
                    {
                        float a=j*Mathf.PI*2/7;
                        var petal=art.Ball(vase,"Daisy petal",new Vector3(x+Mathf.Cos(a)*.10f,h+Mathf.Sin(a)*.10f,-.02f),new Vector3(.15f,.07f,.06f),CafeShapes.Hex("#f5e9d6"));
                        petal.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);
                    }
                    art.Ball(vase,"Daisy center",new Vector3(x,h,-.06f),Vector3.one*.08f,CafeShapes.Hex("#d9b980"));
                }
                else
                {
                    for(int j=-1;j<=1;j++)art.Ball(vase,"Tulip petal",new Vector3(x+j*.045f,h,.01f-Mathf.Abs(j)*.04f),new Vector3(.12f,.21f,.13f),CafeShapes.Hex(j==0 ? "#d2a4b4" : "#e2bcc8"));
                }
            }
        }
        public void Dispose(){if(root!=null)UnityEngine.Object.Destroy(root.gameObject);}
    }
}
