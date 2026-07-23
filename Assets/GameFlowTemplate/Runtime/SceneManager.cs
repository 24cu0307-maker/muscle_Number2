/*━━━━━━━━━
@file SceneManager.cs
@brief ゲーム開始・終了などのシーン管理
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks Unity標準SceneManagerとは名前空間で分離
━━━━━━━━━*/

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GameFlowTemplate
{
    /// <summary>
    /// ゲーム開始、リトライ、結果画面などのシーン遷移を担当するマネージャーです。
    /// Unity標準のSceneManagerとは名前空間で分離しています。
    /// </summary>
    public sealed class SceneManager : MonoBehaviour
    {
        private const string EmptySceneName = ""; //シーン名未設定を判定するための空文字

        [SerializeField] private string m_titleSceneName = EmptySceneName;  //タイトル画面のシーン名
        [SerializeField] private string m_gameSceneName = EmptySceneName;   //インゲーム画面のシーン名
        [SerializeField] private string m_resultSceneName = EmptySceneName; //結果画面のシーン名
        [SerializeField] private bool m_bUseResultScene = false;            //ゲーム終了時に結果シーンへ遷移するか

        public event Action<string> SceneLoadRequested;                     //シーンロード要求時に通知するイベント

        public void LoadTitleScene()
        {
            //タイトル画面へ遷移する。
            LoadScene(m_titleSceneName);
        

        }

        public void LoadGameScene()
        {
            //インゲーム画面へ遷移する。
            LoadScene(m_gameSceneName);
        }

        public void LoadResultScene()
        {
            //結果画面へ遷移する。
            LoadScene(m_resultSceneName);
        }

        public void LoadScene(string _scenename)
        {
            //指定シーン名が空なら、誤遷移を防ぐため何もしない。
            if (string.IsNullOrWhiteSpace(_scenename)) { return; }

            SceneLoadRequested?.Invoke(_scenename);
            UnitySceneManager.LoadScene(_scenename);
        }

        public void LoadResultSceneIfNeeded()
        {
            //結果専用シーンを使う設定の場合だけ遷移する。
            if (!m_bUseResultScene) { return; }

            LoadResultScene();
        }
    }
}
