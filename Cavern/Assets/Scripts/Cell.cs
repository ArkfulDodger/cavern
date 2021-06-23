using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class Cell : MonoBehaviour
{
    public static List<Cell> cellArray = new List<Cell>();
    public static List<Cell> accessibleCells = new List<Cell>();
    public bool collected;
    public Region location;
    public Form formReq;
    public bool inTunnel;
    Light2D lightSpot;
    float iRadMin = 0.4f;
    float iRadMax = 0.5f;
    float oRadMin = 1.3f;
    float oRadMax = 1.5f;
    float defaultIntensity = 2f;
    Transform glowScale1;
    Transform glowScale2;
    SpriteRenderer glowRenderer1;
    SpriteRenderer glowRenderer2;
    float maxSize = 7f;
    float fadeTime = 3f;
    float vanishTime = 0.3f;
    Transform playerTransform;
    //PlayerController player;


    private void Awake() {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        //player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        cellArray.Add(this);
    }

    private void OnEnable() {
        EventBroker.TunnelEntryComplete += UpdateForTunnelEntry;
        EventBroker.TunnelExitBegun += UpdateForTunnelExit;
    }

    private void OnDisable() {
        EventBroker.TunnelEntryComplete -= UpdateForTunnelEntry;
        EventBroker.TunnelExitBegun-= UpdateForTunnelExit;
    }

    // Start is called before the first frame update
    void Start()
    {
        //if (player)

        lightSpot = GetComponent<Light2D>();
        glowScale1 = transform.GetChild(0);
        glowRenderer1 = glowScale1.gameObject.GetComponent<SpriteRenderer>();
        glowScale2 = transform.GetChild(1);
        glowRenderer2 = glowScale2.gameObject.GetComponent<SpriteRenderer>();

        StartCoroutine(Glow());
    }

   IEnumerator Glow()
   {
       float timer1 = 0;
       float timer2 = fadeTime * 0.5f;

       while (true)
       {
            if (timer1 > fadeTime)
                timer1 = 0;

            if (timer2 > fadeTime)
                timer2 = 0;

            if (timer1 < timer2)
            {
                float radius = Mathf.Lerp(oRadMax, oRadMin, Mathf.Pow(timer1,1.2f)/Mathf.Pow(fadeTime * 0.5f,1.2f));
                lightSpot.pointLightOuterRadius = radius;
                radius = Mathf.Lerp(iRadMax, iRadMin, Mathf.Pow(timer1,1.2f)/Mathf.Pow(fadeTime * 0.5f,1.2f));
                lightSpot.pointLightInnerRadius = radius;
            }
            else
            {
                float radius = Mathf.Lerp(oRadMin, oRadMax, Mathf.Pow(timer2,1.2f)/Mathf.Pow(fadeTime * 0.5f,1.2f));
                lightSpot.pointLightOuterRadius = radius;
                radius = Mathf.Lerp(iRadMin, iRadMax, Mathf.Pow(timer2,1.2f)/Mathf.Pow(fadeTime * 0.5f,1.2f));
                lightSpot.pointLightInnerRadius = radius;
            }

            float currentScale = Mathf.Lerp(0, maxSize, timer1/fadeTime);
            glowScale1.localScale = new Vector3(currentScale, currentScale, 1);
            float currentAlpha = Mathf.Lerp(1, 0, timer1/fadeTime);
            glowRenderer1.color = new Color(1, 1, 1, currentAlpha);

            currentScale = Mathf.Lerp(0, maxSize, timer2/fadeTime);
            glowScale2.localScale = new Vector3(currentScale, currentScale, 1);
            currentAlpha = Mathf.Lerp(1, 0, timer2/fadeTime);
            glowRenderer2.color = new Color(1, 1, 1, currentAlpha);

            timer1 += Time.deltaTime;
            timer2 += Time.deltaTime;

            yield return null;
        }
    }

    IEnumerator Vanish(SpriteRenderer player)
    {
        float startScale = transform.localScale.x;
        float startIRad = lightSpot.pointLightInnerRadius;
        float startORad = lightSpot.pointLightOuterRadius;

        float timer = 0f;
        while (timer < vanishTime)
        {
            transform.position = player.bounds.center;

            float scale = Mathf.Lerp(startScale, 0, timer/vanishTime);
            transform.localScale = new Vector3(scale, scale, 1);

            lightSpot.pointLightInnerRadius = Mathf.Lerp(startIRad, 0, timer/vanishTime);
            lightSpot.pointLightOuterRadius = Mathf.Lerp(startORad, 0, timer/vanishTime);

            timer += Time.deltaTime;

            yield return null;
        }
        transform.position = player.bounds.center;
        transform.localScale = Vector3.zero;
        lightSpot.pointLightInnerRadius = 0;
        lightSpot.pointLightOuterRadius = 0;

        EventBroker.CellAbsorbedCall();

        yield return null;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (collected)
            return;

        PlayerController playerController = other.gameObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            collected = true;
            EventBroker.CellCollectedCall();
            StopAllCoroutines();
            StartCoroutine(Vanish(other.GetComponent<SpriteRenderer>()));
        }
    }

    void UpdateForTunnelEntry()
    {
        if (inTunnel)
        {
            StopCoroutine("DimTunnelGlow");
            StartCoroutine("BringUpTunnelGlow");
        }
    }

    void UpdateForTunnelExit()
    {
        if (inTunnel)
        {
            StopCoroutine("BringUpTunnelGlow");
            StartCoroutine("DimTunnelGlow");
        }
    }

    IEnumerator BringUpTunnelGlow()
    {
        float currentIntensity = lightSpot.intensity;
        float timer = 0f;
        while (timer < 1f)
        {
            lightSpot.intensity = Mathf.Lerp(currentIntensity, defaultIntensity, timer/1);
            timer += Time.deltaTime;
            yield return null;
        }
        lightSpot.intensity = defaultIntensity;
        yield return null;
    }

    IEnumerator DimTunnelGlow()
    {
        if ((playerTransform.position - transform.position).magnitude <= 2.5f)
        {
            float currentIntensity = lightSpot.intensity;
            float timer = 0;
            while (timer < 1)
            {
                lightSpot.intensity = Mathf.Lerp(currentIntensity, 0, timer/1);
                timer += Time.deltaTime;
                yield return null;
            }
            lightSpot.intensity = 0;
        }
        else
        {
            lightSpot.intensity = 0;
        }

        yield return null;
    }
}
