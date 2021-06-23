using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileList: ScriptableObject
{
    public string listName;
    public List<Tile> tileList;
}
