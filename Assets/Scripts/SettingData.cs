using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SettingData
{
    public readonly static CursorChange Instance = new CursorChange();

    public string audioDirectory = string.Empty;
    public string chartDirectory = string.Empty;

    public float bgm_volume = 1f;
    public bool bgm_isMute = false;
    public float se_volume = 1f;
    public bool se_isMute = false;

    public SettingData()
    {
        audioDirectory = string.Empty;
        chartDirectory = string.Empty;

        bgm_volume = 1f;
        bgm_isMute = false;
        se_volume = 1f;
        se_isMute = false;
    }
}
