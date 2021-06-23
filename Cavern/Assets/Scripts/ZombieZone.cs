using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieZone : MonoBehaviour
{
    [SerializeField] bool active = true;
    [SerializeField] bool alwaysActive;
    [SerializeField] bool vertical;
    [SerializeField] bool groundingRequired;
    [SerializeField] float pause;
    [SerializeField] ZombieOutEvent outEvent;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log(gameObject.name + "triggered");
        if (active && other.tag == "Player" && other is CircleCollider2D)
        {
            // Debug.Log(this.name + "zombie called");
            if (!alwaysActive)
                active = false;
            GameManager.instance.SetZombieOutEventTo(outEvent);
            GameManager.instance.playerController.StartZombieX(transform.position, vertical, groundingRequired, pause);
        }
    }
}
