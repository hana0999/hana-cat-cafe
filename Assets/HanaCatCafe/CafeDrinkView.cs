using System.Collections.Generic;
using UnityEngine;

namespace Hana.CatCafe
{
    // Procedural hollow ceramic cup; no downloaded models or assets required.
    public sealed class CafeDrinkView
    {
        readonly CafeShapes art;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Transform> foam = new List<Transform>();
        Transform cup, liquid, pitcher, stream, paw;
        public Transform Root { get; private set; }
        public CafeDrinkView(CafeShapes shapes) { art = shapes; }

        public void Build(Transform parent, CafeDrink state)
        {
            Clear();
            Root = art.Group(parent, "Cloud milk", new Vector3(-.25f, 1.46f, -.65f));
            art.Disc(Root, "Milk saucer", Vector3.zero, .64f, .06f, CafeShapes.Hex("#e6d2c0"));
            cup = art.Group(Root, "Ceramic cup", Vector3.zero);
            Color color = CafeShapes.Hex(new[] { "#c4d0bd", "#dfbdc6", "#c4cddd" }[state.CupColor]);
            BuildWall(cup, color);
            art.Disc(cup, "Cup base", new Vector3(0, .09f, 0), .27f, .10f, color);
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2 / 28;
                art.Ball(cup, "Rounded handle", new Vector3(.32f + Mathf.Cos(a)*.20f,.34f+Mathf.Sin(a)*.20f,0), Vector3.one*.085f, color);
            }
            liquid = art.Disc(cup, "Milk level", Vector3.zero, .269f, .01f, CafeShapes.Hex(new[]{"#e8d4af","#ddaeb9","#a68170"}[state.Flavor]));
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2/12;
                foam.Add(art.Ball(cup,"Foam cloud",new Vector3(Mathf.Cos(a)*.15f,.56f,Mathf.Sin(a)*.15f),new Vector3(.20f,.09f,.20f),CafeShapes.Hex("#fff0db")));
            }
            foam.Add(art.Ball(cup,"Foam center",new Vector3(0,.56f,0),new Vector3(.30f,.09f,.30f),CafeShapes.Hex("#fff0db")));
            paw=art.Group(cup,"Paw milk art",Vector3.zero);
            Color ink=CafeShapes.Hex("#c6a17e");
            art.Ball(paw,"Pad",new Vector3(0,.612f,-.055f),new Vector3(.14f,.009f,.11f),ink);
            for(int i=0;i<3;i++)art.Ball(paw,"Toe",new Vector3((i-1)*.085f,.612f,.055f),new Vector3(.055f,.009f,.06f),ink);
            pitcher=art.Group(Root,"Milk jug",new Vector3(.32f,1.12f,.05f));
            art.Ball(pitcher,"Jug body",Vector3.zero,new Vector3(.36f,.45f,.32f),CafeShapes.Hex("#e0c2c9"));
            var spout=art.Disc(pitcher,"Spout",new Vector3(-.18f,-.04f,0),.07f,.27f,CafeShapes.Hex("#e0c2c9"));
            spout.localRotation=Quaternion.Euler(0,0,-65);
            stream=art.Disc(Root,"Milk stream",Vector3.zero,.022f,.5f,CafeShapes.Hex(new[]{"#fff0db","#e5bcc5","#b69785"}[state.Flavor]));
            Sync(state,false);
        }
        void BuildWall(Transform parent, Color color)
        {
            const int n=64;
            // Continuous outside, lip and inside, with inward-facing inner normals.
            Vector2[] profile={new Vector2(.26f,.08f),new Vector2(.32f,.60f),new Vector2(.28f,.60f),new Vector2(.23f,.13f)};
            var vertices=new Vector3[profile.Length*(n+1)];
            var triangles=new List<int>();
            for(int r=0;r<profile.Length;r++)for(int i=0;i<=n;i++)
            {
                float a=i*Mathf.PI*2/n;
                vertices[r*(n+1)+i]=new Vector3(Mathf.Cos(a)*profile[r].x,profile[r].y,Mathf.Sin(a)*profile[r].x);
                if(r==profile.Length-1 || i==n)continue;
                int k=r*(n+1)+i, next=k+n+1;
                triangles.Add(k);triangles.Add(next);triangles.Add(k+1);
                triangles.Add(k+1);triangles.Add(next);triangles.Add(next+1);
            }
            var mesh=new Mesh { name="Hollow cup mesh",vertices=vertices,triangles=triangles.ToArray() };
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject("Hollow cup wall");go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=art.Material(color);
        }
        public void Sync(CafeDrink state, bool held)
        {
            if(Root==null)return;
            cup.gameObject.SetActive(state.Current!=CafeDrink.Stage.Cup);
            liquid.gameObject.SetActive(state.Fill>0);
            float surface=.14f+.38f*state.Fill;
            liquid.localPosition=new Vector3(0,surface,0);
            // Follow the inner wall taper so liquid never protrudes through the ceramic.
            float radius=Mathf.Lerp(.23f,.28f,(surface-.13f)/.47f)-.005f;
            liquid.localScale=new Vector3(radius*2,.006f,radius*2);
            for(int i=0;i<foam.Count;i++)foam[i].gameObject.SetActive(state.Foam>=(i+1f)/foam.Count);
            paw.gameObject.SetActive(state.Ready);
            bool pouring=held && (state.Current==CafeDrink.Stage.Pour || state.Current==CafeDrink.Stage.Foam);
            pitcher.gameObject.SetActive(pouring);stream.gameObject.SetActive(pouring);
            float angle=state.Foam*Mathf.PI*4;
            Vector3 offset=state.Current==CafeDrink.Stage.Foam?new Vector3(Mathf.Cos(angle)*.09f,0,Mathf.Sin(angle)*.09f):Vector3.zero;
            pitcher.localPosition=new Vector3(.32f,1.12f,.05f)+offset;
            pitcher.localRotation=Quaternion.Euler(0,0,pouring?18:0);
            float bottom=state.Current==CafeDrink.Stage.Foam ? .58f : surface;
            float length=1.04f-bottom;
            stream.localPosition=new Vector3(0,bottom+length/2,0)+offset;
            stream.localScale=new Vector3(.044f,length/2,.044f);
        }
        void Clear()
        {
            if(Root!=null){Root.gameObject.SetActive(false);Object.Destroy(Root.gameObject);}
            foreach(var m in meshes)Object.Destroy(m);
            meshes.Clear();foam.Clear();
        }
        public void SetCeramic(Color color)
        {
            if(cup==null)return;
            foreach(var r in cup.GetComponentsInChildren<Renderer>(true))
                if(r.name=="Hollow cup wall"||r.name=="Cup base"||r.name=="Rounded handle")r.sharedMaterial=art.Material(color);
        }
        public void Dispose(){Clear();}
    }
}
