using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Ur10Mover;
using UnityEngine;
using RosMessageTypes.Geometry;
using UnityEngine.UI;
using TMPro;

public class PlanRequestGeneratorRealTime : MonoBehaviour
{
    const float k_JointAssignmentWait = 0.05f;

    public DrawServiceRealTime DrawServiceRealTime;

    public TMP_Dropdown recordOrientationDropdown;
    public RealRobotCommunication RealRobotCommunication;
    public TrajectoryHelperFunctions HelperFunctions;
    public TrajectoryPlanner TrajectoryPlanner;
    public PrevRecordedTrajectories PrevRecordedTrajectories;
    private Queue<double[]> requestQueue = new Queue<double[]>();
    private bool waitingForResponse = false;

    // Seyrek liste: sadece her trajectory segmentinin SON noktası.
    // Gerçek robota gönderilecek waypoint listesi bu.
    public List<double[]> previousPoints = new List<double[]>();

    // Yoğun liste: her trajectory'deki TÜM ara noktalar.
    // Replay / scrub / slider (GetOnePointNext, GetOnePointBack, PlayRestOfTrajectory)
    // bu liste üzerinden çalışır, böylece zıplama olmaz.
    public List<double[]> previousPointsDense = new List<double[]>();

    public List<Vector3> previousPoses = new List<Vector3>();
    public List<Quaternion> previousOrientations = new List<Quaternion>();
    public RealRobotCommunication realRobotCommunication;
    
    private double[] jointConfig;
    public int currentIndexPointer = 0;

    // Forward/rewind butonlarının kaç yoğun nokta atlayacağı.
    // Dense liste eklenince tek nokta atlamak gözle görülmez oldu; bunu
    // Inspector'dan (veya burada) ihtiyaca göre ayarla (örn. 10-30 arası).
    public int stepSize = 15;

    public Button backButton;
    public Button nextButton;

    // bar to show the current position while inspecting trajectory
    public GameObject sliderPosition;
    public GameObject bar;

    // buttons for play/pause trajectory
    public GameObject playButton;
    public GameObject pauseButton;

    public GameObject executeOnRealRobotButton;


    // no need
    /*
    public Button drawButton;
    public Button trainButton;
    public Button testButton;
    */

    public void Start()
    {
        jointConfig = HelperFunctions.CurrentJointConfig();
        StartCoroutine(ProcessRequests());

    }

    public bool HasPendingWork()
    {
        return waitingForResponse || requestQueue.Count > 0;
    }

    public void AddRequestToQueue(double[] poseInfo)
    {
        Debug.LogWarning("target added " + poseInfo);
        requestQueue.Enqueue(poseInfo);
    } 
    private IEnumerator ProcessRequests()
    {
        while (true)
        {
            if (requestQueue.Count > 0 && !waitingForResponse)
            {
                waitingForResponse = true;
                double[] poseInfo = requestQueue.Dequeue();
                Vector3 pose = new Vector3((float)poseInfo[0], (float)poseInfo[1], (float)poseInfo[2]);
                Quaternion orientation = new Quaternion((float)poseInfo[3], (float)poseInfo[4], (float)poseInfo[5], (float)poseInfo[6]);
                Debug.LogWarning("target popped " + pose);
                GenerateRequest(pose, orientation);
            }
            yield return new WaitForSeconds(0.1f);
        }
        
    }

    private void GenerateRequest(Vector3 pose, Quaternion orientation)
    {
        var request = new PlannerServiceRequest();
        request.request_type = "realTime";
        if (recordOrientationDropdown.value == 0) {
            request.input_msg = "down";
        }
        else if (recordOrientationDropdown.value == 3) {
            request.input_msg = "hook";
        }
        request.joints_input = jointConfig;

        previousPoses.Add(pose);

        // orientation = Quaternion.Euler(180, 0,0);
        previousOrientations.Add(orientation);
        
        PoseMsg[] pose_list = new PoseMsg[1];
        pose_list[0] = HelperFunctions.GeneratePoseMsg(pose, orientation);
        request.pose_list = pose_list;
        Debug.LogWarning("Request Sent");
        Debug.LogWarning(request);
        TrajectoryPlanner.SendRequest(request);
    } 
    
    public void ProcessResponse(PlannerServiceResponse response)
    {
        if (response.output_msg == "Timeout")
        {
            waitingForResponse = false;   // <-- ekleyin, state'e bakılmaksızın kilidi açsın
            if (DrawServiceRealTime.isStateDrawTrajectory())
                DrawServiceRealTime.UpdateDrawingState(true);
        }
        else {
            jointConfig = response.trajectories[0].joint_trajectory.points.Last().positions;
            StartCoroutine(ExecuteTrajectories(response));
        }
    }
    
    /*
    IEnumerator ExecuteTrajectories(PlannerServiceResponse response)
    {

        // For every trajectory plan returned
        for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
        {
            var lastPoint = response.trajectories[poseIndex].joint_trajectory.points.Last();
            // For every robot pose in trajectory plan
            foreach (var t in response.trajectories[poseIndex].joint_trajectory.points)
            {
                if (t == lastPoint)
                {
                    previousPoints.Add(HelperFunctions.GetJointAngles(t));
                }

                HelperFunctions.SetJointAngles(t);
               
                yield return new WaitForSeconds(k_JointAssignmentWait);
                waitingForResponse = false;
            }
        }
        
    }
    */

