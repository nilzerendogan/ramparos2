using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DrawServiceRealTime: MonoBehaviour
{
    public OVRHand hand;
    public LineRenderer lineRenderer;
    private Color lineColor = Color.magenta;
    private float lineWidth = 0.015f;
    public PlanRequestGeneratorRealTime planRequestGeneratorRealTime;
    public HandOrientation HandOrientation;
    public TMP_Dropdown recordOrientationDropdown;
    public HandOrientation handOrientation;
    public TrainAndTest trainAndTest;
    public GameObject bar;
    public GameObject sliderPosition;
    public GameObject loadingText;
    public GameObject playButton;
    public GameObject pauseButton;
    public Button backButton;
    public Button nextButton;
    public Button redrawButton;
    public Button addToTrainingButton;
    public GameObject executeOnRealRobotButton;


    public Button addContextButton;
    public GameObject contextPrefab;
    private GameObject obstacle;
    public Button[] contextMenu;
    private bool isContextual = false;

    public TMP_Text debugText;

    private State state;
    // private float threshold = 0.01f;

    private int WAY_POINT_FREQ = 4 ;

    public GameObject collisionWarning;
    private bool collisionDetectedinTrajectory = false;

    public GameObject collisionIndicatorPrefab;
    private List<GameObject> collisionIndicators = new List<GameObject>();

    [Header("RAMPA Anti-Drift Configuration")]
    public Transform robotBaseTransform; // Unity Hierarchy'deki link_base buraya sürüklenecek



// TODO - record hand orientation as well


    private void Start() {
        
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // Çizgiyi dünya yerine yerel (local) uzayda çizdiriyoruz
        lineRenderer.useWorldSpace = false;

        // Çizgi objesini hiyerarşide robot tabanının altına bağlayarak sabitleşmesini sağlıyoruz
        if (robotBaseTransform != null)
        {
            lineRenderer.transform.SetParent(robotBaseTransform, true);
            lineRenderer.transform.localPosition = Vector3.zero;
            lineRenderer.transform.localRotation = Quaternion.identity;
        }

        loadingText.GetComponent<TMP_Text>().text = "pinch to start drawing";

        executeOnRealRobotButton.SetActive(false);

        ResetDrawingState();
    }

    public void HandleAddContextButton() {
        if (!isContextual) {
            isContextual = true;
            foreach (var button in contextMenu)
            {
                button.interactable = true;
            }
            obstacle = Instantiate(contextPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            addContextButton.GetComponentInChildren<TMP_Text>().text = "remove context";
        }
        else {
            isContextual = false;
            Destroy(obstacle);
            foreach (var button in contextMenu)
            {
                button.interactable = false;
            }
            addContextButton.GetComponentInChildren<TMP_Text>().text = "add context";
        }
    }

    public void IncXScale() {
        obstacle.transform.localScale += new Vector3(0.02f, 0, 0);
    }
    public void DecXScale() {
        if (obstacle.transform.localScale.x > 0.03f)
            obstacle.transform.localScale -= new Vector3(0.02f, 0, 0);
        
    }
    public void IncYScale() {
        obstacle.transform.localScale += new Vector3(0, 0.02f, 0);
    }
    public void DecYScale() {
        if (obstacle.transform.localScale.y > 0.03f)
            obstacle.transform.localScale -= new Vector3(0, 0.02f, 0);
    }
    public void IncZScale() {
        obstacle.transform.localScale += new Vector3(0, 0, 0.02f);
    }
    public void DecZScale() {
        if (obstacle.transform.localScale.z > 0.03f)
            obstacle.transform.localScale -= new Vector3(0, 0, 0.02f);
    }

    IEnumerator DrawTrajectory(float interval)
    {
        int numberOfPoints = lineRenderer.positionCount;
        bool isFirstPart = true;
        bool isPinching = false;
        loadingText.GetComponent<TMP_Text>().text = "pinch to start drawing";

        while (true)
        {
            if (recordOrientationDropdown.value == 0)
                isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            else
                isPinching = OVRInput.Get(OVRInput.Button.One);

            if (isPinching && isFirstPart)
            {
                isFirstPart = false;
                addContextButton.interactable = false;
                loadingText.GetComponent<TMP_Text>().text = "drawing trajectory";
                if (recordOrientationDropdown.value != 0)
                    handOrientation.ShowIndicator(true);
            }

            // Pinch bırakıldı -> her frame kontrol et, modulo şartı YOK
            if (!isPinching && !isFirstPart)
            {
                if (!planRequestGeneratorRealTime.HasPendingWork())
                {
                    if (recordOrientationDropdown.value != 0)
                        handOrientation.ShowIndicator(false);
                    UpdateDrawingState();
                    break;
                }
                else
                {
                    loadingText.GetComponent<TMP_Text>().text = "finishing trajectory...";
                }

                yield return new WaitForSeconds(interval);
                continue;
            }

            // Hâlâ pinch ediliyorsa nokta ekle (WAY_POINT_FREQ throttling burada kalabilir)
            if (isPinching && !isFirstPart)
            {
                Vector3 localHandPos = robotBaseTransform != null ?
                    robotBaseTransform.InverseTransformPoint(hand.PointerPose.position) : hand.PointerPose.position;

                if (numberOfPoints % WAY_POINT_FREQ == 0)
                {
                    if (recordOrientationDropdown.value == 1)
                    {
                        handOrientation.UpdateHandOrientationIndicator(localHandPos, localHandPos);
                        Quaternion worldOrientation = handOrientation.GetRotation();
                        Quaternion localOrientation = robotBaseTransform != null ?
                            Quaternion.Inverse(robotBaseTransform.rotation) * worldOrientation : worldOrientation;

                        double[] poseInfo = {localHandPos.x, localHandPos.y, localHandPos.z, localOrientation.x, localOrientation.y, localOrientation.z, localOrientation.w};
                        planRequestGeneratorRealTime.AddRequestToQueue(poseInfo);
                    }
                    else
                    {
                        debugText.text += "adding point with fixed orientation\n";
                        Quaternion worldOrientation = Quaternion.Euler(180, 0, 0);
                        Quaternion localOrientation = robotBaseTransform != null ?
                            Quaternion.Inverse(robotBaseTransform.rotation) * worldOrientation : worldOrientation;

                        double[] poseInfo = {localHandPos.x, localHandPos.y, localHandPos.z, localOrientation.x, localOrientation.y, localOrientation.z, localOrientation.w};
                        planRequestGeneratorRealTime.AddRequestToQueue(poseInfo);
                    }
                }

                numberOfPoints++;
                lineRenderer.positionCount = numberOfPoints;
                lineRenderer.SetPosition(numberOfPoints - 1, localHandPos);
            }

            yield return new WaitForSeconds(interval);
        }
    }
    
    public void UpdateDrawingState(bool finalized = false)
    {
        switch (state)
        {
            case State.Initial:
                state = State.DrawTrajectory;
                lineRenderer.positionCount = 0;
                
                handleMenu(true);
                foreach (var button in contextMenu)
                {
                    button.interactable = false;
                }
                executeOnRealRobotButton.SetActive(false);
            
                StartCoroutine(DrawTrajectory(0.05f));
                break;

            case State.DrawTrajectory:
                if (finalized) {
                    loadingText.GetComponent<TMP_Text>().text = "no solution found";
                    ResetDrawingState(true);
                }
                else {
                    state = State.InspectTrajectory;
                    planRequestGeneratorRealTime.SetJointAnglesForRealRobot();
                    executeOnRealRobotButton.SetActive(true);
                    planRequestGeneratorRealTime.SetCurrentIndexPointer();
                    handleMenu(false);
                }
                
                
                break;
            case State.InspectTrajectory:
                if (finalized)
                {
                    // add to training is clicked
                    ResetDrawingState(true);
                }
                else
                {

                    // enters here when the user clicks on "redraw from current waypoint" button
                    state = State.DrawTrajectory;
                    
                    handleMenu(true);
                    redrawButton.interactable = false;

                    //empty queue
                    planRequestGeneratorRealTime.EmptyQueue();
                    /*
                    double remainingPointsRate = (double) planRequestGeneratorRealTime.currentIndexPointer  / planRequestGeneratorRealTime.previousPoints.Count;
                    int remainingPoints = (int)Math.Floor(lineRenderer.positionCount * remainingPointsRate);    
                    */
                    int remainingPoints = planRequestGeneratorRealTime.currentIndexPointer * WAY_POINT_FREQ;
                    Vector3[] newPositions = new Vector3[remainingPoints];
                    for (int i = 0; i < remainingPoints; i++)
                    {
                        newPositions[i] = lineRenderer.GetPosition(i);
                    }

                    lineRenderer.positionCount = remainingPoints;
                    lineRenderer.SetPositions(newPositions);
                    planRequestGeneratorRealTime.previousPoints =
                        planRequestGeneratorRealTime.previousPoints.GetRange(0,
                            planRequestGeneratorRealTime.currentIndexPointer);

                    planRequestGeneratorRealTime.previousPoses =
                        planRequestGeneratorRealTime.previousPoses.GetRange(0,
                            planRequestGeneratorRealTime.currentIndexPointer);

                    StartCoroutine(DrawTrajectory(0.05f));
                    
                }

                break;
        }
        
    }


    public void ResetDrawingState(bool anotherTrajectory = false)
    {
        state = State.Initial;

        planRequestGeneratorRealTime.ResetGenerator();
        lineRenderer.positionCount = 0;

        collisionDetectedinTrajectory = false;
        collisionWarning.SetActive(false);
        
        if (!anotherTrajectory) {
            if (isContextual) {
                Destroy(obstacle);
            }
            foreach (var button in contextMenu)
            {
                button.interactable = false;
            }
            isContextual = false;
            addContextButton.GetComponentInChildren<TMP_Text>().text = "add context";
        }
        

        StopAllCoroutines();

        // set the buttons to be non-interactable in the initial state
        playButton.GetComponent<Button>().interactable = false;
        pauseButton.SetActive(false);
        backButton.interactable = false;
        nextButton.interactable = false;
        redrawButton.interactable = false;
        addToTrainingButton.interactable = false;
        planRequestGeneratorRealTime.PrevRecordedTrajectories.SetInteractable(true);
        addContextButton.interactable = true;
        
        


        // also reset the slider handle position to middle
        Vector3 currRectTransform = sliderPosition.GetComponent<RectTransform>().anchoredPosition;
        currRectTransform.x = 0;
        sliderPosition.GetComponent<RectTransform>().anchoredPosition = currRectTransform;
    }

    public void SendTrainingData()
    {
        if (isContextual)
            trainAndTest.SendTrainingData(planRequestGeneratorRealTime.previousPoses, planRequestGeneratorRealTime.previousOrientations, obstacle.transform.localScale.y);
        else 
            trainAndTest.SendTrainingData(planRequestGeneratorRealTime.previousPoses, planRequestGeneratorRealTime.previousOrientations);
        planRequestGeneratorRealTime.ResetGenerator(true);
        UpdateDrawingState(true);
    }
    private enum State
    {
        Initial,
        DrawTrajectory,
        InspectTrajectory
    }


    // handle the bar and the loading text while drawing and inspecting the trajectory
    private void handleMenu(bool loading)
    {

        if (collisionDetectedinTrajectory) {
            collisionWarning.SetActive(true);
        }
        bar.SetActive(!loading);
        loadingText.SetActive(loading);
        planRequestGeneratorRealTime.PrevRecordedTrajectories.SetInteractable(!loading);
        
        redrawButton.interactable = !loading;
        backButton.interactable = !loading;
        addToTrainingButton.interactable = !loading;

        

        if (!loading) {
            // set slider position to end of bar
            Vector3 currRectTransform = sliderPosition.GetComponent<RectTransform>().anchoredPosition;
            currRectTransform.x = bar.GetComponent<RectTransform>().sizeDelta.x / 2;
            sliderPosition.GetComponent<RectTransform>().anchoredPosition = currRectTransform;
        }
    
    }

    public void SetCollisionDetected(Vector3 contactPoint)
    {
        if (state == State.DrawTrajectory) {
            collisionDetectedinTrajectory = true;
        }
        GameObject collisionIndicator = Instantiate(collisionIndicatorPrefab, contactPoint, Quaternion.identity);
        collisionIndicators.Add(collisionIndicator);
    }

    public bool isStateDrawTrajectory()
    {
        return state == State.DrawTrajectory;
    }
}