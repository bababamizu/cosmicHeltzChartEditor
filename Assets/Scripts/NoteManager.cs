using chChartEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.IO;
using SFB;

public class NoteManager : MonoBehaviour{

    [SerializeField]
    private GameManager gameMng;

    [SerializeField]
    private LineManager lineMng;

    [SerializeField]
    private ObjectPool mouseNotesPool;
    [SerializeField]
    private ObjectPool keybordNotesPool;
    [SerializeField]
    private ObjectPool exKeybordNotesPool;
    [SerializeField]
    private ObjectPool areaMoveObjectPool;
    [SerializeField]
    private ObjectPool gimmickObjectPool;

    [SerializeField]
    private GameObject[] notePrefab = new GameObject[9];
    [SerializeField]
    private GameObject[] objectPrefab = new GameObject[9];

    private string initDirectory;

    [System.NonSerialized]
    public List<List<NoteData>> notesDatas = new List<List<NoteData>>();
    [System.NonSerialized]
    public List<List<ObjectData>> objDatas = new List<List<ObjectData>>();

    public Array notesTypeArray = Enum.GetValues(typeof(NotesType));
    public Array otherObjTypeArray = Enum.GetValues(typeof(OtherObjectsType));
    
    void Start()
    {
        foreach (NotesType notesType in notesTypeArray)
            notesDatas.Add(new List<NoteData>());
        foreach (OtherObjectsType objType in otherObjTypeArray)
            objDatas.Add(new List<ObjectData>());

        initDirectory = PlayerPrefs.GetString("cH.editor.chartDirectory", string.Empty);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetString("cH.editor.chartDirectory", initDirectory);
    }


    #region [ノーツの追加を行うメソッド群]

    /// <summary>
    /// オブジェクトY座標を指定してノーツを追加する
    /// </summary>
    public GameObject AddNoteFromPos(float objPosY, int type, float length, float posX, float data, bool isImport)
    {
        return AddNote(lineMng.GetNoteBarFromNotePos(objPosY, gameMng.camExpansionRate), objPosY, type, length, posX, data, isImport);
    }

    /// <summary>
    /// barを指定してノーツを追加する
    /// </summary>
    public GameObject AddNoteFromBar(float bar, int type, float length, float posX, float data, bool isImport)
    {
        return AddNote(bar, lineMng.GetNotePosFromNoteBar(bar, gameMng.camExpansionRate), type, length, posX, data, isImport);
    }

