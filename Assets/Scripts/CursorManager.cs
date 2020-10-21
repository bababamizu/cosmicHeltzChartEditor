using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CursorType
{
    Default = 0,
    Choice,
    LR_Arrow,
    UD_Arrow
}

public class CursorManager : MonoBehaviour {

    [SerializeField]
    private Texture2D[] sprite = new Texture2D[4];
    [Compact]
    public Vector2[] hotSpot = new Vector2[4];

    public void SetCursor(CursorType type)
    {
        Cursor.SetCursor(sprite[(int)type], hotSpot[(int)type], CursorMode.ForceSoftware);
    }

    // Texture2Dをリサイズする
    static Texture2D ResizeTexture(Texture2D srcTexture, int newWidth, int newHeight)
    {
        var resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Graphics.ConvertTexture(srcTexture, resizedTexture);
        return resizedTexture;
    }
}
