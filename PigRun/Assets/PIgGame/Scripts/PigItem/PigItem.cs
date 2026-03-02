using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigItem : MonoBehaviour
{
    Rigidbody pigRigidbody;
    
    float speed=5f;
    
    // Start is called before the first frame update
    void Start()
    {
        pigRigidbody=GetComponent<Rigidbody>();
    }


    void OnMouseUpAsButton()
    {
        pigRigidbody.isKinematic=false;
        Debug.Log("PigItem Had Mouse Button");
        pigRigidbody.AddForce(transform.forward*speed,ForceMode.Impulse);
    }
}
