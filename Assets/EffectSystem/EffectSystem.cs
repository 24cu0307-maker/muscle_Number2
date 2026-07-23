using UnityEngine;

//エフェクトをまとめた構造体各構造体に名前を割り振る
[System.Serializable]
public struct EffectData
{
    [SerializeField] private string             s_meffectName;
    [SerializeField] private ParticleSystem[]   pa_mparticles;
    [SerializeField] private AudioSource[]      as_maudioSources;
    [SerializeField] private LightController[] lc_mlightControllers;

    public string EffectName => s_meffectName;
    public ParticleSystem[] Particles => pa_mparticles;
    public AudioSource[] AudioSources => as_maudioSources;
    public LightController[] LightControllers=> lc_mlightControllers;
}

public class EffectSystem : MonoBehaviour
{
    [SerializeField] private EffectData[] EffectDatas;

    public void PlayEffect(string _effectName)
    {
        foreach (EffectData EffectData in EffectDatas)
        {
            //指定された名前と一致した場合に再生してループを抜ける
            if(EffectData.EffectName == _effectName)
            {
                EffectPlay(EffectData);
                return;
            }
        }
    }

    //構造体内にあるエフェクトを再生
    private void EffectPlay(EffectData Effects)
    {
        foreach (ParticleSystem Particle in Effects.Particles)
        {
            Particle.Play();
        }

        foreach (AudioSource Sound in Effects.AudioSources)
        {
            Sound.Play();
        }

        foreach (LightController lightController in Effects.LightControllers)
        {
            lightController.Illumination();
        }
    }
}
