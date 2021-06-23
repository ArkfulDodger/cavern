using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Notifications : MonoBehaviour
{
    [SerializeField] TMP_Text noticeText;
    [SerializeField] Animator animator;
    public static string text;
    [SerializeField] float shortTime;
    [SerializeField] float midTime;
    [SerializeField] float longTime;

    private void OnEnable()
    {
        EventBroker.ShortNotice += FireShortNotice;
        EventBroker.Notice += FireNotice;
        EventBroker.LongNotice += FireLongNotice;
        EventBroker.StartPlay += StartPlay;
        EventBroker.AllCellsCollected += AllCellsCollected;
    }

    private void OnDisable()
    {
        EventBroker.ShortNotice -= FireShortNotice;
        EventBroker.Notice -= FireNotice;
        EventBroker.LongNotice -= FireLongNotice;
        EventBroker.StartPlay -= StartPlay;
        EventBroker.AllCellsCollected -= AllCellsCollected;
    }

    void FireShortNotice()
    {
        DisplayNotification();
        StartCoroutine(RemoveNotificationAfterSeconds(shortTime));
    }

    void FireNotice()
    {
        DisplayNotification();
        StartCoroutine(RemoveNotificationAfterSeconds(midTime));
    }

    void FireLongNotice()
    {
        DisplayNotification();
        StartCoroutine(RemoveNotificationAfterSeconds(longTime));
    }

    void ClearNotifications()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Hidden"))
            return;
        else
        {
            StopAllCoroutines();
            animator.SetTrigger("out");
        }
    }

    void DisplayNotification()
    {
        ClearNotifications();
        noticeText.text = text;
        animator.SetBool("visible", true);
    }

    IEnumerator RemoveNotificationAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        animator.SetBool("visible", false);
    }


    void StartPlay()
    {
        text = "Use the ARROW KEYS\nto move";
        FireNotice();
    }

    void AllCellsCollected()
    {
        text = "ALL CELLS COLLECTED!";
        FireNotice();
    }
}
