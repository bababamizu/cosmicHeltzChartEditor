using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace chChartEditor
{
    public static class General
    {
        public static Vector2 objectAreaPos = new Vector2(5000f, -3000f);   // オブジェクトエリアの位置
        public static float windowAreaWidth = 800f;          // ゲーム中に描画される範囲    
        public static float keybordNotesAreaWidth = 400f;   // キーボードノーツの範囲

        public static float[] initData = new float[]
        {
        0f,     // 00 : Hit
        0f,     // 01 : Hold
        0.3f,   // 02 : Click
        0.3f,   // 03 : Catch
        0f,     // 04 : Flick_L
        0f,     // 05 : Flick_R
        0.3f,   // 06 : Swing
        0f,     // 07 : ExHit
        0f,     // 08 : ExHold
        0f,     // 09 : 
        0f,     // 10 : Bar
        128f,   // 11 : Bpm
        0f,     // 12 : Stop
        0f,     // 13 : AreaMove
        };
    }

    public enum difficulty  // 難易度
    {
        LIGHT,
        BASIC,
        HARD,
        PLANET,
        SPECIAL
    }

    

    public struct Snap
    {
        public bool toggle;
        public int split;
    }

    public struct NOTES_LISTPOS
    {
        public int type;
        public int num;

        public NOTES_LISTPOS(int _type, int _num)
        { 
            type = _type; 
            num = _num;
        }
    }

    public static class ExMethod
    {
        // タッチ座標をキャンバス座標に変換
        public static Vector2 GetPosition_touchToCanvas(RectTransform canvasRect, Vector2 touchPosition)
        {
            Vector2 canvasSize = canvasRect.sizeDelta;                  // キャンバスのサイズ
            Vector2 gameRes = new Vector2(Screen.width, Screen.height); // ゲーム画面の解像度(環境により変化)
            Vector2 canvasPosition;
            canvasPosition.x = (touchPosition.x - gameRes.x / 2.0f) / (gameRes.x / 2.0f) * (canvasSize.x / 2.0f);
            canvasPosition.y = (touchPosition.y - gameRes.y / 2.0f) / (gameRes.y / 2.0f) * (canvasSize.y / 2.0f);
            return canvasPosition;
        }

       
    }
    
}
