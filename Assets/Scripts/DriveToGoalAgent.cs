using System;
using System.Collections;
using System.Collections.Generic;
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
        sensor.AddObservation(transform.position);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Vertical");
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var driveValue = actions.ContinuousActions[0];
        var turnValue = actions.ContinuousActions[1];
        carController.SetInput(driveValue, turnValue);

        if (didDriveOffRoad())
        {
            Debug.Log("Drove off road!");
        }
    }

    private bool didDriveOffRoad()
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
}
