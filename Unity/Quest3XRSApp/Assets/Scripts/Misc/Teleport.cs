using UnityEngine;

public class Teleport: MonoBehaviour
{

    public ArticulationBody ab;
    public GameObject grabbable;
    


    void FixedUpdate()
    {
        ab.TeleportRoot(grabbable.transform.position + new Vector3(0,0.1f,0.3f),grabbable.transform.rotation);
    }
}
