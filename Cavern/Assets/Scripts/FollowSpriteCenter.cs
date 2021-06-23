using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSpriteCenter : MonoBehaviour
{
    [SerializeField] SpriteRenderer followSprite;

    // Update is called once per frame
    void LateUpdate()
    {
        if (transform.position != followSprite.bounds.center)
            transform.position = followSprite.bounds.center;
    }
}
