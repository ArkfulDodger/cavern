using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitClimbTrigger : MonoBehaviour
{
    bool active = true;
    private void OnTriggerStay2D(Collider2D other)
    {
        // Debug.Log(gameObject.name + "triggered");
        if (active && other.tag == "Player" && other is CircleCollider2D && GameManager.instance.playerController.GetState() == State.climb)
        {
            active = false;
            GameManager.instance.SetZombieOutEventTo(ZombieOutEvent.ending);
            GameManager.instance.playerController.StartZombieX(transform.position, true, false, 0);
        }
    }
}
