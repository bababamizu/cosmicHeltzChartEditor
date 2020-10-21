using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour {

    // ゲームオブジェクトのDictionary
    private Dictionary<int, List<GameObject>> pooledGameObjects = new Dictionary<int, List<GameObject>>();

    // ゲームオブジェクトをpooledGameObjectsから取得する。必要であれば新たに生成する
    public GameObject GetGameObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // プレハブのインスタンスIDをkeyとする
        int key = prefab.GetInstanceID();

        // Dictionaryにkeyが存在しなければ作成する
        if (pooledGameObjects.ContainsKey(key) == false)
        {

            pooledGameObjects.Add(key, new List<GameObject>());
        }

        List<GameObject> gameObjects = pooledGameObjects[key];

        GameObject go = null;

        for (int i = 0; i < gameObjects.Count; i++)
        {

            go = gameObjects[i];

            // 現在非アクティブ（未使用）であれば
            if (go.activeSelf == false)
            {

                // 位置を設定する
                go.transform.localPosition = position;

                // 角度を設定する
                go.transform.localRotation = rotation;

                // これから使用するのでアクティブにする
                go.SetActive(true);

                return go;
            }
        }

        // 使用できるものがないので新たに生成する
        go = Instantiate(prefab, position, rotation) as GameObject;

        // ObjectPoolがアタッチされているGameObjectの子要素にする
        go.transform.SetParent(gameObject.transform, false);

        // リストに追加
        gameObjects.Add(go);

        return go;
    }

    // ゲームオブジェクトを非アクティブにする。こうすることで再利用可能状態にする
    public void ReleaseGameObject(GameObject go)
    {
        // 非アクティブにする
        go.SetActive(false);
    }



    public void ReleaseAllGameObjects(GameObject prefab)
    {
        // プレハブのインスタンスIDをkeyとする
        int key = prefab.GetInstanceID();

        // Dictionaryにkeyが存在しなければ終了
        if (pooledGameObjects.ContainsKey(key) == false)
            return;


        List<GameObject> gameObjects = pooledGameObjects[key];

        GameObject go = null;

        for (int i = 0; i < gameObjects.Count; i++)
        {

            go = gameObjects[i];

            ReleaseGameObject(go);
        }
    }


    /// <summary>
    /// Y座標が最も近いオブジェクトを返す
    /// </summary>
    public GameObject SartchNearGameObjectY(GameObject prefab, float positionY)
    {
        // プレハブのインスタンスIDをkeyとする
        int key = prefab.GetInstanceID();

        // Dictionaryにkeyが存在しなければ終了
        if (pooledGameObjects.ContainsKey(key) == false)
            return null;


        List<GameObject> gameObjects = pooledGameObjects[key];

        GameObject go = null;

        GameObject mostNearGO = null;
        float minDistance = -1f;

        for (int i = 0; i < gameObjects.Count; i++)
        {

            go = gameObjects[i];

            // アクティブかつクリック位置より下の線を対象とする
            if (go.activeSelf == true && go.transform.position.y <= positionY)
            {

                if (mostNearGO != null)
                {
                    if (Mathf.Abs(positionY - go.transform.position.y) < minDistance)
                    {
                        mostNearGO = go;
                        minDistance = Mathf.Abs(positionY - go.transform.position.y);
                    }
                }

                else
                {
                    mostNearGO = go;
                    minDistance = Mathf.Abs(positionY - go.transform.position.y);
                }

            }

        }

        return mostNearGO;
    }


    /// <summary>
    /// X座標が最も近いオブジェクトを返す
    /// </summary>
    public GameObject SartchNearGameObjectX(GameObject prefab, float positionX)
    {
        // プレハブのインスタンスIDをkeyとする
        int key = prefab.GetInstanceID();

        // Dictionaryにkeyが存在しなければ終了
        if (pooledGameObjects.ContainsKey(key) == false)
            return null;


        List<GameObject> gameObjects = pooledGameObjects[key];

        GameObject go = null;

        GameObject mostNearGO = null;
        float minDistance = -1f;

        for (int i = 0; i < gameObjects.Count; i++)
        {
            go = gameObjects[i];

            // アクティブな線を対象とする
            if (go.activeSelf == true)
            {
                if (mostNearGO != null)
                {
                    if (Mathf.Abs(positionX - go.transform.position.x) < minDistance)
                    {
                        mostNearGO = go;
                        minDistance = Mathf.Abs(positionX - go.transform.position.x);
                    }
                }

                else
                {
                    mostNearGO = go;
                    minDistance = Mathf.Abs(positionX - go.transform.position.x);
                }
            }

        }

        return mostNearGO;
    }
}
