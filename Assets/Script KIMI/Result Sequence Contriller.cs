using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class ResultSequenceController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector resultDirector;
    [SerializeField] private PlayableDirector archiveDirector;
    [SerializeField] private PlayableDirector endingDirector;

    [Header("BGM")]
    [SerializeField] private AudioSource resultBGM;
    [SerializeField] private float fadeOutTime = 1.5f;

    private void OnEnable()
    {
        resultDirector.stopped += OnResultFinished;
        archiveDirector.stopped += OnArchiveFinished;
        endingDirector.stopped += OnEndingFinished;
    }

    private void OnDisable()
    {
        resultDirector.stopped -= OnResultFinished;
        archiveDirector.stopped -= OnArchiveFinished;
        endingDirector.stopped -= OnEndingFinished;
    }

    private void Start()
    {
        if (resultBGM != null)
        {
            resultBGM.Play();
        }
    }

    private void OnResultFinished(PlayableDirector director)
    {
        archiveDirector.Play();
    }

    private void OnArchiveFinished(PlayableDirector director)
    {
        endingDirector.Play();
    }

    private void OnEndingFinished(PlayableDirector director)
    {
        StartCoroutine(FadeOutBGM());
    }

    private IEnumerator FadeOutBGM()
    {
        float startVolume = resultBGM.volume;

        float time = 0f;

        while (time < fadeOutTime)
        {
            time += Time.deltaTime;

            resultBGM.volume =
                Mathf.Lerp(startVolume, 0f, time / fadeOutTime);

            yield return null;
        }

        resultBGM.Stop();
        resultBGM.volume = startVolume;
    }
}