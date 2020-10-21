using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hypertext;

public class HyperText : MonoBehaviour
{
    RegexHypertext textUI;
    const string RegexUrl = @"https?://(?:[!-~]+\.)+[!-~]+";

    void Start()
    {
        textUI = GetComponent<RegexHypertext>();
        ShowMessage();
    }

    public void ShowMessage()
    {
        textUI.OnClick(RegexUrl, Color.blue, url => OpenBrowser(url));
    }

    public void OpenBrowser(string url)
    {
        Application.OpenURL(url);
    }
}
