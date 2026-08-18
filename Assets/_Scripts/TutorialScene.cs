using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Holistic", LoadSceneMode.Additive);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
