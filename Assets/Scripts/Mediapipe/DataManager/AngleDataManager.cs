using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleDataManager : MonoBehaviour
{
    public static AngleDataManager Instance { get; private set; }

    public AngleData angleData { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            angleData = new AngleData();

            // シーンが切り替わっても破棄しない
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    /*
    private void Awake()
    {
        angleData = new AngleData();
    }
    */
    /*
    private void Update()
    {
        angleData = new AngleData();
    }
    */
}
