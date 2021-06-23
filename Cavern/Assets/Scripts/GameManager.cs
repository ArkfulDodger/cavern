using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.Audio;

public enum Region
{
    centralPassage, chambers, maze, centralCavern, narrowCliffs, northPassage, openStair, upperCavern, crevice, westPassage, eastPassage, deep, finalAscent
}

public class GameManager : MonoBehaviour
{

    public static GameManager instance;
    public Tilemap tunnelMap;
    public Tilemap tunnelBlinders;
    public GameObject tunnelVision;
    public SpriteRenderer tunnelVisionViewPort;
    public Image blackoutPanel;
    public CanvasGroup titleCard;
    float tunnelVisionDefaultScale = 4f;
    float tunnelVisionTransitionDuration = 1f;
    float tunnelBlinderALpha = 0.5f;
    public bool isNewGame;
    [SerializeField] public GameObject player;
    public PlayerController playerController;
    [SerializeField] GameObject birthSequence;
    Vector3 playerStartingLocation = Vector3.zero;

    public int cellCount = 0;
    public int maxCells = 80;
    public int nextCellGoal;
    public bool upgradeAvailable;
    public ZombieOutEvent currentZombieOutEvent;

    public Region listenRegion;
    
    Dictionary<Region, float> regionVolume = new Dictionary<Region, float>()
    {
        {Region.finalAscent, -12},
        {Region.upperCavern, -6},
        {Region.northPassage, 0},
        {Region.centralCavern, 6},
        {Region.deep, 9}
    };
    public AudioMixer DeepMixer;
    
