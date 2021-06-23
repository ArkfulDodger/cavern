using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.Tilemaps;

public class DeepSenseCam : MonoBehaviour
{
    //Camera Components
    CinemachineVirtualCamera vCamPlayer;
    CinemachineTransposer vCamTransposer;
    float targetOrthoSize;
    [SerializeField] float startingTargetOrthoSize;
    [SerializeField] float finalTargetOrthoSize;

    float refOrthoSize;
    float refYOffset;

    // Tunnel View Components
    [SerializeField] float targetTVScaleFloat = 50f;
    Vector3 targetTVScale;
    Color refTunnelMapColor;
    Color targetTunnelBlinderColor = new Color(1, 1, 1, 0.239215f);
    Vector3 refTVScale;
    [SerializeField] float zoomTime;
    Tilemap tunnelMap;
    Tilemap tunnelBlinders;
    Transform tunnelVision;

    bool deepSenseOn;

    private void Awake()
    {
        vCamPlayer = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CinemachineVirtualCamera>();
        vCamTransposer = vCamPlayer.GetCinemachineComponent<CinemachineTransposer>();
    }

    private void Start()
    {
        if (GameManager.instance.cellCount == GameManager.instance.maxCells)
            targetOrthoSize = finalTargetOrthoSize;
        else
            targetOrthoSize = startingTargetOrthoSize;

        tunnelMap = GameManager.instance.tunnelMap;
        tunnelBlinders = GameManager.instance.tunnelBlinders;
        tunnelVision = GameManager.instance.tunnelVision.transform; 

        targetTVScale = new Vector3(targetTVScaleFloat, targetTVScaleFloat, 1); 
    }

    private void OnEnable()
    {
        EventBroker.DeepSenseEnter += EnterDeepSense;
        EventBroker.AllCellsCollected += FinalCellChange;
    }

    private void OnDisable()
    {
        EventBroker.DeepSenseEnter -= EnterDeepSense;
        EventBroker.AllCellsCollected -= FinalCellChange;
    }

    void FinalCellChange()
    {
        targetOrthoSize = finalTargetOrthoSize;
    }

    void EnterDeepSense()
    {
        if (!deepSenseOn)
        {
            deepSenseOn = true;
            refOrthoSize = vCamPlayer.m_Lens.OrthographicSize;
            refYOffset = vCamTransposer.m_FollowOffset.y;
            refTunnelMapColor = GameManager.instance.tunnelMap.color;
            refTVScale = tunnelVision.localScale;

            StartCoroutine(DeepSenseOn());
        }
    }

    void CheckTriggerKeyReleased()
    {
        if (!Input.GetKey(KeyCode.RightShift) && !Input.GetKey(KeyCode.LeftShift))
            deepSenseOn = false;
    }

    IEnumerator DeepSenseOn()
    {
        float timer = 0;

        while (deepSenseOn && timer < zoomTime)
        {
            float lerpVal = Mathf.Sqrt(timer)/Mathf.Sqrt(zoomTime);
            vCamPlayer.m_Lens.OrthographicSize = Mathf.Lerp(refOrthoSize, targetOrthoSize, lerpVal);
            vCamTransposer.m_FollowOffset.y = Mathf.Lerp(refYOffset, 0, lerpVal);
            tunnelMap.color = Color.Lerp(refTunnelMapColor, Color.white, lerpVal);
            tunnelBlinders.color = Color.Lerp(refTunnelMapColor, targetTunnelBlinderColor, lerpVal);
            tunnelVision.localScale = Vector3.Lerp(refTVScale, targetTVScale, lerpVal);

            timer += Time.deltaTime;
            CheckTriggerKeyReleased();
            yield return null;
        }

        // finalize cam settings if still engaged after breaking loop
        if (deepSenseOn)
        {
            vCamPlayer.m_Lens.OrthographicSize = targetOrthoSize;
            vCamTransposer.m_FollowOffset.y = 0;
            tunnelMap.color = Color.white;
            tunnelBlinders.color = targetTunnelBlinderColor;
            tunnelVision.localScale = targetTVScale;
        }

        // continue checking for release input until given
        while (deepSenseOn)
        {
            CheckTriggerKeyReleased();
            yield return null;
        }

        float startOrtho = vCamPlayer.m_Lens.OrthographicSize;
        float startYOffset = vCamTransposer.m_FollowOffset.y;
        Color startMapColor = tunnelMap.color;
        Color startBlindersColor = tunnelBlinders.color;
        Vector3 startTVScale = tunnelVision.localScale;
        float zoomOutTime = timer < zoomTime ? timer : zoomTime;
        timer = 0;

        while (timer < zoomOutTime)
        {
            float lerpVal = Mathf.Sqrt(timer)/Mathf.Sqrt(zoomOutTime);
            vCamPlayer.m_Lens.OrthographicSize = Mathf.Lerp(startOrtho, refOrthoSize, lerpVal);
            vCamTransposer.m_FollowOffset.y = Mathf.Lerp(startYOffset, refYOffset, lerpVal);
            tunnelMap.color = Color.Lerp(startMapColor, refTunnelMapColor, lerpVal);
            tunnelBlinders.color = Color.Lerp(startBlindersColor, refTunnelMapColor, lerpVal);
            tunnelVision.localScale = Vector3.Lerp(startTVScale, refTVScale, lerpVal);

            timer += Time.deltaTime;
            yield return null;
        }
        vCamPlayer.m_Lens.OrthographicSize = refOrthoSize;
        vCamTransposer.m_FollowOffset.y = refYOffset;
        tunnelMap.color = refTunnelMapColor;
        tunnelBlinders.color = refTunnelMapColor;
        tunnelVision.localScale = refTVScale;

        EventBroker.DeepSenseExitCall();

        yield return null;
    }
}
