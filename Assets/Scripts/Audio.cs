using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour {

    // Use this for initialization
    AudioSource audioSource = null;

    void Start () {
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
	}
	
	// Update is called once per frame
	void Update () {
        if(audioSource != null)
            Debug.Log(audioSource.isPlaying + " " + audioSource.time + " / " + audioSource.clip.length);
	}
}
