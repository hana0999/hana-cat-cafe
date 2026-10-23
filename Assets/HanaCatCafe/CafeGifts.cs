using System;
using UnityEngine;
namespace Hana.CatCafe
{
    [Serializable]
    public sealed class CafeGifts
    {
        public int claimed;
        public bool ribbon,flowers;
        const string Key="Hana.CatCafe.Gifts.v1";
        public bool Has(int id)=>id>=0 && id<3 && (claimed & (1<<id))!=0;
        public bool Available(int id,CafeGuestBook book)=>id>=0 && id<3 && !Has(id) && book.entries[id].met && book.entries[id].Hearts>=2;
        public bool Claim(int id,CafeGuestBook book)
        {
            if(!Available(id,book))return false;
            claimed|=1<<id;
            if(id==0)ribbon=true;if(id==1)flowers=true;
            return true;
        }
        public void Normalize(){claimed&=7;ribbon&=Has(0);flowers&=Has(1);}
        public static CafeGifts Load()
        {
            try{var g=JsonUtility.FromJson<CafeGifts>(PlayerPrefs.GetString(Key,"{}"))??new CafeGifts();g.Normalize();return g;}
            catch{return new CafeGifts();}
        }
        public void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
    }
}
