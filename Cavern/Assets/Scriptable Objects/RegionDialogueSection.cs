using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Test {pass, fail, less, lessEqual, equal, greaterEqual, greater}

[CreateAssetMenu]
public class RegionDialogueSection : ScriptableObject
{
    [SerializeField] int dIndex;
    public bool noPlayMusic;
    public bool noPlayVocal;
    public AudioClip vocalClip;

    [SerializeField] Test regionTest;
    [SerializeField] Region regionRef;

    [SerializeField] Test formTest;
    [SerializeField] Form formRef;

    [SerializeField] Test visitTest;
    [SerializeField] int visitRef;
    
    [SerializeField] Test cellTest;
    [SerializeField] int cellRef;

    public List<DialogueStep> steps = new List<DialogueStep>();


    public bool PassesAllTests(Region currentRegion, Form currentForm, int currentVisitNum, int currentCells)
    {
        if (TestRegion(currentRegion, regionTest, regionRef) && TestForm(currentForm, formTest, formRef) &&
            TestInt(currentVisitNum, visitTest, visitRef) && TestInt(currentCells, cellTest, cellRef))
            return true;
        else
            return false;
    }

    bool TestRegion(Region testRegion, Test testType, Region refRegion)
    {
        switch (testType)
        {
            case Test.pass:
            {
                return true;
            }

            case Test.fail:
            {
                return false;
            }

            case Test.less:
            {
                return testRegion < refRegion;
            }

            case Test.lessEqual:
            {
                return testRegion <= refRegion;
            }

            case Test.equal:
            {
                return testRegion == refRegion;
            }

            case Test.greaterEqual:
            {
                return testRegion >= refRegion;
            }

            case Test.greater:
            {
                return testRegion > refRegion;
            }

            default:
            {
                Debug.LogError("test used invalid enum");
                return false;
            }
        }
    }

    bool TestForm(Form testForm, Test testType, Form refForm)
    {
        switch (testType)
        {
            case Test.pass:
            {
                return true;
            }

            case Test.fail:
            {
                return false;
            }

            case Test.less:
            {
                return testForm < refForm;
            }

            case Test.lessEqual:
            {
                return testForm <= refForm;
            }

            case Test.equal:
            {
                return testForm == refForm;
            }

            case Test.greaterEqual:
            {
                return testForm >= refForm;
            }

            case Test.greater:
            {
                return testForm > refForm;
            }

            default:
            {
                Debug.LogError("test used invalid enum");
                return false;
            }
        }
    }


    bool TestInt(int testInt, Test testType, int refInt)
    {
        switch (testType)
        {
            case Test.pass:
            {
                return true;
            }

            case Test.fail:
            {
                return false;
            }

            case Test.less:
            {
                return testInt < refInt;
            }

            case Test.lessEqual:
            {
                return testInt <= refInt;
            }

            case Test.equal:
            {
                return testInt == refInt;
            }

            case Test.greaterEqual:
            {
                return testInt >= refInt;
            }

            case Test.greater:
            {
                return testInt > refInt;
            }

            default:
            {
                Debug.LogError("test used invalid enum");
                return false;
            }
        }
    }
}
