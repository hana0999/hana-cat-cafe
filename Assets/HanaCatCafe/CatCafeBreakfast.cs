using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        void BuildBreakfast(Color flavor)
        {
            bool pancakes=dessertKind==CafeRecipes.Pancakes;
            Color baked=Color.Lerp(flavor,CafeShapes.Hex("#d7ad7e"),.4f);
            if(step!=Step.Sponge)
            {
                if(pancakes)
                {
                    for(int layer=0;layer<3;layer++)
                    {
                        float y=.10f+layer*.12f;
                        art.Disc(cake,"Pancake soft edge",new Vector3(0,y,0),.51f,.12f,baked);
                        art.Disc(cake,"Pancake toasted top",new Vector3(0,y+.055f,0),.48f,.012f,Color.Lerp(baked,CafeShapes.Hex("#b98858"),.22f));
                    }
                }
                else
                {
                    art.Box(cake,"Waffle base",new Vector3(0,.10f,0),new Vector3(1.02f,.14f,1.02f),baked);
                    Color edge=Color.Lerp(baked,CafeShapes.Hex("#f3dfb7"),.18f);
                    for(int i=0;i<7;i++)
                    {
                        float v=-.48f+i*.16f;
                        art.Box(cake,"Waffle ridge",new Vector3(v,.19f,0),new Vector3(.045f,.08f,1.02f),edge);
                        art.Box(cake,"Waffle ridge",new Vector3(0,.19f,v),new Vector3(1.02f,.08f,.045f),edge);
                    }
                }
            }
            float top=pancakes?.415f:.245f;
            for(int j=0;j<50;j++)
            {
                float t=(j+1)/50f;
                var syrup=art.Disc(cake,"Maple syrup",new Vector3(0,top+j*.0002f,0),.39f*t,.012f,CafeShapes.Hex("#b98558"));
                syrup.gameObject.SetActive(false);creamPieces.Add(syrup);
            }
            if(puddingCream && step!=Step.Sponge)
            {
                var butter=art.Box(cake,"Butter square",new Vector3(0,top+.075f,0),new Vector3(.25f,.12f,.23f),CafeShapes.Hex("#f0d599"));
                butter.localRotation=Quaternion.Euler(0,18,0);
            }
        }
    }
}
