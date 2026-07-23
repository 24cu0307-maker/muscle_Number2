using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionDataManager : MonoBehaviour
{
    public static PositionDataManager Instance { get; private set; }

    public PositionData positionData { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            positionData = new PositionData();

            // シーンが切り替わっても破棄しない
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

/*
public class PositionDataManager : MonoBehaviour
{
    public PositionData positionData { get; private set; }


    private void Awake()
    {
        positionData = new PositionData();
    }

    /*
    private void Update()
    {
        positionData = new PositionData();
    }
    //
}
*/