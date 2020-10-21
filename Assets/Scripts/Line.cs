using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : GimmickObject {

    public int number;  // 小節番号(1から)
    public bool isMeasureChange = false; // この小節で拍子が変化するか否か
    public int measure_numer = 4;   // 分子
    public int measure_denom = 4;   // 分母

    public void SetUp(int num, float bar, bool isMeasureChange, int measure_n, int measure_d)
    {
        this.number = num;
        this.bar = bar;
        UpdateMeasure(isMeasureChange, measure_n, measure_d);
    }

    public void UpdateMeasure(bool _isBeatChange, int numer, int denom)
    {
        isMeasureChange = _isBeatChange;
        measure_numer = numer;
        measure_denom = denom;
        UpdateUIData();
    }

    override protected void UpdateUIData()
    {
        transform.GetChild(0).GetComponent<TextMesh>().text = string.Format("{0:000}", number);
        transform.GetChild(1).GetComponent<MeshRenderer>().enabled = isMeasureChange;
        transform.GetChild(1).GetComponent<TextMesh>().text = string.Format("{0}/{1}", measure_numer, measure_denom);
    }

}
