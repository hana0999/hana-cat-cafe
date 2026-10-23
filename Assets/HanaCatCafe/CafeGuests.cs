using System;
using UnityEngine;

namespace Hana.CatCafe
{
    public sealed class CafeGuestDefinition
    {
        public readonly string Id, Name, Fur, Stripe, Ribbon;
        public readonly int UnlockOrders, Flavor, Topping;
        public readonly float BobSpeed, TailSpeed, TailRange;
        public CafeGuestDefinition(string id,string name,string fur,string stripe,string ribbon,
            int unlock,int flavor,int topping,float bob,float tail,float range)
        {
            Id=id;Name=name;Fur=fur;Stripe=stripe;Ribbon=ribbon;
            UnlockOrders=unlock;Flavor=flavor;Topping=topping;
            BobSpeed=bob;TailSpeed=tail;TailRange=range;
        }
    }
    public static class CafeGuests
    {
        // Stable indices are also save IDs: append new guests, never reorder existing ones.
        public static readonly CafeGuestDefinition[] All =
        {
            new CafeGuestDefinition("orange","奶油", "#d9b082","#bb9068","#b8c6a7",0,0,0,2.2f,3f,23),
            new CafeGuestDefinition("black","芝麻", "#706975","#5f5966","#d9b4c2",3,2,1,1.1f,1.2f,10),
            new CafeGuestDefinition("gray","雲朵", "#b5bbc8","#959daf","#c3afd1",6,1,2,1.6f,1.8f,15),
            new CafeGuestDefinition("calico","蜜桃", "#efd9bd","#ad8873","#d6aab8",9,1,0,2.0f,2.4f,18),
            new CafeGuestDefinition("cream","奶茶", "#dfc9a5","#c0a17f","#a9bec3",12,0,2,1.3f,1.5f,13),
            new CafeGuestDefinition("tuxedo","奧利奧","#756c76","#605866","#b6c5ba",15,2,2,1.7f,2f,18),
            new CafeGuestDefinition("point","摩卡","#e0ccb7","#9d8177","#c5b1ce",18,2,0,1.2f,1.4f,14),
            new CafeGuestDefinition("white","雪酪","#f1e8d9","#d8d0c4","#d2acba",21,0,1,2f,2.6f,22)
        };
        public static bool Unlocked(int index,int served)
            => index>=0 && index<All.Length && served>=All[index].UnlockOrders;
    }
    [Serializable]
    public sealed class CafeGuestRecord
    {
        public bool met;
        public int bond;
        public int orders;
        public int Hearts => Math.Min(5,bond/3);
        public void AddBond(int amount){bond=Math.Min(15,Math.Max(0,bond)+Math.Max(0,amount));}
    }
    [Serializable]
    public sealed class CafeGuestBook
    {
        public int version=1;
        public int lastGuest=-1;
        public CafeGuestRecord[] entries;
        const string Key="Hana.CatCafe.Guests.v1";

        public CafeGuestBook(){Normalize();}
        public void Normalize()
        {
            var previous=entries;
            entries=new CafeGuestRecord[CafeGuests.All.Length];
            for(int i=0;i<entries.Length;i++)
            {
                entries[i]=previous!=null && i<previous.Length && previous[i]!=null ? previous[i] : new CafeGuestRecord();
                entries[i].bond=Mathf.Clamp(entries[i].bond,0,15);
                entries[i].orders=Mathf.Clamp(entries[i].orders,0,100000);
            }
            lastGuest=Mathf.Clamp(lastGuest,-1,entries.Length-1);
        }
        public int Next(int served)
        {
            for(int i=0;i<entries.Length;i++)if(CafeGuests.Unlocked(i,served)&&!entries[i].met)return i;
            for(int offset=1;offset<=entries.Length;offset++)
            {
                int i=(lastGuest+offset)%entries.Length;
                if(CafeGuests.Unlocked(i,served))return i;
            }
            return 0;
        }
        public void Meet(int index){entries[index].met=true;lastGuest=index;}
        public static CafeGuestBook Load()
        {
            try
            {
                var result=JsonUtility.FromJson<CafeGuestBook>(PlayerPrefs.GetString(Key,"{}"));
                if(result==null || result.version!=1)return new CafeGuestBook();
                result.Normalize();return result;
            }
            catch{return new CafeGuestBook();}
        }
        public void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
    }
}
