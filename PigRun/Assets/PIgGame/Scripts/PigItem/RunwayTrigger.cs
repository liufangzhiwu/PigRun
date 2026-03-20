using UnityEngine;

public class RunwayTrigger : MonoBehaviour
{
    private RunwayPath runwayPath;

    private void Awake()
    {
        runwayPath = GetComponent<RunwayPath>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PigItem pig = other.GetComponent<PigItem>();
        if (pig != null&&pig.CurrentState is MovingState)
        {
            // 小猪进入跑道，传递路径信息和进入点
            pig.EnterRunway(runwayPath, other.transform.position);
        }
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     PigItem pig = other.GetComponent<PigItem>();
    //     if (pig != null)
    //     {
    //         pig.ExitRunway();
    //     }
    // }
}