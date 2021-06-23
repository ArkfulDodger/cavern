using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListenZone : MonoBehaviour
{
    [SerializeField] bool active;
    bool canListen;
    [SerializeField] Region region;
    Animator animator;
    PlayerController player;
    BoxCollider2D zoneCollider;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        zoneCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        EventBroker.ExitDialogue += ExitDialogue;
    }

    private void OnDisable()
    {
        EventBroker.ExitDialogue -= ExitDialogue;
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GameManager.instance.player.GetComponent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (active && other.CompareTag("Player") && other is CircleCollider2D)
        {
            canListen = true;
            GameManager.instance.UpdateListenRegion(region);
            animator.SetBool("visible", true);
        }
    }

    private void OnTriggerStay2D(Collider2D other) {
        if (active && canListen && other is CircleCollider2D && player.CanListen() && Input.GetKey(KeyCode.Return))
        {
            canListen = false;
            animator.SetTrigger("out");
            animator.SetBool("visible", false);
            EventBroker.EnterDialogueCall();
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player") && other is CircleCollider2D)
        {
            canListen = false;
            animator.SetBool("visible", false);
            StopAllCoroutines();
        }
    }

    void ExitDialogue()
    {
        if (GameManager.instance.listenRegion == region)
        {
            if (Input.GetKey(KeyCode.Return))
                StartCoroutine(ReactivateListen());
            else
                canListen = true;
            
            active = true;
            animator.SetBool("visible", true);
        }
    }

    IEnumerator ReactivateListen()
    {
        while(!canListen)
        {
            if (Input.GetKeyUp(KeyCode.Return))
                canListen = true;
            
            yield return null;
        }
        yield return null;
    }
}
