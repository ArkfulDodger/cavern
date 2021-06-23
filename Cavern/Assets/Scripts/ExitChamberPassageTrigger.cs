using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitChamberPassageTrigger : MonoBehaviour
{
    bool entering;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player" && other is CircleCollider2D)
        {
            entering = other.transform.position.y < transform.position.y ? true : false;
            
            if (entering)
            {
                EventBroker.SwitchToExitCamCall();
                EventBroker.FadeOutMusicCall();
            }
            else
            {
                EventBroker.SwitchToPlayerCamCall();
                EventBroker.FadeInMainMusicCall();
            }
        }
    }
}
