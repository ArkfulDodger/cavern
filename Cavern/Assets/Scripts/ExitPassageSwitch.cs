using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitPassageSwitch : MonoBehaviour
{
    [SerializeField] GameObject IntoPassageTrigger;
    [SerializeField] GameObject OutOfPassageTrigger;

    private void OnEnable()
    {
        EventBroker.EnteredExitChamber += EnteredChamber;
        EventBroker.LeftExitChamber += ExitedChamber;
    }

    private void OnDisable()
    {
        EventBroker.EnteredExitChamber -= EnteredChamber;
        EventBroker.LeftExitChamber -= ExitedChamber;
    }

    private void Start()
    {
        if (GameManager.instance.player.transform.position.y > transform.position.y)
        {
            EnteredChamber();
        }
        else
        {
            ExitedChamber();
        }
    }

    void ExitedChamber()
    {
        IntoPassageTrigger.SetActive(true);
        OutOfPassageTrigger.SetActive(false);
    }

    void EnteredChamber()
    {
        IntoPassageTrigger.SetActive(false);
        OutOfPassageTrigger.SetActive(true);
    }
}