    /// <summary>
    /// 実際に画面・リストへのノーツの追加処理を行う
    /// </summary>
    public GameObject AddNote(float bar, float objPosY, int type, float length, float posX, float data, bool isImport)
    {
        if (type == (int)NotesType.N_Hit && posX == 0)
            type = (int)NotesType.N_ExHit;
        else if (type == (int)NotesType.N_Hold && posX == 0)
            type = (int)NotesType.N_ExHold;
        else if (type == (int)NotesType.N_ExHit && posX != 0)
            type = (int)NotesType.N_Hit;
        else if (type == (int)NotesType.N_ExHold && posX != 0)
            type = (int)NotesType.N_Hold;

        GameObject obj = null;
        float objPosX = 0f;
        float maxBar = gameMng.GetBarLength();

        switch (type)
        {
            case (int)NotesType.N_Hit:
            case (int)NotesType.N_Hold:
                objPosX = -250f + 100f * posX;
                obj = keybordNotesPool.GetGameObject(notePrefab[type], new Vector3(objPosX, objPosY, 0f), Quaternion.identity);
                break;

            case (int)NotesType.N_ExHit:
            case (int)NotesType.N_ExHold:
                posX = 0f;
                objPosX = 0f;
                obj = exKeybordNotesPool.GetGameObject(notePrefab[type], new Vector3(objPosX, objPosY, 0f), Quaternion.identity);
                break;

            case (int)NotesType.N_Click:
            case (int)NotesType.N_Catch:
            case (int)NotesType.N_Flick_L:
            case (int)NotesType.N_Flick_R:
            case (int)NotesType.N_Swing:
                objPosX = posX * General.windowAreaWidth / 2f;
                obj = mouseNotesPool.GetGameObject(notePrefab[type], new Vector3(objPosX, objPosY, 0f), Quaternion.identity);
                break;

            case (int)OtherObjectsType.B_Bpm:
                objPosX = 0f;
                obj = gimmickObjectPool.GetGameObject(objectPrefab[type - 10], new Vector3(objPosX, objPosY, 0f), Quaternion.identity);
                obj.GetComponent<GimmickObject>().SetUp(bar, data);
                break;

            case (int)OtherObjectsType.B_Stop:
                objPosX = 0f;
                if (!isImport)
                    length = 0.25f;
                obj = gimmickObjectPool.GetGameObject(objectPrefab[type - 10], new Vector3(objPosX, objPosY, 0f), Quaternion.identity);
                obj.GetComponent<GimmickObject>().SetUp(bar, length * 4f);
                break;

            case (int)OtherObjectsType.B_AreaMove:
                objPosX = posX * General.windowAreaWidth / 2f;
                obj = areaMoveObjectPool.GetGameObject(objectPrefab[type - 10], new Vector3(objPosX, objPosY, 0f), Quaternion.identity);
                break;

            default:
                obj = null;
                break;
        }

        if (obj != null || (type == (int)OtherObjectsType.B_Bar && isImport))
        {
            NoteData noteData;
            ObjectData objData;

            switch (type)
            {
                case (int)NotesType.N_Hit:
                case (int)NotesType.N_ExHit:
                    noteData = new NoteData(bar);
                    noteData.position = posX;
                    noteData.obj = obj;
                    SetNotesTime(noteData, type);
                    notesDatas[type].Add(noteData);
                    break;

                case (int)NotesType.N_Hold:
                case (int)NotesType.N_ExHold:
                    if (!isImport)
                        length = 0.5f;
                    // Hold終点が終了時間を超える場合、Hold長を終了時間までに修正する
                    if (bar + length >= maxBar)
                        length = maxBar - bar;
                    noteData = new NoteData(bar);
                    noteData.length_bar = length;
                    noteData.position = posX;
                    noteData.obj = obj;
                    SetNotesTime(noteData, type);
                    notesDatas[type].Add(noteData);
                    gameMng.ChangeHoldLength(obj, (int)posX, length);
                    break;

                case (int)NotesType.N_Click:
                case (int)NotesType.N_Catch:
                case (int)NotesType.N_Swing:
                    noteData = new NoteData(bar);
                    noteData.position = posX;
                    noteData.width = data;
                    noteData.obj = obj;
                    SetNotesTime(noteData, type);
                    notesDatas[type].Add(noteData);
                    gameMng.ChangeNoteWidth(obj, posX, data, true);
                    break;

                case (int)NotesType.N_Flick_L:
                case (int)NotesType.N_Flick_R:
                    noteData = new NoteData(bar);
                    noteData.position = posX;
                    noteData.obj = obj;
                    SetNotesTime(noteData, type);
                    notesDatas[type].Add(noteData);
                    break;

                case (int)OtherObjectsType.B_Bar:    // 小節線
                    objData = new ObjectData(bar);
                    objData.length_bar = length;
                    objData.data = data;
                    objDatas[(int)OtherObjectsType.B_Bar - 10].Add(objData);

                    break;
                case (int)OtherObjectsType.B_Bpm:    // BPM変化
                    objData = new ObjectData(bar);
                    objData.data = data;
                    objData.obj = obj;
                    objDatas[(int)OtherObjectsType.B_Bpm - 10].Add(objData);
                    if (!isImport)
                    {
                        UpdateAllNotesTime();
                        gameMng.ChangeVerticalLine(false);
                    }  
                    break;

                case (int)OtherObjectsType.B_Stop:   // 譜面停止
                    objData = new ObjectData(bar);
                    objData.length_bar = length;
                    objData.obj = obj;
                    objDatas[(int)OtherObjectsType.B_Stop - 10].Add(objData);
                    if (!isImport)
                    {
                        UpdateAllNotesTime();
                        gameMng.ChangeVerticalLine(false);
                    }
                    break;

                case (int)OtherObjectsType.B_AreaMove:   // レーン移動
                    objData = new ObjectData(bar);
                    objData.position = posX;
                    objData.data = 1;   // ソート用一時変数
                    objData.obj = obj;
                    objDatas[(int)OtherObjectsType.B_AreaMove - 10].Add(objData);
                    UpdateAreaMove();
                    break;

                default:
                    break;
            }
        }
        if (!isImport)
            gameMng.UpdateNotesCount();

        return obj;
    }

    #endregion


    #region [指定したノーツの探索・削除を行うメソッド群]

    /// <summary>
    /// 指定したy座標に当てはまるノーツのうち対象のx座標に一致するノーツを返す
    /// </summary>
    public NOTES_LISTPOS FindNoteFromPos(List<int> notesTypeList, float posX, float posY)
    {
        return FindNote(notesTypeList, posX, lineMng.GetNoteBarFromNotePos(posY, gameMng.camExpansionRate));
    }

