using chChartEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LineManager : MonoBehaviour {

    [SerializeField]
    private GameManager gameMng;
    [SerializeField]
    private NoteManager noteMng;

    [SerializeField]
    private ObjectPool linePool;
    [SerializeField]
    private ObjectPool snapYPool;
    [SerializeField]
    private ObjectPool snapXPool;

    [SerializeField]
    private GameObject linePrefab;
    [SerializeField]
    private GameObject snapY_prefab;
    [SerializeField]
    private GameObject snapX_prefab;

    [SerializeField]
    private GameObject endLine;
    [SerializeField]
    private GameObject fillterObj;

    [SerializeField]
    private LineRenderer[] vLines = new LineRenderer[7];
    [SerializeField]
    private SortingGroup areaMoveObjSort;

    [SerializeField]
    private Light light;

    public void SnapY_toggle(bool isSnap)
    {
        snapYPool.gameObject.SetActive(isSnap);
    }
    public void SnapX_toggle(bool isSnap)
    {
        snapXPool.gameObject.SetActive(isSnap);
    }

    public void SetUpBPMChange()
    {
        // 小節線リストを全消去
        noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Clear();
        // 最初の小節線データを追加 (4/4拍子)
        ObjectData firstMeasure = new ObjectData(0f);
        firstMeasure.length_bar = 1f;
        firstMeasure.data = 4;
        noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Add(firstMeasure);

        // BPM変化リストを全消去
        noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Clear();
        // 最初のBPMデータを追加
        noteMng.AddNoteFromBar(0f, (int)OtherObjectsType.B_Bpm, 0f, 0f, 128f, false);

        // 譜面停止リストを全消去
        noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10].Clear();

        // レーン位置変化リストを全消去
        noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10].Clear();
        // 最初のレーン位置変化データを追加
        noteMng.AddNoteFromBar(0f, (int)OtherObjectsType.B_AreaMove, 0f, 0f, 0f, false);
    }

    /// <summary>
    /// スナップ線と小節線を設定する
    /// </summary>
    public void SetLine(float length, int split, bool toggle)
    {
        snapYPool.gameObject.SetActive(true);

        linePool.ReleaseAllGameObjects(linePrefab);
        snapYPool.ReleaseAllGameObjects(snapY_prefab);

        Vector3 position;
        float bar_tmp;
        float bar_offset;
        float bpm;
        // length_barには小節長を、dataには分母を格納することで、分子/分母のデータを引き出す

        if (noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Count > 0)
            bpm = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][0].data;
        else
            return;

        bar_offset = GetBarOffset();

        float measure = 0f;
        int measure_d = 0;  // 拍子の分母
        int measure_n = 0;  // 拍子の分子
        bool isChanged = false;
        bar_tmp = 0f;
        int barCnt = 1;
        int listPtr = 0;

        float maxBar = GetNoteBarFromMusicTime(length) - bar_offset;

        while (bar_tmp <= maxBar)
        {

            // 小節線データが足りない場合は増やす
            if (listPtr >= noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Count)
            {
                ObjectData newBar = new ObjectData(bar_tmp);
                newBar.length_bar = measure;
                newBar.data = measure_d;
                noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Add(newBar);
            }

            // 拍子変化時
            if (noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].length_bar != measure || noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].data != measure_d)
            {
                measure = noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].length_bar;
                measure_d = (int)noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].data;
                measure_n = (int)(measure * measure_d);
                isChanged = true;
            }
            else
                isChanged = false;


            position = new Vector3(0f, GetNotePosFromNoteBar(bar_tmp, gameMng.camExpansionRate), 0f);

            GameObject obj = linePool.GetGameObject(linePrefab, position, Quaternion.identity);
            obj.GetComponent<Line>().SetUp(barCnt, bar_tmp, isChanged, measure_n, measure_d);
            noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].obj = obj;

            bar_tmp += measure;
            barCnt++;
            listPtr++;

        }

        

        // 小節線データが多すぎる場合は減らす
        if (listPtr < noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Count)
        {
            for (int i = noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Count - 1; i >= listPtr; i--)
                noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].RemoveAt(i);
        }

        endLine.transform.localPosition = new Vector3(0f, GetNotePosFromNoteBar(maxBar, gameMng.camExpansionRate), 0f);

        measure = noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][0].length_bar;
        float nextBar = measure;
        bar_tmp = 0f;
        barCnt = 1;
        listPtr = Mathf.Min(noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Count - 1, 1);
        int cnt = 0;

        GameObject snapObj;
        LineRenderer lineRenderer;

        while (bar_tmp <= maxBar) {

            // 引いたグリッド線が1小節分に達した場合：次小節に移動
            if (cnt >= split * measure)
            {

                bar_tmp = nextBar;
                cnt = 0;
                barCnt++;
                
                if (noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].bar == bar_tmp)
                    measure = noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10][listPtr].length_bar;

                if (listPtr + 1 < noteMng.objDatas[(int)OtherObjectsType.B_Bar - 10].Count)
                    listPtr++;

                nextBar += measure;
            }

            position = new Vector3(0f, GetNotePosFromNoteBar(bar_tmp, gameMng.camExpansionRate), 0f);

            snapObj = snapYPool.GetGameObject(snapY_prefab, position, Quaternion.identity);
            lineRenderer = snapObj.GetComponent<LineRenderer>();

            if (split >= 8 && cnt % (split / 4f) == 0)
            {
                lineRenderer.startColor = new Color(1.0f, 0.0f, 0.0f);
                lineRenderer.endColor = new Color(1.0f, 0.0f, 0.0f);
            }
            else if (split >= 16 && cnt % (split / 4f) == (split / 8f))
            {
                lineRenderer.startColor = new Color(0.0f, 0.8f, 1.0f);
                lineRenderer.endColor = new Color(0.0f, 0.8f, 1.0f);
            }
            else if (split >= 32 && cnt % (split / 8f) == (split / 16f))
            {
                lineRenderer.startColor = new Color(1.0f, 0.4f, 0.8f);
                lineRenderer.endColor = new Color(1.0f, 0.4f, 0.8f);
            }
            else
            {
                lineRenderer.startColor = new Color(0.5f, 0.5f, 0.5f);
                lineRenderer.endColor = new Color(0.5f, 0.5f, 0.5f);
            }

            bar_tmp += 1f / split;

            cnt++;
                
        }

        if(!toggle)
            snapYPool.gameObject.SetActive(false);
    }

    public void SetVerticalLine(float length)
    {
        float maxPos = GetNotePosFromMusicTime(length, gameMng.camExpansionRate);

        for (int i = 0; i < vLines.Length; i++) {

            vLines[i].SetPosition(0, new Vector3(vLines[i].GetPosition(0).x, 0f, 0f));
            vLines[i].SetPosition(1, new Vector3(vLines[i].GetPosition(0).x, maxPos, 0f));
        }
        fillterObj.transform.localPosition = new Vector3(0f, maxPos / 2f, 0f);
        fillterObj.transform.localScale = new Vector3(8f, maxPos / 80f, 0f);
    }

    public void SetSnapX(float length, int split, bool toggle)
    {
        GameObject snapObj;
        LineRenderer lineRenderer;

        snapXPool.ReleaseAllGameObjects(snapX_prefab);

        float position;
        float maxPos = GetNotePosFromMusicTime(length, gameMng.camExpansionRate);
        
        for (int i = 0; i <= split; i++)
        {
            position = -General.windowAreaWidth / 2f + General.windowAreaWidth * (i / (float)split);
            snapObj = snapXPool.GetGameObject(snapX_prefab, new Vector3(position, 0f, 0f), Quaternion.identity);
            lineRenderer = snapObj.GetComponent<LineRenderer>();

            lineRenderer.SetPosition(0, new Vector3(0f, 0f, 0f));
            lineRenderer.SetPosition(1, new Vector3(0f, maxPos, 0f));
        }
        
        if (!toggle)
            snapXPool.gameObject.SetActive(false);
    }


    /// <summary>
    /// 走査中の譜面タイプに従ってどのノーツを明るくするかを決定する
    /// </summary>
    public void SetLightMask(ChartType chartType)
    {
        switch (chartType)
        {
            case ChartType.keybord:
                areaMoveObjSort.sortingOrder = -2;
                light.cullingMask = LayerMask.GetMask(new string[] { "keyBordNotes", "keyBordLanes" });
                break;
            case ChartType.exKeybord:
                areaMoveObjSort.sortingOrder = -2;
                light.cullingMask = LayerMask.GetMask(new string[] { "exKeyBordNotes", "keyBordLanes" });
                break;
            case ChartType.mouse:
                areaMoveObjSort.sortingOrder = -2;
                light.cullingMask = LayerMask.GetMask(new string[] { "mouseNotes" });
                break;
            case ChartType.areaMove:
                areaMoveObjSort.sortingOrder = 3;
                light.cullingMask = LayerMask.GetMask(new string[] { "areaMoveObjects" });
                break;
            case ChartType.gimmick:
                areaMoveObjSort.sortingOrder = -2;
                light.cullingMask = LayerMask.GetMask(new string[] { "gimmickObjects" });
                break;
        }
    }


    #region { 時間と小節位置の相互計算 }

    /// <summary>
    /// 現在時間から現在の小節位置(STOP反映なし)を算出
    /// </summary>
    public float GetNoteBarFromMusicTime(float sec)
    {

        float bar = 0;
        float time = 0;
        float bpm;

        if (noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Count > 0)
            bpm = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][0].data;
        else
            bpm = 128f;


        if (sec < 0)
            return sec / 4f * (bpm / 60f);

        // 指定時間を越えるまでタイムを加算
        int i;
        for (i = 1; i < noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Count; i++)
        {
            // １つ前のbarと新しいbar間の小節数とBPMから秒を算出
            float add = (noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][i].bar_forTime - bar) * 4f / (bpm / 60f);

            // 現在のテンポ値で目標時間に到達する場合は抜ける
            if (time + add > sec)
                break;

            // 次のテンポ値に移行
            time += add;                                                                // 経過時間を加算
            bpm = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][i].data;           // 次のBPMをセット
            bar = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][i].bar_forTime;    // 計算済みのカウントをセット


        }

        // 指定時間と1つ前までの時間の差分 = 現在のBPMになってから指定時間までの時間(秒)
        float sub = sec - time;

        // 現在のBPMになってから指定時間までの小節数を算出し、barに加える
        bar += sub / 4f * (bpm / 60f);

        // 小節位置からSTOP遅延分を引く
        float stop_length = 0f;
        int listPtr = 0;
        // 停止を探し、停止の分だけ譜面を後ろにずらす
        while (listPtr < noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10].Count)
        {
            // 現在位置より前に停止の始点がある場合
            if (noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar_forTime < bar)
            {
                // 現在停止中の場合：停止の始点を返す
                if (noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar_forTime + noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar > bar)
                {
                    bar = noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar;
                    stop_length = 0f;
                    break;
                }
                // 過去に停止があった場合：停止時間を加算して次の停止へ読み進める
                else
                {
                    stop_length += noteMng.objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar;
                    listPtr++;
                }

            }
            else
                break;
        }

        return bar - stop_length;
    }

    /// <summary>
    /// 現在時間からノーツの相対y座標を算出
    /// </summary>
    public float GetNotePosFromMusicTime(float time, float camRate)
    {
        if (time < 0f)
            return 0;

        float offset = gameMng.GetOffset();

        if (time < offset)
            return (time / offset * GetBarOffset()) * camRate;
        else
            return (GetNoteBarFromMusicTime(time) + GetBarOffset()) * camRate;

    }

    /// <summary>
    /// 小節位置からノーツの相対y座標を算出
    /// </summary>
    public float GetNotePosFromNoteBar(float bar, float camRate)
    {
        if (bar < 0f)
            return 0;

        return (bar + GetBarOffset()) * camRate;

    }

    /// <summary>
    /// 現在の小節位置(STOP反映済)から現在時間を算出
    /// </summary>
    public float GetNoteTimeFromNoteBar(float bar)
    {
        float currentBar = 0f;
        float time = 0f;
        float bpm;

        if (noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Count > 0)
            bpm = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][0].data;
        else
            bpm = 128f;

        if (bar < 0f)
            return 0f;

        // 指定barを越えるまでbarを加算
        for (int i = 1; i < noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Count; i++)
        {
            // １つ前のBPM地点のbarと現在のBPM地点のbarとの差を算出
            float add = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][i].bar_forTime - currentBar;

            // 指定barが現在のBPM範囲内に存在するとき抜ける
            if (currentBar + add > bar)
                break;

            // 現在時間を現在のBPMの時間だけ追加
            time += add * 4f / (bpm / 60f);

            // 次のテンポ値に移行
            currentBar += add;                                                  // 経過barを加算
            bpm = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][i].data;   // 次のBPMをセット


        }

        // 指定barと1つ前までのbarの差分 = 現在のBPMになってから指定barまでのbar
        float subBar = bar - currentBar;

        // 現在のBPMになってから指定tickまでの時間を算出し、timeに加える
        time += subBar * 4f / (bpm / 60f);

        return time;
    }


    /// <summary>
    /// ノーツの相対y座標から小節位置を算出
    /// </summary>
    public float GetNoteBarFromNotePos(float pos, float camRate)
    {
        if (pos < 0f)
            return 0f;

        return pos / camRate - GetBarOffset();
    }

    #endregion


    /// <summary>
    /// 現在のキーボードレーンの位置を取得する
    /// </summary>
    public float GetKeybordLanePosition(float bar)
    {
        if (noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10].Count < 1)
            return 0f;
        if (bar < 0f)
            return noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][0].position;

        float current_bar = 0f;
        float pos = noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][0].position;
        for (int i = 1; i < noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10].Count; i++)
        {
            if(noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].bar <= bar)
            {
                current_bar = noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].bar;
                pos = noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].position;
            }
            else
            {
                float bar_length = noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].bar - current_bar;
                float bar_diff = bar - current_bar;
                float bar_perc = bar_diff / bar_length;

                float pos_diff = noteMng.objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].position - pos;
                pos += pos_diff * bar_perc;
                break;
            }
        }
        return pos;
    }

    /// <summary>
    /// 譜面のオフセット(秒)を取得して小節位置に変換する
    /// </summary>
    public float GetBarOffset()
    {
        if (noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10].Count < 1)
            return 0f;

        float bpm = noteMng.objDatas[(int)OtherObjectsType.B_Bpm - 10][0].data;
        float bar_offset = gameMng.GetOffset() / (60f / bpm * 4f);

        return bar_offset;
    }

}
