using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent (typeof (BoxCollider2D))]
public class CameraZoomZone : MonoBehaviour
{
    BoxCollider2D zoneCollider;
    CinemachineVirtualCamera vCamPlayer;
    CinemachineTransposer vCamTransposer;
    [SerializeField] Transform playerTransform;

    float defaultOrthoSize;
    float defaultOffsetY;
    [SerializeField] float zoneOrthoSize = 7.57f;
    [SerializeField] float zoneOffsetY = 5.95f;

    [SerializeField] Vector3 referencePoint;
    [SerializeField] float innerRadius = 2f;
    [SerializeField] float outerRadius = 11.7f;

    [SerializeField] bool inZone;

    [SerializeField] float orthoChangeLimiter = 0.0075f;
    [SerializeField] float offsetYChangeLimiter = 0.012f;
    [SerializeField] float rapidChangeTime = 1;
    [SerializeField] float slowChangeTime = 3;
    bool manualZone = true;

    bool paused;


    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        vCamPlayer = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CinemachineVirtualCamera>();
        vCamTransposer = vCamPlayer.GetCinemachineComponent<CinemachineTransposer>();
        defaultOrthoSize = vCamPlayer.m_Lens.OrthographicSize;
        defaultOffsetY = vCamTransposer.m_FollowOffset.y;
    }

    private void OnEnable()
    {
        EventBroker.TunnelEntryBegun += TunnelExit;
        EventBroker.DeepSenseEnter += PauseFunction;
        EventBroker.DeepSenseExit += ResumeFunction;
        EventBroker.LookStart += ManualZoneActivate;
        EventBroker.ManualZoomOut += ManualZoneActivate;
        EventBroker.ZoomCamToDefault += ManualRevertSlow;
        EventBroker.ZoneActive += TurnOffManual;
    }

    private void OnDisable()
    {
        EventBroker.TunnelEntryBegun -= TunnelExit;
        EventBroker.DeepSenseEnter -= PauseFunction;
        EventBroker.DeepSenseExit -= ResumeFunction;
        EventBroker.LookStart -= ManualZoneActivate;
        EventBroker.ManualZoomOut -= ManualZoneActivate;
        EventBroker.ZoomCamToDefault -= ManualRevertSlow;
        EventBroker.ZoneActive -= TurnOffManual;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!inZone)
        {
            if (other.CompareTag("Player") && other is CircleCollider2D)
            {
                playerTransform = other.gameObject.transform;
                EnteredZone();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (inZone)
        {
            if (other.CompareTag("Player") && other is CircleCollider2D && !ColliderIsInsideTriggerCollider(other))
            {
                ExitedZone();
            }
        }
    }

    bool ColliderIsInsideTriggerCollider(Collider2D other)
    {
        Vector2 position = other.gameObject.transform.position;
        if (zoneCollider.bounds.Contains(position))
            return true;
        else
            return false;
    }

    void EnteredZone()
    {
        inZone = true;
        
        if (!manualZone)
        {
            StopAllCoroutines();
            float distanceFromRef = (referencePoint - playerTransform.position).magnitude;
            if(distanceFromRef < innerRadius)
                StartCoroutine(ImmediateEntry());
            else
                StartCoroutine(ZoomZoneActive());
        }
    }

    void ExitedZone()
    {
        if (!manualZone)
        {
            inZone = false;
            StopAllCoroutines();
            StartCoroutine(RevertCamera());
        }
    }

    void TunnelExit()
    {
        if (inZone)
            ExitedZone();
    }

    void PauseFunction()
    {
        paused = true;
    }

    void ResumeFunction()
    {
        paused = false;
    }

    IEnumerator ZoomZoneActive()
    {
        while (inZone)
        {        
            while (paused)
            {
                yield return null;
            }

            // find the distance from the player to the reference point
            float distanceFromRef = (referencePoint - playerTransform.position).magnitude;

            // get the current cam values and set variables for the target values
            float currentOrthoSize = vCamPlayer.m_Lens.OrthographicSize;
            float currentOffsetY = vCamTransposer.m_FollowOffset.y;
            float targetOrthoSize;
            float targetOffsetY;

            // SET TARGET VALUES
                // set target values to default if outside the outer radius
                if(distanceFromRef > outerRadius)
                {
                    targetOrthoSize = defaultOrthoSize;
                    targetOffsetY = defaultOffsetY;
                }

                // set target values to the full zone value if within the inner radius
                else if (distanceFromRef < innerRadius)
                {
                    targetOrthoSize = zoneOrthoSize;
                    targetOffsetY = zoneOffsetY;
                }

                // Lerp target values bbased on position if elsewhere within the zone
                else
                {
                    float lerpVal = (outerRadius - distanceFromRef) / (outerRadius - innerRadius);
                    targetOrthoSize = Mathf.Lerp(defaultOrthoSize, zoneOrthoSize, lerpVal);
                    targetOffsetY = Mathf.Lerp(defaultOffsetY, zoneOffsetY, lerpVal);
                }

            // apply change to orthographic size, restricted to limiter
            if (currentOrthoSize != targetOrthoSize)
            {
                float orthoChange = targetOrthoSize - currentOrthoSize;
                orthoChange = Mathf.Sign(orthoChange) * Mathf.Min(Mathf.Abs(orthoChange), orthoChangeLimiter);
                vCamPlayer.m_Lens.OrthographicSize += orthoChange;
            }

            // apply change to Y offset, restricted to limiter
            if (currentOffsetY != targetOffsetY)
            {
                float offsetYChange = targetOffsetY - currentOffsetY;
                offsetYChange = Mathf.Sign(offsetYChange) * Mathf.Min(Mathf.Abs(offsetYChange), offsetYChangeLimiter);
                vCamTransposer.m_FollowOffset.y += offsetYChange;
            }

            yield return null;
        }

        yield return null;
    }

    IEnumerator RevertCamera()
    {
        float currentOrthoSize = vCamPlayer.m_Lens.OrthographicSize;
        float currentOffsetY = vCamTransposer.m_FollowOffset.y;

        if (currentOrthoSize != defaultOrthoSize || currentOffsetY != defaultOffsetY)
        {
            float time = 0;
            while (time < rapidChangeTime)
            {
                vCamPlayer.m_Lens.OrthographicSize = Mathf.Lerp(currentOrthoSize, defaultOrthoSize, time/rapidChangeTime);
                vCamTransposer.m_FollowOffset.y = Mathf.Lerp(currentOffsetY, defaultOffsetY, time/rapidChangeTime);

                time += Time.deltaTime;
                yield return null;
            }
            vCamPlayer.m_Lens.OrthographicSize = defaultOrthoSize;
            vCamTransposer.m_FollowOffset.y = defaultOffsetY;
        }
    }

    IEnumerator RevertCameraSlow()
    {
        float currentOrthoSize = vCamPlayer.m_Lens.OrthographicSize;
        float currentOffsetY = vCamTransposer.m_FollowOffset.y;

        if (currentOrthoSize != defaultOrthoSize || currentOffsetY != defaultOffsetY)
        {
            float time = 0;
            while (time < slowChangeTime)
            {
                vCamPlayer.m_Lens.OrthographicSize = Mathf.Lerp(currentOrthoSize, defaultOrthoSize, time/slowChangeTime);
                vCamTransposer.m_FollowOffset.y = Mathf.Lerp(currentOffsetY, defaultOffsetY, time/slowChangeTime);

                time += Time.deltaTime;
                yield return null;
            }
            vCamPlayer.m_Lens.OrthographicSize = defaultOrthoSize;
            vCamTransposer.m_FollowOffset.y = defaultOffsetY;
        }
    }

    IEnumerator ImmediateEntry()
    {
        float currentOrthoSize = vCamPlayer.m_Lens.OrthographicSize;
        float currentOffsetY = vCamTransposer.m_FollowOffset.y;

        if (currentOrthoSize != zoneOrthoSize || currentOffsetY != zoneOffsetY)
        {
            float time = 0;
            while (time < rapidChangeTime)
            {
                vCamPlayer.m_Lens.OrthographicSize = Mathf.Lerp(currentOrthoSize, zoneOrthoSize, time/rapidChangeTime);
                vCamTransposer.m_FollowOffset.y = Mathf.Lerp(currentOffsetY, zoneOffsetY, time/rapidChangeTime);

                time += Time.deltaTime;
                yield return null;
            }
            vCamPlayer.m_Lens.OrthographicSize = zoneOrthoSize;
            vCamTransposer.m_FollowOffset.y = zoneOffsetY;
        }

        if (inZone)
            StartCoroutine(ZoomZoneActive());

        yield return null;
    }

    void ManualZoneActivate()
    {
        inZone = true;
        manualZone = true;
        StartCoroutine(ZoomZoneActive());
    }

    void ManualRevertSlow()
    {
        inZone = false;
        manualZone = true;
        StopAllCoroutines();
        StartCoroutine(RevertCameraSlow());
    }

    void TurnOffManual()
    {
        manualZone = false;
    }
}
