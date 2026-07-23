/*━━━━━━━━━
@file ScoreRankingStorage.cs
@brief スコアランキングCSVの保存と読み込み
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks Excelで扱えるUTF-8 CSVを使用
━━━━━━━━━*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// スコアランキングCSVを実際に読み書きするクラスです。
    /// 保存場所、CSV形式への変換、CSV形式からの復元を担当します。
    /// </summary>
    public static class ScoreRankingStorage
    {
        private const string FolderName = "RankingData";       //CSVを入れるフォルダ名。保存先未指定時はプロジェクト直下に作成する
        private const string FileName = "game-scores.csv";     //スコアランキングCSVのファイル名
        private const string ProjectRootPath = "..";           //Application.dataPathからプロジェクト直下へ戻る相対パス
        private const string CsvSeparator = ",";               //CSV列の区切り文字
        private const string CsvQuote = "\"";                  //CSV文字列を囲むダブルクォート
        private const string CsvEscapedQuote = "\"\"";         //文字列内のダブルクォートをCSV形式で表すための文字
        private const string Header = "ID,PlayerName,Score,RecordedAtUtc"; //新規CSVに書く列名
        private const int HeaderLineCount = 1;                 //CSV先頭のヘッダ行数
        private const int ColumnCount = 4;                     //スコアCSVを1件の記録として読むために必要な列数
        private const int IdColumn = 0;                        //ID列
        private const int PlayerNameColumn = 1;                //プレイヤー名列
        private const int ScoreColumn = 2;                     //スコア列
        private const int RecordedAtUtcColumn = 3;             //保存日時列

        private static string m_sharedDirectoryOverride;       //保存先・読込元を固定したい時に使う共有フォルダパス

        public static string SharedFilePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(m_sharedDirectoryOverride))
                {
                    return Path.Combine(m_sharedDirectoryOverride, FileName);
                }

                return Path.Combine(ProjectOrBuildRoot, FolderName, FileName);
            }
        }

        private static string ProjectOrBuildRoot
        {
            get
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, ProjectRootPath));
            }
        }

        public static void SetSharedDirectory(string _directorypath)
        {
            //空欄なら自動パス、値があれば絶対パスまたはプロジェクト直下基準の相対パスとして扱う。
            m_sharedDirectoryOverride = string.IsNullOrWhiteSpace(_directorypath)
                ? null
                : ResolveDirectoryPath(_directorypath.Trim());
        }

        public static ScoreRankingRecord[] Load()
        {
            if (!File.Exists(SharedFilePath)) { return Array.Empty<ScoreRankingRecord>(); }

            try
            {
                List<ScoreRankingRecord> records = new List<ScoreRankingRecord>(); //読み込んだ記録
                string[] lines = File.ReadAllLines(SharedFilePath, Encoding.UTF8); //CSV全行

                for (int i = HeaderLineCount; i < lines.Length; ++i)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        continue;
                    }

                    List<string> columns = ParseCsvLine(lines[i]); //CSV列
                    ScoreRankingRecord record;                     //CSVから復元した記録

                    if (!TryCreateRecord(columns, out record))
                    {
                        Debug.LogWarning($"スコアCSVの{i + HeaderLineCount}行目を読み飛ばしました。");
                        continue;
                    }

                    records.Add(record);
                }

                return records.ToArray();
            }
            catch (Exception _exception)
            {
                Debug.LogError($"スコアランキングデータの読み込みに失敗しました: {_exception.Message}");
                return Array.Empty<ScoreRankingRecord>();
            }
        }

        public static bool Append(ScoreRankingRecord _record)
        {
            try
            {
                string directory = Path.GetDirectoryName(SharedFilePath); //CSV保存フォルダ

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool isNewFile = !File.Exists(SharedFilePath) || new FileInfo(SharedFilePath).Length == 0; //新規CSVか

                using (StreamWriter writer = new StreamWriter(SharedFilePath, true, new UTF8Encoding(true)))
                {
                    if (isNewFile)
                    {
                        writer.WriteLine(Header);
                    }

                    writer.WriteLine(ToCsvLine(_record));
                }

                return true;
            }
            catch (Exception _exception)
            {
                Debug.LogError($"スコアランキングデータの保存に失敗しました: {_exception.Message}");
                return false;
            }
        }

        private static string ResolveDirectoryPath(string _directorypath)
        {
            string path = _directorypath; //Inspectorや関数から渡された保存先文字列

            if (string.Equals(Path.GetFileName(path), FileName, StringComparison.OrdinalIgnoreCase))
            {
                path = Path.GetDirectoryName(path);
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return ProjectOrBuildRoot;
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(ProjectOrBuildRoot, path));
        }

        private static string ToCsvLine(ScoreRankingRecord _record)
        {
            return string.Join(CsvSeparator, new[]
            {
                Escape(_record.m_id),
                Escape(_record.m_playerName),
                _record.m_score.ToString(CultureInfo.InvariantCulture),
                Escape(_record.m_recordedAtUtc)
            });
        }

        private static bool TryCreateRecord(List<string> _columns, out ScoreRankingRecord _record)
        {
            int score; //CSVから読み取ったスコア
            _record = null;

            if (_columns.Count < ColumnCount)
            {
                return false;
            }

            if (!int.TryParse(_columns[ScoreColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out score))
            {
                return false;
            }

            _record = new ScoreRankingRecord
            {
                m_id = _columns[IdColumn],
                m_playerName = _columns[PlayerNameColumn],
                m_score = score,
                m_recordedAtUtc = _columns[RecordedAtUtcColumn]
            };

            return true;
        }

        private static string Escape(string _value)
        {
            string value = _value ?? string.Empty; //CSV用文字列
            return CsvQuote + value.Replace(CsvQuote, CsvEscapedQuote) + CsvQuote;
        }

        private static List<string> ParseCsvLine(string _line)
        {
            List<string> values = new List<string>();  //CSV列一覧
            StringBuilder value = new StringBuilder(); //現在解析中の列
            bool isQuoted = false;                     //ダブルクォート内か

            for (int i = 0; i < _line.Length; ++i)
            {
                char current = _line[i]; //現在の文字

                if (current == '"')
                {
                    if (isQuoted && i + HeaderLineCount < _line.Length && _line[i + HeaderLineCount] == '"')
                    {
                        value.Append('"');
                        ++i;
                    }
                    else
                    {
                        isQuoted = !isQuoted;
                    }
                }
                else if (current == ',' && !isQuoted)
                {
                    values.Add(value.ToString());
                    value.Length = 0;
                }
                else
                {
                    value.Append(current);
                }
            }

            values.Add(value.ToString());
            return values;
        }
    }
}
