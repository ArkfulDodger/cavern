using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DialogueStep
{
    public bool isActionStep;
    [TextArea]
    public string text;
}