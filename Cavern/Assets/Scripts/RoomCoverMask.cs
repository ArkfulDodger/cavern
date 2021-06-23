using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent (typeof (Tilemap), typeof (TilemapRenderer), typeof (TilemapCollider2D))]
[RequireComponent (typeof (Rigidbody2D), typeof (CompositeCollider2D))]
public class RoomCoverMask : MonoBehaviour
{
    [SerializeField] bool roomOpen;
    [SerializeField] bool overworldUnlockable;
    [SerializeField] Material runtimeMaterial;
    Tilemap roomMap;
    TilemapRenderer mapRenderer;
    TilemapCollider2D mapCollider;
    Rigidbody2D mapRigidbody;
    CompositeCollider2D mapCompositeCollider;


    // Start is called before the first frame update
    void Awake()
    {
        mapRenderer = GetComponent<TilemapRenderer>();
        mapCollider = GetComponent<TilemapCollider2D>();
        mapRigidbody = GetComponent<Rigidbody2D>();
        mapCompositeCollider = GetComponent<CompositeCollider2D>();
        roomMap = GetComponent<Tilemap>();

        mapRenderer.material = runtimeMaterial;
        mapRenderer.sortingLayerName = "Stencils";
        mapRenderer.sortingOrder = 0;

        mapCollider.usedByComposite = true;

        mapRigidbody.bodyType = RigidbodyType2D.Static;
        mapRigidbody.simulated = true;

        mapCompositeCollider.isTrigger = true;
    }

    private void Start()
    {
        if (roomOpen)
            OpenRoom();
        else
            CloseRoom();
    }

    void CloseRoom()
    {
        roomMap.color = Vector4.zero;
        roomOpen = false;
    }

    void OpenRoom()
    {
        roomMap.color = Vector4.one;
        roomOpen = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!roomOpen)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && (overworldUnlockable || player.exiting))
            {
                OpenRoom();
            }
        }
    }
}
