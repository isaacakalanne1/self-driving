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

    public override void OnEpisodeBegin()
    {
        carController.TryGetComponent<Rigidbody>(out Rigidbody rigidBody);
        rigidBody.velocity = Vector3.zero;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var turnValue = actions.ContinuousActions[0];
        carController.SetInput(turnValue);

        if (DidDriveOffRoad())
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
}
