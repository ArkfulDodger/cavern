using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileListCollection: ScriptableObject
{
    public string collectionName;
    public List<TileList> listCollection;
}
