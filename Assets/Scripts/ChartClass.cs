using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AllObjectsType
{
    N_Hit = 0,
    N_Hold,
    N_Click,
    N_Catch,
    N_Flick_L,
    N_Flick_R,
    N_Swing,
    N_ExHit,
    N_ExHold,
    B_Bar = 10,
    B_Bpm = 11,
    B_Stop = 12,
    B_AreaMove = 13
}
public enum NotesType
{
    N_Hit = 0,
    N_Hold,
    N_Click,
    N_Catch,
    N_Flick_L,
    N_Flick_R,
    N_Swing,
    N_ExHit,
    N_ExHold,
}
public enum OtherObjectsType
{
    B_Bar = 10,
    B_Bpm = 11,
    B_Stop = 12,
    B_AreaMove = 13
}
public enum ChartType
{
    keybord = 0,
    exKeybord,
    mouse,
    areaMove,
    gimmick,
    none = 99
}


public class ObjectData
{
    public float bar = 0f;                  // このデータの開始位置(小節位置)
    public float length_bar = 0f;           // このデータの長さ(Holdでない場合は0, 小節位置)
    public float bar_forTime = 0f;          // このデータの開始位置(小節位置, STOP位置反映)
    public float length_bar_forTime = 0f;   // このデータの長さ(Holdでない場合は0, 小節位置, STOP位置反映)
    public float time = 0f;                 // このデータの開始位置(秒, STOP位置反映)
    public float length_time = 0f;          // このデータの長さ(Holdでない場合は0, 秒, STOP位置反映)
    public float position = 0f;             // 左右位置(レーン系の場合はレーン番号(整数)、それ以外の場合は両端を(-1, 1)とする小数)
    public float data = 0f;                 // データ(BPM値やSTOP値など)
    public GameObject obj = null;           // 描画されるオブジェクト

    public ObjectData(float _bar)
    {
        bar = _bar;
    }
}

public class NoteData : ObjectData
{
    
    public float width;             // 左右幅(全体を2とする)

    public NoteData(float _bar) : base(_bar)
    {

    }
}

public class ExportData
{
    public float bar;
    public int type;
    public float length;
    public float position;
    public float data;

    public ExportData(float _bar, int _type,  float _len, float _pos, float _data)
    {
        bar = _bar;
        type = _type;
        length = _len;
        position = _pos;
        data = _data;
    }
}
