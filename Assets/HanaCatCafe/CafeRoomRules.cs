using UnityEngine;
namespace Hana.CatCafe
{
    public static class CafeRoomRules
    {
        public static Vector3 ClampWalk(Vector3 position)
        {
            if(float.IsNaN(position.x)||float.IsInfinity(position.x)||float.IsNaN(position.z)||float.IsInfinity(position.z))return new Vector3(0,2.45f,-4.4f);
            return new Vector3(Mathf.Clamp(position.x,-5.2f,5.2f),2.45f,Mathf.Clamp(position.z,-9.1f,-2.5f));
        }
        public static Vector2 Look(Vector2 current,Vector2 delta)
            =>new Vector2(Mathf.Repeat(current.x+delta.x*.22f+180,360)-180,Mathf.Clamp(current.y+delta.y*.18f,-35,65));
    }
}
