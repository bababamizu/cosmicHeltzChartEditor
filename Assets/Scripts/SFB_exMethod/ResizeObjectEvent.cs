using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeObjectEvent : MonoBehaviour
{
    public void OnPointerEnter()
    {
        CursorChange.Instance.SetCursor(CursorType.DownRight_Arrow);
    }

    public void OnPointerExit()
    {
        CursorChange.Instance.SetCursor(CursorType.Default);
    }
}
