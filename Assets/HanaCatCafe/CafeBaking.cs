using UnityEngine;
namespace Hana.CatCafe
{
    [System.Serializable]
    public sealed class CafeBaking
    {
        public enum Stage { Pour,FirstSide,Turn,SecondSide,Done }
        public Stage stage;
        public float amount;
        public bool waffle;
        public CafeBaking(bool isWaffle){waffle=isWaffle;}
        public void Tick(float dt,bool held)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
            if(stage==Stage.Turn||stage==Stage.Done)return;
            if(stage==Stage.Pour&&!held)return;
            amount=Mathf.Clamp01(amount+dt/(stage==Stage.Pour?2f:2.5f));
            if(amount<1)return;
            if(stage==Stage.Pour){stage=Stage.FirstSide;amount=0;}
            else if(stage==Stage.FirstSide){stage=Stage.Turn;amount=0;}
            else {stage=Stage.Done;amount=1;}
        }
        public void Turn(){if(stage!=Stage.Turn)return;stage=Stage.SecondSide;amount=0;}
        public void Undo(){stage=Stage.Pour;amount=0;}
    }
}
