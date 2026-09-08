using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class ResultTimelineEnd : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private string titleSceneName = "Title_OP";

    private void OnEnable()
    {
        director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        director.stopped -= OnTimelineStopped;
    }

    private void OnTimelineStopped(PlayableDirector playableDirector)
    {
        SceneManager.LoadScene(titleSceneName);
    }
}