    private void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        playerController = player.GetComponent<PlayerController>();
    }

    private void OnEnable() {
        EventBroker.CellCollected += CollectCell;
        EventBroker.StartPlay += StartPlay;
        EventBroker.TransformComplete += UpdateCellGoal;
        EventBroker.StartFinalDialogue += SetRegionForEndFont;
        EventBroker.Blackout += Blackout;
        EventBroker.TitleCard += TitleCardIn;
        EventBroker.Upgrade += Upgraded;
        EventBroker.Instructions += Instruction;
        //EventBroker.StartFinalDialogue += DeactivatePlayer;
    }

    private void OnDisable() {
        EventBroker.CellCollected -= CollectCell;
        EventBroker.StartPlay -= StartPlay;
        EventBroker.TransformComplete -= UpdateCellGoal;
        EventBroker.StartFinalDialogue -= SetRegionForEndFont;
        EventBroker.Blackout -= Blackout;
        EventBroker.TitleCard -= TitleCardIn;
        EventBroker.Upgrade -= Upgraded;
        EventBroker.Instructions -= Instruction;
        //EventBroker.StartFinalDialogue -= DeactivatePlayer;
    }

    private void Start()
    {
        // Application.targetFrameRate = 30;

        tunnelVision.transform.localScale = new Vector3(0,0,tunnelVision.transform.localScale.z);
        tunnelVisionViewPort.color = new Color(1, 1, 1, 0);
        blackoutPanel.color = new Color(blackoutPanel.color.r, blackoutPanel.color.g, blackoutPanel.color.b, 0);
        titleCard.alpha = 0;

        if (isNewGame)
        {
            cellCount = 0;
            birthSequence.transform.SetPositionAndRotation(playerStartingLocation, Quaternion.identity);
            birthSequence.SetActive(true);
            player.transform.SetPositionAndRotation(playerStartingLocation, Quaternion.identity);
            player.SetActive(false);
            
        }
        else
        {
            player.SetActive(true);
            birthSequence.SetActive(false);
        }

        UpdateCellGoal();
    }

    void StartPlay()
    {
        player.SetActive(true);
        birthSequence.SetActive(false);
    }

    public void EnterTunnel()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionTunnelVision(true));
    }

    public void ExitTunnel()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionTunnelVision(false));
    }

    void CollectCell()
    {
        cellCount++;
        
        if (cellCount == maxCells)
            EventBroker.AllCellsCollectedCall();

        UpdateUpgradeAvailable();
    }

    void UpdateCellGoal()
    {
        switch (playerController.form)
        {
            case Form.worm:
            {
                nextCellGoal = 5;
                break;
            }

            case Form.claws:
            {
                nextCellGoal = 15;
                break;
            }

            case Form.legs:
            {
                nextCellGoal = 30;
                break;
            }

            case Form.glider:
            {
                nextCellGoal = 50;
                break;
            }

            case Form.deep:
            {
                nextCellGoal = maxCells;
                break;
            }
        }

        UpdateUpgradeAvailable();
    }

    void UpdateUpgradeAvailable()
    {
        if (cellCount >= nextCellGoal)
            upgradeAvailable = true;
        else
            upgradeAvailable = false;
    }

    IEnumerator TransitionTunnelVision(bool entering)
    {
        float startScale = tunnelVision.transform.localScale.x;
        float endScale = entering ? tunnelVisionDefaultScale : 0;
        float startAlpha = blackoutPanel.color.a;
        float endAlpha = entering ? 1 : 0;
        float blinderEndAlpha = entering ? tunnelBlinderALpha : 0;

        float timer = 0f;
        float duration = tunnelVisionTransitionDuration;

        while (timer < duration)
        {
            float currentScale = Mathf.Lerp(startScale, endScale, timer/duration);
            tunnelVision.transform.localScale = new Vector3(currentScale, currentScale, tunnelVision.transform.localScale.z);

            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, timer/duration);
            float blinderAlpha = Mathf.Lerp(startAlpha, blinderEndAlpha, timer/duration);
            tunnelMap.color = new Color(currentAlpha, currentAlpha, currentAlpha, currentAlpha);
            tunnelBlinders.color = new Color(1, 1, 1, blinderAlpha);
            tunnelVisionViewPort.color = new Color(1, 1, 1, currentAlpha);
            blackoutPanel.color = new Color(blackoutPanel.color.r, blackoutPanel.color.g, blackoutPanel.color.b, currentAlpha);

            timer += Time.deltaTime;
            yield return null;
        }
        tunnelVision.transform.localScale = new Vector3(endScale, endScale, tunnelVision.transform.localScale.z);
        tunnelMap.color = new Color(1, 1, 1, endAlpha);
        tunnelBlinders.color = new Color(1, 1, 1, blinderEndAlpha);
        tunnelVisionViewPort.color = new Color(1, 1, 1, endAlpha);
        blackoutPanel.color = new Color(blackoutPanel.color.r, blackoutPanel.color.g, blackoutPanel.color.b, endAlpha);
    }

    void Blackout()
    {
        StartCoroutine(BlackoutFade());
    }

    IEnumerator BlackoutFade()
    {
        float timer = 0;
        Color startingColor = blackoutPanel.color;

        while (timer < 2)
        {
            blackoutPanel.color = Color.Lerp(startingColor, Color.black, timer/2);
            timer += Time.deltaTime;
            yield return null;
        }
        blackoutPanel.color = Color.black;
    }


    void SetRegionForEndFont()
    {
        UpdateListenRegion(Region.northPassage);
    }

    public void UpdateListenRegion(Region region)
    {
        listenRegion = region;
        UpdateDeepVolume();
    }

    void UpdateDeepVolume()
    {
        DeepMixer.SetFloat("DeepVol", regionVolume[listenRegion]);
    }

    public void TitleCardIn()
    {
        StartCoroutine(FadeInTitleCard());
    }

    IEnumerator FadeInTitleCard()
    {
        float timer = 0;
        while (timer < 6.5f)
        {
            titleCard.alpha = Mathf.Lerp(0, 1, Mathf.Pow(timer,3)/Mathf.Pow(6.5f, 3));
            timer += Time.deltaTime;
            yield return null;
        }
        titleCard.alpha = 1;
    }


    public void SetZombieOutEventTo(ZombieOutEvent zEvent)
    {
        currentZombieOutEvent = zEvent;
    }


    void Upgraded()
    {
        switch (playerController.form)
        {
            case Form.claws:
            {
                Notifications.text = "Obtained CLAWS";
                EventBroker.ShortNoticeCall();
                break;
            }

            case Form.legs:
            {
                Notifications.text = "Obtained LEGS";
                EventBroker.ShortNoticeCall();
                break;
            }

            case Form.glider:
            {
                Notifications.text = "Obtained\nGLIDER FORM";
                EventBroker.ShortNoticeCall();
                break;
            }

            case Form.deep:
            {
                Notifications.text = "Obtained\nDEEP SENSE";
                EventBroker.ShortNoticeCall();
                break;
            }

            default:
            {
                break;
            }
        }
    }

    void Instruction()
    {
        switch (playerController.form)
        {
            case Form.claws:
            {
                Notifications.text = "Press UP to CLIMB\nPress SPACE to let go";
                EventBroker.LongNoticeCall();
                break;
            }

            case Form.legs:
            {
                Notifications.text = "Press SPACE to JUMP\n - hold to jump higher\n - tap to short hop";
                EventBroker.LongNoticeCall();
                break;
            }

            case Form.glider:
            {
                Notifications.text = "Press SPACE while midair\nto GLIDE\nUse ARROW KEYS to turn";
                EventBroker.LongNoticeCall();
                break;
            }

            case Form.deep:
            {
                Notifications.text = "Hold SHIFT while still\nto use DEEP SENSE";
                EventBroker.LongNoticeCall();
                break;
            }

            default:
            {
                break;
            }
        }
    }

    // public void DeactivatePlayer()
    // {
    //     StartCoroutine(DeactivatePlayerDelay());
    // }

    // IEnumerator DeactivatePlayerDelay()
    // {
    //     yield return new WaitForSeconds(1);

    //     player.SetActive(false);
    // }
}
