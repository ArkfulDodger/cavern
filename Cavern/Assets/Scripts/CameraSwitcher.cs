using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera playerCam;
    [SerializeField] CinemachineVirtualCamera exitCam;
    [SerializeField] CinemachineVirtualCamera transitionCam;
    [SerializeField] Canvas blackoutCanvas;

    float yDivide = 92.5f;

    private void OnEnable()
    {
        EventBroker.SwitchToExitCam += GoToExitCam;
        EventBroker.SwitchToPlayerCam += GoToPlayerCam;
    }

    private void OnDisable()
    {
        EventBroker.SwitchToExitCam -= GoToExitCam;
        EventBroker.SwitchToPlayerCam -= GoToPlayerCam;
    }

    private void Start()
    {
        //transitionCam.gameObject.SetActive(false);
        playerCam.Priority = 1;
        exitCam.Priority = 1;

        if (GameManager.instance.player.transform.position.y > yDivide)
        {
            playerCam.gameObject.SetActive(false);
            exitCam.gameObject.SetActive(true);
        }
        else
        {
            playerCam.gameObject.SetActive(true);
            exitCam.gameObject.SetActive(false);
        }
    }


    void GoToExitCam()
    {
        StopAllCoroutines();
        StartCoroutine(FadeToExitCam());
    }

    void GoToPlayerCam()
    {
        StopAllCoroutines();
        StartCoroutine(FadeToPlayerCam());
    }    

    IEnumerator FadeToExitCam()
    {
        //transitionCam.gameObject.SetActive(true);

        yield return new WaitForSeconds(1);

        playerCam.gameObject.SetActive(false);
        exitCam.gameObject.SetActive(true);

        //transitionCam.gameObject.SetActive(false);
    }

    IEnumerator FadeToPlayerCam()
    {
        //transitionCam.gameObject.SetActive(true);

        yield return new WaitForSeconds(1);

        playerCam.gameObject.SetActive(true);
        exitCam.gameObject.SetActive(false);

        //transitionCam.gameObject.SetActive(false);
    }

}
