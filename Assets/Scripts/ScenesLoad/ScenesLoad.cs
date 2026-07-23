using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

///<summary>
///— ‚ÅMediapipe‚ð“®‚©‚·
///</summary>

public class ScenesLoad : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return SceneManager.LoadSceneAsync(
            "Holistic",
            LoadSceneMode.Additive);


        /*
        yield return SceneManager.LoadSceneAsync(
            "InGame",
            LoadSceneMode.Additive);
        */
    }
}
