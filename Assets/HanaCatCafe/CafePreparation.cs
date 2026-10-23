using UnityEngine;
namespace Hana.CatCafe
{
    [System.Serializable]
    public sealed class CafePreparation
    {
        public enum Stage { Press,Mix,Pour,Set,Unmold,Done }
        public Stage stage;
        public float amount;
        public CafePreparation(bool pudding){stage=pudding?Stage.Mix:Stage.Press;}
        public void Tick(float dt,bool held)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0||stage==Stage.Unmold||stage==Stage.Done)return;
            if(stage!=Stage.Set&&!held)return;
            amount=Mathf.Clamp01(amount+dt/(stage==Stage.Set?3f:2f));
            if(amount<1)return;
            stage=stage==Stage.Press?Stage.Done:stage==Stage.Mix?Stage.Pour:stage==Stage.Pour?Stage.Set:Stage.Unmold;
            amount=0;
        }
        public void Unmold(){if(stage==Stage.Unmold)stage=Stage.Done;}
        public void Undo(bool pudding){stage=pudding?Stage.Mix:Stage.Press;amount=0;}
    }
    [System.Serializable]
    public sealed class CafeShopSettings
    {
        public bool rain=true,dusk,leftCushion,rightCushion;
        const string Key="Hana.CatCafe.Shop.v1";
        public static CafeShopSettings Load()
        {
            try{return JsonUtility.FromJson<CafeShopSettings>(PlayerPrefs.GetString(Key,"{\"rain\":true}"))??new CafeShopSettings();}
            catch{return new CafeShopSettings();}
        }
        public void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
    }
}
