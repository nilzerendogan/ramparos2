using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Ur10Mover;
using Unity.Robotics.ROSTCPConnector;

public class HandleGripper : MonoBehaviour
{
    public ArticulationBody finger1;
    public ArticulationBody finger2;

    private string m_RosServiceName_Gripper = "gripper";
    private ROSConnection m_Ros;

    int state = 0;

    void Start()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterRosService<GripperServiceRequest, GripperServiceResponse>(m_RosServiceName_Gripper);
    }

    void Update()
    {
        // B button on the right Touch controller. GetDown (not Get) so a single
        // press = a single toggle, instead of firing every frame the button is
        // held.
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            handleClick();
        }
    }

    public void handleClick() {

        if (state == 0) {
            state = 1;
            SetFingerTargets(5f);
            SendGripperCommand("close");
        } else {
            state = 0;
            SetFingerTargets(0.0f);
            SendGripperCommand("open");
        }

    }

    // NOTE: finger1/finger2 were previously unassigned in the scene (fileID: 0),
    // which meant handleClick() threw a silent NullReferenceException in the
    // headset build and did nothing. Guarding with null checks means the real
    // robot gripper command below will still fire even before that Inspector
    // assignment is fixed -- but you should still assign them so the virtual
    // gripper animates too.
    private void SetFingerTargets(float target)
    {
        if (finger1 != null)
        {
            var xDrive = finger1.xDrive;
            xDrive.target = target;
            finger1.xDrive = xDrive;
        }
        else
        {
            Debug.LogWarning("HandleGripper: finger1 is not assigned in the Inspector.");
        }

        if (finger2 != null)
        {
            var xDrive = finger2.xDrive;
            xDrive.target = target;
            finger2.xDrive = xDrive;
        }
        else
        {
            Debug.LogWarning("HandleGripper: finger2 is not assigned in the Inspector.");
        }
    }

    private void SendGripperCommand(string command)
    {
        var request = new GripperServiceRequest();
        request.input_msg = command;
        m_Ros.SendServiceMessage<GripperServiceResponse>(m_RosServiceName_Gripper, request, HandleGripperResponse);
    }

    private void HandleGripperResponse(GripperServiceResponse response)
    {
        Debug.Log("Gripper service response: " + response.output_msg);
    }
}
