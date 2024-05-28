using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System;
using TMPro;
using Photon.Pun;

public class MultiSpin : MonoBehaviour
{
    #region Variable Declaration

    public int playerNum = 0;
    //Spinner variables
    [SerializeField]
    private GameObject spinner;
    private float spinSpeed = 0;
    private bool isSpinning;
    [SerializeField]private GameObject pivot;
    [SerializeField] private GameObject lid;

    [SerializeField] private bool isLidOpened = false;
    [SerializeField] private bool isMultispinCoroutine = false;
    [SerializeField] private bool isSpinnerTriggered = false;
    [SerializeField] private bool isSpinCoroutine = false;

    //Test tube position checking variables
    [SerializeField]
    private int defaultTubeAmount = 3;
    private int spinnerPosCount;
    [SerializeField] private bool[] testTubePlaceholder;
    private List<bool[]> correctArrangement;
    public bool isBalanced = false;

    //Explosion variables
    [SerializeField] private ParticleSystem explosion;

    [SerializeField]
    private TMP_Text debug, correctSequence;
    private string whichHasTestTube, currentSequence;
    PhotonView View;
    #endregion

    #region Unity Methods
    // Start is called before the first frame update
    void Start()
    {   
        InitGame();
    }

    // Update is called once per frame
    void Update()
    {
        //if (PhotonNetwork.IsConnected)
        //View.RPC("PhotonUpdate", RpcTarget.AllBuffered);
    }

    void InitGame() {
        View = GetComponent<PhotonView>();
        spinnerPosCount = spinner.transform.childCount;
        testTubePlaceholder = new bool[spinnerPosCount];
        TubeCorrectSequence(defaultTubeAmount);
        SetCorrectPlacingText();
    }

