using System;
using UnityEngine;

namespace Memeopoly
{
    public enum MemeopolySfx
    {
        Roll, Step, Build, Reward, Heist, Failure, Menu
    }

    /// <summary>
    /// Fully original synthesised game sounds. No downloaded audio, licenses,
    /// codecs, remote APIs or external audio middleware. WebGL sound begins
    /// only after the user taps, respecting Safari's audio gesture restriction.
    /// </summary>
    public sealed class MemeopolyAudio
    {
        private const int Hz=22050;
        private readonly AudioSource effects;
        private readonly AudioSource ambient;
        private readonly AudioClip[] clips;
        private bool muted;
        private bool unlocked;
        private static AudioClip ambientClip;

        public MemeopolyAudio(Transform parent,bool startMuted)
        {
            var go=new GameObject("MEMEOPOLY ORIGINAL PROCEDURAL SOUND");
            go.transform.SetParent(parent,false);
            effects=go.AddComponent<AudioSource>();
            effects.playOnAwake=false;
            effects.spatialBlend=0;
            effects.volume=.64f;
            ambient=go.AddComponent<AudioSource>();
            ambient.playOnAwake=false;
            ambient.spatialBlend=0;
            ambient.loop=true;
            ambient.volume=.085f;
            clips=new AudioClip[7];
            clips[(int)MemeopolySfx.Roll]=Tone("dice tumble",.26f,220,460,0);
            clips[(int)MemeopolySfx.Step]=Tone("step click",.060f,520,380,1);
            clips[(int)MemeopolySfx.Build]=Tone("building constructed",.65f,240,850,2);
            clips[(int)MemeopolySfx.Reward]=Tone("meme jackpot",.62f,520,1150,2);
            clips[(int)MemeopolySfx.Heist]=Tone("vault opened",.45f,180,720,2);
            clips[(int)MemeopolySfx.Failure]=Tone("rival thwarted",.36f,390,120,0);
            clips[(int)MemeopolySfx.Menu]=Tone("menu toggle",.09f,560,760,1);
            if(ambientClip==null)ambientClip=MakeAmbient();
            ambient.clip=ambientClip;
            SetMuted(startMuted);
        }

        private static AudioClip Tone(string name,float seconds,float startHz,float endHz,int character)
        {
            int samples=Mathf.CeilToInt(seconds*Hz);
            var pcm=new float[samples];
            double phase=0,phase2=0;
            for(int i=0;i<samples;i++)
            {
                float t=(float)i/Math.Max(1,samples-1);
                float frequency=Mathf.Lerp(startHz,endHz,t);
                phase+=2.0*Math.PI*frequency/Hz;
                phase2+=2.0*Math.PI*frequency*1.98/Hz;
                float attack=Mathf.Clamp01(t*23);
                float release=(float)Math.Pow(1-t,1.8);
                float envelope=attack*release;
                float primary=(float)Math.Sin(phase);
                float harmonic=(float)Math.Sin(phase2);
                float wave=character==1?Mathf.Sign(primary)*.48f+harmonic*.18f:
                    character==2?primary*.52f+harmonic*.23f:
                    primary*.62f+harmonic*.18f;
                pcm[i]=Mathf.Clamp(wave*envelope*.64f,-.85f,.85f);
            }
            var clip=AudioClip.Create(name,samples,1,Hz,false);
            clip.SetData(pcm,0);
            return clip;
        }

        private static AudioClip MakeAmbient()
        {
            // 16-beat looping instrumental bed, purposely soft so feedback,
            // taps and dialog remain clear. Seamless at 8 seconds.
            const int samples=Hz*8;
            float[] pcm=new float[samples];
            float[] chord={110f,146.832f,164.814f,195.998f};
            float[] bass={55f,73.416f,82.407f,65.406f};
            for(int i=0;i<samples;i++)
            {
                double time=(double)i/Hz;
                int bar=(int)(time/2)%4;
                float eighth=(float)(time%0.5);
                float pluck=(float)Math.Exp(-eighth*10);
                float tick=(float)Math.Exp(-((time%0.25)*40));
                float smooth=(float)(Math.Sin(2*Math.PI*chord[bar]*time)*.12+
                    Math.Sin(2*Math.PI*bass[bar]*time)*.13+
                    Math.Sin(2*Math.PI*(chord[bar]*2)*time)*pluck*.065);
                float rhythm=(float)(Math.Sin(2*Math.PI*1000*time)*tick*.008);
                pcm[i]=(smooth+rhythm)*.34f;
            }
            // Crossfade the first/last 0.1 seconds to avoid a loop click.
            const int fade=Hz/10;
            for(int i=0;i<fade;i++)
            {
                float a=(float)i/fade;
                float first=pcm[i],last=pcm[samples-fade+i];
                pcm[i]=first*a+last*(1-a);
                pcm[samples-fade+i]=last*a+first*(1-a);
            }
            var clip=AudioClip.Create("Memeopoly eight second seamless theme",samples,1,Hz,false);
            clip.SetData(pcm,0);
            return clip;
        }

        public void SetMuted(bool value)
        {
            muted=value;
            effects.mute=value;
            ambient.mute=value;
            if (value && ambient.isPlaying) ambient.Pause();
            else if(!value && unlocked && !ambient.isPlaying) ambient.UnPause();
        }

        public void Play(MemeopolySfx kind)
        {
            if(muted)return;
            if(!unlocked)
            {
                unlocked=true;
                ambient.Play();
            }
            effects.PlayOneShot(clips[(int)kind]);
        }
    }
}
