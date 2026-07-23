/*━━━━━━━━━
@file RankingDisplaySystem.cs
@brief アタッチだけで使えるランキング表示セットアップ
@author 24CU0000 Name
@date 2026/07/09
最終更新日 2026/07/09
@remarks 表示側UIへ順位・名前・時間を流し込む
━━━━━━━━━*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CrossProjectRanking
{
    /// <summary>
    /// 表示側Unityに置くための実機向けセットアップスクリプトです。
    /// 空のGameObjectにこのスクリプトを付けるだけで、読み込み・出力用コンポーネントがそろいます。
    /// 既存UIがある場合は各Textへ割り当て、未設定なら確認用Textを自動作成します。
    /// </summary>
    [RequireComponent(typeof(RankingDataLoader))]
    [RequireComponent(typeof(RankingOutput))]
    public sealed class RankingDisplaySystem : MonoBehaviour
    {
        private const string DefaultSharedDirectoryOverride = "../RankingData"; //親フォルダのRankingDataを共有フォルダとして使う初期設定
        private const int DefaultMaximumRows = 10;                             //初期状態で表示するランキング件数
        private const int DefaultTargetFrameRate = 10;                          //ランキング表示専用画面の初期FPS。ゲーム側への負荷を抑える
        private const int MinimumValue = 1;                                     //Inspectorで0以下が入らないようにするための最小値
        private const float DefaultTextWidth = 900.0f;                          //自動生成Textの横幅
        private const float DefaultTextHeight = 700.0f;                         //自動生成Textの縦幅
        private const float DefaultTextPositionX = 40.0f;                       //自動生成TextのX位置
        private const float DefaultTextPositionY = -40.0f;                      //自動生成TextのY位置
        private const int DefaultFontSize = 32;                                 //自動生成Textの文字サイズ
        private const string LineBreak = "\n";                                  //複数行表示用の改行文字
        private const string EmptyMessage = "ランキングデータがありません";    //表示対象がない時の表示文

        [Tooltip("入力側と表示側で共有するRankingDataフォルダ。親フォルダ共有形式なら ../RankingData のままで使います。")]
        [SerializeField] private string m_sharedDirectoryOverride = DefaultSharedDirectoryOverride; //CSV読込元フォルダ。入力側と同じ値にする
        [SerializeField, Min(MinimumValue)] private int m_maximumRows = DefaultMaximumRows; //UIへ表示・出力する最大ランキング件数
        [SerializeField, Min(MinimumValue)] private int m_targetFrameRate = DefaultTargetFrameRate; //表示側UnityのFPS。ランキング用途なので低めにする
        [SerializeField] private bool m_bReduceCpuLoad = true;                 //ONならFPSを下げ、表示側UnityのCPU負荷を抑える
        [SerializeField] private bool m_bCreateTextWhenMissing = true;         //Text未設定時に確認用UIを自動生成するか
        [SerializeField] private TMP_Text m_singleRankingText;                 //ランキング全体を1つのTextにまとめて表示する場合の出力先
        [SerializeField] private TMP_Text[] m_rankTexts;                       //順位だけを個別Textへ表示する場合の出力先配列
        [SerializeField] private TMP_Text[] m_playerNameTexts;                 //プレイヤー名だけを個別Textへ表示する場合の出力先配列
        [SerializeField] private TMP_Text[] m_timeTexts;                       //時間だけを個別Textへ表示する場合の出力先配列
        [SerializeField] private string m_emptyMessage = EmptyMessage;         //記録がない時に表示する文言

        private RankingDataLoader m_loader;                                    //CSVを読み込み、更新を検知するコンポーネント
        private RankingOutput m_output;                                        //読み込んだランキングをUI向けの行データへ変換するコンポーネント

        private void Awake()
        {
            //表示専用Unityは高FPSが不要なため、必要に応じてフレームレートを下げる。
            if (m_bReduceCpuLoad)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = m_targetFrameRate;
            }

            //同じGameObjectにある読み込み部品と出力部品を取得する。
            m_loader = GetComponent<RankingDataLoader>();
            m_output = GetComponent<RankingOutput>();

            //LoaderのAwakeより後でも、Startの読み込み前に共有フォルダを反映する。
            ApplySettings();

            if (m_singleRankingText == null && !HasColumnText() && m_bCreateTextWhenMissing)
            {
                m_singleRankingText = CreateDefaultRankingText();
            }
        }

        private void OnEnable()
        {
            if (m_output != null)
            {
                m_output.Outputted += Render;
            }
        }

        private void Start()
        {
            if (m_output != null)
            {
                ApplySettings();
                m_output.Reload();
                Render(m_output.Rows);
            }
        }

        private void OnDisable()
        {
            if (m_output != null)
            {
                m_output.Outputted -= Render;
            }
        }

        public RankingOutputRow[] GetRows()
        {
            //外部UIスクリプトがランキング配列を直接取得したい場合に使う。
            if (m_output == null) { return new RankingOutputRow[0]; }

            return m_output.GetRows();
        }

        public void Reload()
        {
            //外部ボタンなどから手動更新したい場合に使う。
            if (m_output == null) { return; }

            ApplySettings();
            m_output.Reload();
        }

        private void ApplySettings()
        {
            //読み込み・出力部品へ、アタッチ用スクリプト側の設定を反映する。
            //これにより、Inspectorでこのスクリプトだけ見れば基本設定を完結できる。
            if (m_loader != null)
            {
                m_loader.SetSharedDirectoryOverride(m_sharedDirectoryOverride);
            }
            else
            {
                RankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
            }

            if (m_output != null)
            {
                m_output.SetMaximumRows(m_maximumRows);
            }
        }

        private void Render(RankingOutputRow[] _rows)
        {
            RankingOutputRow[] rows = _rows ?? new RankingOutputRow[0]; //表示するランキング行

            RenderSingleText(rows);
            RenderColumnTexts(rows);
        }

        private void RenderSingleText(RankingOutputRow[] _rows)
        {
            if (m_singleRankingText == null) { return; }

            if (_rows.Length == 0)
            {
                m_singleRankingText.text = m_emptyMessage;
                return;
            }

            List<string> lines = new List<string>(); //TextMeshProへまとめて入れるランキング行

            for (int i = 0; i < _rows.Length; ++i)
            {
                lines.Add($"{_rows[i].m_rank,2}位  {_rows[i].m_playerName}  {_rows[i].m_timeText}");
            }

            m_singleRankingText.text = string.Join(LineBreak, lines);
        }

        private void RenderColumnTexts(RankingOutputRow[] _rows)
        {
            int count = GetMaximumColumnCount(); //UI列配列のうち、実際に更新する最大件数

            for (int i = 0; i < count; ++i)
            {
                bool hasRow = i < _rows.Length; //この行に表示するランキングデータが存在するか

                SetText(m_rankTexts, i, hasRow ? _rows[i].m_rank.ToString() : string.Empty);
                SetText(m_playerNameTexts, i, hasRow ? _rows[i].m_playerName : string.Empty);
                SetText(m_timeTexts, i, hasRow ? _rows[i].m_timeText : string.Empty);
            }
        }

        private bool HasColumnText()
        {
            //既存UIが1つでも割り当て済みなら、自動Text生成は不要と判断する。
            return HasAnyText(m_rankTexts) || HasAnyText(m_playerNameTexts) || HasAnyText(m_timeTexts);
        }

        private static bool HasAnyText(TMP_Text[] _texts)
        {
            if (_texts == null) { return false; }

            for (int i = 0; i < _texts.Length; ++i)
            {
                if (_texts[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetMaximumColumnCount()
        {
            //順位・名前・時間の配列の中で一番長い件数まで更新する。
            int rankCount = m_rankTexts == null ? 0 : m_rankTexts.Length;               //順位Text数
            int nameCount = m_playerNameTexts == null ? 0 : m_playerNameTexts.Length;   //名前Text数
            int timeCount = m_timeTexts == null ? 0 : m_timeTexts.Length;               //時間Text数

            return Mathf.Max(rankCount, nameCount, timeCount);
        }

        private static void SetText(
            TMP_Text[] _texts,
            int _index,
            string _value)
        {
            //指定配列にTextが存在する場合だけ文字を更新する。
            if (_texts == null || _index < 0 || _index >= _texts.Length || _texts[_index] == null) { return; }

            _texts[_index].text = _value;
        }

        private TMP_Text CreateDefaultRankingText()
        {
            GameObject canvasObject = new GameObject("RankingCanvas"); //確認用Textを乗せるCanvas
            Canvas canvas = canvasObject.AddComponent<Canvas>();       //画面表示用Canvas
            CanvasScalerBridge.AddScaler(canvasObject);                //解像度差を吸収するためのScaler
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject textObject = new GameObject("RankingText");     //ランキング文字を表示するText
            textObject.transform.SetParent(canvasObject.transform, false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            RectTransform rectTransform = text.rectTransform;          //Textの表示範囲
            rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            rectTransform.pivot = new Vector2(0.0f, 1.0f);
            rectTransform.anchoredPosition = new Vector2(DefaultTextPositionX, DefaultTextPositionY);
            rectTransform.sizeDelta = new Vector2(DefaultTextWidth, DefaultTextHeight);

            text.fontSize = DefaultFontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.text = m_emptyMessage;

            return text;
        }
    }
}
