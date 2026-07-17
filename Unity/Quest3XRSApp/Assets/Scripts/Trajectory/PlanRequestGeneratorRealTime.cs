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

    // ESKİ: private Queue<double[]> requestQueue = new Queue<double[]>();
    // YENİ: Kuyruk yerine SADECE en son hedef tutuluyor. Elin ROS meşgulken
    // geçtiği ara noktalar hiç işlenmiyor; ROS boşa çıkınca elin O ANKİ
    // (en güncel) pozisyonuna gidiliyor. Böylece gecikme birikmiyor.
    private double[] latestTarget;
    private bool hasNewTarget = false;

    private bool waitingForResponse = false;

    // ROS'tan planı gelmiş ama ghost robotta henüz görsel olarak oynatılmamış
    // segmentlerin kuyruğu. waitingForResponse yalnızca "plan cevabı bekleniyor
    // mu"yu ifade ediyor, görsel oynatım süresini KAPSAMIYOR.
    private Queue<PlannerServiceResponse> playbackQueue = new Queue<PlannerServiceResponse>();
    private bool isPlayingBack = false;

    // Seyrek liste: sadece her trajectory segmentinin SON noktası.
    // Gerçek robota gönderilecek waypoint listesi bu.
    public List<double[]> previousPoints = new List<double[]>();

    // Yoğun liste: her trajectory'deki TÜM ara noktalar.
    // Replay / scrub / slider bu liste üzerinden çalışır.
    public List<double[]> previousPointsDense = new List<double[]>();

    public List<Vector3> previousPoses = new List<Vector3>();
    public List<Quaternion> previousOrientations = new List<Quaternion>();
    public RealRobotCommunication realRobotCommunication;

    private double[] jointConfig;
    public int currentIndexPointer = 0;

    public int stepSize = 15;

    public Button backButton;
    public Button nextButton;

    public GameObject sliderPosition;
    public GameObject bar;

    public GameObject playButton;
    public GameObject pauseButton;

    public GameObject executeOnRealRobotButton;

    private float lastRequestSentTime;

    private void GenerateRequest(Vector3 pose, Quaternion orientation)
    {
        var request = new PlannerServiceRequest();
        request.request_type = "realTime";
        if (recordOrientationDropdown.value == 0)
            request.input_msg = "down";
        else if (recordOrientationDropdown.value == 3)
            request.input_msg = "hook";
        request.joints_input = jointConfig;

        previousPoses.Add(pose);
        previousOrientations.Add(orientation);

        PoseMsg[] pose_list = new PoseMsg[1];
        pose_list[0] = HelperFunctions.GeneratePoseMsg(pose, orientation);
        request.pose_list = pose_list;

        lastRequestSentTime = Time.realtimeSinceStartup;
        TrajectoryPlanner.SendRequest(request);
    }

    public void ProcessResponse(PlannerServiceResponse response)
    {
        float roundTripMs = (Time.realtimeSinceStartup - lastRequestSentTime) * 1000f;
        Debug.Log($"[RAMPA] round-trip: {roundTripMs:F1} ms");

        if (response.output_msg == "Timeout")
        {
            waitingForResponse = false;
            if (DrawServiceRealTime.isStateDrawTrajectory())
                DrawServiceRealTime.UpdateDrawingState(true);
        }
        else
        {
            jointConfig = response.trajectories[0].joint_trajectory.points.Last().positions;
            waitingForResponse = false;
            playbackQueue.Enqueue(response);
        }
    }
    public void Start()
    {
        jointConfig = HelperFunctions.CurrentJointConfig();
        StartCoroutine(ProcessRequests());
        StartCoroutine(PlaybackWorker());
    }

    public bool HasPendingWork()
    {
        return waitingForResponse || hasNewTarget || playbackQueue.Count > 0 || isPlayingBack;
    }

    public void AddRequestToQueue(double[] poseInfo)
    {
        // Artık kuyruğa eklemiyoruz, sadece en güncel hedefi güncelliyoruz.
        latestTarget = poseInfo;
        hasNewTarget = true;
    }

    private IEnumerator ProcessRequests()
    {
        while (true)
        {
            if (hasNewTarget && !waitingForResponse)
            {
                waitingForResponse = true;
                hasNewTarget = false;

                double[] poseInfo = latestTarget;
                Vector3 pose = new Vector3((float)poseInfo[0], (float)poseInfo[1], (float)poseInfo[2]);
                Quaternion orientation = new Quaternion((float)poseInfo[3], (float)poseInfo[4], (float)poseInfo[5], (float)poseInfo[6]);
                GenerateRequest(pose, orientation);
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    /*
    private void GenerateRequest(Vector3 pose, Quaternion orientation)
    {
        var request = new PlannerServiceRequest();
        request.request_type = "realTime";
        if (recordOrientationDropdown.value == 0)
        {
            request.input_msg = "down";
        }
        else if (recordOrientationDropdown.value == 3)
        {
            request.input_msg = "hook";
        }
        request.joints_input = jointConfig;

        previousPoses.Add(pose);
        previousOrientations.Add(orientation);

        PoseMsg[] pose_list = new PoseMsg[1];
        pose_list[0] = HelperFunctions.GeneratePoseMsg(pose, orientation);
        request.pose_list = pose_list;

        TrajectoryPlanner.SendRequest(request);
    }
    */

    /*
    public void ProcessResponse(PlannerServiceResponse response)
    {
        if (response.output_msg == "Timeout")
        {
            waitingForResponse = false;
            if (DrawServiceRealTime.isStateDrawTrajectory())
                DrawServiceRealTime.UpdateDrawingState(true);
        }
        else
        {
            // Bir sonraki segmentin IK seed'i için gereken son joint config'i
            // hemen güncelle.
            jointConfig = response.trajectories[0].joint_trajectory.points.Last().positions;

            // Kilidi hemen aç: ProcessRequests bir sonraki (en güncel) hedefi
            // ghost robotun görsel oynatımını beklemeden gönderebilsin.
            waitingForResponse = false;

            playbackQueue.Enqueue(response);
        }
    }

    */

    private IEnumerator PlaybackWorker()
    {
        while (true)
        {
            if (playbackQueue.Count > 0)
            {
                isPlayingBack = true;

                // Backlog varsa en yeni response'a atla; eski ara segmentleri
                // oynatmak ghost ile elin arasındaki farkı sadece büyütür.
                PlannerServiceResponse response = null;
                while (playbackQueue.Count > 0)
                {
                    response = playbackQueue.Dequeue();
                }

                yield return StartCoroutine(ExecuteTrajectories(response));
                isPlayingBack = false;
            }
            else
            {
                yield return null;
            }
        }
    }

    IEnumerator ExecuteTrajectories(PlannerServiceResponse response)
    {
        // Ara noktaları animasyonla tek tek oynatmıyoruz — real-time takipte
        // buna gerek yok ve gecikmeyi katlıyordu. Sadece segmentin SON
        // konfigürasyonuna anında atlıyoruz.
        for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
        {
            var lastPoint = response.trajectories[poseIndex].joint_trajectory.points.Last();

            previousPoints.Add(HelperFunctions.GetJointAngles(lastPoint));
            previousPointsDense.Add(HelperFunctions.GetJointAngles(lastPoint));
            HelperFunctions.SetJointAngles(lastPoint);
        }
        yield return null;
    }

    IEnumerator ExecuteTrajectory(double[] trajectory)
    {
        HelperFunctions.SetSliders(trajectory);
        yield return new WaitForSeconds(k_JointAssignmentWait);
    }

    public void SetJointAnglesForRealRobot()
    {
        realRobotCommunication.setJointAngles(previousPoints);
    }

    public void ResetGenerator(bool addToTrainingSet = false)
    {
        if (addToTrainingSet)
        {
            if (previousPoints.Count > 0)
            {
                PrevRecordedTrajectories.AddTrajectory(previousPoses, previousOrientations);
            }
            PrevRecordedTrajectories.HandleButtons();
        }

        jointConfig = HelperFunctions.CurrentJointConfig();

        waitingForResponse = false;
        hasNewTarget = false;
        playbackQueue.Clear();

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

    IEnumerator PlayRestOfTrajectoryCoroutine()
    {
        playButton.SetActive(false);
        pauseButton.SetActive(true);

        backButton.interactable = false;
        nextButton.interactable = false;

        for (; currentIndexPointer < previousPointsDense.Count - 1; currentIndexPointer++)
        {
            StartCoroutine(ExecuteTrajectory(previousPointsDense[currentIndexPointer + 1]));
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

    public void PlayRestOfTrajectory()
    {
        StartCoroutine(PlayRestOfTrajectoryCoroutine());
    }

    public void PauseTrajectory()
    {
        StopAllCoroutines();
        UpdateSliderHandle();
        playButton.SetActive(true);
        pauseButton.SetActive(false);

        if (currentIndexPointer < previousPointsDense.Count - 1)
        {
            playButton.GetComponent<Button>().interactable = true;
            nextButton.interactable = true;
        }
        else
        {
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
        hasNewTarget = false;
        playbackQueue.Clear();
    }

    public bool isWaitingForResponse()
    {
        return waitingForResponse;
    }

    private void UpdateSliderHandle()
    {
        Vector3 currRectTransform = sliderPosition.GetComponent<RectTransform>().anchoredPosition;
        currRectTransform.x =
            (bar.GetComponent<RectTransform>().sizeDelta.x) * (currentIndexPointer / ((float)previousPointsDense.Count - 1)) - bar.GetComponent<RectTransform>().sizeDelta.x / 2;
        sliderPosition.GetComponent<RectTransform>().anchoredPosition = currRectTransform;
    }
}