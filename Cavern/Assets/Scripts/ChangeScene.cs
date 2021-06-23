using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public CanvasGroup blackoutCanvas;
    float fadeOutTime = 2f;
    float fadeInTime = 7f;
    List<string> sceneLoadingQueue = new List<string>();
    List<string> sceneUnloadingQueue = new List<string>();
    AudioSource audioSource;


    public static ChangeScene instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else if (instance != this)
            Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (sceneLoadingQueue.Contains(scene.name))
            sceneLoadingQueue.Remove(scene.name);
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (sceneUnloadingQueue.Contains(scene.name))
            sceneUnloadingQueue.Remove(scene.name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    public void ChangeToScene(string sceneName)
    {
        Debug.Log("Called Scene Change to " + sceneName);
        StartCoroutine(FadeToScene(sceneName));
    }

    IEnumerator FadeToScene(string newSceneName)
    {
        // get the starting alpha of the blackout and initialize the timer
        float startingAlpha = blackoutCanvas.alpha;
        float timer = 0;

        // fade to black over fade time
        while (timer < fadeOutTime)
        {
            blackoutCanvas.alpha = Mathf.Lerp(startingAlpha, 1, timer/fadeOutTime);
            audioSource.volume = Mathf.Lerp(1, 0.7f, timer/fadeOutTime);

            timer += Time.deltaTime;
            yield return null;
        }
        blackoutCanvas.alpha = 1;
        audioSource.volume = 0.7f;

        // in blackout, load new scene
        sceneLoadingQueue.Add(newSceneName);
        SceneManager.LoadScene(newSceneName, LoadSceneMode.Single);

        // wait until new scene is loaded
        while(sceneLoadingQueue.Contains(newSceneName))
        {
            yield return null;
        }

        EventBroker.RunGameCall();

        // fade blackout panel out
        startingAlpha = blackoutCanvas.alpha;
        timer = 0;
        while (timer < fadeInTime)
        {
            blackoutCanvas.alpha = Mathf.Lerp(startingAlpha, 0, timer/fadeInTime);
            audioSource.volume = Mathf.Lerp(0.7f, 0, timer/fadeInTime);

            timer += Time.deltaTime;
            yield return null;
        }
        blackoutCanvas.alpha = 0;
        audioSource.Stop();

        yield return null;
    }
}
