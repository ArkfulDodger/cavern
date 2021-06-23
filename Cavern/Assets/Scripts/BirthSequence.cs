using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirthSequence : MonoBehaviour
{
    Animator animator;
    Music music;
    bool birthTriggered;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        music = GameManager.instance.GetComponent<Music>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!birthTriggered && music.openingTheme.time > 8f)
        {
            birthTriggered = true;
            animator.SetTrigger("birth");
        }
    }
}
