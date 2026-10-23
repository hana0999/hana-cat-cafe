using System.Collections.Generic;
using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        readonly List<Renderer> photoHidden=new List<Renderer>();
        readonly List<Renderer> photoSubjects=new List<Renderer>();
        Renderer[] photoScene;float lastPhotoAspect;
        Vector3 photoCatPosition,photoCatScale,photoCakePosition,photoDrinkPosition,photoTrayPosition;
        Quaternion photoCatRotation;
        bool photoPosesSaved;Vector3[] photoSteamPositions;
        void SavePhotoPoses()
        {
            photoCatPosition=cat.position;photoCatRotation=cat.rotation;photoCatScale=cat.localScale;
            photoCakePosition=cake.position;photoDrinkPosition=drinkView.Root.position;photoTrayPosition=serviceTray.position;photoPosesSaved=true;photoSteamPositions=new Vector3[steam.Count];for(int i=0;i<steam.Count;i++)photoSteamPositions[i]=steam[i].position;
        }
        void RestorePhotoPoses()
        {
            if(!photoPosesSaved)return;
            cat.position=photoCatPosition;cat.rotation=photoCatRotation;cat.localScale=photoCatScale;
            cake.position=photoCakePosition;drinkView.Root.position=photoDrinkPosition;serviceTray.position=photoTrayPosition;for(int i=0;i<steam.Count;i++)steam[i].position=photoSteamPositions[i];
        }
        void PoseForPhoto()
        {
            RestorePhotoPoses();
            if(cameraPreset!=2)return;
            cat.position=new Vector3(-1.45f,0,.8f);cat.rotation=Quaternion.identity;cat.localScale=Vector3.one;
            cake.position=new Vector3(1.4f,photoCakePosition.y,-.55f);
            drinkView.Root.position=new Vector3(2.7f,photoDrinkPosition.y,-.65f);
            serviceTray.position=new Vector3(2.05f,photoTrayPosition.y,-.6f);for(int i=0;i<steam.Count;i++)steam[i].position+=drinkView.Root.position-photoDrinkPosition;
        }
        readonly List<Bounds> photoTargets=new List<Bounds>();
        void RestorePhotoVisibility()
        {
            foreach(var r in photoHidden)if(r!=null)r.enabled=true;
            photoHidden.Clear();
        }
        void AddPhotoSubject(Transform root)
        {
            if(root==null||!root.gameObject.activeInHierarchy)return;
            bool found=false;Bounds bounds=new Bounds();
            foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)
            {photoSubjects.Add(r);if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
            if(found)photoTargets.Add(bounds);
        }
        void FramePhotoSubjects()
        {
            RestorePhotoVisibility();photoSubjects.Clear();photoTargets.Clear();lastPhotoAspect=view.aspect;
            AddPhotoSubject(cake);
            if(cameraPreset==2){AddPhotoSubject(cat);AddPhotoSubject(drinkView.Root);}
            if(photoSubjects.Count==0)AddPhotoSubject(cat);
            Bounds bounds=new Bounds(photoSubjects[0].bounds.center,Vector3.zero);
            foreach(var r in photoSubjects)bounds.Encapsulate(r.bounds);
            orbitYaw=Mathf.Clamp(orbitYaw,cameraPreset==2?-30:-60,cameraPreset==2?30:60);
            orbitPitch=Mathf.Clamp(orbitPitch,cameraPreset==2?18:25,cameraPreset==2?60:85);
            float pitch=orbitPitch*Mathf.Deg2Rad,yaw=orbitYaw*Mathf.Deg2Rad;
            Vector3 direction=new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),-Mathf.Cos(yaw)*Mathf.Cos(pitch));
            view.transform.position=bounds.center+direction*10;
            view.transform.rotation=Quaternion.LookRotation(-direction);
            float fit=.5f;
            foreach(var r in photoSubjects)
            {
                Bounds b=r.bounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    Vector3 local=view.transform.InverseTransformPoint(corner);
                    fit=Mathf.Max(fit,Mathf.Abs(local.y),Mathf.Abs(local.x)/Mathf.Max(.3f,view.aspect));
                }
            }
            view.orthographicSize=fit*1.18f*Mathf.Max(1,photoZoom);
            // Orthographic rays are parallel. Bounds tests work without colliders.
            // Exclude the subjects; restore all hidden environment renderers on exit.
            var subjects=new HashSet<Renderer>(photoSubjects);
            var samples=new List<Vector3>();
            foreach(var b in photoTargets)
            {
                samples.Add(b.center);
                for(int i=0;i<8;i++)samples.Add(b.center+Vector3.Scale(b.extents*.85f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            }
            if(photoScene==null)photoScene=world.GetComponentsInChildren<Renderer>();
            foreach(var r in photoScene)
            {
                if(r==null||!r.enabled||!r.gameObject.activeInHierarchy||subjects.Contains(r))continue;
                bool blocks=false;
                foreach(var point in samples)
                {
                    Vector3 screen=view.WorldToScreenPoint(point);
                    Ray ray=view.ScreenPointToRay(screen);
                    float targetDistance=Vector3.Dot(point-ray.origin,ray.direction);
                    if(r.bounds.IntersectRay(ray,out float distance)&&distance>=0&&distance<targetDistance-.025f){blocks=true;break;}
                }
                if(blocks)
                {
                    Transform group=r.transform;while(group.parent!=null&&group.parent!=world)group=group.parent;
                    foreach(var part in group.GetComponentsInChildren<Renderer>())
                        if(part.enabled&&!subjects.Contains(part)){part.enabled=false;photoHidden.Add(part);}
                }
            }
        }
    }
}
