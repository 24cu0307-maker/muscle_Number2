using UnityEngine;
using System.Collections;

public class DelayedBGM : MonoBehaviour
{
    // 再生するAudio Source
    [SerializeField]
    private AudioSource audioSource;

    // ゲーム開始から何秒後にBGMを再生するか
    [SerializeField]
    private float delay = 0.5f;

    // このGameObjectが開始されたときに実行される
    private IEnumerator Start()
    {
        // 指定した秒数だけ待つ
        yield return new WaitForSeconds(delay);

        // 待ち時間が終わったらBGMを再生する
        audioSource.Play();
    }
}