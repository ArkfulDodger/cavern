using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class HintDialogueSection : ScriptableObject
{
    public Region region;
    public List<DialogueStep> steps = new List<DialogueStep>();
}
