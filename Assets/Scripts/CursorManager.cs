using UnityEngine;

public enum CursorType
{
    None = -1,
    Default = 0,
    Choice,
    LeftRight_Arrow,
    UpDown_Arrow,
    DownRight_Arrow
}

public class CursorManager : MonoBehaviour {

    [SerializeField]
    private Texture2D[] sprite = new Texture2D[5];
    [Compact]
    public Vector2[] hotSpot = new Vector2[5];


    private void Awake()
    {
        CursorChange.Instance.SetCursorTexture(sprite, hotSpot);
    }

    // Texture2Dをリサイズする
    static Texture2D ResizeTexture(Texture2D srcTexture, int newWidth, int newHeight)
    {
        var resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Graphics.ConvertTexture(srcTexture, resizedTexture);
        return resizedTexture;
    }
}
