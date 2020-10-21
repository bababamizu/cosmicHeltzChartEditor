using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GimmickObject : MonoBehaviour {

    public float bar;
    public float value;

    public void SetUp(float bar, float value)
    {
        this.bar = bar;
        UpdateValue(value);
    }

    public void UpdateValue(float value)
    {
        this.value = value;
        UpdateUIData();
    }

    virtual protected void UpdateUIData()
    {
        transform.GetChild(0).GetComponent<TextMesh>().text = string.Format("{0}", value);
    }

}
