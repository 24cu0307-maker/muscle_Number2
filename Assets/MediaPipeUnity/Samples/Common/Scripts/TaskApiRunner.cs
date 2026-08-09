// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mediapipe.Unity.Sample
{
  public abstract class BaseRunner : MonoBehaviour
  {
    private static readonly string _BootstrapName = nameof(Bootstrap);
    private static Bootstrap _sharedBootstrap;

    [SerializeField] private GameObject _bootstrapPrefab;

#pragma warning disable IDE1006
    // TODO: make it static
    protected virtual string TAG => GetType().Name;
#pragma warning restore IDE1006

    protected Bootstrap bootstrap;
    protected bool isPaused;

    private readonly Stopwatch _stopwatch = new();

    protected virtual IEnumerator Start()
    {
      bootstrap = FindBootstrap();
      yield return new WaitUntil(() => bootstrap.isFinished);

      Play();
    }

    /// <summary>
    ///   Start the main program from the beginning.
    /// </summary>
    public virtual void Play()
    {
      isPaused = false;
      _stopwatch.Restart();
    }

    /// <summary>
    ///   Pause the main program.
    /// <summary>
    public virtual void Pause()
    {
      isPaused = true;
    }

    /// <summary>
    ///    Resume the main program.
    ///    If the main program has not begun, it'll do nothing.
    /// </summary>
    public virtual void Resume()
    {
      isPaused = false;
    }

    /// <summary>
    ///   Stops the main program.
    /// </summary>
    public virtual void Stop()
    {
      isPaused = true;
      _stopwatch.Stop();
    }

    protected long GetCurrentTimestampMillisec() => _stopwatch.IsRunning ? _stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond : -1;

    protected Bootstrap FindBootstrap()
    {
      // 複数のRunnerが同じフレームで初期化されてもBootstrapを一つだけ共有する。
      // GameObject.Findだけに頼ると、生成タイミングによって各RunnerがPrefabを
      // 個別にInstantiateする可能性があるため、静的参照を共通の取得口にする。
      if (_sharedBootstrap != null)
      {
        return _sharedBootstrap;
      }

      _sharedBootstrap = FindFirstObjectByType<Bootstrap>(FindObjectsInactive.Include);

      if (_sharedBootstrap == null)
      {
        Debug.Log("Initializing the Bootstrap GameObject");
        var bootstrapObj = Instantiate(_bootstrapPrefab);
        bootstrapObj.name = _BootstrapName;
        DontDestroyOnLoad(bootstrapObj);
        _sharedBootstrap = bootstrapObj.GetComponent<Bootstrap>();
      }

      return _sharedBootstrap;
    }
  }
}
