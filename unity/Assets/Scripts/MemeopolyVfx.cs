using UnityEngine;

namespace Memeopoly
{
    /// <summary>
    /// Lightweight celebratory effects authored as runtime Unity particles.
    /// No imported textures or shader bundles required for iPhone/Android/WebGL.
    /// </summary>
    public static class MemeopolyVfx
    {
        public static void Burst(Transform parent,Vector3 localPosition,bool reward,int count=36)
        {
            var go=new GameObject(reward?"Meme reward confetti":"Rival impact sparks");
            go.transform.SetParent(parent,false);
            go.transform.localPosition=localPosition;
            var emitter=go.AddComponent<ParticleSystem>();
            emitter.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=emitter.main;
            main.loop=false;
            main.duration=.38f;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.5f,1.1f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(2.4f,5.7f);
            main.startSize=new ParticleSystem.MinMaxCurve(.10f,.20f);
            main.gravityModifier=.7f;
            main.maxParticles=150;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=reward?
                new ParticleSystem.MinMaxGradient(
                    new Color(1f,.85f,.27f),new Color(.29f,.94f,.88f)):
                new ParticleSystem.MinMaxGradient(
                    new Color(1f,.28f,.44f),new Color(.58f,.32f,1f));

            var emission=emitter.emission;
            emission.rateOverTime=0;
            var shape=emitter.shape;
            shape.shapeType=ParticleSystemShapeType.Sphere;
            shape.radius=.18f;
            emitter.Emit(Mathf.Clamp(count,1,100));
            Object.Destroy(go,2f);
        }
    }
}
