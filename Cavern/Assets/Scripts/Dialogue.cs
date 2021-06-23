using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    [SerializeField] Vector3 standardPosition = new Vector3(0, -120, 0);
    [SerializeField] Vector3 centralCavernPosition = new Vector3(0, 56, 0);
    // Dictionary<Region, float> fontContainerWidth = new Dictionary<Region, float>()
    // {
    //     {Region.finalAscent, 240},
    //     {Region.upperCavern, 290},
    //     {Region.northPassage, 340},
    //     {Region.centralCavern, 372},
    //     {Region.deep, 482},
    // };
    Dictionary<Region, float> fontSize = new Dictionary<Region, float>()
    {
        {Region.finalAscent, 18f},
        {Region.upperCavern, 21f},
        {Region.northPassage, 24f},
        {Region.centralCavern, 27f},
        {Region.deep, 30f}
    };
    Dictionary<Region, int> visitCounter = new Dictionary<Region, int>
    {
        {Region.finalAscent, 1},
        {Region.upperCavern, 1},
        {Region.northPassage, 1},
        {Region.centralCavern, 1},
        {Region.deep, 1}
    };
    Dictionary<Region, bool> regionHintGiven = new Dictionary<Region, bool>
    {
        {Region.upperCavern, false},
        {Region.northPassage, false},
        {Region.centralCavern, false},
    };
    Dictionary<Region, string> regionTerm = new Dictionary<Region, string>()
    {
        {Region.centralPassage, "Passage beneath the Main Cavern"},
        {Region.chambers, "Chambers"},
        {Region.maze, "Maze"},
        {Region.centralCavern, "Central Cavern"},
        {Region.narrowCliffs, "Narrow Cliffs"},
        {Region.northPassage, "Pass above the Central Cavern"},
        {Region.openStair, "Open Stair"},
        {Region.upperCavern, "Upper Cavern"},
        {Region.crevice, "Crevice on the Cavern's edge"},
        {Region.westPassage, "Passage beneath the Maze"},
        {Region.eastPassage, "Passage beneath the Chambers"},
        {Region.deep, "Cavern depths"},
        {Region.finalAscent, "Passage to the surface"}
    };
    Dictionary<Form, Region> upgradeLocationByForm = new Dictionary<Form, Region>()
    {
        {Form.worm, Region.centralCavern},
        {Form.claws, Region.northPassage},
        {Form.legs, Region.upperCavern},
        {Form.glider, Region.deep}
    };

    bool finalHintGiven;

    [SerializeField] RectTransform containerBG;
    [SerializeField] RectTransform container;
    [SerializeField] Image promptArrow;
    [SerializeField] TMP_Text dialogueText;
    Animator animator;
    bool inDialogue;
    bool pastLook;
    bool endingStarted;
    bool dialoguePaused;
    public bool autoscroll;
    public float scrollSpeed;
    public float textScrollInterval;
    bool fullTextDisplayed;
    [SerializeField] Region currentRegion;

    [SerializeField] List<RegionDialogue> regionDialogues = new List<RegionDialogue>();
    [SerializeField] List<RegionDialogueSection> endingDialogue = new List<RegionDialogueSection>();
    [SerializeField] List<HintDialogueSection> regionHints = new List<HintDialogueSection>();
    [SerializeField] List<HintDialogueSection> finalHints = new List<HintDialogueSection>();
    List<DialogueStep> currentDialogue = new List<DialogueStep>();
    Dictionary<Region, int> accessibleCells = new Dictionary<Region, int>();
    int dIndex;
    string currentText;
    
    PlayerController player;
    DialogueActions dialogueActions;
    public AudioSource deepAudio;
    public AudioSource skyAudio;
    public AudioClip defaultVocal;
    public AudioClip firstVocal;
    AudioClip currentVocal;
    bool noPlayVocal;
    bool noPlayMusic;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        dialogueActions = GetComponent<DialogueActions>();
    }
    
    private void OnEnable()
    {
        EventBroker.EnterDialogue += EnterDialogue;
        EventBroker.TransformComplete += ResumeDialogue;
        EventBroker.LookStart += LookStart;
        EventBroker.LookEnd += ResumeDialogue;
        EventBroker.LookEnd += SwitchCavernBoxPosition;
        EventBroker.StartFinalDialogue += StartFinalDialogue;
    }

    private void OnDisable()
    {
        EventBroker.EnterDialogue -= EnterDialogue;
        EventBroker.TransformComplete -= ResumeDialogue;
        EventBroker.LookStart -= LookStart;
        EventBroker.LookEnd -= ResumeDialogue;
        EventBroker.LookEnd -= SwitchCavernBoxPosition;
        EventBroker.StartFinalDialogue -= StartFinalDialogue;
    }

    private void Start()
    {
        player = GameManager.instance.player.GetComponent<PlayerController>();
        inDialogue = false;
    }

    void EnterDialogue()
    {
        inDialogue = true;
        dIndex = 0;
        currentRegion = GameManager.instance.listenRegion;

        UpdatePositionAndSize();
        CompileDialogueSteps();

        visitCounter[currentRegion]++;
        
        StartCoroutine("DialogueSequence");
        
        animator.SetBool("visible", true);
    }

    public void PauseDialogue()
    {
        dialoguePaused = true;
        animator.SetBool("visible", false);
    }

    public void ResumeDialogue()
    {
        dialoguePaused = false;
        IncrementDialogueStep();
        animator.SetBool("visible", true);
    }

    void ExitDialogue()
    {
        dialogueText.text = "";
        inDialogue = false;
        StopCoroutine("DialogueSequence");
        animator.SetBool("visible", false);
        if(!endingStarted)
        {
            EventBroker.ExitDialogueCall();
            EventBroker.StopDialogueMusicCall();
        }
    }

    void UpdatePositionAndSize()
    {
        if (GameManager.instance.listenRegion == Region.centralCavern && pastLook)
            containerBG.localPosition = centralCavernPosition;
        else
            containerBG.localPosition = standardPosition;

        //container.sizeDelta = new Vector2(fontContainerWidth[currentRegion], container.sizeDelta.y);
        dialogueText.fontSize = fontSize[currentRegion];
    }

    void SwitchCavernBoxPosition()
    {
        pastLook = true;
        containerBG.localPosition = centralCavernPosition;
    }

    void CompileDialogueSteps()
    {
        currentVocal = null;
        noPlayVocal = false;
        noPlayMusic = false;
        currentDialogue.Clear();
        
        if (endingStarted)
            AddEndingDialogue();
        else
        {
            AddRegionDialogue();

            if (currentDialogue.Count.Equals(0))
            {
                noPlayMusic = true;;
                AddHintDialogue();
            }
        }

        if (!noPlayVocal && currentVocal == null)
            currentVocal = defaultVocal;
    }

    void UpdateDialogueAudio(RegionDialogueSection section)
    {
        if (section.noPlayMusic)
            noPlayMusic = true;

        if (section.noPlayVocal)
            noPlayVocal = true;
        
        if (currentVocal == null && !noPlayVocal && section.vocalClip != null)
            currentVocal = section.vocalClip;
    }

    void AddEndingDialogue()
    {
        foreach (var section in endingDialogue)
        {
            if (section.PassesAllTests(currentRegion, player.form, visitCounter[currentRegion], GameManager.instance.cellCount))
            {
                UpdateDialogueAudio(section);
                currentDialogue.AddRange(section.steps);
            }
        }
    }

    void AddRegionDialogue()
    {
        List<RegionDialogueSection> regionDialogueSections = GetRegionDialogueSections();

        foreach (var section in regionDialogueSections)
        {
            if (section.PassesAllTests(currentRegion, player.form, visitCounter[currentRegion], GameManager.instance.cellCount))
            {
                UpdateDialogueAudio(section);
                currentDialogue.AddRange(section.steps);
            }
        }            
    }

    List<RegionDialogueSection> GetRegionDialogueSections()
    {
        foreach (RegionDialogue regionDialogue in regionDialogues)
        {
            if (regionDialogue.region == currentRegion)
                return regionDialogue.sections;
        }

        return null;
    }

    void AddHintDialogue()
    {
        if (GameManager.instance.cellCount != GameManager.instance.maxCells)
        {
            // cells remain, and there is a region hint for this region, ADD
            AddRegionHint();
            
            // if upgrade available, give direction
            if (GameManager.instance.upgradeAvailable)
                AddUpgradeDirectionDialogue();

            // if any accessible cells remain, pick one at random and give hint
            AddCellHint();
        }
        else
        {
            if (!finalHintGiven)
                currentDialogue.AddRange(finalHints[0].steps);
            currentDialogue.AddRange(finalHints[1].steps);
        }
    }

    void AddRegionHint()
    {
        if (regionHintGiven.ContainsKey(currentRegion) && !regionHintGiven[currentRegion])
        {
            foreach (HintDialogueSection section in regionHints)
            {
                if (section.region == currentRegion)
                {
                    regionHintGiven[currentRegion] = true;
                    currentDialogue.AddRange(section.steps);
                    break;
                }
            }
        }
    }

    void AddCellHint()
    {
        UpdateAccessibleCells();
        if (accessibleCells.Count == 0)
            return;

        bool multipleCells;
        Region hintRegion = GetHintRegion(out multipleCells);
        AddCellHintToDialogue(hintRegion, multipleCells);
    }

    void AddCellHintToDialogue(Region hintRegion, bool multipleCells)
    {
        string cellRef1 = multipleCells ? "cells" : "a cell";
        string hereRef = hintRegion == currentRegion ? "here " : "";
        string cellRef2 = multipleCells ? "they" : "it";
        
        string text0 = "However, if you wish to\ncontinue your search";
        string text1 = "I sense " + cellRef1 + " remaining " + hereRef + "in the\n" + regionTerm[hintRegion];
        string text2 = "Even as you are now, " + cellRef2 + "\nshould be reachable";

        DialogueStep step0  = new DialogueStep(){isActionStep = false, text = text0};
        DialogueStep step1 = new DialogueStep(){isActionStep = false, text = text1};
        DialogueStep step2 = new DialogueStep(){isActionStep = false, text = text2};

        if (GameManager.instance.upgradeAvailable)
            currentDialogue.Add(step0);
        currentDialogue.Add(step1);
        currentDialogue.Add(step2);
    }

    void AddUpgradeDirectionDialogue()
    {
        string preposition = player.form == Form.glider ? "below" : "above";
        string action = player.form == Form.glider ? "Find" : "Listen for";
        string text1 = "I see your glow has become\neven brighter!";
        string text2 = action + " me " + preposition + " in\nthe " + regionTerm[upgradeLocationByForm[player.form]];

        DialogueStep step1 = new DialogueStep(){isActionStep = false, text = text1};
        DialogueStep step2 = new DialogueStep(){isActionStep = false, text = text2};

        currentDialogue.Add(step1);
        currentDialogue.Add(step2);
    }

    Region GetHintRegion(out bool multipleCells)
    {
        if (accessibleCells.ContainsKey(currentRegion))
        {
            multipleCells = accessibleCells[currentRegion] > 1 ? true : false;
            return currentRegion;
        }

        int randomIndex = UnityEngine.Random.Range(0, accessibleCells.Count);
        int i = 0;

        foreach (var kvp in accessibleCells)
        {
            if (i == randomIndex)
            {
                multipleCells = kvp.Value > 1 ? true : false;
                return kvp.Key;
            }
            i++;
        }

        Debug.LogError("Hint Region not found by index");
        multipleCells = true;
        return currentRegion;
    }

    void UpdateAccessibleCells()
    {
        accessibleCells.Clear();

        foreach (Cell cell in Cell.cellArray)
        {
            if (!cell.collected && player.form >= cell.formReq)
            {
                if (accessibleCells.ContainsKey(cell.location))
                    accessibleCells[cell.location]++;
                else
                    accessibleCells.Add(cell.location, 1);
            }
        }
    }

    IEnumerator DialogueSequence()
    {
        if (!noPlayVocal)
            deepAudio.PlayOneShot(currentVocal);

        if (!noPlayMusic)
            EventBroker.PlayDialogueMusicCall();

        ExecuteDialogueStep();
        yield return null;

        float timer = 0;
        while (inDialogue)
        {
            // Hold progression if paused
            while (dialoguePaused)
            {
                yield return null;
            }

            // Progress according to scroll or input
            if (autoscroll)
            {
                if (timer > scrollSpeed)
                {
                    timer = 0;
                    IncrementDialogueStep();
                }
                else
                    timer += Time.deltaTime;
            }
            else
            {
                if (DialogueInputReceived())
                {
                    if (fullTextDisplayed)
                        IncrementDialogueStep();
                    else
                    {
                        StopCoroutine("ScrollText");
                        dialogueText.text = currentText;
                        fullTextDisplayed = true;
                    }
                }
            }

            yield return null;
        }

        yield return null;
    }

    bool DialogueInputReceived()
    {
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.RightArrow))
            return true;
        else
            return false;
    }

    public void IncrementDialogueStep()
    {
        dIndex++;
        ExecuteDialogueStep();
    }

    void ExecuteDialogueStep()
    {
        if (dIndex >= currentDialogue.Count)
        {
            ExitDialogue();
        }
        else
        {
            if (currentDialogue[dIndex].isActionStep)
            {
                dialogueActions.actions[currentDialogue[dIndex].text]();
            }
            else
            {
                StopCoroutine("ScrollText");
                currentText = currentDialogue[dIndex].text;
                StartCoroutine("ScrollText");
            }
        }
    }

    IEnumerator ScrollText()
    {
        fullTextDisplayed = false;
        string visibleText = "";
        string hiddenText = currentText;

        foreach (char character in currentText)
        {

            visibleText += character;
            if (hiddenText.Length > 1)
                hiddenText = hiddenText.Substring(1);
            else
                hiddenText = "";

            dialogueText.text = "<color=#ffffffff>" + visibleText + "</color><color=#ffffff00>" + hiddenText + "</color>";
            yield return new WaitForSeconds(textScrollInterval);
        }
        dialogueText.text = currentText;
        fullTextDisplayed = true;
        yield return null;
    }

    void LookStart()
    {
        deepAudio.PlayOneShot(firstVocal);
    }


    void StartFinalDialogue()
    {
        endingStarted = true;
        autoscroll = true;
        promptArrow.color = new Color(1,1,1,0);
        EnterDialogue();
    }

    public void FontAutoSizeOn()
    {
        dialogueText.enableAutoSizing = true;
    }

    public void FontAutoSizeOff()
    {
        dialogueText.enableAutoSizing = false;
        dialogueText.fontSize = fontSize[currentRegion];
    }
}