    /// <summary>
    /// 指定した小節位置に当てはまるノーツのうち対象のx座標に一致するノーツを返す
    /// </summary>
    public NOTES_LISTPOS FindNote(List<int> notesTypeList, float posX, float bar)
    {
        // 時間順にソート
        SortAllData();

        NOTES_LISTPOS pos = new NOTES_LISTPOS(-1, -1);

        // 該当するノーツリストの中でレーンが同じもののうち最も近いノーツを呼び出す
        foreach (int type in notesTypeList)
        {
            if (type < 10)
            {
                for (int i = 0; i < notesDatas[type].Count; i++)
                {
                    if (notesDatas[type][i].position == posX)
                    {
                        // 完全一致の時点で探索終了 (誤差が出る場合もあるため、10^-6以内の誤差なら一致とみなす)
                        if (Mathf.Abs(notesDatas[type][i].bar - bar) < Mathf.Pow(10, -6))
                        {
                            pos.type = type;
                            pos.num = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < objDatas[type - 10].Count; i++)
                {
                    if (objDatas[type - 10][i].position == posX)
                    {
                        // 完全一致の時点で探索終了
                        if (Mathf.Abs(objDatas[type - 10][i].bar - bar) < Mathf.Pow(10, -6))
                        {
                            pos.type = type;
                            pos.num = i;
                            break;
                        }
                    }
                }
            }

        }

        return pos;
    }

    /// <summary>
    /// 指定した座標にあるノーツを削除する
    /// </summary>
    public bool DeleteNoteFromPos(List<int> notesTypeList, float posX, float posY)
    {
        return DeleteNote(FindNoteFromPos(notesTypeList, posX, posY));
    }

    /// <summary>
    /// 指定した小節位置にあるノーツを削除する
    /// </summary>
    public bool DeleteNoteFromBar(List<int> notesTypeList, float posX, float bar)
    {
        return DeleteNote(FindNote(notesTypeList, posX, bar));
    }

    /// <summary>
    /// 指定したリスト位置にあるノーツを削除する
    /// </summary>
    public bool DeleteNote(NOTES_LISTPOS notePos)
    {

        if (notePos.num < 0)
            return false;

        if (notePos.type < 10)
        {
            notesDatas[notePos.type][notePos.num].obj.SetActive(false);
            notesDatas[notePos.type].RemoveAt(notePos.num);
        }
        else if (notePos.type == (int)OtherObjectsType.B_Bpm)
        {
            // BPM削除はBPM削除用のメソッドで処理する
            DeleteBpmChange(objDatas[notePos.type - 10][notePos.num].bar);
        }
        else if (notePos.type == (int)OtherObjectsType.B_AreaMove)
        {
            // 最初のエリア移動ノーツだった場合は削除しない
            if (objDatas[notePos.type - 10][notePos.num].bar <= 0f)
                return false;

            objDatas[notePos.type - 10][notePos.num].obj.SetActive(false);
            objDatas[notePos.type - 10].RemoveAt(notePos.num);
            UpdateAreaMove();
        }
        else
        {
            objDatas[notePos.type - 10][notePos.num].obj.SetActive(false);
            objDatas[notePos.type - 10].RemoveAt(notePos.num);
            UpdateAllNotesTime();
        }


        gameMng.UpdateNotesCount();

        return true;
    }

    /// <summary>
    /// 指定したレーンにある範囲内のノーツをまとめて削除する(HOLD更新時)
    /// </summary>
    public void DeleteNotesOfRange(int lane, float pos_begin, float pos_end)
    {
        // positionにはoffset補正がかかっているので、その分を引いておく
        float bar_b = pos_begin / gameMng.camExpansionRate - lineMng.GetBarOffset();
        float bar_e = pos_end / gameMng.camExpansionRate - lineMng.GetBarOffset();

        List<NotesType> notesTypeList;
        if (lane == 0)
            notesTypeList = new List<NotesType>() { NotesType.N_ExHit, NotesType.N_ExHold };
        else
            notesTypeList = new List<NotesType>() { NotesType.N_Hit, NotesType.N_Hold };

        // ノーツリストの中でレーンが同じもののうち範囲内のノーツを非アクティブに
        foreach(NotesType type in notesTypeList)
        {
            for (int i = 0; i < notesDatas[(int)type].Count; i++)
            {
                if (notesDatas[(int)type][i].position == lane)
                {
                    if (bar_b < notesDatas[(int)type][i].bar && notesDatas[(int)type][i].bar <= bar_e)
                        keybordNotesPool.ReleaseGameObject(notesDatas[(int)type][i].obj);
                }
            }
        }



        // 非アクティブなオブジェクトをリストの後ろから除外していく
        foreach (NotesType type in notesTypeList)
        {
            for (int i = notesDatas[(int)type].Count() - 1; i >= 0; i--)
            {
                if (!notesDatas[(int)type][i].obj.activeSelf)
                    notesDatas[(int)type].RemoveAt(i);
            }
        }

        gameMng.UpdateNotesCount();

        return;
    }

    #endregion



    #region [各種情報を返すメソッド群]

    /// <summary>
    /// ノーツ数を取得する
    /// </summary>
    public int GetNotesCount()
    {
        int notesCnt = 0;
        foreach (NotesType notesType in notesTypeArray)
            notesCnt += notesDatas[(int)notesType].Count;

        return notesCnt;

    }

    #endregion


    #region [各種ギミックを編集・削除するメソッド群]

    /// <summary>
    /// 入力済の小節線情報を編集する
    /// </summary>
    public void EditMeasureChange(int num, int measure_n, int measure_d)
    {
        ObjectData editedBarData;
        Line line;
        float measure = measure_n / (float)measure_d;

        editedBarData = objDatas[(int)OtherObjectsType.B_Bar - 10][num];
        editedBarData.length_bar = measure;
        editedBarData.data = measure_d;
        line = editedBarData.obj.GetComponent<Line>();

        // 手前の小節と比較して拍子が異なる(あるいは前の小節が存在しない)場合、拍子変化表示をオンに
        if (num != 0)
        {
            if (objDatas[(int)OtherObjectsType.B_Bar - 10][num].length_bar != objDatas[(int)OtherObjectsType.B_Bar - 10][num - 1].length_bar
                || objDatas[(int)OtherObjectsType.B_Bar - 10][num].data != objDatas[(int)OtherObjectsType.B_Bar - 10][num - 1].data)
            {
                line.UpdateMeasure(true, measure_n, measure_d);
            }
            else
                line.UpdateMeasure(false, measure_n, measure_d);
        }
        else
            line.UpdateMeasure(true, measure_n, measure_d);


        // 次の拍子変化までの小節の拍子を変更する
        for (int i = num + 1; i < objDatas[(int)OtherObjectsType.B_Bar - 10].Count; i++)
        {
            editedBarData = objDatas[(int)OtherObjectsType.B_Bar - 10][i];
            line = editedBarData.obj.GetComponent<Line>();

            if (!line.isMeasureChange)
            {
                editedBarData.length_bar = measure;
                editedBarData.data = measure_d;
                line.UpdateMeasure(false, measure_n, measure_d);
            }
            else
                break;
        }

        gameMng.ChangeSnapLine(false);
    }

    /// <summary>
    /// 入力済の小節線情報を削除し、前小節の情報に合わせる
    /// </summary>
    public void DeleteMeasureChange(int num)
    {
        ObjectData editedBarData;
        Line line;
        float measure;
        int measure_n;
        int measure_d;

        if (num == 0)
            return;

        // 手前の小節に拍子を合わせる
        measure = objDatas[(int)OtherObjectsType.B_Bar - 10][num - 1].length_bar;
        measure_d = (int)objDatas[(int)OtherObjectsType.B_Bar - 10][num - 1].data;
        measure_n = (int)(measure * measure_d);

        editedBarData = objDatas[(int)OtherObjectsType.B_Bar - 10][num];
        editedBarData.length_bar = measure;
        editedBarData.data = measure_d;
        editedBarData.obj.GetComponent<Line>().UpdateMeasure(false, measure_n, measure_d);


        // 次の拍子変化までの小節の拍子を変更する
        for (int i = num + 1; i < objDatas[(int)OtherObjectsType.B_Bar - 10].Count; i++)
        {
            editedBarData = objDatas[(int)OtherObjectsType.B_Bar - 10][i];
            line = editedBarData.obj.GetComponent<Line>();

            if (!line.isMeasureChange)
            {
                editedBarData.length_bar = measure;
                editedBarData.data = measure_d;
                line.UpdateMeasure(false, measure_n, measure_d);
            }
            else
                break;
        }

        gameMng.ChangeSnapLine(false);
    }

    /// <summary>
    /// 入力済のBPM情報を編集する
    /// </summary>
    public void EditBpmChange(float bar, float bpm)
    {
        ObjectData editedBpmData;
        float before_bpm = 0f;
        int i = 0;

        // BPMリストをソート
        objDatas[(int)OtherObjectsType.B_Bpm - 10].Sort((a, b) => a.bar.CompareTo(b.bar));

        // 該当のBPM変化を探す
        for (i = 0; i < objDatas[(int)OtherObjectsType.B_Bpm - 10].Count; i++)
        {
            editedBpmData = objDatas[(int)OtherObjectsType.B_Bpm - 10][i];
            if(editedBpmData.bar == bar)
            {
                // 変化後のBPMが直前のBPMと一致していた場合、変化させたBPM変化を削除する
                if (bpm == before_bpm)
                    DeleteBpmChange(bar);
                else
                {
                    editedBpmData.data = bpm;
                    editedBpmData.obj.GetComponent<GimmickObject>().UpdateValue(bpm);
                }
                break;
            }
            else
                before_bpm = editedBpmData.data;
        }

        // 変化後のBPMが直後のBPMと一致していた場合、直後のBPM変化を削除する
        if(i + 1 < objDatas[(int)OtherObjectsType.B_Bpm - 10].Count)
        {
            if (bpm == objDatas[(int)OtherObjectsType.B_Bpm - 10][i + 1].data)
                DeleteBpmChange(objDatas[(int)OtherObjectsType.B_Bpm - 10][i + 1].bar);
        }

        UpdateAllNotesTime();
        gameMng.ChangeSnapLine(false);
    }


    /// <summary>
    /// 入力済のBPM情報を削除する
    /// </summary>
    public void DeleteBpmChange(float bar)
    {
        ObjectData editedBpmData;
        int i = 0;

        // BPMリストをソート
        objDatas[(int)OtherObjectsType.B_Bpm - 10].Sort((a, b) => a.bar.CompareTo(b.bar));

        // 該当のBPM変化を探す(最初は開始BPMなので除く)
        for (i = 1; i < objDatas[(int)OtherObjectsType.B_Bpm - 10].Count; i++)
        {
            editedBpmData = objDatas[(int)OtherObjectsType.B_Bpm - 10][i];
            if (editedBpmData.bar == bar)
            {
                // 前のBPM変化が次のBPM変化と一致していた場合、次のBPM変化を削除する
                if (i + 1 < objDatas[(int)OtherObjectsType.B_Bpm - 10].Count)
                {
                    if (objDatas[(int)OtherObjectsType.B_Bpm - 10][i - 1].data == objDatas[(int)OtherObjectsType.B_Bpm - 10][i + 1].data)
                    {
                        objDatas[(int)OtherObjectsType.B_Bpm - 10][i + 1].obj.SetActive(false);
                        objDatas[(int)OtherObjectsType.B_Bpm - 10].RemoveAt(i + 1);
                    }
                }
                objDatas[(int)OtherObjectsType.B_Bpm - 10][i].obj.SetActive(false);
                objDatas[(int)OtherObjectsType.B_Bpm - 10].RemoveAt(i);
                UpdateAllNotesTime();
                break;
            }
        }

        gameMng.ChangeSnapLine(false);
    }

    /// <summary>
    /// 入力済の譜面停止情報を編集する
    /// </summary>
    public void EditStopChange(float bar, float length)
    {
        ObjectData editedStopData;
        int i = 0;

        // 譜面停止リストをソート
        objDatas[(int)OtherObjectsType.B_Stop - 10].Sort((a, b) => a.bar.CompareTo(b.bar));

        // 該当のBPM変化を探す
        for (i = 0; i < objDatas[(int)OtherObjectsType.B_Stop - 10].Count; i++)
        {
            editedStopData = objDatas[(int)OtherObjectsType.B_Stop - 10][i];
            if (editedStopData.bar == bar)
            {
                // STOP長が0fの場合、譜面停止を削除する
                if (length == 0f)
                {
                    DeleteNote(new NOTES_LISTPOS((int)OtherObjectsType.B_Stop, i));
                }
                else
                {
                    editedStopData.length_bar = length;
                    editedStopData.obj.GetComponent<GimmickObject>().UpdateValue(length * 4f);
                }
                break;
            }
        }

        UpdateAllNotesTime();
        gameMng.ChangeSnapLine(false);
    }





    #endregion


    #region [指定したノーツの情報を更新するメソッド]

    public void SetNotesTime(ObjectData note, int type)
    {
        float length = 0f;
        int listPtr = 0;

        // 譜面データ始点にSTOP遅延を反映する
        // 停止を探し、停止の分だけ譜面を後ろにずらす
        while (listPtr < objDatas[(int)OtherObjectsType.B_Stop - 10].Count)
        {
            if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar < note.bar)
            {
                length += objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar;
                listPtr++;
            }
            else
                break;
        }

        // 停止分を反映させる
        note.bar_forTime = note.bar + length;

        // HOLD長にSTOP遅延を反映する(HOLD中に停止が含まれている場合のみ)
        if(type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold)
        {
            length = 0f;
            listPtr = 0;

            // 停止を探し、停止の分だけ譜面を後ろにずらす
            while (listPtr < objDatas[(int)OtherObjectsType.B_Stop - 10].Count)
            {
                // 停止開始位置がHOLD終点より前の場合
                if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar < note.data)
                {
                    // 停止開始位置がHOLD始点以降の場合 = HOLD中に停止する場合
                    if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar >= note.bar)
                        length += objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar;

                    listPtr++;
                }
                else
                    break;
            }

            // 停止分を反映させる
            note.length_bar_forTime = note.length_bar + length;
        }

