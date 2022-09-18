using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class DriveToGoalAgent : Agent
{

    private CarController carController;

    private void Awake()
    {
        carController = GetComponent<CarController>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(carController.frontLeftWheelCollider.steerAngle);
    }

    public override void OnEpisodeBegin()
    {
        carController.TryGetComponent<Rigidbody>(out Rigidbody rigidBody);
        rigidBody.velocity = Vector3.zero;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        carController.frontLeftWheelCollider.steerAngle = 0;
        carController.frontRightWheelCollider.steerAngle = 0;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = 0;
        continuousActions[1] = 0;
        continuousActions[2] = 0;
        continuousActions[3] = 0;
        continuousActions[4] = 0;
        if (Input.GetAxisRaw("Horizontal").Equals(-1f))
        {
            continuousActions[1] = 10;            
        } else if (Input.GetAxisRaw("Horizontal").Equals(1f))
        {
            continuousActions[3] = 10;            
        }
        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var highestValue = actions.ContinuousActions.Max();
        Debug.Log("Highest value is " + highestValue);
        var highestIndex = actions.ContinuousActions.ToList().FindIndex(a => a.Equals(highestValue));
        Debug.Log("Highest index is " + highestIndex);
        carController.SetInput(highestIndex);

        if (DidDriveOffRoad() || DidRollOver())
        {
            SetReward(-10000f);
            EndEpisode();
        }
        else
        {
            SetReward(1f);
        }
    }

    private bool DidDriveOffRoad()
    {
        var dividerName = "Divider Mesh Holder";
        var terrainName = "Terrain";
        // Currently only detecting front wheels hitting the divider or terrain
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider.name == dividerName
               || lHit.collider.name == terrainName
               || rHit.collider.name == dividerName
               || rHit.collider.name == terrainName;
    }

    private bool DidRollOver()
    {
        var rotation = transform.rotation.z;
        return rotation < -50 || rotation > 50;
    }
}
