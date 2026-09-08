using UnityEngine;
using UnityEngine.Playables;

public class ResultSequenceController : MonoBehaviour
{
    [SerializeField] private PlayableDirector resultDirector;
    [SerializeField] private PlayableDirector archiveDirector;
    [SerializeField] private PlayableDirector endingDirector;

    private void OnEnable()
    {
        resultDirector.stopped += OnResultFinished;
        archiveDirector.stopped += OnArchiveFinished;
    }

    private void OnDisable()
    {
        resultDirector.stopped -= OnResultFinished;
        archiveDirector.stopped -= OnArchiveFinished;
    }

    private void OnResultFinished(PlayableDirector director)
    {
        archiveDirector.Play();
    }

    private void OnArchiveFinished(PlayableDirector director)
    {
        endingDirector.Play();
    }
}
