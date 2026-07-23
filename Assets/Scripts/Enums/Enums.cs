using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MediapipeBodyPart
{
    // 顔
    nose = 0,              // 鼻
    left_eye_inner,        // 左目（内側）
    left_eye,              // 左目
    left_eye_outer,        // 左目（外側）
    right_eye_inner,       // 右目（内側）
    right_eye,             // 右目
    right_eye_outer,       // 右目（外側）
    left_ear,              // 左耳
    right_ear,             // 右耳
    month_left,            // 口の左端
    month_right,           // 口の右端

    // 上半身
    left_shoulder = 11,    // 左肩
    right_shoulder,        // 右肩
    left_elbow,            // 左肘
    right_elbow,           // 右肘
    left_wrist,            // 左手首
    right_wrist,           // 右手首
    left_pinky,            // 左小指
    right_pinky,           // 右小指
    left_index,            // 左人差し指
    right_index,           // 右人差し指
    left_thumb,            // 左親指
    right_thumb,           // 右親指

    // 下半身
    left_hip = 23,         // 左腰
    right_hip,             // 右腰
    left_knee,             // 左膝
    right_knee,            // 右膝
    left_ankle,            // 左足首
    right_ankle,           // 右足首
    left_heel,             // 左かかと
    right_heel,            // 右かかと
    left_foot_index,       // 左つま先
    right_foot_index = 32, // 右つま先

    // ===== 追加 =====

    lower_spine = 33,      // 下部背骨（腰付近）
    middle_spine,          // 中部背骨
    upper_spine,           // 上部背骨（胸付近）
    neck = 36,             // 首
}

public enum MediapipeHandPart
{
    wrist = 0,             // 手首
    thumb_cmc,             // 親指付け根（手根中手関節）
    thumb_mcp,             // 親指第1関節（中手指節関節）
    thumb_ip,              // 親指第2関節（指節間関節）
    thumb_tip,             // 親指先端

    index_finger_mcp,      // 人差し指付け根
    index_finger_pip,      // 人差し指第1関節
    index_finger_dip,      // 人差し指第2関節
    index_finger_tip,      // 人差し指先端

    middle_finger_mcp,     // 中指付け根
    middle_finger_pip,     // 中指第1関節
    middle_finger_dip,     // 中指第2関節
    middle_finger_tip,     // 中指先端

    ring_finger_mcp,       // 薬指付け根
    ring_finger_pip,       // 薬指第1関節
    ring_finger_dip,       // 薬指第2関節
    ring_finger_tip,       // 薬指先端

    pinky_mcp,             // 小指付け根
    pinky_pip,             // 小指第1関節
    pinky_dip,             // 小指第2関節
    pinky_tip,             // 小指先端

}

public enum PoseUI
{
    Approaching = 0,
    Failure,
    Success,
    Waiting = 3,
}








