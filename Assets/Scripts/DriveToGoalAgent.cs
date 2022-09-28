using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public enum LaneChangeState
{
    Restricted,
    Changing
}

public class DriveToGoalAgent : Agent
{

    private CarController carController;
    private bool isChangingLane = false;
    private static String lane1Mesh = "Lane 1 Mesh Holder";
    private static String lane2Mesh = "Lane 2 Mesh Holder";
    private static String dividerMesh = "Divider Mesh Holder";
    private static String terrain = "Terrain";
    private String targetLane = lane2Mesh;

    private void Awake()
    {
        carController = GetComponent<CarController>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var steerAngle = (int) Math.Round(carController.frontLeftWheelCollider.steerAngle, 0);
        var steerAngleDiscretized = (int) Math.Round(steerAngle + carController.maxSteeringAngle, 0);
        sensor.AddObservation(steerAngleDiscretized);

        // 1. Get distance between ego and object
        // 2. Get relative position of object as Vec3
        //   - Create raycast from ego to object
        //   - relativeDirection = raycast.direction - ego.direction
        //   - May need to send raycast out in direction of ego (transform.forward) to get ego.direction
        // 3. Send data as observation (e.g, [5, 0.5, 0.5, 0.5] means this object is 5 units away, diagonally in all 3 planes)
    }

    public override void OnEpisodeBegin()
    {
        carController.TryGetComponent<Rigidbody>(out Rigidbody rigidBody);
        rigidBody.velocity = Vector3.zero;
        transform.localPosition = new Vector3((float)-42.41,(float)0.01,(float)1062.4);
        transform.localRotation = Quaternion.Euler(0, -120, 0);
        carController.frontLeftWheelCollider.steerAngle = 0;
        carController.frontRightWheelCollider.steerAngle = 0;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = 0;
        continuousActions[1] = 0;
        continuousActions[2] = 0;
        // continuousActions[3] = 0;
        // continuousActions[4] = 0;
        if (Input.GetAxisRaw("Horizontal").Equals(-1f))
        {
            continuousActions[1] = 10;            
        } else if (Input.GetAxisRaw("Horizontal").Equals(1f))
        {
            continuousActions[2] = 10;            
        }

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var highestValue = actions.ContinuousActions.Max();
        var highestIndex = actions.ContinuousActions.ToList().FindIndex(a => a.Equals(highestValue));
        carController.SetInput(highestIndex);

        CheckIfShouldTriggerLaneChange();
        if (isChangingLane)
        {
            if (IsOnlyTouching(targetLane))
            {
                SetReward(500f);
            } else if (IsTouching(terrain))
            {
                SetReward(-10000f);
                EndEpisode();
            }
        } else if (IsTouching(dividerMesh) || IsTouching(terrain))
        {
            Debug.Log("Drove off road or rolled over!");
            SetReward(-10000f);
            EndEpisode();
        }
        else
        {
            SetReward(1f);
        }
    }

    private void CheckIfShouldTriggerLaneChange()
    {
        if (!isChangingLane)
        {
            isChangingLane = IsChangingLane();
            if (TriggeredLaneChange())
            {
                ToggleTargetLane();
            }
        }
    }

    private bool TriggeredLaneChange()
    {
        return isChangingLane;
    }

    private bool IsChangingLane()
    {
        return true;
    }

    private bool IsTouching(String colliderName)
    {
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider?.name == colliderName
               || rHit.collider?.name == colliderName;
    }
    
    private bool IsOnlyTouching(String colliderName)
    {
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider?.name == colliderName
               && rHit.collider?.name == colliderName;
    }

    private void ToggleTargetLane()
    {
        targetLane = targetLane.Equals(lane1Mesh) ? lane2Mesh : lane1Mesh;
    }
}
