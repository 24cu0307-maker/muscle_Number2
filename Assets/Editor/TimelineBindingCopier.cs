//君山_AIまるまるコピーのコードです。タイムラインの設定をコピーしたかったのですがそのままでは出来そうになかったのでエディタを作りました。

// Unity Editorを拡張するために必要
using UnityEditor;

// GameObjectやObjectなど、Unityの基本機能を使用するために必要
using UnityEngine;

// PlayableDirectorを使用するために必要
using UnityEngine.Playables;

// TimelineAssetやTrackAssetを使用するために必要
using UnityEngine.Timeline;

// Listを使用するために必要
using System.Collections.Generic;


// Unity Editor上に専用ウィンドウを作成するクラス
// 今回は「TimelineのBindingをコピーするツール」を作る
public class TimelineBindingCopier : EditorWindow
{
    // コピー元のPlayable Director
    // 例：Opening_Director
    private PlayableDirector sourceDirector;

    // コピー先のPlayable Director
    // 例：TitleIdle_Director
    private PlayableDirector targetDirector;


    // ---------------------------------------------------------
    // Unity上部のToolsメニューにツールを追加する
    // ---------------------------------------------------------

    // Unity上部に
    //
    // Tools
    // └ Timeline Binding Copier
    //
    // という項目を追加する
    [MenuItem("Tools/Timeline Binding Copier")]
    public static void OpenWindow()
    {
        // Timeline Binding Copierという名前のEditorウィンドウを開く
        GetWindow<TimelineBindingCopier>("Timeline Binding Copier");
    }


    // ---------------------------------------------------------
    // Editorウィンドウの見た目を作る
    // ---------------------------------------------------------

    private void OnGUI()
    {
        // ウィンドウ上部にタイトルを表示
        GUILayout.Label(
            "Timeline Binding Copier",
            EditorStyles.boldLabel
        );

        // 少し空白を入れる
        EditorGUILayout.Space();


        // コピー元Directorを指定する欄
        //
        // HierarchyからOpening_Directorなどを
        // ドラッグ＆ドロップして設定する
        sourceDirector =
            (PlayableDirector)EditorGUILayout.ObjectField(
                "Source Director",
                sourceDirector,
                typeof(PlayableDirector),
                true
            );


        // コピー先Directorを指定する欄
        //
        // HierarchyからTitleIdle_Directorなどを
        // ドラッグ＆ドロップして設定する
        targetDirector =
            (PlayableDirector)EditorGUILayout.ObjectField(
                "Target Director",
                targetDirector,
                typeof(PlayableDirector),
                true
            );


        // 少し空白を入れる
        EditorGUILayout.Space();


        // 「Copy Bindings」というボタンを作る
        if (GUILayout.Button("Copy Bindings"))
        {
            // ボタンが押されたらBindingのコピー処理を実行
            CopyBindings();
        }
    }


    // ---------------------------------------------------------
    // Bindingを実際にコピーする処理
    // ---------------------------------------------------------

