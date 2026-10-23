using UnityEngine;
namespace Hana.CatCafe
{
    // Original synthesized sounds: no external audio assets or downloads.
    public sealed class CafeSounds:System.IDisposable
    {
        readonly AudioSource effects,flow;
        readonly AudioClip pour,purr,chime,plate;
        public bool Muted {get;private set;}
        const string Key="Hana.CatCafe.Muted.v1";
        public CafeSounds(GameObject host)
        {
            effects=host.AddComponent<AudioSource>();flow=host.AddComponent<AudioSource>();
            effects.playOnAwake=flow.playOnAwake=false;effects.spatialBlend=flow.spatialBlend=0;
            effects.volume=.30f;flow.volume=.12f;flow.loop=true;
            pour=Make(0,1);purr=Make(1,1.2f);chime=Make(2,.65f);plate=Make(3,.28f);
            flow.clip=pour;SetMuted(PlayerPrefs.GetInt(Key,0)==1,false);
        }
        public void SetMuted(bool value,bool save=true)
        {
            Muted=value;effects.mute=flow.mute=value;
            if(value)Stop();
            if(save){PlayerPrefs.SetInt(Key,value?1:0);PlayerPrefs.Save();}
        }
        public void Flow(bool active)
        {
            if(active&&!Muted){if(!flow.isPlaying)flow.Play();}
            else if(flow.isPlaying)flow.Stop();
        }
        public void Play(int kind)
        {
            if(Muted)return;
            effects.Stop();effects.clip=kind==0?plate:kind==1?purr:chime;effects.Play();
        }
        public void Stop(){effects.Stop();flow.Stop();}
        static AudioClip Make(int kind,float seconds)
        {
            const int rate=22050;var data=new float[Mathf.RoundToInt(rate*seconds)];
            var random=new System.Random(702+kind);float smooth=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;
                smooth=Mathf.Lerp(smooth,(float)random.NextDouble()*2-1,.12f);
                float v;
                if(kind==0)v=smooth*(.45f+.25f*Mathf.Sin(2*Mathf.PI*7*t))+.06f*Mathf.Sin(2*Mathf.PI*340*t);
                else if(kind==1)v=(Mathf.Sin(2*Mathf.PI*90*t)*.45f+smooth*.4f)*(.55f+.45f*Mathf.Sin(2*Mathf.PI*25*t));
                else if(kind==2)v=(Mathf.Sin(2*Mathf.PI*660*t)+.4f*Mathf.Sin(2*Mathf.PI*990*t))*Mathf.Exp(-6*t)*.35f;
                else v=(Mathf.Sin(2*Mathf.PI*1250*t)+.3f*Mathf.Sin(2*Mathf.PI*1900*t))*Mathf.Exp(-20*t)*.4f;
                float fade=Mathf.Min(1,Mathf.Min(t/.02f,(seconds-t)/.04f));data[i]=v*fade;
            }
            var clip=AudioClip.Create("Cafe handmade sound "+kind,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void Dispose()
        {
            Stop();Object.Destroy(effects);Object.Destroy(flow);
            Object.Destroy(pour);Object.Destroy(purr);Object.Destroy(chime);Object.Destroy(plate);
        }
    }
}
