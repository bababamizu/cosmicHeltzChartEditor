using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorChange
{
    public readonly static CursorChange Instance = new CursorChange();

    private Texture2D[] sprite = new Texture2D[5];
    private Vector2[] hotSpot = new Vector2[5];

    CursorType currentType = CursorType.None;

    public void SetCursorTexture(Texture2D[] sprites, Vector2[] _hotSpots)
    {
        sprite = sprites;
        hotSpot = _hotSpots;
    }

    public void SetCursor(CursorType type)
    {
        if(type != currentType)
        {
            Cursor.SetCursor(sprite[(int)type], hotSpot[(int)type], CursorMode.ForceSoftware);
            currentType = type;
        }
    }

    // Texture2Dをリサイズする
    private static Texture2D ResizeTexture(Texture2D srcTexture, int newWidth, int newHeight)
    {
        var resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Graphics.ConvertTexture(srcTexture, resizedTexture);
        return resizedTexture;
    }
}
