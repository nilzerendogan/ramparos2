using UnityEngine;

/// <summary>
/// Elin pozisyonunu, ROS'a gitmeden, lokal CCD IK ile anlık takip eder.
/// jointTransforms sırası: shoulder_pan -> shoulder_lift -> elbow -> wrist_1 -> wrist_2 -> wrist_3
/// Bu transformlar GERÇEK robotun ArticulationBody hiyerarşisi olabilir (o zaman doğrudan görsel
/// takip sağlar) ya da paralel, physics'siz bir "ghost" iskelet olabilir (fiziksel robotla
/// çakışmayı önlemek için tercih edilir; sonucu SetSliders ile gerçek robota yazarsın).
/// </summary>
public class RealtimeIKFollower : MonoBehaviour
{
    [Header("Joint zinciri (base -> end effector)")]
    public Transform[] jointTransforms;

    [Header("Her joint'in KENDİ lokal ekseni (URDF'e göre doğrulanmalı)")]
    public Vector3[] jointLocalAxes = new Vector3[7]
    {
        Vector3.right, // link7
        Vector3.right, // link6
        Vector3.right, // link5
        Vector3.right, // link4
        Vector3.right, // link3
        Vector3.right, // link2
        Vector3.up  // link1
    };

    public Transform endEffector;

    [Header("Çözücü ayarları")]
    [Range(1, 30)] public int iterations = 12;
    public float maxDegreesPerJointPerIteration = 15f;
    public float positionToleranceMeters = 0.004f;

    public TrajectoryHelperFunctions helperFunctions;

    // Her joint'in başlangıca göre BİRİKMİŞ açısı (radyan) - slider'a çevirmek için tutulur.
    private float[] accumulatedAngleRad;

    private void Awake()
    {
        accumulatedAngleRad = new float[jointTransforms.Length];
    }

    /// <summary>
    /// Her frame (pinch aktifken) çağır. targetWorldPos = hand.PointerPose.position
    /// (ya da robotBaseTransform'a göre local pozisyon).
    /// </summary>
    public void SolveAndApply(Vector3 targetWorldPos)
    {
        if (jointTransforms == null || jointTransforms.Length == 0 || endEffector == null) return;

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = jointTransforms.Length - 1; i >= 0; i--)
            {
                Transform joint = jointTransforms[i];
                Vector3 axisWorld = joint.TransformDirection(jointLocalAxes[i]).normalized;

                Vector3 toEnd = Vector3.ProjectOnPlane(endEffector.position - joint.position, axisWorld);
                Vector3 toTarget = Vector3.ProjectOnPlane(targetWorldPos - joint.position, axisWorld);

                if (toEnd.sqrMagnitude < 1e-8f || toTarget.sqrMagnitude < 1e-8f) continue;

                float angle = Vector3.SignedAngle(toEnd, toTarget, axisWorld);
                angle = Mathf.Clamp(angle, -maxDegreesPerJointPerIteration, maxDegreesPerJointPerIteration);

                joint.Rotate(jointLocalAxes[i], angle, Space.Self);
                accumulatedAngleRad[i] += angle * Mathf.Deg2Rad;
            }

            if (Vector3.Distance(endEffector.position, targetWorldPos) < positionToleranceMeters)
                break;
        }

        ApplyToSliders();
    }

    private void ApplyToSliders()
    {
        double[] sliderValues = new double[accumulatedAngleRad.Length];
        for (int i = 0; i < accumulatedAngleRad.Length; i++)
        {
            float wrapped = accumulatedAngleRad[i];
            if (wrapped > Mathf.PI) wrapped -= 2f * Mathf.PI;
            else if (wrapped < -Mathf.PI) wrapped += 2f * Mathf.PI;
            accumulatedAngleRad[i] = wrapped;

            // TrajectoryHelperFunctions.CurrentJointConfig ile aynı ölçek:
            // joints[i] = slider.value * 360 * Deg2Rad  =>  slider.value = radyan * Rad2Deg / 360
            sliderValues[i] = wrapped * Mathf.Rad2Deg / 360.0;
        }
        helperFunctions.SetSliders(sliderValues);
    }

    /// <summary>Yeni çizime başlarken birikmiş açıyı sıfırlamak istersen kullan.</summary>
    public void ResetAccumulatedAngles()
    {
        for (int i = 0; i < accumulatedAngleRad.Length; i++)
            accumulatedAngleRad[i] = 0f;
    }
}