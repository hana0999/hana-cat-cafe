using System;

namespace Hana.CatCafe
{
    // Pure state: switching workstations does not discard a partially made drink.
    public sealed class CafeDrink
    {
        public enum Stage { Cup, Pour, Foam, Ready }
        public Stage Current { get; private set; }
        public int Flavor { get; private set; }
        public void ChooseFlavor(int flavor) { if(Current==Stage.Cup && flavor>=0 && flavor<3)Flavor=flavor; }
        public int CupColor { get; private set; }
        public float Fill { get; private set; }
        public float Foam { get; private set; }
        public bool Ready => Current == Stage.Ready;

        public void ChooseCup(int color)
        {
            if (Current != Stage.Cup || color < 0 || color > 2) return;
            CupColor = color;
            Current = Stage.Pour;
        }
        public void Tick(float seconds, bool held)
        {
            if (!held || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            // One stage at a time: excess time never spills into the next step.
            if (Current == Stage.Pour)
            {
                Fill = Math.Min(1f, Fill + seconds / 2.4f);
                if (Fill >= 1f) Current = Stage.Foam;
            }
            else if (Current == Stage.Foam)
            {
                Foam = Math.Min(1f, Foam + seconds / 1.8f);
                if (Foam >= 1f) Current = Stage.Ready;
            }
        }
        public void Undo()
        {
            if (Current == Stage.Ready) { Foam = 0; Current = Stage.Foam; }
            else if (Current == Stage.Foam && Foam > 0) Foam = 0;
            else if (Current == Stage.Foam) { Fill = 0; Current = Stage.Pour; }
            else if (Current == Stage.Pour && Fill > 0) Fill = 0;
            else if (Current == Stage.Pour) Current = Stage.Cup;
        }
    }
}
