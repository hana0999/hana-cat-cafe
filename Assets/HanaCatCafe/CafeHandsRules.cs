using UnityEngine;
namespace Hana.CatCafe
{
    public static class CafeHandsRules
    {
        public static bool TrySurface(Vector2 local,float radius,bool ring,out Vector2 result)
        {
            result=Vector2.zero;
            if(float.IsNaN(local.x)||float.IsNaN(local.y)||float.IsInfinity(local.x)||float.IsInfinity(local.y)||local.sqrMagnitude>.75f*.75f)return false;
            result=ring?(local.sqrMagnitude<.001f?Vector2.right:local.normalized)*.34f:Vector2.ClampMagnitude(local,radius);
            return true;
        }
        public static bool NearTable(Vector3 player,Vector3 cat)
            =>new Vector2(player.x-cat.x,player.z-cat.z).sqrMagnitude<16f;
    }
}
