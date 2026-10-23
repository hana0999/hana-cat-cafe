using UnityEngine;
namespace Hana.CatCafe
{
    public static class CafeInteractionRules
    {
        public static bool TryPlace(Vector2 local,out Vector3 offset)
        {
            offset=Vector3.zero;
            if(float.IsNaN(local.x)||float.IsNaN(local.y)||float.IsInfinity(local.x)||float.IsInfinity(local.y)||local.sqrMagnitude>.45f*.45f)return false;
            Vector2 safe=Vector2.ClampMagnitude(local,.17f);
            offset=new Vector3(safe.x,-.48f*safe.magnitude/.28f,safe.y);return true;
        }
        public static bool NearGuest(Vector2 pointer,Vector2 guest)=>(pointer-guest).sqrMagnitude<=110*110;
    }
}
