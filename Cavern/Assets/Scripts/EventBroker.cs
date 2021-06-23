using UnityEngine;
using System;

public enum ZombieOutEvent {cavernListen, deepListen, ascentListen, enterExitChamber, leaveExitChamber, ending}

public static class EventBroker
{
    public static event Action RunGame;

    public static void RunGameCall()
    {
        if (RunGame != null)
            RunGame();
    }

    public static event Action StartPlay;

    public static void StartPlayCall()
    {
        if (StartPlay != null)
            StartPlay();
    }

    public static event Action CellCollected;

    public static void CellCollectedCall()
    {
        if (CellCollected != null)
            CellCollected();
    }

    public static event Action CellAbsorbed;

    public static void CellAbsorbedCall()
    {
        if (CellAbsorbed != null)
            CellAbsorbed();
    }

    public static event Action TunnelEntryBegun;

    public static void TunnelEntryBegunCall()
    {
        if (TunnelEntryBegun != null)
            TunnelEntryBegun();
    }

    public static event Action TunnelExitBegun;

    public static void TunnelExitBegunCall()
    {
        if (TunnelExitBegun != null)
            TunnelExitBegun();
    }

    public static event Action TunnelEntryComplete;

    public static void TunnelEntryCompleteCall()
    {
        if (TunnelEntryComplete != null)
            TunnelEntryComplete();
    }

    public static event Action TunnelExitComplete;

    public static void TunnelExitCompleteCall()
    {
        if (TunnelExitComplete != null)
            TunnelExitComplete();
    }

    public static event Action InitiateTransformation;

    public static void InitiateTransformationCall()
    {
        if (InitiateTransformation != null)
            InitiateTransformation();
    }

    public static event Action TransformBegun;

    public static void TransformBegunCall()
    {
        if (TransformBegun != null)
            TransformBegun();
    }

    public static event Action TransformComplete;

    public static void TransformCompleteCall()
    {
        if (TransformComplete != null)
            TransformComplete();
    }

    public static event Action DeepSenseEnter;

    public static void DeepSenseEnterCall()
    {
        if (DeepSenseEnter != null)
            DeepSenseEnter();
    }

    public static event Action DeepSenseExit;

    public static void DeepSenseExitCall()
    {
        if (DeepSenseExit != null)
            DeepSenseExit();
    }

    public static event Action AllCellsCollected;

    public static void AllCellsCollectedCall()
    {
        if (AllCellsCollected != null)
            AllCellsCollected();
    }

    public static event Action EnterDialogue;

    public static void EnterDialogueCall()
    {
        if (EnterDialogue != null)
            EnterDialogue();
    }
    public static event Action ExitDialogue;

    public static void ExitDialogueCall()
    {
        if (ExitDialogue != null)
            ExitDialogue();
    }

    public static event Action PlayDialogueMusic;

    public static void PlayDialogueMusicCall()
    {
        if (PlayDialogueMusic != null)
            PlayDialogueMusic();
    }

    public static event Action StopDialogueMusic;

    public static void StopDialogueMusicCall()
    {
        if (StopDialogueMusic != null)
            StopDialogueMusic();
    }

    public static event Action LookStart;

    public static void LookStartCall()
    {
        if (LookStart != null)
            LookStart();
    }

    public static event Action LookEnd;

    public static void LookEndCall()
    {
        if (LookEnd != null)
            LookEnd();
    }

    public static event Action FadeOutMusic;

    public static void FadeOutMusicCall()
    {
        if (FadeOutMusic != null)
            FadeOutMusic();
    }

    public static event Action FadeInMainMusic;

    public static void FadeInMainMusicCall()
    {
        if (FadeInMainMusic != null)
            FadeInMainMusic();
    }

    public static event Action ExitPlayerBegin;

    public static void ExitPlayerBeginCall()
    {
        if (ExitPlayerBegin != null)
            ExitPlayerBegin();
    }

    public static event Action ExitPlayerFinished;

    public static void ExitPlayerFinishedCall()
    {
        if (ExitPlayerFinished != null)
            ExitPlayerFinished();
    }

    public static event Action SwitchToExitCam;

    public static void SwitchToExitCamCall()
    {
        if (SwitchToExitCam != null)
            SwitchToExitCam();
    }

    public static event Action SwitchToPlayerCam;

    public static void SwitchToPlayerCamCall()
    {
        if (SwitchToPlayerCam != null)
            SwitchToPlayerCam();
    }

    public static event Action EnteredExitChamber;

    public static void EnteredExitChamberCall()
    {
        if (EnteredExitChamber != null)
            EnteredExitChamber();
    }

    public static event Action LeftExitChamber;

    public static void LeftExitChamberCall()
    {
        if (LeftExitChamber != null)
            LeftExitChamber();
    }

    public static event Action StartFinalDialogue;

    public static void StartFinalDialogueCall()
    {
        if (StartFinalDialogue != null)
        {
            StartFinalDialogue();
        }
    }

    public static event Action Blackout;

    public static void BlackoutCall()
    {
        if (Blackout != null)
            Blackout();
    }

    public static event Action MusicEnd;

    public static void MusicEndCall()
    {
        if (MusicEnd != null)
            MusicEnd();
    }

    public static event Action TitleCard;

    public static void TitleCardCall()
    {
        if (TitleCard != null)
            TitleCard();
    }

    public static event Action Notice;

    public static void NoticeCall()
    {
        if (Notice != null)
            Notice();
    }

    public static event Action ShortNotice;

    public static void ShortNoticeCall()
    {
        if (ShortNotice != null)
            ShortNotice();
    }

    public static event Action LongNotice;

    public static void LongNoticeCall()
    {
        if (LongNotice != null)
            LongNotice();
    }

    public static event Action Upgrade;

    public static void UpgradeCall()
    {
        if (Upgrade != null)
            Upgrade();
    }

    public static event Action Instructions;

    public static void InstructionsCall()
    {
        if (Instructions != null)
            Instructions();
    }

    public static event Action ZoomCamToDefault;

    public static void ZoomCamToDefaultCall()
    {
        if (ZoomCamToDefault != null)
            ZoomCamToDefault();
    }

    public static event Action ManualZoomOut;

    public static void ManualZoomOutCall()
    {
        if (ManualZoomOut != null)
            ManualZoomOut();
    }

    public static event Action ZoneActive;

    public static void ZoneActiveCall()
    {
        if (ZoneActive != null)
            ZoneActive();
    }
}
