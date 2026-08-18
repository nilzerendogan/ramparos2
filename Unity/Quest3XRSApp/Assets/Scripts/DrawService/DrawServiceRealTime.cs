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

    [Header("Resume-Drawing Anchor (fixes 'line to feet' on redraw)")]
    // How close (meters, in robot-base local space) the hand must get to the
    // last waypoint before a resumed stroke is allowed to start recording.
    public float resumeSnapTolerance = 0.03f;
    // Optional: a small visual marker (e.g. a sphere) shown at the last
    // waypoint while waiting for the hand to return to it. Can be left null.
    public GameObject resumeAnchorMarker;



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

    // ---------------------------------------------------------------
    // FIX: the frame where the pinch/button is released fell straight
    // into the "!isPinching" branch, which only ever checks pending-queue
    // drain state and breaks -- it never enqueued the hand's position at
    // the moment of release. So the exact release point (the true end of
    // the stroke) was silently dropped, and the arm stopped just short of
    // it (a smaller version of the same issue fixed in the "poses" drawing
    // script, since here sampling already happens every tick with no
    // throttle -- but the release-edge point itself was still never sent).
    //
    // Now: a rising->falling edge on isPinching triggers one final
    // AddCurrentPointToQueueAndLine() call before we start waiting for the
    // queue to drain, so the last hand position is always included.
    // ---------------------------------------------------------------
    IEnumerator DrawTrajectory(float interval, Vector3? resumeAnchor = null)
    {
        int numberOfPoints = lineRenderer.positionCount;
        bool isFirstPart = true;
        bool isPinching = false;
        bool wasPinching = false;
        loadingText.GetComponent<TMP_Text>().text = resumeAnchor.HasValue
            ? "move hand to the marked point to continue"
            : "pinch to start drawing";

        if (resumeAnchor.HasValue && resumeAnchorMarker != null)
        {
            resumeAnchorMarker.SetActive(true);
            resumeAnchorMarker.transform.localPosition = resumeAnchor.Value;
        }

        while (true)
        {
            if (recordOrientationDropdown.value == 0)
                isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            else
                isPinching = OVRInput.Get(OVRInput.Button.One);

            // --- Resume gate: don't let a pinch start recording until the hand
            // is physically back near the last waypoint. Without this, whatever
            // the hand's resting position happens to be (often far away) becomes
            // the very next target -- an unreachable jump that gets reported as
            // "no solution found".
            if (isPinching && isFirstPart && resumeAnchor.HasValue)
            {
                Vector3 candidatePos = robotBaseTransform != null
                    ? robotBaseTransform.InverseTransformPoint(hand.PointerPose.position)
                    : hand.PointerPose.position;

                if (Vector3.Distance(candidatePos, resumeAnchor.Value) > resumeSnapTolerance)
                {
                    loadingText.GetComponent<TMP_Text>().text = "move hand to the marked point to continue";
                    yield return new WaitForSeconds(interval);
                    continue;
                }

                if (resumeAnchorMarker != null)
                {
                    resumeAnchorMarker.SetActive(false);
                }
            }

            if (isPinching && isFirstPart)
            {
                isFirstPart = false;
                addContextButton.interactable = false;
                loadingText.GetComponent<TMP_Text>().text = "drawing trajectory";
                if (recordOrientationDropdown.value != 0)
                    handOrientation.ShowIndicator(true);
            }

            // --- Release edge: capture the exact release position exactly
            // once, before we fall into the "waiting for queue to drain" loop.
            if (wasPinching && !isPinching && !isFirstPart)
            {
                numberOfPoints = AddCurrentPointToQueueAndLine(numberOfPoints);
            }

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

                wasPinching = isPinching;
                yield return new WaitForSeconds(interval);
                continue;
            }

            // --- TEK BLOK: hedef güncelleme + çizgi çizimi ---
            if (isPinching && !isFirstPart)
            {
                numberOfPoints = AddCurrentPointToQueueAndLine(numberOfPoints);
            }

            wasPinching = isPinching;
            yield return new WaitForSeconds(interval);
        }
    }

    // Factored out from the loop body so the release-edge case and the
    // normal per-tick case can both guarantee the current hand position is
    // enqueued and drawn identically, with no throttle or gap between them.
    private int AddCurrentPointToQueueAndLine(int numberOfPoints)
    {
        Vector3 localHandPos = robotBaseTransform != null ?
            robotBaseTransform.InverseTransformPoint(hand.PointerPose.position) : hand.PointerPose.position;

        Quaternion localOrientation;
        if (recordOrientationDropdown.value == 1)
        {
            handOrientation.UpdateHandOrientationIndicator(localHandPos, localHandPos);
            Quaternion worldOrientation = handOrientation.GetRotation();
            localOrientation = robotBaseTransform != null ?
                Quaternion.Inverse(robotBaseTransform.rotation) * worldOrientation : worldOrientation;
        }
        else
        {
            Quaternion worldOrientation = Quaternion.Euler(180, 0, 0);
            localOrientation = robotBaseTransform != null ?
                Quaternion.Inverse(robotBaseTransform.rotation) * worldOrientation : worldOrientation;
        }

        double[] poseInfo = {
            localHandPos.x, localHandPos.y, localHandPos.z,
            localOrientation.x, localOrientation.y, localOrientation.z, localOrientation.w
        };
        planRequestGeneratorRealTime.AddRequestToQueue(poseInfo);

        numberOfPoints++;
        lineRenderer.positionCount = numberOfPoints;
        lineRenderer.SetPosition(numberOfPoints - 1, localHandPos);

        return numberOfPoints;
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

                    // Anchor the resumed stroke to the last kept waypoint so the
                    // coroutine can gate on the hand physically returning there
                    // before it starts recording new points (see DrawTrajectory).
                    Vector3? resumeAnchor = remainingPoints > 0
                        ? (Vector3?)lineRenderer.GetPosition(remainingPoints - 1)
                        : null;

                    StartCoroutine(DrawTrajectory(0.05f, resumeAnchor));
                    
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