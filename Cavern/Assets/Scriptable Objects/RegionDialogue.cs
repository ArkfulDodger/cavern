using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class RegionDialogue : ScriptableObject
{
    public Region region;
    public List<RegionDialogueSection> sections = new List<RegionDialogueSection>();
}
