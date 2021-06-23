using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CellCounter : MonoBehaviour
{
    CanvasGroup cellCounterCanvasGroup;
    [SerializeField] TMP_Text cellCountText;

    private void Awake() {
        cellCounterCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start() {
        cellCounterCanvasGroup.alpha = 0;
        StartCoroutine(UpdateCellCounter());
    }

    private void OnEnable() {
        EventBroker.CellCollected += CellCollected;
        EventBroker.Blackout += Blackout;
    }

    private void OnDisable() {
        EventBroker.CellCollected -= CellCollected;
        EventBroker.Blackout -= Blackout;
    }

    private void CellCollected()
    {
        StartCoroutine(UpdateCellCounter());
    }

    IEnumerator UpdateCellCounter()
    {
        yield return new WaitForEndOfFrame();

        cellCountText.text = GameManager.instance.cellCount.ToString();
        
        if (cellCounterCanvasGroup.alpha < 1 && GameManager.instance.cellCount > 0)
            StartCoroutine(FadeInHUD());
        
        yield return null;
    }

    IEnumerator FadeInHUD()
    {
        float timer = 0;
        float duration = 2;

        while (timer < duration)
        {
            cellCounterCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        cellCounterCanvasGroup.alpha = 1;

        yield return null;
    }

    void Blackout()
    {
        StartCoroutine(FadeOutHUD());
    }

    IEnumerator FadeOutHUD()
    {
        float timer = 0;
        float duration = 2;

        while (timer < duration)
        {
            cellCounterCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        cellCounterCanvasGroup.alpha = 0;

        yield return null;
    }
}
