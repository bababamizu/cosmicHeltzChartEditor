using chChartEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    [SerializeField]
    private GameManager gameMng;

    [SerializeField]
    private Button importButton;
    [SerializeField]
    private Button exportButton;
    [SerializeField]
    private Button exportAsButton;

    [SerializeField]
    private Button undoButton;
    [SerializeField]
    private Button redoButton;

    [SerializeField]
    private Button hitButton;
    [SerializeField]
    private Button exHitButton;
    [SerializeField]
    private Button holdButton;
    [SerializeField]
    private Button exHoldButton;

    [SerializeField]
    private Dropdown snapY_split;
    [SerializeField]
    private InputField snapY_customSplit;
    [SerializeField]
    private InputField snapX_split;

    [SerializeField]
    private TMP_InputField offset;
    [SerializeField]
    private Button openSetting;
    [SerializeField]
    private GameObject settings;
    [SerializeField]
    private GameObject guideAndLicense;
    [SerializeField]
    private Text versionText;

    private GameObject guide;
    private GameObject license;

    [SerializeField]
    private InputField music_id;
    [SerializeField]
    private Dropdown diff;
    [SerializeField]
    private Slider level_bar;
    [SerializeField]
    private InputField level_num;
    [SerializeField]
    private InputField charter;

    [SerializeField]
    private Camera areaCam;
    [SerializeField]
    private Slider areaCamSlider;
    [SerializeField]
    private Slider camExpansionSlider;

    [SerializeField]
    private Text TotalNotesText;

    [SerializeField]
    private GameObject BarSetting;
    [SerializeField]
    private GameObject BpmSetting;
    [SerializeField]
    private GameObject StopSetting;

    [SerializeField]
    private Button stop_up;
    [SerializeField]
    private Button stop_down;

    private Snap snap_Y;
    private Snap snap_X;

    private bool autoScr = true;

    [System.NonSerialized]
    public bool isSetting = false;


    private GameObject lineObj;
    private GameObject bpmObj;
    private GameObject stopObj;

    private Button currentChangeNoteButton;

    private float stopTmp;

    void Start() {
        guide = guideAndLicense.transform.GetChild(0).gameObject;
        license = guideAndLicense.transform.GetChild(1).gameObject;

        versionText.text = "Current Version : v" + Application.version;

        currentChangeNoteButton = hitButton;
        snap_Y.toggle = true;
        snap_Y.split = 4;
        snap_X.toggle = false;
        snap_X.split = 8;
        camExpansionSlider.interactable = false;
        openSetting.interactable = false;
        SettingsEnable(false);
        gameMng.UpdateLightMask();


    }

    /// <summary>
    /// Setting画面を開閉する
    /// </summary>
    public void SettingsEnable(bool isOn)
    {
        isSetting = isOn;
        settings.SetActive(isOn);
    }


    #region [各種値を返すメソッド群]

    public bool GetSnapYToggle()
    {
        return snap_Y.toggle;
    }
    public int GetSnapYSplit()
    {
        return snap_Y.split;
    }

    public bool GetSnapXToggle()
    {
        return snap_X.toggle;
    }
    public int GetSnapXSplit()
    {
        return snap_X.split;
    }

    public float GetOffset()
    {
        if (offset.text.IndexOf("-") != -1)
            return 0.00f;
        else
            return Mathf.Max(0f, float.Parse(offset.text));
    }
    public bool GetAutoScr()
    {
        return autoScr;
    }
    public bool GetOpenSetting()
    {
        return isSetting;
    }
    public string GetMusicID()
    {
        if (music_id.text != "")
            return music_id.text;
        else
            return "example";
    }
    public int GetDiff()
    {
        return diff.value;
    }
    public int GetLevel()
    {
        return (int)level_bar.value;
    }
    public string GetCharter()
    {
        if (charter.text != "")
            return charter.text;
        else
            return "-";
    }

    #endregion


    #region [各種値を設定するメソッド群]


    /// <summary>
    /// 譜面に対する各種操作(Settingを開く、譜面のインポート)の可否を設定
    /// </summary>
    public void SetSettingButtonEnable(bool isOn)
    {
        openSetting.interactable = isOn;
        importButton.interactable = isOn;
        exportAsButton.interactable = isOn;
    }

    /// <summary>
    /// 上書き保存の可否を設定
    /// </summary>
    public void SetExportEnable(bool isOn)
    {
        exportButton.interactable = isOn;
    }

    /// <summary>
    /// Setting画面の値を設定
    /// </summary>
    public void ImportSettings(string _music_id, int _diff, int _level, string _charter, float _offset)
    {
        music_id.text = _music_id;
        diff.value = _diff;
        level_bar.value = _level;
        ChangeLevelSlider();
        charter.text = _charter;
        offset.text = _offset.ToString("F3");
        Offset_onEdit();
    }

    /// <summary>
    /// STOP設定画面の上下ボタン(Y軸スナップに合わせてSTOP長を変更)の有効/無効を設定
    /// </summary>
    private void SetStopUpDownButtonActive()
    {
        // 上ボタンはY軸スナップの有効/無効にのみ依存
        stop_up.interactable = snap_Y.toggle;

        // 下ボタンはY軸スナップ有効時かつSTOP値が0を下回らない場合のみ有効
        if (snap_Y.toggle && 4f / snap_Y.split < stopTmp)
            stop_down.interactable = true;
        else
            stop_down.interactable = false;

    }

    #endregion


    #region [ツールバーのUI操作]

    public void ImportButton_down()
    {
        gameMng.Import();
    }

    public void ExportButton_down(bool isExportAs)
    {
        gameMng.Export(isExportAs, false);
    }


    [EnumAction(typeof(AllObjectsType))]
    public void ChangeNoteType(int type)
    {
        gameMng.UpdateNotesType(type);
        gameMng.UpdateLightMask();
    }

    public void UpdateChangeNoteButton(Button button)
    {
        if (currentChangeNoteButton != null)
            currentChangeNoteButton.interactable = true;
        button.interactable = false;
        currentChangeNoteButton = button;
    }

    public void ShiftChangeNoteButton(int type)
    {
        if (currentChangeNoteButton != null)
            currentChangeNoteButton.interactable = true;

        if(type == (int)NotesType.N_Hit)
        {
            holdButton.interactable = false;
            currentChangeNoteButton = holdButton;
        }
        if (type == (int)NotesType.N_ExHit)
        {
            exHoldButton.interactable = false;
            currentChangeNoteButton = exHoldButton;
        }
    }

    public void UnShiftChangeNoteButton(int type)
    {
        if (currentChangeNoteButton != null)
            currentChangeNoteButton.interactable = true;

        if (type == (int)NotesType.N_Hit)
        {
            hitButton.interactable = false;
            currentChangeNoteButton = hitButton;
        }
        if (type == (int)NotesType.N_ExHit)
        {
            exHitButton.interactable = false;
            currentChangeNoteButton = exHitButton;
        }
    }

    public void Offset_onEdit()
    {
        if (offset.text.IndexOf("-") != -1)
            offset.text = "0.00";
        gameMng.ChangeSnapLine(true);
    }

    public void SetTotalNotes(int notes)
    {
        TotalNotesText.text = notes.ToString();
    }

    public void AutoScroll_toggle()
    {
        autoScr = !autoScr;
    }


    #region [Y軸スナップの操作]

    public void SnapY_toggle()
    {
        snap_Y.toggle = !snap_Y.toggle;
        snapY_split.interactable = !snapY_split.interactable;
        gameMng.SnapY_toggle(snap_Y.toggle);
        SetStopUpDownButtonActive();
    }

    public void SnapY_onEdit()
    {
        int value = snapY_split.value;
        bool isChanged = true;
        switch (value)
        {
            case 0:
                snap_Y.split = 4;
                break;
            case 1:
                snap_Y.split = 8;
                break;
            case 2:
                snap_Y.split = 12;
                break;
            case 3:
                snap_Y.split = 16;
                break;
            case 4:
                snap_Y.split = 24;
                break;
            case 5:
                snap_Y.split = 32;
                break;
            case 6:
                snap_Y.split = 48;
                break;
            case 7:
                snap_Y.split = 64;
                break;
            case 8:
                Destroy(snapY_split.transform.GetChild(3).gameObject);
                snapY_customSplit.text = snap_Y.split.ToString();
                snapY_split.gameObject.SetActive(false);
                isChanged = false;
                break;
            default:
                return;
        }

        if (snapY_split.options.Count > 9)
        {
            snapY_split.ClearOptions();
            snapY_split.AddOptions(
                new List<string>()
                {
                    "4分",
                    "8分",
                    "12分",
                    "16分",
                    "24分",
                    "32分",
                    "48分",
                    "64分",
                    "カスタム",
                });
            snapY_split.template.sizeDelta = new Vector2(snapY_split.template.sizeDelta.x, 24f * 9f);
        }

        if (isChanged)
        {
            gameMng.ChangeSnapLineY();
            SetStopUpDownButtonActive();
        }

    }

    public void SnapY_onCustomEdit()
    {
        int num;
        if (!int.TryParse(snapY_customSplit.text, out num))
            snapY_customSplit.text = snap_Y.split.ToString();
        else if (num <= 0)
            snapY_customSplit.text = snap_Y.split.ToString();
        else
        {
            snap_Y.split = num;
            snapY_split.AddOptions(new List<string>() { string.Format("{0}分", num) });
            snapY_split.template.sizeDelta = new Vector2(snapY_split.template.sizeDelta.x, 24f * 10f);
            snapY_split.value = 9;
            gameMng.ChangeSnapLineY();
            SetStopUpDownButtonActive();
        }

        snapY_split.gameObject.SetActive(true);

    }

    #endregion


    #region [X軸スナップの操作]

    public void SnapX_toggle()
    {
        snap_X.toggle = !snap_X.toggle;
        snapX_split.interactable = !snapX_split.interactable;
        gameMng.SnapX_toggle(snap_X.toggle);
    }

    public void SnapX_onEdit()
    {
        int num;
        if (!int.TryParse(snapX_split.text, out num))
            snapX_split.text = "4";
        else if (num <= 0)
            snapX_split.text = "4";

        snap_X.split = num;
        gameMng.ChangeSnapLineX();
    }

    #endregion


    #region [Undo / Redo]
    public void UpdateUndoButtonEnable(bool isEnable)
    {
        undoButton.interactable = isEnable;
    }

    public void UpdateRedoButtonEnable(bool isEnable)
    {
        redoButton.interactable = isEnable;
    }

    #endregion


    #endregion


    #region [Setting画面のUI操作]

    public void SettingButton_down()
    {
        isSetting = !isSetting;
        SettingsEnable(isSetting);
    }

    public void ChangeLevelSlider()
    {
        level_num.text = level_bar.value.ToString();
        ChangeSettingValue();
    }

    public void ChangeSettingValue()
    {
        gameMng.SetEdited(true);
    }




    #endregion


    #region [License画面のUI操作]

    public void GuideAndLicenseButton_Down()
    {
        guideAndLicense.SetActive(!guideAndLicense.activeSelf);
    }

    public void ChangeGuideAndLicenseTab()
    {
        guide.SetActive(!guide.activeSelf);
        license.SetActive(!license.activeSelf);
    }



    #endregion


    #region [全体カメラの操作]

    public void ChangeAreaCamSlider()
    {
        areaCam.transform.localPosition = new Vector3(areaCam.transform.localPosition.x, areaCamSlider.value, areaCam.transform.localPosition.z);
    }

    public void FixAreaCamSlider()
    {
        areaCamSlider.value = areaCam.transform.localPosition.y;
    }

    public void SetMaxAreaCamPosition(float barlength)
    {
        if(barlength * gameMng.camExpansionRate - 2500f > 0f)
            areaCamSlider.maxValue = barlength * gameMng.camExpansionRate - 2500f;
        else
            areaCamSlider.maxValue = 2500f;

        return;
    }

    public float GetMaxAreaCamPosition(float barlength)
    {
        if (barlength * gameMng.camExpansionRate - 2500f > 0f)
            return barlength * gameMng.camExpansionRate - 2500f;
        else
            return 2500f;
    }

    #endregion


    #region [カメラの拡大・縮小]

    public void SetCamExpansionSliderEnable(bool isOn)
    {
        camExpansionSlider.interactable = isOn;
    }

    public void OnChangeCamExpansionSlider()
    {
        gameMng.UpdateCamExpansionRate(300f * Mathf.Pow(2f, camExpansionSlider.value / 4f));
    }

    public void SetCamExpansionSlider(int value)
    {
        camExpansionSlider.value = value;
    }
    public int GetCamExpansionSliderValue()
    {
        return (int)camExpansionSlider.value;
    }

    #endregion


    #region [小節設定]

    public void OpenBarSetting(float posY, GameObject lineObj)
    {
        this.lineObj = lineObj;

        BarSetting.transform.position = new Vector2(BarSetting.transform.position.x, posY);

        if (BarSetting.transform.localPosition.y < 0f)
            BarSetting.transform.localPosition = new Vector2(BarSetting.transform.localPosition.x, BarSetting.transform.localPosition.y + 200f);
        else
            BarSetting.transform.localPosition = new Vector2(BarSetting.transform.localPosition.x, BarSetting.transform.localPosition.y - 30f);

        SetBarSetting();

        BarSetting.SetActive(true);
    }

    private void SetBarSetting()
    {
        Line line = lineObj.GetComponent<Line>();

        BarSetting.transform.GetChild(1).GetComponent<Text>().text = string.Format("{0:000}", line.number);
        // Beat
        BarSetting.transform.GetChild(2).GetChild(1).GetComponent<Toggle>().isOn = line.isMeasureChange;
        BarSetting.transform.GetChild(2).GetChild(2).GetComponent<InputField>().interactable = line.isMeasureChange;
        BarSetting.transform.GetChild(2).GetChild(2).GetComponent<InputField>().text = line.measure_numer.ToString();
        BarSetting.transform.GetChild(2).GetChild(3).GetComponent<InputField>().interactable = line.isMeasureChange;
        BarSetting.transform.GetChild(2).GetChild(3).GetComponent<InputField>().text = line.measure_denom.ToString();

        if (line.number == 1)
            BarSetting.transform.GetChild(2).GetChild(1).GetComponent<Toggle>().interactable = false;
        else
            BarSetting.transform.GetChild(2).GetChild(1).GetComponent<Toggle>().interactable = true;
    }

    public void ChangeMeasureChangeToggle(Toggle toggle)
    {
        BarSetting.transform.GetChild(2).GetChild(2).GetComponent<InputField>().interactable = toggle.isOn;
        BarSetting.transform.GetChild(2).GetChild(3).GetComponent<InputField>().interactable = toggle.isOn;
    }

    public void Beat_onEdit(InputField field)
    {
        int num;
        if (!int.TryParse(field.text, out num))
            field.text = "4";
        else if (num <= 0)
            field.text = "4";
    }

    public void ApplyBarSetting()
    {
        Line line = lineObj.GetComponent<Line>();
        int InputMeasureN;
        int InputMeasureD;

        if(     int.TryParse(BarSetting.transform.GetChild(2).GetChild(2).GetComponent<InputField>().text, out InputMeasureN)
            &&  int.TryParse(BarSetting.transform.GetChild(2).GetChild(3).GetComponent<InputField>().text, out InputMeasureD))
        {
            if (BarSetting.transform.GetChild(2).GetChild(1).GetComponent<Toggle>().isOn)
                gameMng.EditMeasure(line.number, InputMeasureN, InputMeasureD);
            else
                gameMng.DeleteMeasure(line.number);
        }

        CloseBarSetting();
    }

    public void CloseBarSetting()
    {
        BarSetting.SetActive(false);
        this.lineObj = null;
    }
    #endregion


    #region [BPM設定]

    public void OpenBpmSetting(float posY, GameObject bpmObj)
    {
        this.bpmObj = bpmObj;

        BpmSetting.transform.position = new Vector2(BpmSetting.transform.position.x, posY);

        if (BpmSetting.transform.localPosition.y < 0f)
            BpmSetting.transform.localPosition = new Vector2(BpmSetting.transform.localPosition.x, BpmSetting.transform.localPosition.y + 175f);
        else
            BpmSetting.transform.localPosition = new Vector2(BpmSetting.transform.localPosition.x, BpmSetting.transform.localPosition.y - 20f);

        SetBpmSetting();

        BpmSetting.SetActive(true);
    }

    private void SetBpmSetting()
    {
        GimmickObject obj = bpmObj.GetComponent<GimmickObject>();
        BpmSetting.transform.GetChild(2).GetComponent<InputField>().text = obj.value.ToString();
    }

    public void Bpm_onEdit(InputField field)
    {
        float bpm;
        if (!float.TryParse(field.text, out bpm))
            field.text = "128";
        else if (bpm <= 0)
            field.text = "128";
    }

    public void ApplyBpmSetting()
    {
        GimmickObject obj = bpmObj.GetComponent<GimmickObject>();
        float input;

        if (float.TryParse(BpmSetting.transform.GetChild(2).GetComponent<InputField>().text, out input))
            gameMng.EditBpm(obj.bar, input);

        CloseBpmSetting();
    }

    public void CloseBpmSetting()
    {
        BpmSetting.SetActive(false);
        this.bpmObj = null;
    }

    #endregion


    #region [STOP設定]

    public void OpenStopSetting(float posY, GameObject stopObj)
    {
        this.stopObj = stopObj;

        StopSetting.transform.position = new Vector2(StopSetting.transform.position.x, posY);

        if (StopSetting.transform.localPosition.y  < 0f)
            StopSetting.transform.localPosition = new Vector2(StopSetting.transform.localPosition.x, StopSetting.transform.localPosition.y + 165f);
        else
            StopSetting.transform.localPosition = new Vector2(StopSetting.transform.localPosition.x, StopSetting.transform.localPosition.y - 40f);

        SetStopSetting();

        StopSetting.SetActive(true);

    }

    private void SetStopSetting()
    {
        GimmickObject obj = stopObj.GetComponent<GimmickObject>();
        StopSetting.transform.GetChild(2).GetComponent<InputField>().text = string.Format("{0}", obj.value);
        stopTmp = obj.value;
        SetStopUpDownButtonActive();
    }

    public void Stop_onEdit(InputField field)
    {
        float stop;
        if (!float.TryParse(field.text, out stop))
            field.text = "0.25";
        else if (stop <= 0)
            field.text = "0.25";
        else
        {
            stopTmp = stop;
            SetStopUpDownButtonActive();
        }
    }


    // UNDONE : 12分などの循環小数でStop_up/downを行うと小数点以下が切り捨てられてしまう(0.333 * 3 = 0.999 になってしまう) 要修正？
    // 解決策 : 値を変数で保持しておく(ToString()で切り捨てが発生してしまうため)
    public void Stop_up()
    {
        float input;
        if (float.TryParse(StopSetting.transform.GetChild(2).GetComponent<InputField>().text, out input))
        {
            stopTmp += 4f / snap_Y.split;
            StopSetting.transform.GetChild(2).GetComponent<InputField>().text = string.Format("{0}", stopTmp);
            SetStopUpDownButtonActive();
        }
    }

    public void Stop_down()
    {
        float input;
        if (float.TryParse(StopSetting.transform.GetChild(2).GetComponent<InputField>().text, out input))
        {
            if (stopTmp - 4f / snap_Y.split > 0f)
            {
                stopTmp -= 4f / snap_Y.split;
                StopSetting.transform.GetChild(2).GetComponent<InputField>().text = string.Format("{0}", stopTmp);
                SetStopUpDownButtonActive();
            }
        }
    }

    public void ApplyStopSetting()
    {
        GimmickObject obj = stopObj.GetComponent<GimmickObject>();
        float input;

        if (float.TryParse(StopSetting.transform.GetChild(2).GetComponent<InputField>().text, out input))
            gameMng.EditStop(obj.bar, input / 4f);
        
        CloseStopSetting();
    }

    public void CloseStopSetting()
    {
        StopSetting.SetActive(false);
        stopTmp = 0f;
        this.stopObj = null;
    }

    #endregion


}
