using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScene : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Holistic", LoadSceneMode.Additive);
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Filming", LoadSceneMode.Additive);


        GameObject player = Instantiate(playerPrefab);
        DontDestroyOnLoad(player);
    }

    
        //UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial");
    

    // Update is called once per frame
    void Update()
    {
        /*
        Scene holisticScene = SceneManager.GetSceneByName("Holistic1");
        if (holisticScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(holisticScene);
        }
        */
    }
}