    private void CopyBindings()
    {
        // SourceまたはTargetが設定されていなければ処理を止める
        if (sourceDirector == null || targetDirector == null)
        {
            Debug.LogError(
                "Source Director と Target Director を設定してください。"
            );

            return;
        }


        // Source Directorに設定されているPlayable Assetを取得して
        // TimelineAssetとして扱う
        TimelineAsset sourceTimeline =
            sourceDirector.playableAsset as TimelineAsset;


        // Target Directorに設定されているPlayable Assetを取得して
        // TimelineAssetとして扱う
        TimelineAsset targetTimeline =
            targetDirector.playableAsset as TimelineAsset;


        // どちらかがTimelineではなかった場合は処理を止める
        if (sourceTimeline == null || targetTimeline == null)
        {
            Debug.LogError(
                "両方のPlayable DirectorにTimeline Assetを設定してください。"
            );

            return;
        }


        // コピー元Timelineに存在するTrackを保存するリスト
        List<TrackAsset> sourceTracks =
            new List<TrackAsset>();

        // コピー先Timelineに存在するTrackを保存するリスト
        List<TrackAsset> targetTracks =
            new List<TrackAsset>();


        // Source TimelineにあるTrackをすべて取得する
        CollectTracks(
            sourceTimeline.GetRootTracks(),
            sourceTracks
        );


        // Target TimelineにあるTrackをすべて取得する
        CollectTracks(
            targetTimeline.GetRootTracks(),
            targetTracks
        );


        // この操作をUnityのUndo対象にする
        //
        // つまりBindingをコピーした後でも
        // Ctrl + Zで元に戻せるようにする
        Undo.RecordObject(
            targetDirector,
            "Copy Timeline Bindings"
        );


        // 何個Bindingをコピーできたか数える
        int copiedCount = 0;


        // コピー先Timelineに存在するTrackを
        // 1本ずつ調べていく
        foreach (TrackAsset targetTrack in targetTracks)
        {
            // コピー先Trackに対応する
            // コピー元Trackを探す
            TrackAsset sourceTrack =
                FindMatchingTrack(
                    targetTrack,
                    targetTracks,
                    sourceTracks
                );


            // 対応するTrackが見つからなかった場合は
            // このTrackを飛ばして次へ進む
            if (sourceTrack == null)
            {
                continue;
            }


            // Source Directorから、
            // このTrackに割り当てられているGameObject等を取得する
            //
            // 例えば
            //
            // Light Animation Track
            //     ↓
            // LightParts_body
            //
            // の「LightParts_body」の部分を取得している
            Object binding =
                sourceDirector.GetGenericBinding(sourceTrack);


            // Bindingが存在している場合
            if (binding != null)
            {
                // コピー先Directorの対応するTrackに
                // 同じBindingを設定する
                targetDirector.SetGenericBinding(
                    targetTrack,
                    binding
                );


                // コピー成功数を+1
                copiedCount++;
            }
        }


        // Unityに
        // 「Target Directorの内容が変更された」
        // ということを知らせる
        EditorUtility.SetDirty(targetDirector);


        // コピー結果をConsoleに表示
        Debug.Log(
            $"Timeline Binding Copy 完了: " +
            $"{copiedCount}個のBindingをコピーしました。"
        );
    }


    // ---------------------------------------------------------
    // Timeline内に存在するTrackを全部取得する処理
    // ---------------------------------------------------------

    private void CollectTracks(
        IEnumerable<TrackAsset> tracks,
        List<TrackAsset> result
    )
    {
        // 渡されたTrackを1本ずつ確認する
        foreach (TrackAsset track in tracks)
        {
            // Trackをリストへ追加
            result.Add(track);


            // このTrackの中に子Trackが存在する場合
            //
            // 例：
            //
            // Light
            // ├ Animation Track
            // └ Animation Track
            //
            // のような階層にも対応する
            if (track.GetChildTracks() != null)
            {
                // 子Trackについても同じ処理を行う
                CollectTracks(
                    track.GetChildTracks(),
                    result
                );
            }
        }
    }


    // ---------------------------------------------------------
    // コピー先Trackに対応するコピー元Trackを探す
    // ---------------------------------------------------------

    private TrackAsset FindMatchingTrack(
        TrackAsset targetTrack,
        List<TrackAsset> targetTracks,
        List<TrackAsset> sourceTracks
    )
    {
        // コピー先Trackの「階層を含めた名前」を取得
        //
        // 例えば
        //
        // Lights/Center/Rotation
        //
        // のような文字列になる
        string targetPath =
            GetTrackPath(targetTrack);


        // コピー元TimelineのTrackを
        // 1本ずつ確認する
        foreach (TrackAsset sourceTrack in sourceTracks)
        {
            // Trackの種類が違う場合は候補から外す
            //
            // 例えば
            //
            // Animation Track
            // と
            // Activation Track
            //
            // を間違えて対応させないため
            if (sourceTrack.GetType() != targetTrack.GetType())
            {
                continue;
            }


            // コピー元Trackの階層を含めた名前を取得
            string sourcePath =
                GetTrackPath(sourceTrack);


            // 階層＋Track名が同じなら
            // 対応するTrackと判断する
            if (sourcePath == targetPath)
            {
                return sourceTrack;
            }
        }


        // 対応するTrackが見つからなかった
        return null;
    }


    // ---------------------------------------------------------
    // Trackの階層を含めた名前を作る
    // ---------------------------------------------------------

    private string GetTrackPath(TrackAsset track)
    {
        // まず現在のTrack名を取得
        string path = track.name;


        // 親Trackを取得
        TrackAsset parent =
            track.parent as TrackAsset;


        // 親Trackが存在している限り
        // 上の階層へ遡っていく
        while (parent != null)
        {
            // 親Track名を前に追加する
            //
            // 例：
            //
            // Rotation
            //
            // ↓
            //
            // Center/Rotation
            //
            // ↓
            //
            // Lights/Center/Rotation
            //
            path = parent.name + "/" + path;


            // さらに1つ上の親へ移動
            parent =
                parent.parent as TrackAsset;
        }


        // 完成したTrackのパスを返す
        return path;
    }
}
