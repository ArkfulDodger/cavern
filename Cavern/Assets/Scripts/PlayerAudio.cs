using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    PlayerController playerController;
    public AudioSource vocalsSource;
    public AudioSource sfxSource;
    public AudioSource cellSound;
    public List<AudioClip> crestVocals = new List<AudioClip>();
    public List<AudioClip> jumpVocals = new List<AudioClip>();
    public List<AudioClip> fallVocals = new List<AudioClip>();

    public AudioClip climb;
    public AudioClip hitWall;
    public AudioClip land;
    public AudioClip overworldMove;
    public AudioClip scoot;
    public AudioClip walk;
    public AudioClip tunnelMove;
    public AudioClip tunnelCrawl;
    public AudioClip tunnelWorm;
    public AudioClip glide;
    public AudioClip tunnelEnterExit;
    public AudioClip transformWhoosh;
    public AudioClip transformTone;
    public float finalFadeTime = 4f;


    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        EventBroker.CellCollected += PlayCellTone;
        EventBroker.ExitPlayerBegin += FadeOutClimb;
    }

    private void OnDisable()
    {
        EventBroker.CellCollected -= PlayCellTone;
        EventBroker.ExitPlayerBegin -= FadeOutClimb;
    }

    private void Start()
    {
        UpdateFormSounds();
    }

    public void UpdateFormSounds()
    {
        overworldMove = playerController.form > Form.claws ? walk : scoot;
        tunnelMove = playerController.form > Form.worm ? tunnelCrawl : tunnelWorm;
    }

    public void PlayOneShotVocalFromList(List<AudioClip> vocalList)
    {
        int i = Random.Range(0, vocalList.Count);
        vocalsSource.PlayOneShot(vocalList[i]);
    }

    public void PlayOneShotSound(AudioClip sound)
    {
        sfxSource.Stop();
        sfxSource.clip = sound;
        sfxSource.PlayOneShot(sound);
    }

    void PlayCellTone()
    {
        cellSound.Stop();
        cellSound.Play();
    }

    public void StartSoundLoop(AudioClip sound)
    {
        sfxSource.Stop();
        sfxSource.clip = sound;
        sfxSource.loop = true;
        sfxSource.Play();
    }

    public void StopSoundLoopAfterRun(AudioClip sound)
    {
        if (sfxSource.clip == sound)
        {
            sfxSource.loop = false;
        }
        // else
        // {
        //     sfxSource.Stop();
        // }
    }

    public void StopSoundLoopImmediately(AudioClip sound)
    {
        if (sfxSource.clip == sound)
        {
            sfxSource.Stop();
        }
    }

    void FadeOutClimb()
    {
        StartCoroutine(FadeClimb());
    }

    IEnumerator FadeClimb()
    {
        float timer = 0;
        float startVol = sfxSource.volume;
        
        while (timer < finalFadeTime)
        {
            sfxSource.volume = Mathf.Lerp(startVol, 0, timer/finalFadeTime);

            timer += Time.deltaTime;
            yield return null;
        }
        sfxSource.Stop();
        EventBroker.StartFinalDialogueCall();
    }
}
