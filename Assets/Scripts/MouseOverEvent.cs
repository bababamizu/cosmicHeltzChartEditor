using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MouseOverEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject mouseOverObj;

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOverObj.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOverObj.gameObject.SetActive(false);
    }
}
