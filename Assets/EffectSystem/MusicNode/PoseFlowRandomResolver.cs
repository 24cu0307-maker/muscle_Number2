/*━━━━━━━━━*
*@file PoseFlowRandomResolver.cs*
*@brief ランダム指定ポーズIDをポーズ定義CSV内のIDへ置き換える*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks PoseIDが-1の行をポーズ定義CSV内のIDから等確率で抽選する*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ポーズ進行CSVに含まれるランダム指定を解決する
/// </summary>
public static class PoseFlowRandomResolver
{
    private const int ERandomPoseId = -1; //ランダム指定ID

    /// <summary>
    /// PoseIDが-1の行をポーズ定義CSVに存在するPoseIDへ置き換える
    /// </summary>
    public static void Resolve(
        List<CSVDataPoseFlow> _poseFlowsList,
        List<CSVPoseData> _poseDefinitionsList)
    {
        if (_poseFlowsList == null || _poseFlowsList.Count == 0)return;

        List<int> validPoseIdsList =
            CollectValidPoseIds(_poseDefinitionsList);
        if (validPoseIdsList.Count == 0)
        {
            Debug.LogWarning(
                "masslePoseJudge.csvに有効なPoseIDがないため、"
                + "PoseID=-1をランダム変換できません。");
            return;
        }

        int previousPoseId = ERandomPoseId; //直前に確定したPoseID
        for (int i = 0; i < _poseFlowsList.Count; ++i)
        {
            CSVDataPoseFlow poseFlow = _poseFlowsList[i];
            if (poseFlow.PoseID == ERandomPoseId)
            {
                poseFlow.PoseID = SelectRandomPoseId(
                    validPoseIdsList,
                    previousPoseId);
                _poseFlowsList[i] = poseFlow;
            }

            previousPoseId = poseFlow.PoseID;
        }
    }

    /// <summary>
    /// ポーズ定義CSV内に存在する重複のない有効なPoseIDを集める
    /// </summary>
    private static List<int> CollectValidPoseIds(
        List<CSVPoseData> _poseDefinitionsList)
    {
        List<int> validPoseIdsList = new List<int>();
        if (_poseDefinitionsList == null)return validPoseIdsList;

        for (int i = 0; i < _poseDefinitionsList.Count; ++i)
        {
            int poseId = _poseDefinitionsList[i].PoseID;
            if (poseId < 0 || validPoseIdsList.Contains(poseId))continue;

            validPoseIdsList.Add(poseId);
        }

        return validPoseIdsList;
    }

    /// <summary>
    /// 候補が複数ある場合は直前と異なるPoseIDを抽選する
    /// </summary>
    private static int SelectRandomPoseId(
        List<int> _validPoseIdsList,
        int _previousPoseId)
    {
        int randomIndex = UnityEngine.Random.Range(
            0,
            _validPoseIdsList.Count); //最初の抽選位置
        int selectedPoseId = _validPoseIdsList[randomIndex]; //抽選したPoseID
        if (_validPoseIdsList.Count <= 1
            || selectedPoseId != _previousPoseId)return selectedPoseId;

        randomIndex = (
            randomIndex
            + UnityEngine.Random.Range(1, _validPoseIdsList.Count))
            % _validPoseIdsList.Count;
        return _validPoseIdsList[randomIndex];
    }
}