    [PunRPC]
    public void PhotonUpdate(){
        spinner.transform.Rotate(Vector3.forward * spinSpeed);
        lid.GetComponent<Rigidbody>().isKinematic = isMultispinCoroutine;
        CheckSpinning();
        CheckTestTubePos();
        TubeCorrectSequence(CheckTestTubeAmount());
        CheckSpinnerBalance();
        SetDebugText();
        SetCorrectPlacingText();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Hand") {
            View.RPC("PhotonSetLid", RpcTarget.AllBuffered);
            Debug.Log(other.gameObject.name + "Collides");
            isLidOpened = !isLidOpened;
           // PhotonOnTriggerEnter();
        }
    }
    [PunRPC]
    public void PhotonOnTriggerEnter()
    {
        if (!isMultispinCoroutine)
        {
          //StartCoroutine(MultiSpinSequence());
        }
    }

    [PunRPC]
    public void PhotonSetLid() {

        Debug.Log("--Lid Opens--" + isLidOpened);
        if (!isLidOpened)         
        {
            lid.transform.localEulerAngles = new Vector3(-165, 0, -90); //close

            //isSpinnerTriggered = true;

            if (!isSpinCoroutine)
            {
                StartCoroutine(ActivateSpinner());
            }

        }
        else                    
        {
            lid.transform.localEulerAngles = new Vector3(-270, 0, -90); //open

        }

       
    }

    #endregion

    #region Coroutine Sequence
    IEnumerator MultiSpinSequence()
    {
        isMultispinCoroutine = true;
        //yield return StartCoroutine(ActivateSpinner());
       // yield return StartCoroutine(LidTrigger());
        yield return StartCoroutine(ToggleTestTubeHold());
        //yield return StartCoroutine(ActivateSpinner());
        yield return new WaitForSeconds(3f);
        if (!isBalanced) { yield return StartCoroutine(Explode()); } //explode;
        isMultispinCoroutine = false;
        yield break;

    }
    #endregion


    #region Coroutine Methods
    IEnumerator ActivateSpinner()
    {
        isSpinCoroutine = true;
        Debug.Log("---Adjust Spin Speed");
        if (!isLidOpened && isSpinnerTriggered)     //Spinner should only accelerate/decelerate when the lid is closed; and when awoke (default not spinning)
        {
            if (!isSpinning)          //Not spinning: Accelerate
            {
                while (spinSpeed < 15)
                {
                    spinSpeed += .15f;
                    yield return null;
                }
            }
            else                     //Spinning: Decelerate
            {
                while (spinSpeed > 0)
                {
                    spinSpeed -= .15f;
                    yield return null;
                }
            }
        }
        isSpinnerTriggered = false;
        isSpinCoroutine = false;
        yield break;
    }

    IEnumerator LidTrigger()
    {

        Debug.Log("--Lid Opens--" + isLidOpened);
        if (isLidOpened)
        {
            lid.transform.localEulerAngles = new Vector3(-165, 0, -90); //close
        }
        else
        {
            lid.transform.localEulerAngles = new Vector3(-270, 0, -90); //open
        }
        yield break;
    }

    IEnumerator ToggleTestTubeHold()
    {
        lid.GetComponent<MeshCollider>().enabled = !isLidOpened;
        lid.GetComponent<MeshCollider>().convex = !isLidOpened;
        CapsuleCollider[] testTubeHolder = spinner.GetComponentsInChildren<CapsuleCollider>(true);
        for (int i = 0; i < testTubeHolder.Length; i++)
        {
            testTubeHolder[i].enabled = isLidOpened;
        }
        yield break;
    }

    IEnumerator Explode()
    {
        if (isSpinning && !isBalanced)
        {
            explosion.enableEmission = true;
            explosion.Play(true);
            yield return new WaitForSeconds(1);
            explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            explosion.enableEmission = false;
            yield return StartCoroutine(MultiSpinSequence());
        }
    }
    #endregion

    #region Self-defined Methods
    void TubeCorrectSequence(int tubeNumber)
    {
        correctArrangement = new List<bool[]>();
        bool[] correctPlacings = new bool[spinnerPosCount];
        switch (tubeNumber)
        {
            case 0:
                correctPlacings = new bool[spinnerPosCount];
                correctArrangement.Add(correctPlacings);
                break;
            case 2:
                correctPlacings = new bool[spinnerPosCount];
                correctPlacings[0] = correctPlacings[4] = true;
                correctArrangement.Add(correctPlacings);
                break;
            case 3:
                for (int i = 1; i <= 3; i++)
                {
                    correctPlacings = new bool[spinnerPosCount];
                    correctPlacings[0] = true;
                    if (i <= 2) { correctPlacings[3] = true; }
                    else { correctPlacings[2] = true; }
                    if (i == 2) { correctPlacings[6] = true; }
                    else { correctPlacings[5] = true; }
                    correctArrangement.Add(correctPlacings);
                }
                break;
            case 4:
                correctPlacings = new bool[spinnerPosCount];
                for (int i = 0; i < spinnerPosCount; i++)
                {
                    if (i % 2 == 0) { correctPlacings[i] = true; }
                    else { correctPlacings[i] = false; }
                }
                correctArrangement.Add(correctPlacings);
                break;
            case 5:
                for (int i = 1; i <= 3; i++)
                {
                    correctPlacings = new bool[spinnerPosCount];
                    correctPlacings[0] = correctPlacings[3] = correctPlacings[6] = true;
                    if (i <= 2) { correctPlacings[1] = true; }
                    else { correctPlacings[2] = true; }
                    if (i >= 2) { correctPlacings[5] = true; }
                    else { correctPlacings[4] = true; }
                    correctArrangement.Add(correctPlacings);
                }
                break;
            case 6:
                for (int i = 1; i <= 3; i++)
                {
                    correctPlacings = new bool[spinnerPosCount];
                    correctPlacings[i] = correctPlacings[i + 4] = true;
                    for (int j = 0; j < spinnerPosCount; j++) { correctPlacings[j] = !correctPlacings[j]; }
                    correctArrangement.Add(correctPlacings);
                }
                break;
            default:
                break;
        }
    }

    void CheckSpinning()
    {
        if (spinSpeed > 0)
        {
            isSpinning = true;
        }
        else
        {
            isSpinning = false;
        }
    }

    int CheckTestTubeAmount()
    {
        int value = 0;
        for (int i = 0; i < testTubePlaceholder.Length; i++)
        {
            if (testTubePlaceholder[i]) { value += 1; }
        }
        return value;
    }

    void CheckTestTubePos()
    {
        Transform[] testTubePos = new Transform[spinnerPosCount];
        testTubePlaceholder = new bool[spinnerPosCount];
        for (int i = 0; i < spinnerPosCount; i++)
        {
            Transform testTubeToCheck = spinner.transform.GetChild(i);
            if (testTubeToCheck.GetComponent<MultiSpinTestTubeLock>().isOccupied == true)
            {
                for (int j = 0; j < spinnerPosCount; j++)
                {
                    Transform currentTestTube = spinner.transform.GetChild((i + j) % spinnerPosCount);
                    testTubePos[j] = currentTestTube;
                    testTubePlaceholder[j] = currentTestTube.GetComponent<MultiSpinTestTubeLock>().isOccupied;
                }
                break;
            }
        }
    }

    void CheckSpinnerBalance()
    {
        if (!isLidOpened)
        {
            isBalanced = false;
            for (int i = 0; i < correctArrangement.Count; i++)
            {
                if (CompareBooleanArrays(correctArrangement[i], testTubePlaceholder))
                {
                    isBalanced = true;
                    break;
                }
            }
        }
    }

    void SetCorrectPlacingText()
    {
        currentSequence = "Correct Sequence(s):\n";
        for (int i = 0; i < correctArrangement.Count; i++)
        {
            string sequence = "Sequence " + (i+1) + ":\n";
            for (int j = 0; j < correctArrangement[i].Length; j++)
            {
                sequence += "TestTube " + j + ": " + correctArrangement[i][j].ToString() + "\n";
            }
            currentSequence += sequence;
        }
       // correctSequence.text = currentSequence;
    }

    void SetDebugText()
    {
        whichHasTestTube = "";
        for (int i = 0; i < spinnerPosCount; i++)
        {
            whichHasTestTube += "TestTube " + i + ": " + testTubePlaceholder[i].ToString() + "\n";
        }
        String text = "Multispin variables:\n" +
            "isLidOpened: " + isLidOpened.ToString() + "\n" +
            "isMultispinCoroutine: " + isMultispinCoroutine.ToString() + "\n" +
            "isSpinnerTriggered: " + isSpinnerTriggered.ToString() + "\n" +
            "TestTube in machine: " + CheckTestTubeAmount().ToString() + "\n" +
            "Which holes has test tube? \n" + whichHasTestTube + "\n" +
            "Is the test tubes balanced? " + isBalanced.ToString();

        Debug.Log(text);
    }
    #endregion

    #region Intermediate Methods (private)
    private bool CompareBooleanArrays(bool[] boolA, bool[] boolB)
    {
        if (boolA.Length != boolB.Length) return false;
        else
        {
            for (int i = 0; i < boolA.Length; i++)
            {
                if (boolA[i] != boolB[i]) return false;
            }
            return true;
        }
    }
    #endregion
}
