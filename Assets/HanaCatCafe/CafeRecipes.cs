using UnityEngine;
namespace Hana.CatCafe
{
    public static class CafeRecipes
    {
        public const int Cupcake=0,Tart=1,Pudding=2,Pancakes=3,Waffle=4,Donut=5,LayerCake=6,Count=7;
        public static readonly string[] Names={"杯子蛋糕","水果塔","焦糖布丁","鬆餅疊疊","格子鬆餅","糖霜甜甜圈","迷你夾心蛋糕"};
        public static int ForOrder(int completed)=>Mathf.Max(0,completed)%Count;
        public static float Surface(int kind,bool cream)=>kind==Donut ? .38f : kind==LayerCake ? .76f : kind==Pancakes ? (cream?.59f:.46f) : kind==Waffle ? (cream?.40f:.27f) : kind==Tart ? .36f : kind==Pudding ? (cream ? .84f : .60f) : 1.10f;
        public static float PourSeconds(int kind)=>kind==Cupcake ? 2.8f : kind==Tart ? 2.2f : 2.0f;
        public static bool TryPlace(int kind,Vector2 local,out Vector3 offset)
        {
            if(kind==Cupcake)return CafeInteractionRules.TryPlace(local,out offset);
            offset=Vector3.zero;
            if(float.IsNaN(local.x)||float.IsNaN(local.y)||float.IsInfinity(local.x)||float.IsInfinity(local.y))return false;
            float acceptance=kind==Tart||kind>=Pancakes ? .60f : .40f;
            if(local.sqrMagnitude>acceptance*acceptance)return false;
            if(kind==Donut){Vector2 ring=local.sqrMagnitude<.0001f?Vector2.right:local.normalized;offset=new Vector3(ring.x*.32f-.32f,0,ring.y*.32f);return true;}
            Vector2 safe=Vector2.ClampMagnitude(local,kind==Tart||kind>=Pancakes ? .32f : .12f);
            offset=new Vector3(safe.x,0,safe.y);return true;
        }
    }
}
