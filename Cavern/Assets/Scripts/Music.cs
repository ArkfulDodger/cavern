using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Music : MonoBehaviour
{
    public AudioSource mainTheme;
    public AudioSource openingTheme;
    public AudioSource transformationMusic;
    public AudioSource communeOne;
    public AudioSource communeTwo;
    public AudioSource communeEnd;
    AudioSource currentCommuneSource;
    [SerializeField] float communeLoopTime = 15;
    bool opening;
    bool endAllowed;

    List<AudioSource> resumePostDialogue = new List<AudioSource>();
    List<AudioSource> resumePostTransform = new List<AudioSource>();

    Dictionary<AudioSource, float> audioDefaults = new Dictionary<AudioSource, float>();

    private void OnEnable()
    {
        EventBroker.RunGame += RunGame;
        EventBroker.TransformBegun += BeginTransformation;
        EventBroker.TransformComplete += EndTransformation;
        EventBroker.PlayDialogueMusic += PlayDialogueMusic;
        EventBroker.StopDialogueMusic += StopDialogueMusic;
        EventBroker.LookEnd += PlayDialogueMusic;
        EventBroker.FadeInMainMusic += FadeInMainMusic;
        EventBroker.FadeOutMusic += FadeOutAll;
        EventBroker.MusicEnd += QueueUpEnd;
    }

    private void OnDisable()
    {
        EventBroker.RunGame -= RunGame;
        EventBroker.TransformBegun -= BeginTransformation;
        EventBroker.TransformComplete -= EndTransformation;
        EventBroker.PlayDialogueMusic -= PlayDialogueMusic;
        EventBroker.StopDialogueMusic -= StopDialogueMusic;
        EventBroker.LookEnd -= PlayDialogueMusic;
        EventBroker.FadeInMainMusic -= FadeInMainMusic;
        EventBroker.FadeOutMusic -= FadeOutAll;
        EventBroker.MusicEnd -= QueueUpEnd;
    }

    private void Start()
    {
        audioDefaults.Add(mainTheme, mainTheme.volume);
        audioDefaults.Add(openingTheme, openingTheme.volume);
        audioDefaults.Add(transformationMusic, transformationMusic.volume);
        audioDefaults.Add(communeOne, communeOne.volume);
        audioDefaults.Add(communeTwo, communeTwo.volume);
        audioDefaults.Add(communeEnd, communeEnd.volume);

        currentCommuneSource = communeOne;

        if (GameManager.instance.isNewGame)
        {
            opening = true;
            RunGame();
        }
        else
        {
            mainTheme.Play();
        }
    }

    private void RunGame()
    {
        openingTheme.Play();
    }

    private void Update()
    {
        if (opening && openingTheme.time > 23.4f)
        {
            opening = false;
            mainTheme.Play();
            EventBroker.StartPlayCall();
        }

    }

    void BeginTransformation()
    {
        PlayAtDefault(transformationMusic);
        FadeOutAllButClip(transformationMusic, 0.5f, true, false);
    }

    void EndTransformation()
    {
        if (transformationMusic.isPlaying)
            transformationMusic.Stop();
        StopAllCoroutines();
        ConfirmCommuneClip();
        StartCoroutine(FadeIn(currentCommuneSource, 0.5f));
        StartCoroutine(CommuneLoop());
    }

    void PlayDialogueMusic()
    {
        currentCommuneSource = communeOne;
        ResetCommuneAudio();
        FadeOutAllButClip(currentCommuneSource, 0.5f, true, true);
        StartCoroutine(CommuneLoop());
    }

    void StopDialogueMusic()
    {
        if (currentCommuneSource.isPlaying)
        {
            if (currentCommuneSource == communeEnd)
                return;
            else
                FadeOutAllButClip(mainTheme, 0.5f, false, true);
        }
    }
    void PlayAtDefault(AudioSource audio)
    {
        audio.volume = audioDefaults[audio];
        audio.Play();
    }

    void FadeInMainMusic()
    {
        FadeOutAllButClip(mainTheme, 1, true, true);
    }

    void FadeOutAll()
    {
        StopAllCoroutines();

        foreach (KeyValuePair<AudioSource, float> pair in audioDefaults)
        {
            if (pair.Key.isPlaying)
            {
                StartCoroutine(FadeOut(pair.Key, 1, true));
            }
        }
    }

    void FadeOutAllButClip(AudioSource audio, float fadeTime, bool pauseOnly, bool fadeInNewClip)
    {
        StopAllCoroutines();

        foreach (KeyValuePair<AudioSource, float> pair in audioDefaults)
        {
            if (pair.Key != audio && pair.Key.isPlaying)
            {
                StartCoroutine(FadeOut(pair.Key, fadeTime, pauseOnly));
            }
        }

        if (fadeInNewClip)
            StartCoroutine(FadeIn(audio, fadeTime));
    }

    IEnumerator FadeOut(AudioSource audio, float fadeTime, bool pauseOnly)
    {
        float startingVolume = audio.volume;
        float time = 0;
        while (time < fadeTime)
        {
            audio.volume = Mathf.Lerp(startingVolume, 0, time/fadeTime);
            time += Time.deltaTime;
            yield return null;
        }
        audio.volume = 0;

        if (pauseOnly)
            audio.Pause();
        else
            audio.Stop();
    }

    IEnumerator FadeIn(AudioSource audio, float fadeTime)
    {
        float startingVolume = audio.isPlaying ? audio.volume : 0;
        float time = 0;
        audio.Play();
        while (time < fadeTime)
        {
            audio.volume = Mathf.Lerp(startingVolume, audioDefaults[audio], time/fadeTime);
            time += Time.deltaTime;
            yield return null;
        }
        audio.volume = audioDefaults[audio];
    }

    void ResetCommuneAudio()
    {
        communeOne.Stop();
        communeTwo.Stop();
        communeEnd.Stop();
        communeOne.volume = audioDefaults[communeOne];
        communeTwo.volume = audioDefaults[communeTwo];
        communeEnd.volume = audioDefaults[communeEnd];
        currentCommuneSource.Stop();
    }

    void ConfirmCommuneClip()
    {
        if (currentCommuneSource.time > communeLoopTime)
        {
            ResetCommuneAudio();

            if (endAllowed)
            {
                currentCommuneSource = communeEnd;
            }
            else
            {
                if (currentCommuneSource == communeOne)
                    currentCommuneSource = communeTwo;
                else if (currentCommuneSource == communeTwo)
                    currentCommuneSource = communeOne;
            }
        }
        if (communeOne != currentCommuneSource)
            communeOne.volume = audioDefaults[communeOne];
        if (communeTwo != currentCommuneSource)
            communeTwo.volume = audioDefaults[communeTwo];
        if (communeEnd != currentCommuneSource)
            communeEnd.volume = audioDefaults[communeEnd];
    }

    IEnumerator CommuneLoop()
    {
        while(true)
        {
            if (currentCommuneSource.time > communeLoopTime)
            {
                if (endAllowed)
                {
                    currentCommuneSource = communeEnd;
                    EventBroker.TitleCardCall();
                }
                else
                {
                    if (currentCommuneSource == communeOne)
                        currentCommuneSource = communeTwo;
                    else if (currentCommuneSource == communeTwo)
                        currentCommuneSource = communeOne;
                }
                
                currentCommuneSource.Stop();
                currentCommuneSource.Play();
            }

            if (endAllowed && currentCommuneSource == communeEnd)
                yield break;
            else
                yield return null;
        }
    }

    void QueueUpEnd()
    {
        endAllowed = true;
    }
}