using System.Collections.Generic;
using UnityEngine;
namespace Hana.CatCafe
{
    public sealed partial class CatCafePrototype
    {
        sealed class HouseCat
        {
            public Transform root,head,tail;
            public Vector3 headRest;
            public Renderer[] eyes;
            public Vector3[] route;
            public int target;
            public float rest,happyUntil,careRemaining;public int careSpot,careAction;public Vector3 careTarget;
        }
        readonly List<HouseCat> houseCats=new List<HouseCat>();
        readonly List<Transform> rain=new List<Transform>();
        readonly List<Transform> steam=new List<Transform>();
        float atmosphereClockShift;bool guestSettled;
        Transform doorBell;
        Vector3[] guestEntry={new Vector3(0,0,-9.5f),new Vector3(-4.3f,0,-6),new Vector3(-4.3f,0,.8f),new Vector3(-1.45f,0,.8f)};
        void InitAtmosphere()
        {
            panIcon=MakeIcon(36,0);
            Color wood=CafeShapes.Hex("#c4a88f"),paper=CafeShapes.Hex("#f0e3ce"),sage=CafeShapes.Hex("#b9c8b0");
            // Additional pastry displays keep the original central preparation counter intact.
            for(int side=-1;side<=1;side+=2)
            {
                var display=art.Group(world,"Pastry display cabinet",new Vector3(side*5.5f,0,.3f));
                art.Box(display,"Cabinet",new Vector3(0,.65f,0),new Vector3(2.1f,1.3f,1.15f),side<0?counterColor:sage);
                art.Box(display,"Marble top",new Vector3(0,1.36f,0),new Vector3(2.2f,.12f,1.25f),paper);
                for(int j=-1;j<=1;j++)
                {
                    var stand=art.Group(display,"Cake stand",new Vector3(j*.65f,1.45f,0));
                    art.Disc(stand,"Pedestal",new Vector3(0,.08f,0),.09f,.16f,wood);
                    art.Disc(stand,"Stand plate",new Vector3(0,.18f,0),.28f,.035f,paper);
                    MakeDisplayPastry(stand,((side<0?0:3)+j+1)%CafeRecipes.Count);
                }
                art.Box(display,"Menu picture board",new Vector3(0,2.1f,.5f),new Vector3(1.9f,.9f,.07f),wood);
                for(int j=0;j<3;j++)art.Ball(display,"Painted pastry medallion",new Vector3((j-1)*.55f,2.1f,.44f),new Vector3(.34f,.34f,.03f),flavors[j]);
            }
            // Broad side windows, striped awnings and linen curtains.
            for(int side=-1;side<=1;side+=2)
            {
                var window=art.Group(world,"Tall side window",new Vector3(side*7.72f,2.8f,-5.8f));
                art.Box(window,"Window glass",Vector3.zero,new Vector3(.05f,2.7f,4.4f),CafeShapes.Hex("#c8d7d6"));
                for(int j=-1;j<=1;j++)art.Box(window,"Window mullion",new Vector3(-side*.06f,0,j*2.12f),new Vector3(.07f,2.85f,.06f),paper);
                art.Box(window,"Window horizontal",new Vector3(-side*.06f,0,0),new Vector3(.07f,.06f,4.4f),paper);
                for(int j=0;j<12;j++)art.Box(window,"Fabric awning",new Vector3(-side*.30f,1.55f,-2.2f+j*.4f),new Vector3(.65f,.11f,.4f),j%2==0?paper:counterColor);
                for(int j=-1;j<=1;j+=2)art.Ball(window,"Linen curtain",new Vector3(-side*.12f,-.1f,j*2.1f),new Vector3(.20f,2.7f,.5f),paper);
                for(int j=0;j<18;j++)
                    rain.Add(art.Box(window,"Window rain",new Vector3(-side*.04f,0,-1.9f+j*.22f),new Vector3(.015f,.17f,.015f),CafeShapes.Hex("#aebfc1")));
            }
            for(int j=-1;j<=1;j++)
            {
                var lamp=art.Group(world,"Pendant",new Vector3(j*3.8f,4.6f,-4.8f));
                art.Disc(lamp,"Cable",new Vector3(0,.2f,0),.012f,.6f,wood);
                art.Ball(lamp,"Milk glass shade",new Vector3(0,-.2f,0),new Vector3(.65f,.38f,.65f),paper);
            }
            doorBell=art.Group(world,"Entry bell",new Vector3(.55f,3,-10.05f));
            art.Ball(doorBell,"Brass bell",Vector3.zero,new Vector3(.17f,.2f,.17f),CafeShapes.Hex("#bfa16e"));
            for(int i=0;i<5;i++)steam.Add(art.Ball(world,"Milk steam",Vector3.zero,Vector3.one*.05f,CafeShapes.Hex("#e9e0d2")));
            sounds?.Play(2);
            MakeHouseCat(new Vector3(-5.7f,0,-7.8f),"#d6c5b4",new[]{new Vector3(-5.7f,0,-7.8f),new Vector3(-3.8f,0,-7),new Vector3(-4.8f,0,-4),new Vector3(-5.7f,0,-7.8f)});
            MakeHouseCat(new Vector3(6.6f,.62f,-4.1f),"#aab4bd",new[]{new Vector3(6.6f,.62f,-4.1f),new Vector3(5.6f,0,-4),new Vector3(3.7f,0,-6.7f),new Vector3(5.6f,0,-4),new Vector3(6.6f,.62f,-4.1f)});
        }
        void MakeHouseCat(Vector3 position,string fur,Vector3[] route)
        {
            var copy=Instantiate(cat.gameObject,world);copy.name="Resident cat";
            var root=copy.transform;root.position=position;root.localScale=Vector3.one*.48f;root.rotation=Quaternion.identity;
            var welcome=root.Find("Familiar welcome");if(welcome!=null)welcome.gameObject.SetActive(false);
            foreach(var r in root.GetComponentsInChildren<Renderer>())
                if(r.name=="Face"||r.name=="Body"||r.name=="Ear"||r.name=="Paw"||r.name=="Tail")r.sharedMaterial=art.Material(CafeShapes.Hex(fur));
            var head=root.Find("Head");
            houseCats.Add(new HouseCat{root=root,head=head,headRest=head.localPosition,tail=root.Find("Tail pivot"),route=route,target=1,rest=2});
        }
        void UpdateGuestArrival()
        {
            float t=(Time.time-arriveAt)/9f;
            if(reduceMotion||phase!=Phase.Greeting||roomStation==1||t>=1)guestSettled=true;
            if(guestSettled)t=1;
            t=Mathf.Clamp01(t);
            float segment=t*3;int index=Mathf.Min(2,Mathf.FloorToInt(segment));float f=segment-index;
            cat.localPosition=Vector3.Lerp(guestEntry[index],guestEntry[index+1],f);
            cat.localScale=Vector3.one*Mathf.Lerp(.58f,1,Mathf.Clamp01((t-.88f)/.12f));
            if(t<1)
            {
                Vector3 direction=guestEntry[index+1]-guestEntry[index];
                cat.rotation=Quaternion.LookRotation(-direction);
                if(!reduceMotion)cat.localPosition+=Vector3.up*Mathf.Abs(Mathf.Sin(Time.time*7))*.025f;
            }
            else cat.rotation=Quaternion.identity;
        }
        void UpdateAtmosphere()
        {
            if(!Application.isFocused)return;
            float clock=Time.time-atmosphereClockShift;
            foreach(var resident in houseCats)
            {
                if(UpdateHouseCare(resident,clock))continue;
                if(reduceMotion)continue;
                if(resident.rest>0)
                {
                    resident.rest-=Time.deltaTime;
                    resident.root.localScale=new Vector3(.48f,.46f+Mathf.Sin(clock*1.6f)*.006f,.48f);
                    resident.head.localRotation=Quaternion.Euler(12,0,-8);
                    Vector3 player=view.transform.position-resident.head.position;
                    if(player.sqrMagnitude<5){resident.head.localRotation=Quaternion.Euler(0,Mathf.Clamp(-player.x*14,-30,30),0);}
                    if(clock<resident.happyUntil)resident.head.localRotation=Quaternion.Euler(-8,0,-10);
                    continue;
                }
                resident.root.localScale=Vector3.one*.48f;
                Vector3 destination=resident.route[resident.target],delta=destination-resident.root.position;
                resident.root.position=Vector3.MoveTowards(resident.root.position,destination,Time.deltaTime*.48f);
                Vector3 flat=new Vector3(delta.x,0,delta.z);
                if(flat.sqrMagnitude>.001f)resident.root.rotation=Quaternion.Slerp(resident.root.rotation,Quaternion.LookRotation(-flat),Time.deltaTime*4);
                resident.head.localRotation=Quaternion.Euler(Mathf.Sin(clock*6)*3,0,0);
                if(resident.tail!=null)resident.tail.localRotation=Quaternion.Euler(0,Mathf.Sin(clock*3)*15,0);
                if(delta.sqrMagnitude<.003f){resident.target=(resident.target+1)%resident.route.Length;resident.rest=resident.target==1?10:3;}
            }
            for(int i=0;i<rain.Count;i++)
            {
                var p=rain[i].localPosition;p.y=reduceMotion?0:1.2f-Mathf.Repeat(clock*.55f+i*.37f,2.4f);rain[i].localPosition=p;
            }
            for(int i=0;i<steam.Count;i++)
            {
                bool visible=drink!=null&&drink.Fill>0&&!creativeMode&&phase!=Phase.Greeting;
                steam[i].gameObject.SetActive(visible&&!reduceMotion);
                if(!visible)continue;
                float t=Mathf.Repeat(clock*.4f+i*.2f,1);
                steam[i].position=drinkView.Root.position+new Vector3(Mathf.Sin(clock+i)*.035f,.7f+t*.35f,0);
                steam[i].localScale=Vector3.one*(.025f+Mathf.Sin(t*Mathf.PI)*.035f);
            }
            if(doorBell!=null)doorBell.localRotation=Quaternion.Euler(0,0,reduceMotion?0:Mathf.Sin((Time.time-arriveAt)*12)*Mathf.Max(0,1-(Time.time-arriveAt)/2)*14);
        }
        void DrawResidentPets()
        {
            if(roomStation!=0)return;
            foreach(var resident in houseCats)
            {
                if(!RoomPointVisible(resident.head.position))continue;
                if((view.transform.position-resident.head.position).sqrMagnitude>12)continue;
                if(WorldButton(resident.head.position+Vector3.up*.4f,pawIcon,"摸摸住店貓"))
                {resident.rest=6;resident.happyUntil=Time.time-atmosphereClockShift+2;resident.head.localRotation=Quaternion.Euler(-8,0,0);sounds?.Play(1);}
            }
        }
    }
}
