using chChartEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour {

    [SerializeField]
    private AudioManager audioMng;
    [SerializeField]
    private LineManager lineMng;
    [SerializeField]
    private NoteManager noteMng;
    [SerializeField]
    private UIManager uiMng;
    [SerializeField]
    private CursorManager cursorMng;

    [SerializeField]
    private ObjectPool snapPoolY;
    [SerializeField]
    private ObjectPool snapPoolX;
    [SerializeField]
    private GameObject snapY_prefab;
    [SerializeField]
    private GameObject snapX_prefab;

    [SerializeField]
    private GameObject keybordNotesFolder;

    [SerializeField]
    private RectTransform canvasRect;


    [SerializeField]
    private Camera areaCam;
    [SerializeField]
    private Camera spotCam;
    [SerializeField]
    private GameObject currentLine;

    private readonly CommandManager commandMng = new CommandManager();

    private bool isSetLine = false;

    private bool edited = false;

    private int noteTypeId_unShift;
    private int noteTypeId;

    private int rayLayerMask;
    private float time;
    private float bar;
    private float currentPos;

    private float windowWidth;
    private float windowHeight;

    public float camExpansionRate { get; private set; }

    private GameObject[] havingNoteObj;
    private NOTES_LISTPOS[] havingObjPos;

    Vector3 mousePos;
    RaycastHit2D hit;

    float before_time = 0f;
    int playShot_current = 0;

    bool isNormalPlaying;
    public commandMode cmdMode { get; private set; }

    GameObject hitGO;

    void Start () {
        Application.wantsToQuit += WantsToQuit;
        LoadAudioVolume();
        SetCamWidthFromWindowSize(Screen.width, Screen.height);
        camExpansionRate = 600f;
        edited = false;
        uiMng.SetExportEnable(false);
        UpdateNotesType(0);
        cursorMng.SetCursor(CursorType.Default);
        UpdateUndoRedoEnable();
    }
	
	void Update () {

        if (windowWidth != Screen.width || windowHeight != Screen.height)
            SetCamWidthFromWindowSize(Screen.width, Screen.height);

        float wheelInput = Input.GetAxis("Mouse ScrollWheel");

        if (audioMng.audioSource.clip != null)
        {
            isNormalPlaying = true;
            time = audioMng.GetTime();

            // 現在の小節位置を算出(譜面開始位置が0になるようにoffsetの値を引いておく)
            bar = lineMng.GetNoteBarFromMusicTime(time - GetOffset());
            // 現在のバー位置を算出(楽曲開始位置が0になるためoffsetは考慮しない)
            currentPos = lineMng.GetNoteBarFromMusicTime(time) * camExpansionRate;

            float maxPos = GetBarLength() * camExpansionRate - 2500f;

            if (time >= 0f)
            {
                currentLine.transform.localPosition = new Vector2(currentLine.transform.localPosition.x, currentPos);
                spotCam.transform.localPosition = new Vector3(spotCam.transform.localPosition.x, currentPos + 600f, spotCam.transform.localPosition.z);
                keybordNotesFolder.transform.localPosition = new Vector3(lineMng.GetKeybordLanePosition(bar) * General.windowAreaWidth / 2f, 0f, 0f);
                if (uiMng.GetAutoScr())
                {
                    if (currentPos < 2500f)
                        areaCam.transform.localPosition = new Vector3(areaCam.transform.localPosition.x, 2500f, areaCam.transform.localPosition.z);
                    else if (maxPos < currentPos)
                        areaCam.transform.localPosition = new Vector3(areaCam.transform.localPosition.x, maxPos, areaCam.transform.localPosition.z);
                    else
                        areaCam.transform.localPosition = new Vector3(areaCam.transform.localPosition.x, currentPos, areaCam.transform.localPosition.z);

                    uiMng.FixAreaCamSlider();
                }
            }
        }


        // フィールドに対する各種操作 (楽曲読み込み済 かつ uGUIに触れていない かつ Setting画面を開いていない状態)
        if (!EventSystem.current.IsPointerOverGameObject() && isSetLine && !uiMng.GetOpenSetting())
        {
            // マウスの位置にRayを飛ばし、オブジェクトの有無によりマウスポインタを変更する
            mousePos = MousePosToWorldPos(Input.mousePosition);
            hit = Physics2D.Raycast(new Vector3(mousePos.x, mousePos.y, -0.1f), new Vector3(0f, 0f, 1f), 0.2f, rayLayerMask);
            if (hit.collider != null)
            {
                if(hit.collider.gameObject.tag == "Edge")
                    cursorMng.SetCursor(CursorType.LR_Arrow);
                else if (hit.collider.gameObject.name == "end")
                    cursorMng.SetCursor(CursorType.UD_Arrow);
                else
                    cursorMng.SetCursor(CursorType.Choice);
            }
            else
                cursorMng.SetCursor(CursorType.Default);

            
            // Ctrl + マウスホイール : カメラサイズ変更
            // マウスホイール        : 再生位置変更
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                ChangeCamExpansionRate((int)(wheelInput * 10f));
            else
            {
                if (Mathf.Abs(wheelInput) > 0f)
                {
                    audioMng.SetAudioTime(wheelInput * 3f);
                    isNormalPlaying = false;
                }
            }

            // 左クリック押下
            if (Input.GetMouseButtonDown(0))
                OnMouseClick(0);

            // 右クリック押下
            if (Input.GetMouseButtonDown(1))
                OnMouseClick(1);
        }
        else
            cursorMng.SetCursor(CursorType.Default);

        // その他の各種操作

        // 上キー
        if (Input.GetKey(KeyCode.UpArrow))
        {
            audioMng.SetAudioTime(0.08f);
            isNormalPlaying = false;
        }

        // 下キー
        if (Input.GetKey(KeyCode.DownArrow))
        {
            audioMng.SetAudioTime(-0.08f);
            isNormalPlaying = false;
        }

        // Ctrl(Command) + Z         : 元に戻す
        // Ctrl(Command) + Y         : やり直し
        // Ctrl(Command) + S         : 保存
        // Ctrl(Command) + Shift + S : 名前を付けて保存
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
        {
            if (Input.GetKeyDown(KeyCode.Z))
                Undo();
            if (Input.GetKeyDown(KeyCode.Y))
                Redo();
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    Export(true, false);
                else
                    Export(false, false);
            }
                
        }

        // ドラッグ時
        if (Input.GetMouseButton(0) && havingNoteObj != null)
        {
            if (havingNoteObj.Length == 1)
            {
                ChartType chartType = GetChartType(havingObjPos[0].type);

                // 端オブジェクトの場合
                if (havingNoteObj[0].tag == "Edge")
                {
                    float edge_pos = Mathf.Clamp((MousePosToWorldPos(Input.mousePosition).x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f), -1f, 1f);
                    ChangeNoteWidth(
                        havingNoteObj[0].transform.parent.gameObject,
                        noteMng.notesDatas[havingObjPos[0].type][havingObjPos[0].num].position,
                        Mathf.Abs(noteMng.notesDatas[havingObjPos[0].type][havingObjPos[0].num].position - edge_pos) * 2f,
                        false);
                }
                else
                {
                    // キーボード系の場合、y座標のみ移動させる
                    if (chartType == ChartType.keybord || chartType == ChartType.exKeybord)
                    {
                        havingNoteObj[0].transform.position = new Vector3(
                            havingNoteObj[0].transform.position.x,
                            MousePosToWorldPos(Input.mousePosition).y,
                            havingNoteObj[0].transform.position.z
                        );
                    }
                    // マウス系の場合、x, y座標どちらも移動させる(ただしx座標の範囲外には出さない)
                    else if (chartType == ChartType.mouse || chartType == ChartType.areaMove)
                    {
                        // 最初のエリア移動ノーツの場合、y座標は移動させない
                        if (chartType == ChartType.areaMove && havingObjPos[0].num == 0)
                        {
                            havingNoteObj[0].transform.position = new Vector3(
                                General.objectAreaPos.x + Mathf.Clamp(
                                    MousePosToWorldPos(Input.mousePosition).x - General.objectAreaPos.x,
                                    -General.windowAreaWidth / 2f,
                                    General.windowAreaWidth / 2f),
                                havingNoteObj[0].transform.position.y,
                                havingNoteObj[0].transform.position.z
                            );
                        }
                        else
                        {
                            havingNoteObj[0].transform.position = new Vector3(
                                General.objectAreaPos.x + Mathf.Clamp(
                                    MousePosToWorldPos(Input.mousePosition).x - General.objectAreaPos.x,
                                    -General.windowAreaWidth / 2f,
                                    General.windowAreaWidth / 2f),
                                MousePosToWorldPos(Input.mousePosition).y,
                                havingNoteObj[0].transform.position.z
                            );
                        }
                    }

                }
            }

        }

        // 左クリック解除
        if (Input.GetMouseButtonUp(0) && havingNoteObj != null)
        {
            if (havingNoteObj.Length == 1)
                ReleaseDragObject(havingNoteObj[0], false);
            havingNoteObj = null;
            havingObjPos = null;
        }


        // スペースキー : 再生・一時停止
        if (Input.GetKeyDown(KeyCode.Space))
            audioMng.PushPlayButton();

        // シフトキー : Hit / Hold切り替え
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (noteTypeId_unShift == (int)NotesType.N_Hit)
            {
                uiMng.ShiftChangeNoteButton(noteTypeId_unShift);
                noteTypeId = (int)NotesType.N_Hold;
            }
            if (noteTypeId_unShift == (int)NotesType.N_ExHit)
            {
                uiMng.ShiftChangeNoteButton(noteTypeId_unShift);
                noteTypeId = (int)NotesType.N_ExHold;
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            if(noteTypeId_unShift != noteTypeId)
            {
                uiMng.UnShiftChangeNoteButton(noteTypeId_unShift);
                noteTypeId = noteTypeId_unShift;
            }
        }

        // 時間が巻き戻っている場合は効果音探索位置を最初に戻す
        if (time < before_time)
            playShot_current = 0;

        // 効果音再生(通常再生時に限る)
        if (audioMng.audioSource.clip != null && audioMng.audioSource.isPlaying && isNormalPlaying)
            PlayShot(time, Time.deltaTime);
        
        before_time = time;

        // Escキー : 終了
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

    }

    public void InitNotesList()
    {
        noteMng.DeleteAllObjects();
    }

    public void SetEdited(bool isEdited)
    {
        edited = isEdited;
        uiMng.SetExportEnable(isEdited);
    }


    /// <summary>
    /// 現在選択中のノーツの種類を更新する
    /// </summary>
    public void UpdateNotesType(int type)
    {
        noteTypeId_unShift = type;
        noteTypeId = type;
        rayLayerMask = GetLayerMaskForRay();
    }


    #region [カメラ設定]

    /// <summary>
    /// ウィンドウの比率に合わせてカメラの描画範囲を更新する
    /// </summary>
    private void SetCamWidthFromWindowSize(float width, float height)
    {
        windowWidth = width;
        windowHeight = height;

        // 幅を1としたときの高さの値
        float heightRate = height / width;
        // 4/3より小さい場合は960fで固定
        if (heightRate <= 4f / 3f)
        {
            spotCam.orthographicSize = 960f;
            areaCam.orthographicSize = 960f * 3f;
        }

        // 4/3より大きい場合、高さ1ごとに720ずつSizeを大きく
        else
        {
            spotCam.orthographicSize = 960f + (heightRate - 4f / 3f) * 720f;
            areaCam.orthographicSize = (960f + (heightRate - 4f / 3f) * 720f) * 3f;
        }
            

    }

    #endregion


    #region [効果音の再生]

    /// <summary>
    /// 現在時間から効果音を取得する
    /// </summary>
    public void PlayShot(float time, float deltaTime)
    {
        bool isBreak = false;
        for (int i = playShot_current; i < noteMng.guideList.Count; i++)
        {
            if (before_time - 0.04f < noteMng.guideList[i].time && noteMng.guideList[i].time - 0.04f <= time)
                audioMng.PlayClap();
            else if (noteMng.guideList[i].time > time)
            {
                playShot_current = i;
                isBreak = true;
                break;
            }
        }
        if (!isBreak)
            playShot_current = noteMng.guideList.Count;
        return;
    }


    #endregion


    #region [BGM / SE]

    /// <summary>
    /// 音量を初期値に設定する
    /// </summary>
    private void LoadAudioVolume()
    {
        float bgm_volume = PlayerPrefs.GetFloat("chartEditor.bgmVolume", 1f);
        int bgm_isMute = PlayerPrefs.GetInt("chartEditor.bgmMute", 0);

        float se_volume = PlayerPrefs.GetFloat("chartEditor.seVolume", 1f);
        int se_isMute = PlayerPrefs.GetInt("chartEditor.seMute", 0);

        uiMng.SetBgmVolume(bgm_volume, bgm_isMute == 1);
        uiMng.SetSeVolume(se_volume, se_isMute == 1);
    }


    /// <summary>
    /// 音量を保存する
    /// </summary>
    private void SaveAudioVolume()
    {
        PlayerPrefs.SetFloat("chartEditor.bgmVolume", uiMng.GetBgmValue());
        PlayerPrefs.SetFloat("chartEditor.seVolume", uiMng.GetSeValue());

        if(uiMng.GetIsBgmMute())
            PlayerPrefs.SetInt("chartEditor.bgmMute", 1);
        else
            PlayerPrefs.GetInt("chartEditor.bgmMute", 0);

        if (uiMng.GetIsSeMute())
            PlayerPrefs.SetInt("chartEditor.seMute", 1);
        else
            PlayerPrefs.GetInt("chartEditor.seMute", 0);
    }


    public void SetBgmVolume(float value)
    {
        audioMng.audioSource.volume = value;
    }

    public void SetSeVolume(float value)
    {
        audioMng.seSource.volume = value;
    }

    public void SetBgmMute(bool isMute)
    {
        audioMng.audioSource.mute = isMute;
    }

    public void SetSeMute(bool isMute)
    {
        audioMng.seSource.mute = isMute;
    }


    
    
    #endregion


    #region [Undo / Redo]

    public void Undo()
    {
        cmdMode = commandMode.Undo;
        commandMng.Undo();
        cmdMode = commandMode.Do;
        UpdateUndoRedoEnable();
    }
    public void Redo()
    {
        cmdMode = commandMode.Redo;
        commandMng.Redo();
        cmdMode = commandMode.Do;
        UpdateUndoRedoEnable();
    }

    public void UpdateUndoRedoEnable()
    {
        uiMng.UpdateUndoButtonEnable(commandMng.IsCanUndo());
        uiMng.UpdateRedoButtonEnable(commandMng.IsCanRedo());
    }

    #endregion


    #region [スナップ]

    /// <summary>
    /// 曲読み込み時に最初にスナップ線を設定する処理
    /// </summary>
    public void InitLine()
    {
        isSetLine = true;

        uiMng.SetCamExpansionSliderEnable(true);
        uiMng.SetSettingButtonEnable(true);

        lineMng.SetUpBPMChange();

        ChangeVerticalLine(true);
        ChangeSnapLineX();

        // 一旦64分のスナップ線を出現させておく(重いInstantiate()をここでまとめてやっておく)
        lineMng.SetLine(audioMng.GetMusicLength());
        lineMng.SetSnapLine(audioMng.GetMusicLength(), 64, uiMng.GetSnapYToggle());

        // 改めてラインを設定
        lineMng.SetSnapLine(audioMng.GetMusicLength(), uiMng.GetSnapYSplit(), uiMng.GetSnapYToggle());

    }

    /// <summary>
    /// 小節線とスナップ線を変更する
    /// </summary>
    public void ChangeLine(bool isNotesMove)
    {
        if (isSetLine) {
            // 小節線とスナップ線の変更
            lineMng.SetLine(audioMng.GetMusicLength());
            lineMng.SetSnapLine(audioMng.GetMusicLength(), uiMng.GetSnapYSplit(), uiMng.GetSnapYToggle());

            ChangeVerticalLine(isNotesMove);
            ChangeSnapLineX();
            noteMng.UpdateAreaMove();
            edited = true;
        }
    }

    /// <summary>
    /// Y軸スナップ線のみを変更する
    /// </summary>
    public void ChangeSnapLineY()
    {
        if (isSetLine)
            lineMng.SetSnapLine(audioMng.GetMusicLength(), uiMng.GetSnapYSplit(), uiMng.GetSnapYToggle());
    }

    /// <summary>
    /// X軸スナップ線のみを変更する
    /// </summary>
    public void ChangeSnapLineX()
    {
        if (isSetLine)
            lineMng.SetSnapX(audioMng.GetMusicLength(), uiMng.GetSnapXSplit(), uiMng.GetSnapXToggle());
    }


    public void SnapX_toggle(bool isSnap)
    {
        lineMng.SnapX_toggle(isSnap);
    }


    /// <summary>
    /// 楽曲の長さ・ズーム率に合わせて線の長さを変更する
    /// </summary>
    public void ChangeVerticalLine(bool isNotesMove)
    {
        if (audioMng.audioSource.clip != null)
        {
            lineMng.SetVerticalLine(audioMng.GetMusicLength());
            if (isNotesMove)
                noteMng.UpdateNotePosition();
            SetEdited(true);
        }
    }


    #endregion


    #region [描画範囲(ズーム率)設定]

    /// <summary>
    /// カメラの倍率を差分値だけ変更する
    /// </summary>
    public void ChangeCamExpansionRate(int diff)
    {
        if (isSetLine && diff != 0)
        {
            int sliderValue = uiMng.GetCamExpansionSliderValue() + diff;

            if (0f <= sliderValue && sliderValue <= 16f)
            {
                UpdateCamExpansionRate(300f * Mathf.Pow(2f, sliderValue / 4f));
                uiMng.SetCamExpansionSlider(sliderValue);
            }
        }
    }

    /// <summary>
    /// カメラの倍率を更新する
    /// </summary>
    public void UpdateCamExpansionRate(float size)
    {
        if (isSetLine)
        {
            camExpansionRate = size;
            uiMng.SetMaxAreaCamPosition(GetBarLength());
            ChangeLine(false);
            ChangeVerticalLine(true);
            UpdateHoldObjectForCam();
        }
    }

    #endregion


    #region [各種情報の取得]

    public float GetLength()
    {
        return audioMng.GetMusicLength();
    }

    public float GetBarLength()
    {
        return lineMng.GetNoteBarFromMusicTime(audioMng.GetMusicLength());
    }

    public float GetOffset()
    {
        return uiMng.GetOffset();
    }

    #endregion


    #region [各種表示の更新]

    /// <summary>
    /// ノーツ数表示を更新する
    /// </summary>
    public void UpdateNotesCount()
    {
        uiMng.SetTotalNotes(noteMng.GetNotesCount());
    }

    /// <summary>
    /// 指定した譜面タイプが手前に描画されるようにSortingGroupを更新する
    /// </summary>
    public void UpdateLightMask()
    {
        lineMng.SetLightMask(GetChartType(noteTypeId));
    }

    #endregion


    #region [ギミックの編集]

    /// <summary>
    /// 小節リストの指定した番号の項目を編集する
    /// </summary>
    public void EditMeasure(int num, int measure_n, int measure_d)
    {
        // numは小節番号(=始点が1)なので1を引く
        noteMng.EditMeasureChange(num - 1, measure_n, measure_d);

    }

    /// <summary>
    /// 小節リストの指定した番号の項目を削除する
    /// </summary>
    public void DeleteMeasure(int num)
    {
        noteMng.DeleteMeasureChange(num - 1);
        
    }

    /// <summary>
    /// 指定した小節位置にあるBPMを編集する
    /// </summary>
    public void EditBpm(float bar, float bpm)
    {
        noteMng.EditBpmChange(bar, bpm);
    }

    /// <summary>
    /// 指定した小節位置にある譜面停止を編集する
    /// </summary>
    public void EditStop(float bar, float length)
    {
        noteMng.EditStopChange(bar, length);
    }

    #endregion


    #region [Import / Export]

    public void Import()
    {
        if (noteMng.Import())
        {
            SetEdited(false);
            UpdateNotesCount();
        }
            
    }

    public void ImportSettings(string music_id, int diff, int level, string charter, float offset)
    {
        uiMng.ImportSettings(music_id, diff, level, charter, offset);
    }

    public void Export(bool isExportAs, bool permitCancel)
    {
        if (isSetLine)
        {
            if (noteMng.Export(uiMng.GetMusicID(), uiMng.GetDiff(), uiMng.GetLevel(), uiMng.GetCharter(), isExportAs) || permitCancel)
                SetEdited(false);
        }
        
    }

    #endregion



    private bool WantsToQuit()
    {
        SaveAudioVolume();

        if (isSetLine && edited) {
            // TODO : 終了時に保存するか確認するダイアログを出す (現在はExportを開くだけ)
            Export(true, true);
            return true;
        }

        return true;
    }





    /// <summary>
    /// クリックした座標に対してノーツの挿入・削除・編集を行う
    /// </summary>
    private void OnMouseClick(int mode)
    {
        Vector3 canvas_pos = ExMethod.GetPosition_touchToCanvas(canvasRect, Input.mousePosition);
        Vector3 pos = MousePosToWorldPos(Input.mousePosition);

        // 現在のノーツ種類のタイプ(0=キーボードノーツ / 1=Exキーボードノーツ / 2=マウスノーツ / 3=エリア移動 / 4=ギミック系)

        // TODO : タップノーツ選択時、Shiftを入力するとHoldが設置できるようにする
        ChartType chartType = GetChartType(noteTypeId);

        // クリック位置にRayを飛ばす
        RaycastHit2D hit = Physics2D.Raycast(new Vector3(pos.x, pos.y, -0.1f), new Vector3(0, 0, 1), 0.2f, rayLayerMask);

        // 小節番号やギミックの値をクリックした場合、該当する設定画面を開く
        if (hit.collider != null)
        {
            Vector3 objPos = RectTransformUtility.WorldToScreenPoint(spotCam, hit.collider.transform.position);
            if (hit.collider.gameObject.tag == "Bar")
            {
                if (mode == 0)
                    uiMng.OpenBarSetting(objPos.y, hit.collider.gameObject);
                return;
            }
            else if (hit.collider.gameObject.tag == "Bpm")
            {
                if (mode == 0)
                    uiMng.OpenBpmSetting(objPos.y, hit.collider.gameObject);
                return;
            }
            else if (hit.collider.gameObject.tag == "Stop")
            {
                if (mode == 0)
                    uiMng.OpenStopSetting(objPos.y, hit.collider.gameObject);
                return;
            }
        }


        float data = General.initData[noteTypeId];
        List<int> notesTypeList;
        float notePosX = -1f;
        float lanePos = keybordNotesFolder.transform.localPosition.x;

        // クリックした位置の取得 (キーボードならレーンに、マウスなら位置に、ギミックなら0に変換)
        // キーボードノーツ時
        if (chartType == ChartType.keybord)
        {
            if (pos.x - General.objectAreaPos.x - lanePos < -200f || pos.x - General.objectAreaPos.x - lanePos >= 200f)
                return;
            else if (pos.x - General.objectAreaPos.x - lanePos < -100f)
                notePosX = 1;
            else if (pos.x - General.objectAreaPos.x - lanePos < 0f)
                notePosX = 2;
            else if (pos.x - General.objectAreaPos.x - lanePos < 100f)
                notePosX = 3;
            else if (pos.x - General.objectAreaPos.x - lanePos < 200f)
                notePosX = 4;
            else
                return;
        }
        // キーボードExノーツ時
        else if (chartType == ChartType.exKeybord)
        {
            if (pos.x - General.objectAreaPos.x - lanePos < -200f || pos.x - General.objectAreaPos.x - lanePos >= 200f)
                return;
            else
                notePosX = 0;
        }
        // マウスノーツ時
        else if (chartType == ChartType.mouse || chartType == ChartType.areaMove)
        {
            notePosX = (pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
            if (Mathf.Abs(notePosX) > 1f)
                return;
        }
        // ギミック時(BPM変化 / STOP)
        else if (chartType == ChartType.gimmick)
            notePosX = 0f;
        else
            return;

        // ノートが直接クリックされた場合
        if (hit.collider != null)
        {
            // その位置にあるノーツを編集
            EditNote(mode, noteTypeId, notePosX, hit.collider.gameObject);
            return;
        }


        // ノートが直接クリックされていない場合、クリック位置がエリア内かどうか判断
        if (pos.x - General.objectAreaPos.x >= -General.windowAreaWidth / 2f
         && pos.x - General.objectAreaPos.x < General.windowAreaWidth / 2f
         && pos.y - General.objectAreaPos.y >= 0f
         && pos.y - General.objectAreaPos.y < audioMng.GetMusicLength() * camExpansionRate)
        {
            // スナップ有効時
            if (uiMng.GetSnapYToggle() || uiMng.GetSnapXToggle())
            {

                GameObject snapObjY = null;
                GameObject snapObjX = null;

                // スナップ位置を取得
                if (uiMng.GetSnapYToggle())
                {
                    snapObjY = snapPoolY.SartchNearGameObjectY(snapY_prefab, pos.y);
                    if (snapObjY != null)
                        pos.y = snapObjY.transform.position.y;
                    else
                        return;
                }
                if (uiMng.GetSnapXToggle())
                {
                    snapObjX = snapPoolX.SartchNearGameObjectX(snapX_prefab, pos.x);
                    if (snapObjX != null)
                    {
                        pos.x = snapObjX.transform.position.x;
                        if (chartType == ChartType.mouse || chartType == ChartType.areaMove)
                            notePosX = (pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
                    }
                    else
                        return;
                }


                // 当たり判定を持たないBPM変化 / STOP系の場合、直接スナップ位置にあるオブジェクトを検索する
                if (chartType == ChartType.gimmick)
                {
                    // 削除モードかつ最初のBPM設定でない場合、検索・削除を実施
                    if (mode == 1 && lineMng.GetNoteBarFromNotePos(pos.y - General.objectAreaPos.y, camExpansionRate) > 0f)
                    {
                        notesTypeList = new List<int>() { noteTypeId };
                        noteMng.DeleteNoteFromPos(notesTypeList, notePosX, pos.y - General.objectAreaPos.y);
                        return;
                    }
                }
                else
                {
                    // キーボード系の場合、Ray位置をレーン中央に(レーン端をクリックした際のColliderすり抜けを防ぐため)
                    if (chartType == ChartType.keybord)
                        pos.x = -250f + 100f * notePosX + General.objectAreaPos.x - lanePos;
                    else if (chartType == ChartType.exKeybord)
                        pos.x = General.objectAreaPos.x - lanePos;

                    // スナップ先にノートがあるか探索
                    hit = Physics2D.Raycast(new Vector3(pos.x, pos.y, -1f), new Vector3(0, 0, 1), 2, rayLayerMask);

                    // スナップ先にノートがある場合
                    if (hit.collider != null)
                    {
                        // その位置にあるノーツを編集
                        EditNote(mode, noteTypeId, notePosX, hit.collider.gameObject);
                        return;
                    }
                }

                // スナップ先にノートがない場合 : 挿入モードならスナップ位置に挿入、削除モードなら終了
                if (mode == 0)
                {
                    int type = noteTypeId;
                    float bar = lineMng.GetNoteBarFromNotePos(pos.y - General.objectAreaPos.y, camExpansionRate);
                    commandMng.Do(new Command(
                        () => { noteMng.AddNoteFromBar(bar, type, 0f, notePosX, data, false); },
                        () => { noteMng.DeleteNoteFromBar(new List<int>() { type }, notePosX, bar); }
                        ));
                    UpdateUndoRedoEnable();
                    SetEdited(true);
                }

                return;
            }
            // スナップ無効時 : 挿入モードならクリック位置に挿入、削除モードなら終了
            else
            {
                if (mode == 0)
                {
                    int type = noteTypeId;
                    float bar = lineMng.GetNoteBarFromNotePos(pos.y - General.objectAreaPos.y, camExpansionRate);
                    commandMng.Do(new Command(
                        () => { noteMng.AddNoteFromBar(bar, type, 0f, notePosX, data, false); },
                        () => { noteMng.DeleteNoteFromBar(new List<int>() { type }, notePosX, bar); }
                        ));
                    UpdateUndoRedoEnable();
                    SetEdited(true);
                }

                return;
            }
        }
        else
            return;


    }

    /// <summary>
    /// ノーツの選択・削除を行う
    /// </summary>
    private void EditNote(int mode, int type, float notePosX, GameObject obj)
    {
        NOTES_LISTPOS findNote;

        ChartType chartType = GetChartType(type);

       

        if(obj.tag == "Edge")
        {
            type = GetNotesTypeForObjectTag(obj.transform.parent.gameObject);
            notePosX = (obj.transform.parent.position.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
        }
        else
        {
            type = GetNotesTypeForObjectTag(obj);
            // マウスノーツ時、ノーツ位置を更新
            // (キーボードノーツはレーン指定なので変化せず、その他は位置指定がないため更新しなくてよい)
            if (chartType == ChartType.mouse || chartType == ChartType.areaMove)
                notePosX = (obj.transform.position.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
        }
            

        List<int> notesTypeList = new List<int>() { type };

        // 挿入モードならドラッグ開始、削除モードなら削除
        if (mode == 0)
        {
            // ドラッグ対象を検索 (取得したColliderが[Edge]あるいは[Hold]、[Catchの中央オブジェクト]の場合は親、それ以外の場合は子の座標を検索する)
            if (obj.tag == "Edge" || type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold)
                findNote = noteMng.FindNoteFromPos(notesTypeList, notePosX, obj.transform.parent.localPosition.y);
            else if (type == (int)NotesType.N_Catch && obj.name == "dot")
                findNote = noteMng.FindNoteFromPos(notesTypeList, notePosX, obj.transform.parent.localPosition.y);
            else
                findNote = noteMng.FindNoteFromPos(notesTypeList, notePosX, obj.transform.localPosition.y);

            if (findNote.num >= 0)
            {
                // 終点以外のホールドあるいはCatchの中央オブジェクトをクリックした場合、その親オブジェクトを持つ
                // ホールドの終点またはホールド以外のオブジェクトをクリックした場合、そのオブジェクトを持つ
                // (端オブジェクトをクリックした場合は端オブジェクトを持つ)
                
                if ((type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold) && obj.name != "end")
                    havingNoteObj = new GameObject[] { obj.transform.parent.gameObject };
                else if (type == (int)NotesType.N_Catch && obj.name == "dot")
                    havingNoteObj = new GameObject[] { obj.transform.parent.gameObject };
                else
                    havingNoteObj = new GameObject[] { obj };

                if (havingNoteObj != null)
                    havingObjPos = new NOTES_LISTPOS[] { findNote };
            }
        }
        else if (mode == 1)
        {
            float posY;
            float length;
            float data;

            // 削除対象ノーツの情報を取得する(Undo用)
            if (type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold)
                posY = obj.transform.parent.localPosition.y;
            else
                posY = obj.transform.localPosition.y;

            findNote = noteMng.FindNoteFromPos(notesTypeList, notePosX, posY);

            if (findNote.num < 0)
                return;

            if (type < 10)
            {
                length = noteMng.notesDatas[findNote.type][findNote.num].length_bar;
                data = noteMng.notesDatas[findNote.type][findNote.num].width;
            }
            else
            {
                length = noteMng.objDatas[findNote.type - 10][findNote.num].length_bar;
                data = noteMng.objDatas[findNote.type - 10][findNote.num].data;
            }

            // ノーツを削除する
            float bar = lineMng.GetNoteBarFromNotePos(posY, camExpansionRate);
            commandMng.Do(new Command(
                () => { noteMng.DeleteNoteFromBar(notesTypeList, notePosX, bar); },
                () => { noteMng.AddNoteFromBar(bar, type, length, notePosX, data, true); }
                ));
            UpdateUndoRedoEnable();

            SetEdited(true);
        }

        return;
    }


    /// <summary>
    /// ドラッグ中のオブジェクトをドロップした時の動作
    /// </summary>
    private void ReleaseDragObject(GameObject obj, bool isReseted)
    {
        ChartType chartType = GetChartType(havingObjPos[0].type);

        NoteData noteData;
        ObjectData objData;

        Vector3 before_pos;
        float before_width = 0f;
        Vector3 pos = obj.transform.position;
        
        float maxBar = GetBarLength();
        float _posX = (pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);

        // 以前の位置を計算する
        // レーン移動
        if (chartType == ChartType.areaMove)
        {
            objData = noteMng.objDatas[havingObjPos[0].type - 10][havingObjPos[0].num];

            before_pos = new Vector3(
                objData.position * (General.windowAreaWidth / 2f) + General.objectAreaPos.x,
                lineMng.GetNotePosFromNoteBar(objData.bar, camExpansionRate) + General.objectAreaPos.y,
                obj.transform.position.z
            );
        }
        // ノーツ系
        else
        {
            noteData = noteMng.notesDatas[havingObjPos[0].type][havingObjPos[0].num];
            before_width = noteData.width;

            // ホールド終点
            if ((havingObjPos[0].type == (int)NotesType.N_Hold || havingObjPos[0].type == (int)NotesType.N_ExHold) && obj.name == "end")
                before_pos = new Vector3(0f, noteData.length_bar * camExpansionRate, 0f);
            // キーボード系
            else if (chartType == ChartType.keybord || chartType == ChartType.exKeybord)
            {
                before_pos = new Vector3(
                    obj.transform.position.x,
                    lineMng.GetNotePosFromNoteBar(noteData.bar, camExpansionRate) + General.objectAreaPos.y,
                    obj.transform.position.z
                );
            }
            // マウス系
            else
            {
                before_pos = new Vector3(
                    noteData.position * (General.windowAreaWidth / 2f) + General.objectAreaPos.x,
                    lineMng.GetNotePosFromNoteBar(noteData.bar, camExpansionRate) + General.objectAreaPos.y,
                    obj.transform.position.z
                );
            }
        }


        // オブジェクトが終了線より奥にある場合 : ノーツ位置を元に戻して終了
        if (isReseted || pos.y - General.objectAreaPos.y > lineMng.GetNoteBarFromMusicTime(audioMng.GetMusicLength()) * camExpansionRate)
        {
            // ホールド終点
            if ((havingObjPos[0].type == (int)NotesType.N_Hold || havingObjPos[0].type == (int)NotesType.N_ExHold) && obj.name == "end")
                obj.transform.localPosition = before_pos;
            else
                obj.transform.position = before_pos;

            return;
        }

        // オブジェクトが開始線より手前にある場合 : 開始線上にノーツを設置
        else if (pos.y - General.objectAreaPos.y < lineMng.GetNotePosFromNoteBar(0f, camExpansionRate))
        {
            pos = new Vector3(
                pos.x,
                lineMng.GetNotePosFromNoteBar(0f, camExpansionRate) + General.objectAreaPos.y,
                pos.z
            );

        }

        // オブジェクトが両端より外にある場合
        if (Mathf.Abs(_posX) > 1f)
        {
            _posX = Mathf.Clamp(_posX, -1f, 1f);
            // ノーツ位置を両端の位置に戻す
            pos = new Vector3(
                _posX * (General.windowAreaWidth / 2f) + General.objectAreaPos.x,
                pos.y,
                pos.z
            );
        }
        

        // スナップ有効時
        if (uiMng.GetSnapYToggle() || uiMng.GetSnapXToggle())
        {
            GameObject snapObjY = null;
            GameObject snapObjX = null;

            // スナップ位置を取得
            if (uiMng.GetSnapYToggle())
            {
                snapObjY = snapPoolY.SartchNearGameObjectY(snapY_prefab, pos.y);
                if (snapObjY != null)
                    pos.y = snapObjY.transform.position.y;
                else
                    return;
            }
            if (uiMng.GetSnapXToggle())
            {
                if(chartType == ChartType.mouse || chartType == ChartType.areaMove)
                {
                    snapObjX = snapPoolX.SartchNearGameObjectX(snapX_prefab, pos.x);
                    if (snapObjX != null)
                        pos.x = snapObjX.transform.position.x;
                    else
                        return;
                }
            }
        }

        // ノーツ位置を更新
        int type = havingObjPos[0].type;
        float bar, nextBar;
        if (obj.tag == "Edge")
        {
            float edge_pos = Mathf.Clamp((pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f), -1f, 1f);
            float posX = obj.transform.parent.localPosition.x / (General.windowAreaWidth / 2f);

            commandMng.Do(new Command(
                () => { ChangeNoteWidth(obj.transform.parent.gameObject, posX, Mathf.Abs(posX - edge_pos) * 2f, true); },
                () => { ChangeNoteWidth(obj.transform.parent.gameObject, posX, before_width, true); },
                () => { ChangeNoteWidth(obj.transform.parent.gameObject, posX, Mathf.Abs(posX - edge_pos) * 2f, true); }
                ));
            UpdateUndoRedoEnable();
            
            return;
        }
        else
        {
            if ((type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold) && obj.name == "end")
            {
                obj.transform.localPosition = before_pos;
                bar = lineMng.GetNoteBarFromNotePos(obj.transform.parent.localPosition.y, camExpansionRate);
                nextBar = lineMng.GetNoteBarFromNotePos(pos.y - General.objectAreaPos.y, camExpansionRate);
            }
            else
            {
                obj.transform.position = before_pos;
                bar = lineMng.GetNoteBarFromNotePos(obj.transform.localPosition.y, camExpansionRate);
                nextBar = lineMng.GetNoteBarFromNotePos(pos.y - General.objectAreaPos.y, camExpansionRate);
            }
            
            commandMng.Do(new Command(
                () =>
                {
                    Vector3 _pos = new Vector3(obj.transform.position.x, lineMng.GetNotePosFromNoteBar(bar, camExpansionRate) + General.objectAreaPos.y, obj.transform.position.z);
                    Vector3 _after_pos = new Vector3(pos.x, lineMng.GetNotePosFromNoteBar(nextBar, camExpansionRate) + General.objectAreaPos.y, pos.z);
                    obj.transform.position = _pos;
                    UpdateDropNote(obj, type, _after_pos, before_width);
                },
                () => {
                    if ((type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold) && obj.name == "end")
                        obj.transform.localPosition = before_pos;
                    else
                        obj.transform.position = before_pos;
                    Vector3 _pos = new Vector3(pos.x, lineMng.GetNotePosFromNoteBar(nextBar, camExpansionRate) + General.objectAreaPos.y, pos.z);
                    Vector3 _after_pos = new Vector3(obj.transform.position.x, lineMng.GetNotePosFromNoteBar(bar, camExpansionRate) + General.objectAreaPos.y, obj.transform.position.z);
                    obj.transform.position = _pos;
                    UpdateDropNote(obj, type, _after_pos, before_width);
                },
                () =>
                {
                    Vector3 _pos = new Vector3(obj.transform.position.x, lineMng.GetNotePosFromNoteBar(bar, camExpansionRate) + General.objectAreaPos.y, obj.transform.position.z);
                    Vector3 _after_pos = new Vector3(pos.x, lineMng.GetNotePosFromNoteBar(nextBar, camExpansionRate) + General.objectAreaPos.y, pos.z);
                    obj.transform.position = _pos;
                    UpdateDropNote(obj, type, _after_pos, before_width);
                }
                ));
            UpdateUndoRedoEnable();

        }

    }


    /// <summary>
    /// ノーツの位置を変更して情報を更新する
    /// </summary>
    public void UpdateDropNote(GameObject obj, int type, Vector3 after_pos, float width)
    {
        List<int> notesTypeList;
        NOTES_LISTPOS objPos;

        ChartType chartType = GetChartType(type);
        float notePosX;

        Vector3 pos;
        if ((type == (int)NotesType.N_Hold || type == (int)NotesType.N_ExHold) && obj.name == "end")
            pos = obj.transform.parent.position;
        else
            pos = obj.transform.position;

        float maxBar = GetBarLength();

        NoteData noteData;
        ObjectData objData;

        // 移動前の座標を基準にしてノーツを検索する
        // レーン移動の場合
        if (chartType == ChartType.areaMove)
        {
            notesTypeList = new List<int>() { type };
            notePosX = (pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
        }
        // キーボード系ノーツの場合
        else if (chartType == ChartType.keybord || chartType == ChartType.exKeybord)
        {
            float lanePos = keybordNotesFolder.transform.localPosition.x;

            // ノーツのレーンの取得
            if (chartType == ChartType.exKeybord)
            {
                notePosX = 0;
                notesTypeList = new List<int>() { (int)AllObjectsType.N_ExHit, (int)AllObjectsType.N_ExHold };
            }
            else
            {
                if (pos.x - General.objectAreaPos.x + lanePos < -200f || pos.x - General.objectAreaPos.x + lanePos >= 200f)
                    return;
                else if (pos.x - General.objectAreaPos.x + lanePos < -100f)
                    notePosX = 1;
                else if (pos.x - General.objectAreaPos.x + lanePos < 0f)
                    notePosX = 2;
                else if (pos.x - General.objectAreaPos.x + lanePos < 100f)
                    notePosX = 3;
                else if (pos.x - General.objectAreaPos.x + lanePos < 200f)
                    notePosX = 4;
                else
                    return;

                notesTypeList = new List<int>() { (int)NotesType.N_Hit, (int)NotesType.N_Hold };
            }
        }
        // マウス系ノーツの場合
        else if (chartType == ChartType.mouse)
        {
            notePosX = (pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
            notesTypeList = new List<int>() { type };
        }
        else
            return;

        // ノーツを検索
        objPos = noteMng.FindNoteFromPos(notesTypeList, notePosX, pos.y - General.objectAreaPos.y);
        if (objPos.num < 0)
            return;

        // ノーツを移動
        obj.transform.position = after_pos;

        // レーン移動の場合
        if (chartType == ChartType.areaMove)
        {
            objData = noteMng.objDatas[objPos.type - 10][objPos.num];
            notePosX = (after_pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);

            // 移動先にノーツがあるか検索
            notesTypeList = new List<int>() { (int)OtherObjectsType.B_AreaMove };
            NOTES_LISTPOS findObjPos = noteMng.FindNoteFromPos(notesTypeList, notePosX, obj.transform.localPosition.y);

            // ノーツが見つかり、そのノーツが自身でない場合は削除
            if (findObjPos.num >= 0 && !Equals(findObjPos, objPos))
                noteMng.DeleteNote(findObjPos);

            // 譜面データを更新する
            objData.bar = lineMng.GetNoteBarFromNotePos(obj.transform.localPosition.y, camExpansionRate);
            objData.position = notePosX;
            noteMng.UpdateAreaMove();
        }

        else
        {
            noteData = noteMng.notesDatas[objPos.type][objPos.num];
            float lanePos = keybordNotesFolder.transform.localPosition.x;

            // キーボード系ノーツの場合
            if (chartType == ChartType.keybord || chartType == ChartType.exKeybord)
            {
                // ノーツのレーンの取得
                if (chartType == ChartType.exKeybord)
                {
                    notePosX = 0;
                    notesTypeList = new List<int>() { (int)NotesType.N_ExHit, (int)NotesType.N_ExHold };
                }
                else
                {
                    if (after_pos.x - General.objectAreaPos.x + lanePos < -200f || after_pos.x - General.objectAreaPos.x + lanePos >= 200f)
                        return;
                    else if (after_pos.x - General.objectAreaPos.x + lanePos < -100f)
                        notePosX = 1;
                    else if (after_pos.x - General.objectAreaPos.x + lanePos < 0f)
                        notePosX = 2;
                    else if (after_pos.x - General.objectAreaPos.x + lanePos < 100f)
                        notePosX = 3;
                    else if (after_pos.x - General.objectAreaPos.x + lanePos < 200f)
                        notePosX = 4;
                    else
                        return;

                    notesTypeList = new List<int>() { (int)NotesType.N_Hit, (int)NotesType.N_Hold };
                }
            }
            // マウス系ノーツの場合
            else if (chartType == ChartType.mouse)
            {
                notePosX = (after_pos.x - General.objectAreaPos.x) / (General.windowAreaWidth / 2f);
                notesTypeList = new List<int>();
            }
            else
                return;

            // ホールド終点
            if ((objPos.type == (int)NotesType.N_Hold || objPos.type == (int)NotesType.N_ExHold) && obj.name == "end")
            {
                GameObject startObj = obj.transform.parent.GetChild(0).gameObject;
                GameObject holdObj = obj.transform.parent.GetChild(1).gameObject;

                float length_bar = (obj.transform.position.y - startObj.transform.position.y) / camExpansionRate;

                // オブジェクトが終了線より後ろにある場合 : 元に戻す
                if (length_bar <= 0f)
                {
                    // ノーツ位置を譜面データに従って元に戻す
                    obj.transform.localPosition = new Vector3(0f, noteData.length_bar * camExpansionRate, 0f);
                    return;
                }
                ChangeHoldLength(obj.transform.parent.gameObject, (int)notePosX, length_bar);
            }

            // ホールド終点以外
            else
            {
                // オブジェクトが両端より外にある場合、両端の位置に直す
                if (chartType == ChartType.mouse && Mathf.Abs(notePosX) > 1f)
                {
                    notePosX = Mathf.Clamp(notePosX, -1f, 1f);
                    // ノーツ位置を両端の位置に戻す
                    obj.transform.position = new Vector3(
                        notePosX * (General.windowAreaWidth / 2f) + General.objectAreaPos.x,
                        obj.transform.position.y,
                        obj.transform.position.z
                    );
                }

                // 移動先にノーツがあるか検索 (キーボード系のみ)
                if (chartType == ChartType.keybord || chartType == ChartType.exKeybord)
                {
                    NOTES_LISTPOS findObjPos = noteMng.FindNoteFromPos(notesTypeList, notePosX, obj.transform.localPosition.y);
                    // ノーツが見つかり、そのノーツが自身でない場合は削除
                    if (findObjPos.num >= 0 && !Equals(findObjPos, objPos))
                        noteMng.DeleteNote(findObjPos);
                }

                // 譜面データを更新する
                noteData.bar = lineMng.GetNoteBarFromNotePos(obj.transform.localPosition.y, camExpansionRate);
                noteData.position = notePosX;

                // Holdノーツであれば、Hold範囲内のノーツを削除する
                if (objPos.type == (int)NotesType.N_Hold || objPos.type == (int)NotesType.N_ExHold)
                {
                    // ノーツ移動の結果Holdが終了線を超えた場合、Hold長を終了線までに修正する
                    if (noteData.bar + noteData.length_bar >= maxBar)
                    {
                        noteData.length_bar = maxBar - noteData.bar;
                        ChangeHoldLength(obj, (int)notePosX, noteData.length_bar);
                    }
                    noteMng.DeleteNotesOfRange((int)notePosX, obj.transform.localPosition.y, obj.transform.localPosition.y + noteData.length_bar * camExpansionRate);
                }

                noteMng.UpdateAllNotesTime();


                // 可変幅ノーツの場合、ノーツ幅を更新
                if (type == (int)NotesType.N_Click || type == (int)NotesType.N_Catch || type == (int)NotesType.N_Swing)
                    ChangeNoteWidth(obj, notePosX, width, true);
            }
        }
    }


    /// <summary>
    /// Click / Catch / Swing の幅を変更する (resizeEnableがtrueの場合はノーツ位置によってサイズを補正する)
    /// </summary>
    public void ChangeNoteWidth(GameObject obj, float position, float width, bool resizeEnable)
    {
        int type = GetNotesTypeForObjectTag(obj);
        List<int> notesTypeList;
        float fixedWidth;

        // ノーツが画面外にはみ出す場合は修正する
        if (resizeEnable)
        {
            if (position + width / 2f > 1f)
                fixedWidth = Mathf.Abs((1f - position) * 2f);
            else if (position - width / 2f < -1f)
                fixedWidth = Mathf.Abs((1f + position) * 2f);
            else
                fixedWidth = width;

            // ノーツ幅が非常に短い場合は削除する
            // (ノーツ保持中に削除するとエラーが出るため、保持中は削除しない)
            if (fixedWidth < 0.1f)
            {
                // ノーツを削除する
                notesTypeList = new List<int>() { type };
                float bar = lineMng.GetNoteBarFromNotePos(obj.transform.localPosition.y, camExpansionRate);
                switch (cmdMode)
                {
                    // 通常実行時、コマンドを追加
                    case commandMode.Do:
                        commandMng.Do(new Command(
                            () => { noteMng.DeleteNoteFromBar(notesTypeList, position, bar); },
                            () => { noteMng.AddNoteFromBar(bar, type, 0f, position, width, true); }
                            ));
                        break;
                    // Undoでの実行時、もう1回Undoを実行(ノートの移動・幅変更処理の前に戻る)
                    case commandMode.Undo:
                        commandMng.Undo();
                        break;
                    // Redoでの実行時、もう1回Redoを実行(ノートの削除を行う)
                    case commandMode.Redo:
                        commandMng.Redo();
                        break;
                }

                UpdateUndoRedoEnable();
                return;
            }
        }
        else
            fixedWidth = width;

        float posX = position * (General.windowAreaWidth / 2f);
        float lineLength = fixedWidth * (General.windowAreaWidth / 2f);
        float lineWidth;
        LineRenderer line = obj.GetComponent<LineRenderer>();

        if (type == (int)NotesType.N_Click)
            lineWidth = 16f;
        else if (type == (int)NotesType.N_Catch)
            lineWidth = 8f;
        else if (type == (int)NotesType.N_Swing)
            lineWidth = 16f;
        else
            return;

        BoxCollider2D collider = obj.GetComponent<BoxCollider2D>();

        // Catch の場合
        if (type == (int)NotesType.N_Catch)
        {
            line.SetPosition(0, new Vector3(-lineLength / 2f + 4f, 0f, 0f));
            line.SetPosition(1, new Vector3(lineLength / 2f - 4f, 0f, 0f));
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            LineRenderer bgLine = obj.transform.GetChild(0).GetComponent<LineRenderer>();
            bgLine.SetPosition(0, new Vector3(-lineLength / 2f, 0f, 0f));
            bgLine.SetPosition(1, new Vector3(lineLength / 2f, 0f, 0f));

            obj.transform.GetChild(2).localPosition = new Vector3(-lineLength / 2f + 1f, 0f, 0f);
            obj.transform.GetChild(3).localPosition = new Vector3(lineLength / 2f - 1f, 0f, 0f);

            collider.size = new Vector2(lineLength - 8f, lineWidth);
        }
        // Click / Swingの場合
        else if (type == (int)NotesType.N_Click || type == (int)NotesType.N_Swing)
        {
            line.SetPosition(0, new Vector3(-lineLength / 2f + 4f, 0f, 0f));
            line.SetPosition(1, new Vector3(lineLength / 2f - 4f, 0f, 0f));
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            obj.transform.GetChild(0).localPosition = new Vector3(-lineLength / 2f + 4f, 0f, 0f);
            obj.transform.GetChild(1).localPosition = new Vector3(lineLength / 2f - 4f, 0f, 0f);
            obj.transform.GetChild(2).localPosition = new Vector3(-lineLength / 2f, 0f, 0f);
            obj.transform.GetChild(3).localPosition = new Vector3(lineLength / 2f, 0f, 0f);

            collider.size = new Vector2(lineLength - 8f, lineWidth);
        }
        else
            return;

        // ノーツ保持中の場合は譜面データの更新は行わない
        if (!resizeEnable)
            return;


        // 譜面データを取得する
        notesTypeList = new List<int>() { type };
        NOTES_LISTPOS notePos = noteMng.FindNoteFromPos(notesTypeList, position, obj.transform.localPosition.y);

        if (notePos.num < 0)
            return;

        // 譜面データを更新する
        noteMng.notesDatas[notePos.type][notePos.num].width = fixedWidth;
    }



    /// <summary>
    /// ロングノーツの長さを変更する
    /// </summary>
    public void ChangeHoldLength(GameObject obj, int lane, float length_bar)
    {
        GameObject holdObj = obj.transform.GetChild(1).gameObject;
        GameObject endObj = obj.transform.GetChild(2).gameObject;

        // 終点ノートの位置変更
        endObj.transform.localPosition = new Vector3(endObj.transform.localPosition.x, length_bar * camExpansionRate, endObj.transform.localPosition.z);

        // ロングノートの位置変更(通常カメラ用)
        LineRenderer line = holdObj.GetComponent<LineRenderer>();
        BoxCollider2D collider = holdObj.GetComponent<BoxCollider2D>();
        if (lane == 0)
        {
            line.SetPosition(0, new Vector3(0f, 0f, 0f));
            line.SetPosition(1, new Vector3(0f, length_bar * camExpansionRate, 0f));
            line.startWidth = 380f;
            line.endWidth = 380f;
            collider.offset = new Vector2(0f, length_bar * camExpansionRate / 2f);
            collider.size = new Vector2(380f, length_bar * camExpansionRate - 20f);
        }
        else
        {
            line.SetPosition(0, new Vector3(0f, 0f, 0f));
            line.SetPosition(1, new Vector3(0f, length_bar * camExpansionRate, 0f));
            line.startWidth = 60f;
            line.endWidth = 60f;
            collider.offset = new Vector2(0f, length_bar * camExpansionRate / 2f);
            collider.size = new Vector2(60f, length_bar * camExpansionRate - 20f);
        }

        // ホールド範囲内のノーツを削除する
        noteMng.DeleteNotesOfRange(lane, obj.transform.localPosition.y, obj.transform.localPosition.y + length_bar * camExpansionRate);

        // 譜面データを取得する
        List<int> notesTypeList = new List<int>() { (int)NotesType.N_Hold, (int)NotesType.N_ExHold };
        NOTES_LISTPOS pos = noteMng.FindNoteFromPos(notesTypeList, lane, obj.transform.localPosition.y);

        if (pos.num < 0)
            return;

        // 譜面データを更新する
        noteMng.notesDatas[pos.type][pos.num].length_bar = length_bar;
        noteMng.UpdateAllNotesTime();
    }




    /// <summary>
    /// カメラの拡大率に合わせてロングノートの描画長を調整する
    /// </summary>
    public void UpdateHoldObjectForCam()
    {
        List<NotesType> notesTypeList = new List<NotesType>() { NotesType.N_Hold, NotesType.N_ExHold };
        foreach (NotesType type in notesTypeList)
        {
            foreach (NoteData item in noteMng.notesDatas[(int)type])
            {
                GameObject parentObj = item.obj;
                GameObject holdObj = parentObj.transform.GetChild(1).gameObject;
                GameObject endObj = parentObj.transform.GetChild(2).gameObject;
                float length_bar = item.length_bar;
                int lane = (int)item.position;

                // 終点ノートの位置変更
                endObj.transform.localPosition = new Vector3(endObj.transform.localPosition.x, length_bar * camExpansionRate, endObj.transform.localPosition.z);

                // ロングノートの位置変更(通常カメラ用)
                LineRenderer line = holdObj.GetComponent<LineRenderer>();
                BoxCollider2D collider = holdObj.GetComponent<BoxCollider2D>();
                if (type == NotesType.N_ExHold)
                {
                    line.SetPosition(0, new Vector3(0f, 0f, 0f));
                    line.SetPosition(1, new Vector3(0f, length_bar * camExpansionRate, 0f));
                    line.startWidth = 380f;
                    line.endWidth = 380f;
                    collider.offset = new Vector2(0f, length_bar * camExpansionRate / 2f);
                    collider.size = new Vector2(380f, length_bar * camExpansionRate - 20f);
                }
                else
                {
                    line.SetPosition(0, new Vector3(0f, 0f, 0f));
                    line.SetPosition(1, new Vector3(0f, length_bar * camExpansionRate, 0f));
                    line.startWidth = 60f;
                    line.endWidth = 60f;
                    collider.offset = new Vector2(0f, length_bar * camExpansionRate / 2f);
                    collider.size = new Vector2(60f, length_bar * camExpansionRate - 20f);
                }
            }
        }
    }




    #region [その他汎用メソッド]

    /// <summary>
    /// 画面のクリック座標をワールド座標に変換する
    /// </summary>
    private Vector3 MousePosToWorldPos(Vector2 mousePos)
    {
        Vector3 canvas_pos = ExMethod.GetPosition_touchToCanvas(canvasRect, mousePos);

        Vector3 worldPos = spotCam.ScreenToWorldPoint(mousePos);

        return worldPos;
    }


    /// <summary>
    /// ノーツデータがどの譜面属性(キーボード系 / マウス系 / 移動系 / ギミック系)に属するかを返す
    /// </summary>
    public ChartType GetChartType(int type)
    {
        switch (type)
        {
            // キーボード系
            case (int)NotesType.N_Hit:
            case (int)NotesType.N_Hold:
                return ChartType.keybord;

            // Exキーボード系
            case (int)NotesType.N_ExHit:
            case (int)NotesType.N_ExHold:
                return ChartType.exKeybord;

            // マウス系
            case (int)NotesType.N_Click:
            case (int)NotesType.N_Catch:
            case (int)NotesType.N_Flick_L:
            case (int)NotesType.N_Flick_R:
            case (int)NotesType.N_Swing:
                return ChartType.mouse;

            // 移動系
            case (int)OtherObjectsType.B_AreaMove:
                return ChartType.areaMove;

            // ギミック系
            case (int)OtherObjectsType.B_Bar:
            case (int)OtherObjectsType.B_Bpm:
            case (int)OtherObjectsType.B_Stop:
                return ChartType.gimmick;
            default:
                return ChartType.none;
        }
    }

    /// <summary>
    /// 対象のゲームオブジェクトのタグを確認してノートの種類を返す
    /// </summary>
    public int GetNotesTypeForObjectTag(GameObject obj)
    {
        switch(obj.tag)
        {
            case "Hit":
                return (int)NotesType.N_Hit;
            case "Hold":
                return (int)NotesType.N_Hold;
            case "Click":
                return (int)NotesType.N_Click;
            case "Catch":
                return (int)NotesType.N_Catch;
            case "Flick_L":
                return (int)NotesType.N_Flick_L;
            case "Flick_R":
                return (int)NotesType.N_Flick_R;
            case "Swing":
                return (int)NotesType.N_Swing;
            case "ExHit":
                return (int)NotesType.N_ExHit;
            case "ExHold":
                return (int)NotesType.N_ExHold;
            case "Bar":
                return (int)OtherObjectsType.B_Bar;
            case "Bpm":
                return (int)OtherObjectsType.B_Bpm;
            case "Stop":
                return (int)OtherObjectsType.B_Stop;
            case "AreaMove":
                return (int)OtherObjectsType.B_AreaMove;
            default:
                return -1;
        }
    }


    /// <summary>
    /// 現在のノートの種類からRayで当たり判定を確認するためのレイヤーマスクを返す
    /// </summary>
    private int GetLayerMaskForRay()
    {
        // 現在のノーツ種類のタイプ(0=キーボードノーツ / 1=Exキーボードノーツ / 2=マウスノーツ / 3=エリア移動 / 4=ギミック系)
        ChartType chartType = GetChartType(noteTypeId);

        // 現在のノーツ種別に応じてRayのレイヤーマスクを設定
        switch (chartType)
        {
            // キーボード時
            case ChartType.keybord:
                return LayerMask.GetMask(new string[] { "keyBordNotes", "gimmickObjects" });
            // Exキーボード時
            case ChartType.exKeybord:
                return LayerMask.GetMask(new string[] { "exKeyBordNotes", "gimmickObjects" });
            // マウス時
            case ChartType.mouse:
                return LayerMask.GetMask(new string[] { "mouseNotes", "gimmickObjects" });
            // エリア移動
            case ChartType.areaMove:
                return LayerMask.GetMask(new string[] { "areaMoveObjects", "gimmickObjects" });
            // その他
            default:
                return LayerMask.GetMask(new string[] { "gimmickObjects" });
        }
    }


    #endregion

}
