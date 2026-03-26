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
        AnimalBase animal = other.GetComponent<AnimalBase>();
        if (animal != null && animal.CurrentState is MovingState)
        {
            animal.EnterRunway(runwayPath, other.transform.position);
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
