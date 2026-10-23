using System;
using UnityEngine;

namespace Hana.CatCafe
{
    // Only permanent progress is saved. Each Play session begins with a fresh order.
    [Serializable]
    public sealed class CafeProgress
    {
        public int version = 1;
        public int served;
        public int affection;
        const string Key = "Hana.CatCafe.Progress.v1";
        public bool BlackCatUnlocked => served >= 3;

        public static CafeProgress Load()
        {
            try
            {
                var p = JsonUtility.FromJson<CafeProgress>(PlayerPrefs.GetString(Key, "{}"));
                if (p == null || p.version != 1) return new CafeProgress();
                p.served = Mathf.Clamp(p.served, 0, 100000);
                p.affection = Mathf.Clamp(p.affection, 0, 100000);
                return p;
            }
            catch { return new CafeProgress(); }
        }
        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
        public static bool Matches(int flavor, int topping, int wantedFlavor, int wantedTopping)
        {
            return flavor == wantedFlavor && topping == wantedTopping;
        }
    }
}
