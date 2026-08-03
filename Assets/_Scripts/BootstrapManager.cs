using GameFlowTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapManager : MonoBehaviour
{
    public static BootstrapManager Instance;

    public ExcelLoader ExcelLoader;
    public GameManager GameManager;


    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(ExcelLoader.gameObject);
        DontDestroyOnLoad(GameManager.gameObject);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }
}