    IEnumerator ExecuteTrajectories(PlannerServiceResponse response)
    {
        for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
        {
            var lastPoint = response.trajectories[poseIndex].joint_trajectory.points.Last();
            foreach (var t in response.trajectories[poseIndex].joint_trajectory.points)
            {
                if (t == lastPoint) previousPoints.Add(HelperFunctions.GetJointAngles(t));

                // Her ara noktayı da yoğun listeye ekle, böylece replay/scrub
                // ilk hareketteki kadar akıcı olur.
                previousPointsDense.Add(HelperFunctions.GetJointAngles(t));

                HelperFunctions.SetJointAngles(t);
                yield return new WaitForSeconds(k_JointAssignmentWait);
            }
        }
        waitingForResponse = false;   // döngülerin tamamen dışında, sadece bir kez
    }
    
    IEnumerator ExecuteTrajectory(double[] trajectory)
    {
        HelperFunctions.SetSliders(trajectory);
        yield return new WaitForSeconds(k_JointAssignmentWait);
    }
    
    
    public void SetJointAnglesForRealRobot() {
        // Gerçek robota hâlâ seyrek (waypoint) listesi gönderiliyor.
        realRobotCommunication.setJointAngles(previousPoints);
    }
    

    public void ResetGenerator(bool addToTrainingSet = false)
    {

        if (addToTrainingSet) {
            // store the current trajectory
            if (previousPoints.Count > 0) {
                // realRobotCommunication.setJointAngles(previousPoints);
                PrevRecordedTrajectories.AddTrajectory(previousPoses, previousOrientations);
                // executeOnRealRobotButton.SetActive(true);
            }

            // handle show-traj buttons
            PrevRecordedTrajectories.HandleButtons();
        }


        //why?
        jointConfig = HelperFunctions.CurrentJointConfig();

        waitingForResponse = false;
        requestQueue.Clear();

        previousPoints.Clear();
        previousPointsDense.Clear();
        previousPoses.Clear();
        previousOrientations.Clear();

        currentIndexPointer = 0;

    }


    public void GetOnePointBack()
    {
        
        currentIndexPointer = Mathf.Max(currentIndexPointer - stepSize, 0);
        
        nextButton.interactable = true;
        playButton.GetComponent<Button>().interactable = true;

        if (currentIndexPointer <= 0)
        {
            backButton.interactable = false;
        }

        UpdateSliderHandle();
            
        StartCoroutine(ExecuteTrajectory(previousPointsDense[currentIndexPointer]));
    }
    
    public void GetOnePointNext()
    {
        currentIndexPointer = Mathf.Min(currentIndexPointer + stepSize, previousPointsDense.Count - 1);
        backButton.interactable = true;
        if (currentIndexPointer >= previousPointsDense.Count - 1)
        {
            playButton.GetComponent<Button>().interactable = false;
            nextButton.interactable = false;
        }

        UpdateSliderHandle();

        StartCoroutine(ExecuteTrajectory(previousPointsDense[currentIndexPointer]));

    }

    // coroutine to play the rest of the trajectory
    IEnumerator PlayRestOfTrajectoryCoroutine() {

        playButton.SetActive(false);
        pauseButton.SetActive(true);

        backButton.interactable = false;
        nextButton.interactable = false;

        for (; currentIndexPointer < previousPointsDense.Count - 1; currentIndexPointer++){

            StartCoroutine(ExecuteTrajectory(previousPointsDense[currentIndexPointer+1]));

            UpdateSliderHandle();

            yield return new WaitForSeconds(k_JointAssignmentWait);
        }

        UpdateSliderHandle();

        playButton.SetActive(true);
        playButton.GetComponent<Button>().interactable = false;
        pauseButton.SetActive(false);
        backButton.interactable = true;
        nextButton.interactable = false;

    }

    public void PlayRestOfTrajectory() {
        StartCoroutine(PlayRestOfTrajectoryCoroutine());
    }

    public void PauseTrajectory() {
        StopAllCoroutines();
        UpdateSliderHandle();
        playButton.SetActive(true);
        pauseButton.SetActive(false);

        if (currentIndexPointer < previousPointsDense.Count - 1) {
            playButton.GetComponent<Button>().interactable = true;
            nextButton.interactable = true;
        }
        else {
            playButton.GetComponent<Button>().interactable = false;
        }
        backButton.interactable = true;
    }




    public void SetCurrentIndexPointer()
    {
        currentIndexPointer = previousPointsDense.Count - 1;
    }

    public void EmptyQueue()
    {
        requestQueue.Clear();
    }

    public bool isWaitingForResponse()
    {
        return waitingForResponse;
    }

    private void UpdateSliderHandle() {
        Vector3 currRectTransform = sliderPosition.GetComponent<RectTransform>().anchoredPosition;
        currRectTransform.x = 
            (bar.GetComponent<RectTransform>().sizeDelta.x) * (currentIndexPointer / ((float)previousPointsDense.Count - 1)) - bar.GetComponent<RectTransform>().sizeDelta.x / 2;
        sliderPosition.GetComponent<RectTransform>().anchoredPosition = currRectTransform;
    }
    
    
    

}