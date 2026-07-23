using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

///<summary>
///ŽžŠÔŒo‰ß
///</summary>

public class Timer : MonoBehaviour
{
    private float time;

    private bool isRunning = true;

    void Update()
    {

        if (!isRunning) return;

        time += Time.deltaTime;
    }

    public float CurrentTime()
    {
        return time;
    }

    public void StopTime()
    {
        isRunning = false;
    }

    public void StartTime()
    {
        isRunning = true;
    }

}

/*
class InGametimer
{
    bool check;
    public InGametimer(bool b_check)
    {
        check = b_check;
    }

    public float InGametime(float time)
    {
        if(check)
        {
            time += Time.deltaTime;
        }
        return time;
    }
    
}
*/