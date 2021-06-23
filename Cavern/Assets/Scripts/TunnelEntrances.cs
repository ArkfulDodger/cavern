using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TunnelEntrances : MonoBehaviour
{
    Tilemap tilemap;
    Grid grid;
    public List<Tile> downEntrances;
    public List<Tile> upEntrances;
    public List<Tile> rightEntrances;
    public List<Tile> leftEntrances;

    // Start is called before the first frame update
    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        grid = tilemap.layoutGrid;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider is BoxCollider2D || other.collider is CapsuleCollider2D)
        {
            PlayerController playerController = other.collider.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Vector3Int cellPosition = grid.WorldToCell(other.GetContact(0).point);
                Tile tile = tilemap.GetTile<Tile>(cellPosition);
                Direction entranceDirection;

                if (rightEntrances.Contains(tile))
                    entranceDirection = Direction.right;
                else if (leftEntrances.Contains(tile))
                    entranceDirection = Direction.left;
                else if (upEntrances.Contains(tile))
                    entranceDirection = Direction.up;
                else
                    entranceDirection = Direction.down;

                playerController.FlagAtEntrance(entranceDirection);
                //Debug.Log("in entrance");
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.collider is BoxCollider2D || other.collider is CapsuleCollider2D)
        {
            PlayerController playerController = other.collider.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.FlagLeftEntrance();
                //Debug.Log("left entrance");
            }
        }
    }

    public bool CanEnterHereWithInput(KeyCode key, Vector2 collisionPoint, out Vector2 entryPoint)
    {
        Vector3Int cellPosition = grid.WorldToCell(collisionPoint);
        Tile tile = tilemap.GetTile<Tile>(cellPosition);

            if (key == KeyCode.DownArrow && downEntrances.Contains(tile))
            {
                entryPoint = (Vector2)grid.GetCellCenterWorld(cellPosition) - new Vector2(0, 0.5f);
                // Debug.Log("Can enter down at (" + entryPoint.x + ", " + entryPoint.y + ")");
                return true;
            }
            else if (key == KeyCode.UpArrow && upEntrances.Contains(tile))
            {
                entryPoint = (Vector2)grid.GetCellCenterWorld(cellPosition) + new Vector2(0, 0.5f);
                // Debug.Log("Can enter down at (" + entryPoint.x + ", " + entryPoint.y + ")");
                return true;
            }   
            else if (key == KeyCode.RightArrow && rightEntrances.Contains(tile))
            {
                entryPoint = (Vector2)grid.GetCellCenterWorld(cellPosition) + new Vector2(0.5f, 0);
                // Debug.Log("Can enter down at (" + entryPoint.x + ", " + entryPoint.y + ")");
                return true;
            }
            else if (key == KeyCode.LeftArrow && leftEntrances.Contains(tile))
            {
                entryPoint = (Vector2)grid.GetCellCenterWorld(cellPosition) - new Vector2(0.5f, 0);
                // Debug.Log("Can enter down at (" + entryPoint.x + ", " + entryPoint.y + ")");
                return true;
            }
            else
            {
                entryPoint = collisionPoint;
                return false;
            }
    }

}
