﻿using chChartEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Video;
using SFB;
using System.IO;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private GameManager gameMng;
    [SerializeField]
    private UIManager uiMng;

    [SerializeField]
    private InputField inputFieldPathName;
    [SerializeField]
    private InputField inputFieldFileName;

    public Image playButtonImage;
    public Sprite playButton;
    public Sprite pauseButton;

    public Text timeText_current;
    public Text timeText_length;
    public Slider timeSlider;

    public AudioSource audioSource;
    public VideoPlayer videoPlayer;
    public RawImage rawImage;

    public AudioSource seSource;
    public AudioClip seClip;
    public bool isBegin = false;
    
    private float musicLength;
    float videoLength;

    private bool isLoading = false;

    private string initDirectory;

    private void Start()
    {
        initDirectory = PlayerPrefs.GetString("chartEditor.audioDirectory", string.Empty);
        musicLength = 0f;
        videoLength = 0f;
    }

    private void Update()
    {
        if (audioSource.clip != null) {
            timeText_current.text = string.Format("{0:00}:{1:00}", (int)(audioSource.time / 60), (int)(audioSource.time % 60));
            timeSlider.value = audioSource.time;

            if ((audioSource.time + Time.deltaTime) > audioSource.clip.length && audioSource.isPlaying)
                PushPlayButton();
            else if (videoLength > 0f)
            {
                if (videoPlayer.time + Time.deltaTime > videoLength && videoPlayer.isPlaying)
                    videoPlayer.Pause();
            }
        }

        
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetString("chartEditor.audioDirectory", initDirectory);
    }

    public float GetMusicLength()
    {
        if (audioSource.clip != null)
            return musicLength;
        else
            return -1f;

    }


    public float GetTime()
    {
        if (audioSource.clip != null)
            return audioSource.time;
        else
            return -1f;
            
    }

    public void PushPlayButton()
    {
        if(audioSource.clip != null) {
            
            if (audioSource.isPlaying) {
                audioSource.Pause();
                playButtonImage.sprite = playButton;

            } else {
                if (isBegin) {
                    audioSource.UnPause();
                    
                } else {
                    audioSource.Play();
                    isBegin = true;
                }
                playButtonImage.sprite = pauseButton;
            }
        }

        if (videoLength > 0f)
        {

            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
            else if(videoPlayer.time < videoLength)
                videoPlayer.Play();
        }
    }

    public void PushStopButton()
    {
        if (audioSource.clip != null) {
            // 0秒で一時停止状態に(停止状態だと手動での再生位置変更ができないため)
            audioSource.Pause();
            audioSource.time = 0f;
            playButtonImage.sprite = playButton;
        }
        if (videoLength > 0f)
        {
            // 0秒で一時停止状態に
            videoPlayer.Pause();
            videoPlayer.frame = 0;
        }
    }

    public void PlayClap()
    {
        seSource.PlayOneShot(seClip);
    }

    public void ChangeTimeSlider()
    {
        if (audioSource.clip != null)
            audioSource.time = timeSlider.value;
        if (videoLength > 0f)
        {
            videoPlayer.frame = (long)Mathf.Min(timeSlider.value * videoPlayer.frameRate, videoPlayer.frameCount - 1f);
            if(videoPlayer.frame >= videoPlayer.frameCount - 1f)
                videoPlayer.Pause();
        }
            

    }
    

    public void SetAudioTime(float value)
    {
        if (audioSource.clip != null)
            audioSource.time = Mathf.Clamp(audioSource.time + value, 0f, audioSource.clip.length);

        if (videoLength > 0f)
            videoPlayer.frame = (long)Mathf.Clamp(videoPlayer.frame + value * videoPlayer.frameRate, 0f, (long)videoPlayer.frameCount);
    }




    public void OpenExistFile()
    {
        var extension = new[] {
            new ExtensionFilter("音声ファイル", "ogg", "wav"),
            new ExtensionFilter("画像ファイル", "png"),
        };


        /*
        var extension = new[] {
            new ExtensionFilter("音声ファイル", "ogg", "wav"),
            new ExtensionFilter("動画ファイル", "mp4"),
            new ExtensionFilter("画像ファイル", "png"),
        };
        */

        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", initDirectory, extension, false);

        if (paths.Length != 0)
        {
            initDirectory = Path.GetDirectoryName(paths[0]);

            string fileExtension = paths[0].Substring(paths[0].Length - 3, 3);

            switch (fileExtension)
            {
                case "ogg":
                case "wav":
                    StartCoroutine("StreamAudioFile", paths[0]);
                    break;
                case "mp4":
                    StartCoroutine("StreamVideoFile", paths[0]);
                    break;
                case "png":
                    StartCoroutine("StreamImageFile", paths[0]);
                    break;
            }
            
        }

    }

    IEnumerator StreamAudioFile(string filePath)
    {
        // ファイル名
        string fileName = Path.GetFileName(filePath);

        // ファイルパスを表示
        inputFieldPathName.text = filePath;
        // ファイル名を表示
        inputFieldFileName.text = fileName;

        // 拡張子によってエンコード形式を設定
        AudioType audioType;
        switch (Path.GetExtension(filePath))
        {
            case ".wav":
            case ".WAV":
                audioType = AudioType.WAV;
                break;
            case ".ogg":
            case ".OGG":
                audioType = AudioType.OGGVORBIS;
                break;
            default:
                audioType = AudioType.WAV;
                break;
        }

        //ソース指定し音楽を開く
        //音楽ファイルロード
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, audioType))
        {

            //読み込み完了まで待機
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("Error: " + request.responseCode + " : " + request.error);
            }
            else if (request.isDone)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip.name = fileName;
                musicLength = audioSource.clip.length;
                timeText_length.text = string.Format("{0:00}:{1:00}", (int)(musicLength / 60), (int)(musicLength % 60));
                timeSlider.maxValue = musicLength;
                isBegin = false;

                gameMng.InitNotesList();
                gameMng.SetSnapLine();
                uiMng.SetMaxAreaCamPosition(gameMng.GetBarLength());


                // 0秒で一時停止状態に(停止状態だと手動での再生位置変更ができないため)
                audioSource.Play();
                audioSource.Pause();
                audioSource.time = 0f;
            }
        }
    }


    IEnumerator StreamVideoFile(string fileName)
    {
        rawImage.gameObject.SetActive(false);
        videoPlayer.gameObject.SetActive(true);

        videoPlayer.source = VideoSource.Url;
        // パスを設定.
        videoPlayer.url = fileName;
        // 読込完了時のコールバックを設定
        videoPlayer.prepareCompleted += PrepareCompleted;

        // 読込開始
        videoPlayer.Prepare();

        isLoading = true;
        //読み込み完了まで待機
        while (isLoading)
            yield return null;

        
    }
    
    void PrepareCompleted(VideoPlayer vp)
    {
        videoLength = (float)videoPlayer.frameCount / videoPlayer.frameRate;

        vp.prepareCompleted -= PrepareCompleted;
        isLoading = false;
    }

    /*
    void StreamImageFile(string fileName)
    {
        byte[] bytes;
        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
            BinaryReader bin = new BinaryReader(fileStream);
            bytes = bin.ReadBytes((int)bin.BaseStream.Length);
            bin.Close();
        }
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);

        rend.material.mainTexture = texture;

        rend.gameObject.SetActive(true);
        videoPlayer.gameObject.SetActive(false);
    }
    */

    IEnumerator StreamImageFile(string filePath)
    {

        //ソース指定し音楽を開く
        //音楽ファイルロード
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file:///" + filePath))
        {

            //読み込み完了まで待機
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("Error: " + request.responseCode + " : " + request.error);
            }
            else if (request.isDone)
            {
                rawImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                rawImage.gameObject.SetActive(true);
                videoPlayer.gameObject.SetActive(false);
            }
        }
    }

}
