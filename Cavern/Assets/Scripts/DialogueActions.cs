using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueActions : MonoBehaviour
{
    public delegate void DialogueAction();
    DialogueAction Look;
    DialogueAction Transformation;
    DialogueAction ZoomTransformation;
    DialogueAction ZoneActive;
    DialogueAction ManualZoomOut;
    DialogueAction Sky;
    DialogueAction Blackout1;
    DialogueAction Blackout2;
    DialogueAction MusicEnd;
    DialogueAction Pause;
    DialogueAction FontAutoOn;
    DialogueAction FontAutoOff;
    DialogueAction Instructions;
    public Dictionary<string, DialogueAction> actions = new Dictionary<string, DialogueAction>();
    Dialogue dialogue;
    

    private void Awake()
    {
        dialogue = GetComponent<Dialogue>();
    }

    private void OnEnable() {
        Look += LookEvent;
        Transformation += TransformationEvent;
        ZoomTransformation += ZoomTransformationEvent;
        ZoneActive += ZoneActiveEvent;
        ManualZoomOut += ManualZoomOutEvent;
        Sky += SkyEvent;
        Blackout1 += Blackout1Event;
        Blackout2 += Blackout2Event;
        MusicEnd += MusicEndEvent;
        Pause += PauseEvent;
        FontAutoOn += FontAutoOnEvent;
        FontAutoOff += FontAutoOffEvent;
        Instructions += InstructionsEvent;
    }

    // Start is called before the first frame update
    void Start()
    {
        actions.Add("LOOK", Look);
        actions.Add("TRANSFORMATION", Transformation);
        actions.Add("ZOOMTRANSFORMATION", ZoomTransformation);
        actions.Add("ZONEACTIVE", ZoneActive);
        actions.Add("MANUALZOOMOUT", ManualZoomOut);
        actions.Add("SKY", Sky);
        actions.Add("BLACKOUT1", Blackout1);
        actions.Add("BLACKOUT2", Blackout2);
        actions.Add("MUSICEND", MusicEnd);
        actions.Add("PAUSE", Pause);
        actions.Add("FONTAUTOON", FontAutoOn);
        actions.Add("FONTAUTOOFF", FontAutoOff);
        actions.Add("INSTRUCTIONS", Instructions);
    }

    void LookEvent()
    {
        dialogue.PauseDialogue();
        EventBroker.LookStartCall();
    }

    void TransformationEvent()
    {
        dialogue.PauseDialogue();
        EventBroker.InitiateTransformationCall();
    }

    void ZoomTransformationEvent()
    {
        dialogue.PauseDialogue();
        EventBroker.InitiateTransformationCall();
        EventBroker.ZoomCamToDefaultCall();
    }

    void ZoneActiveEvent()
    {
        EventBroker.ZoneActiveCall();
        dialogue.IncrementDialogueStep();
    }

    void ManualZoomOutEvent()
    {
        EventBroker.ManualZoomOutCall();
        dialogue.IncrementDialogueStep();
    }

    void SkyEvent()
    {
        dialogue.skyAudio.Play();
        dialogue.IncrementDialogueStep();
    }

    void Blackout1Event()
    {
        dialogue.PauseDialogue();
        EventBroker.BlackoutCall();
        StartCoroutine(ContinueDialogueAfterSeconds(2));
    }

    void Blackout2Event()
    {
        dialogue.PauseDialogue();
        EventBroker.BlackoutCall();
        dialogue.scrollSpeed = 4.2f;
        StartCoroutine(ContinueDialogueAfterSeconds(3));
    }

    void MusicEndEvent()
    {
        EventBroker.MusicEndCall();
        dialogue.IncrementDialogueStep();
    }

    void PauseEvent()
    {
        dialogue.PauseDialogue();
        StartCoroutine(ContinueDialogueAfterSeconds(2));
    }

    void FontAutoOnEvent()
    {
        dialogue.FontAutoSizeOn();
        dialogue.IncrementDialogueStep();
    }

    void FontAutoOffEvent()
    {
        dialogue.FontAutoSizeOff();
        dialogue.IncrementDialogueStep();
    }

    void InstructionsEvent()
    {
        EventBroker.InstructionsCall();
        dialogue.IncrementDialogueStep();
    }

    IEnumerator ContinueDialogueAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        dialogue.ResumeDialogue();
    }
}