        // 譜面データの時間を計算
        note.time = lineMng.GetNoteTimeFromNoteBar(note.bar_forTime);
        if (note.length_bar_forTime > 0f)
            note.length_time = lineMng.GetNoteTimeFromNoteBar(note.bar_forTime + note.length_bar_forTime) - note.time;

        
    }


    /// <summary>
    /// エリア移動ノーツの情報を更新する
    /// </summary>
    public void UpdateAreaMove()
    {
        int count = objDatas[(int)OtherObjectsType.B_AreaMove - 10].Count;
        // リストが空なら終了
        if (count <= 0)
            return;

        LineRenderer lineRenderer;
        Vector3 lineVec;

        // 譜面停止リストをソート
        objDatas[(int)OtherObjectsType.B_AreaMove - 10].Sort(CompareAreaMoveData);

        // エリア移動ノーツ間の線を引きなおす
        for (int i = 0; i < count - 1; i++)
        {
            // 前のデータと違う小節位置の場合はソート用変数を0に、そうでない場合は1に
            //  → 同じ小節位置であれば後に配置されたほうが常に1(=ソート順が後方)になる
            if(objDatas[(int)OtherObjectsType.B_AreaMove - 10][i + 1].bar != objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].bar)
                objDatas[(int)OtherObjectsType.B_AreaMove - 10][i + 1].data = 0f;
            else
                objDatas[(int)OtherObjectsType.B_AreaMove - 10][i + 1].data = 1f;

            lineRenderer = objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].obj.GetComponent<LineRenderer>();
            lineVec = new Vector3(
                (objDatas[(int)OtherObjectsType.B_AreaMove - 10][i + 1].position - objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].position) * General.windowAreaWidth / 2f,
                objDatas[(int)OtherObjectsType.B_AreaMove - 10][i + 1].obj.transform.localPosition.y - objDatas[(int)OtherObjectsType.B_AreaMove - 10][i].obj.transform.localPosition.y,
                0f
                );
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, lineVec);
        }

        // 最後のエリア移動ノーツ以降は楽曲終了までy軸に並行にラインを引く(以降のレーン位置変化がないため)
        lineRenderer = objDatas[(int)OtherObjectsType.B_AreaMove - 10][count - 1].obj.GetComponent<LineRenderer>();
        lineVec = new Vector3(
                0f,
        lineMng.GetNotePosFromNoteBar(gameMng.GetBarLength() - lineMng.GetBarOffset(), gameMng.camExpansionRate) - objDatas[(int)OtherObjectsType.B_AreaMove - 10][count - 1].obj.transform.localPosition.y,
                0f
                );
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, lineVec);
    }



    #endregion


    #region [全てのノーツの情報を更新するメソッド]

    /// <summary>
    /// offset変更時、ノーツ位置をまとめて調整する
    /// </summary>
    public void UpdateNotePosition()
    {
        float maxBar = gameMng.GetBarLength() - lineMng.GetBarOffset();

        foreach (NotesType notesType in notesTypeArray)
        {
            foreach (NoteData note in notesDatas[(int)notesType])
                note.obj.transform.localPosition = new Vector3(note.obj.transform.localPosition.x, lineMng.GetNotePosFromNoteBar(note.bar, gameMng.camExpansionRate), 0f);
        }

        foreach (OtherObjectsType objType in otherObjTypeArray)
        {
            // 小節線オブジェクトはSetLine()で更新するため除外する
            if (objType == OtherObjectsType.B_Bar)
                continue;

            foreach (ObjectData objData in objDatas[(int)objType - 10])
                objData.obj.transform.localPosition = new Vector3(objData.obj.transform.localPosition.x, lineMng.GetNotePosFromNoteBar(objData.bar, gameMng.camExpansionRate), 0f);
        }

        SortAllData();

        // 後方から順に探索し、終了時間を超えるノーツがあれば削除する
        foreach (NotesType notesType in notesTypeArray)
        {
            NoteData note;
            for (int i = notesDatas[(int)notesType].Count - 1; i >= 0; i--)
            {
                note = notesDatas[(int)notesType][i];

                // 終了時間を超えている場合、該当ノーツを削除
                if (note.bar > maxBar)
                    DeleteNote(new NOTES_LISTPOS((int)notesType, i));

                // Holdノーツの場合
                else if (notesType == NotesType.N_Hold || notesType == NotesType.N_ExHold)
                {
                    // Hold終点が終了時間を超える場合、Hold長を終了時間までに修正する
                    if (note.bar + note.length_bar > maxBar)
                        gameMng.ChangeHoldLength(note.obj, (int)note.position, maxBar - note.bar);
                }

            }
                
        }

        foreach (OtherObjectsType objType in otherObjTypeArray)
        {
            // 小節線オブジェクトは除外する
            if (objType == OtherObjectsType.B_Bar)
                continue;

            ObjectData objData;
            for (int i = objDatas[(int)objType - 10].Count - 1; i >= 0; i--)
            {
                objData = objDatas[(int)objType - 10][i];
                // 終了時間を超えている場合、該当ノーツを削除
                if (objData.bar > maxBar)
                    DeleteNote(new NOTES_LISTPOS((int)objType, i));
            }
        }
    }

    /// <summary>
    /// 全ノーツの時間を更新する
    /// </summary>
    public void UpdateAllNotesTime()
    {
        float length = 0f;
        int listPtr = 0;

        // 譜面データ始点にSTOP遅延を反映する
        foreach (NotesType notesType in notesTypeArray)
        {
            length = 0f;
            listPtr = 0;
            foreach (NoteData note in notesDatas[(int)notesType])
            {
                // 停止を探し、停止の分だけ譜面を後ろにずらす
                while (listPtr < objDatas[(int)OtherObjectsType.B_Stop - 10].Count)
                {
                    if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar < note.bar)
                    {
                        length += objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar;
                        listPtr++;
                    }
                    else
                        break;
                }

                // 停止分を反映させる
                note.bar_forTime = note.bar + length;
            }
        }

        // HOLDノーツの終点の小節位置を一時的にdataに格納し、HOLDノーツをdataで昇順ソート
        foreach (NoteData note in notesDatas[(int)NotesType.N_Hold])
            note.data = note.bar + note.length_bar;

        notesDatas[(int)NotesType.N_Hold].Sort((a, b) => a.data.CompareTo(b.data));

        // HOLD長にSTOP遅延を反映する(HOLD中に停止が含まれている場合のみ)
        length = 0f;
        listPtr = 0;
        foreach (NoteData note in notesDatas[(int)NotesType.N_Hold])
        {
            // 停止を探し、停止の分だけ譜面を後ろにずらす
            while (listPtr < objDatas[(int)OtherObjectsType.B_Stop - 10].Count)
            {
                // 停止開始位置がHOLD終点より前の場合
                if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar < note.data)
                {
                    // 停止開始位置がHOLD始点以降の場合 = HOLD中に停止する場合
                    if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar >= note.bar)
                        length += objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar;

                    listPtr++;
                }
                else
                    break;
            }

            // 停止分を反映させる
            note.length_bar_forTime = note.length_bar + length;
        }

        // BPM変化 / レーン変化にSTOP遅延を反映する
        foreach (OtherObjectsType otherObjType in otherObjTypeArray)
        {
            // 小節線 / STOPは除外
            if (otherObjType == OtherObjectsType.B_Bar || otherObjType == OtherObjectsType.B_Stop)
                continue;

            length = 0;
            listPtr = 0;
            foreach (ObjectData obj in objDatas[(int)otherObjType - 10])
            {
                // 停止を探し、停止の分だけ譜面を後ろにずらす
                while (listPtr < objDatas[(int)OtherObjectsType.B_Stop - 10].Count)
                {
                    if (objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].bar < obj.bar)
                    {
                        length += objDatas[(int)OtherObjectsType.B_Stop - 10][listPtr].length_bar;
                        listPtr++;
                    }
                    else
                        break;
                }

                // 停止分を反映させる
                obj.bar_forTime = obj.bar + length;
            }
        }

        length = 0;
        // STOPデータ位置にSTOP遅延を反映する
        foreach (ObjectData obj in objDatas[(int)OtherObjectsType.B_Stop - 10])
        {
            // 遅延を反映させる
            obj.bar_forTime = obj.bar + length;

            // 停止中に停止が入ることはない(はず)なので、停止長は変化しない
            obj.length_bar_forTime = obj.length_bar;

            // 次の停止は今までの停止分だけ遅れてやってくる
            length += obj.length_bar;

        }

        // 全データを昇順にソート
        SortAllData();

        // 譜面データの時間を計算
        foreach (NotesType notesType in notesTypeArray)
        {
            foreach (NoteData note in notesDatas[(int)notesType])
            {
                note.time = lineMng.GetNoteTimeFromNoteBar(note.bar_forTime);
                if (note.length_bar_forTime > 0f)
                    note.length_time = lineMng.GetNoteTimeFromNoteBar(note.bar_forTime + note.length_bar_forTime) - note.time;
            }
        }

        // その他データの時間を計算
        foreach (OtherObjectsType objType in otherObjTypeArray)
        {
            // 小節線は除外
            if (objType == OtherObjectsType.B_Bar)
                continue;

            foreach (ObjectData obj in objDatas[(int)objType - 10])
            {
                obj.time = lineMng.GetNoteTimeFromNoteBar(obj.bar_forTime);
                if (obj.length_bar_forTime > 0)
                    obj.length_time = lineMng.GetNoteTimeFromNoteBar(obj.bar_forTime + obj.length_bar_forTime) - lineMng.GetNoteTimeFromNoteBar(obj.bar_forTime);
            }
        }

        // BPMデータの長さを設定する
        int endCnt = objDatas[(int)OtherObjectsType.B_Bpm - 10].Count - 1;

        for (int i = 0; i < endCnt; i++)
            objDatas[(int)OtherObjectsType.B_Bpm - 10][i].length_time = objDatas[(int)OtherObjectsType.B_Bpm - 10][i + 1].time - objDatas[(int)OtherObjectsType.B_Bpm - 10][i].time;

        objDatas[(int)OtherObjectsType.B_Bpm - 10][endCnt].length_time = gameMng.GetLength() - objDatas[(int)OtherObjectsType.B_Bpm - 10][endCnt].time;

    }


    /// <summary>
    /// 全てのオブジェクトを削除する (小節線も削除する 初期化用)
    /// </summary>
    public void DeleteAllObjects()
    {
        // オブジェクトの削除
        foreach (NotesType notesType in notesTypeArray)
        {
            foreach (NoteData note in notesDatas[(int)notesType])
            {
                if (note.obj != null)
                    note.obj.SetActive(false);
            }
        }
        foreach (OtherObjectsType objType in otherObjTypeArray)
        {
            foreach (ObjectData obj in objDatas[(int)objType - 10])
                if (obj.obj != null)
                    obj.obj.SetActive(false);
        }

        notesDatas = new List<List<NoteData>>();
        foreach (NotesType notesType in notesTypeArray)
            notesDatas.Add(new List<NoteData>());

        objDatas = new List<List<ObjectData>>();
        foreach (OtherObjectsType objType in otherObjTypeArray)
            objDatas.Add(new List<ObjectData>());

        gameMng.UpdateNotesCount();
    }

    #endregion


    #region [その他・汎用メソッド群]

    /// <summary>
    /// 全ての譜面データを昇順にソートするメソッド
    /// </summary>
    public void SortAllData()
    {
        foreach (NotesType notesType in notesTypeArray)
            notesDatas[(int)notesType].Sort((a, b) => a.bar.CompareTo(b.bar));

        foreach (OtherObjectsType objType in otherObjTypeArray)
            objDatas[(int)objType - 10].Sort((a, b) => a.bar.CompareTo(b.bar));
    }

    /// <summary>
    /// エリア移動リストを出現順→data順にソートする関数
    /// </summary>
    private static int CompareAreaMoveData(ObjectData a, ObjectData b)
    {
        if (a.bar < b.bar)
            return -1;
        else if (a.bar > b.bar)
            return 1;
        else
        {
            if (a.data < b.data)
                return -1;
            else if (a.data > b.data)
                return 1;
            else
                return 0;
        }
    }

    #endregion


    #region [Import / Export]

    public bool Export(string music_id, int diff, int level, string charter, bool isExportAs)
    {
        Debug.Log(initDirectory);
        string path = initDirectory + string.Format(@"\{0}[{1}].csv", music_id, diff);
        if (!File.Exists(path) || isExportAs)
        {
            var extension = new[] {
                new ExtensionFilter("Chart File", "csv")
            };
            path = StandaloneFileBrowser.SaveFilePanel("Save File", initDirectory, string.Format("{0}[{1}].csv", music_id, diff), extension);
            
        }

        return ExportChart(path, music_id, diff, level, charter);
    }

    private bool ExportChart(string path, string music_id, int diff, int level, string charter)
    {

        if (path.Length != 0)
        {

            using (StreamWriter sw = new StreamWriter(path, false))
            {
                try
                {
                    initDirectory = Path.GetDirectoryName(path);

                    // ヘッダー出力 (ID, 難易度, レベル, 譜面製作者, 開始前BPM, オフセット)
                    string[] s1 = {
                        music_id,
                        diff.ToString(),
                        level.ToString(),
                        charter,
                        objDatas[(int)OtherObjectsType.B_Bpm - 10][0].data.ToString(),
                        gameMng.GetOffset().ToString()
                    };
                    string s2 = string.Join(",", s1);
                    sw.WriteLine(s2);

                    // データ出力
                    List<ExportData> exportList = new List<ExportData>();
                    NotesType exportNotesType;
                    foreach (NotesType notesType in notesTypeArray)
                    {
                        if (notesType == NotesType.N_ExHit)
                            exportNotesType = NotesType.N_Hit;
                        else if (notesType == NotesType.N_ExHold)
                            exportNotesType = NotesType.N_Hold;
                        else
                            exportNotesType = notesType;

                        foreach (NoteData note in notesDatas[(int)notesType])
                            exportList.Add(new ExportData(note.bar, (int)exportNotesType, note.length_bar, note.position, note.width));
                    }

                    foreach (OtherObjectsType objType in otherObjTypeArray)
                    {
                        foreach (ObjectData obj in objDatas[(int)objType - 10])
                            exportList.Add(new ExportData(obj.bar, (int)objType, obj.length_bar, obj.position, obj.data));
                    }

                    // 昇順にソート
                    exportList.Sort(CompareExportData);

                    foreach (ExportData data in exportList)
                    {
                        // 行ごとの出力処理
                        string[] str = { data.bar.ToString(), data.type.ToString(), data.length.ToString(), data.position.ToString(), data.data.ToString() };
                        string str2 = string.Join(",", str);
                        if (str2 != "" && str != null)
                            sw.WriteLine(str2);
                    }

                    Debug.Log("Exported : " + path);

                    return true;
                }
                catch
                {
                    return false;
                }
            }


        }
        else
            return false;
    }


    public bool Import()
    {
        var extension = new[] {
            new ExtensionFilter("Chart File", "csv")
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", initDirectory, extension, false);

        if (paths.Length != 0) {

            DeleteAllObjects();

            #region { 変数やリストの準備 }


            List<string[]> readDataStr = new List<string[]>();

            int dataCnt = -1;

            float bar;
            int type;
            float length;
            float pos;
            float data;

            FileStream fs;
            string strStream;

            #endregion

            // 譜面データを読み込み、分割する
            try
            {
                using (fs = new FileStream(paths[0], FileMode.Open))
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            initDirectory = Path.GetDirectoryName(paths[0]);

                            strStream = reader.ReadToEnd();
                            // 行に分ける
                            string[] lines = strStream.Split('\n');

                            // カンマ分けをしてデータを完全分割
                            for (int i = 0; i < lines.Length; i++)
                                readDataStr.Add(lines[i].Split(','));

                            DeleteAllObjects();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return false;
            }

            dataCnt = -1;

            // テキストの読み込み
            foreach (string[] line in readDataStr)
            {
                dataCnt++;

                // ヘッダ部
                if (dataCnt == 0)
                {
                    if (line.Length != 6)
                        throw new Exception(Path.GetFileName(paths[0]) + " - ヘッダ部の書式が異なります");

                    gameMng.ImportSettings(line[0], int.Parse(line[1]), int.Parse(line[2]), line[3], float.Parse(line[5]));
                    // startBpm = float.Parse(line[4]);
                    continue;
                }

                if (line.Length != 5)
                    continue;

                // 小節線のみ追加
                if (float.TryParse(line[0], out bar) && int.TryParse(line[1], out type) &&
                    float.TryParse(line[2], out length) && float.TryParse(line[3], out pos) &&
                    float.TryParse(line[4], out data))
                {
                    if(type == (int)OtherObjectsType.B_Bar)
                        AddNoteFromBar(bar, type, length, pos, data, true);
                }

            }

            // 全データを昇順にソート
            SortAllData();

            // スナップ線を設定
            gameMng.ChangeSnapLine(true);


            // 再度テキストの読み込み
            foreach (string[] line in readDataStr)
            {
                dataCnt++;

                // ヘッダ部
                if (dataCnt == 0)
                    continue;

                if (line.Length != 5)
                    continue;

                // オブジェクトを追加
                if (float.TryParse(line[0], out bar) && int.TryParse(line[1], out type) &&
                    float.TryParse(line[2], out length) && float.TryParse(line[3], out pos) &&
                    float.TryParse(line[4], out data))
                {
                    AddNoteFromBar(bar, type, length, pos, data, true);
                }

            }

            // 全データを昇順にソート
            SortAllData();

            // スナップ線を設定
            gameMng.ChangeSnapLine(true);

            // 時間を設定
            UpdateAllNotesTime();

            Debug.Log("Imported : " + paths[0]);

            return true;
        }
        else
            return false;

    }

    /// <summary>
    /// ExportDataを出現順→ノーツ種別順→位置順にソートする関数
    /// </summary>
    private static int CompareExportData(ExportData a, ExportData b)
    {
        if (a.bar < b.bar)
            return -1;
        else if (a.bar > b.bar)
            return 1;
        else
        {
            if (a.type < b.type)
                return -1;
            else if (a.type > b.type)
                return 1;
            else
            {
                if (a.position < b.position)
                    return -1;
                else if (a.position > b.position)
                    return 1;
                else
                    return 0;
            }
        }
    }

    #endregion





}