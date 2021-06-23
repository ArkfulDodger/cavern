using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicFadeZone : MonoBehaviour
{
    [SerializeField] bool alwaysActive;
    [SerializeField] bool active;
    [SerializeField] bool fadeIn;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (active && other.tag == "Player" && other is CircleCollider2D)
        {
            if (!alwaysActive)
                active = false;

            if (fadeIn)
                EventBroker.FadeInMainMusicCall();
            else
                EventBroker.FadeOutMusicCall();
        }
    }
}
