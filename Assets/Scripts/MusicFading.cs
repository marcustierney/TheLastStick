using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicFading : MonoBehaviour
{
    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            return;
        }

        audioSource.ignoreListenerPause = true;

        audioSource.volume = 0f;
        StartCoroutine(Fade(true, 10f, .1f));
        StartCoroutine(Fade(false, 2f, 0f));
    }

    public IEnumerator Fade(bool fadeIn, float duration, float targetVolume)
    {
        if (!fadeIn)
        {
            double lengthOfSource = (double)audioSource.clip.samples / audioSource.clip.frequency;
            yield return new WaitForSeconds((float)lengthOfSource - duration);
        }
        float time = 0;
        float startVolume = audioSource.volume;
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }
        yield break;
    }
}
