using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = System.Random;

enum LaneChangeState
{
    Restricted,
    ControlledAccess,
    Failed
}

public class DriveToGoalAgent : Agent
{

    private CarController carController;
    private const string Lane1Mesh = "Lane 1 Mesh Holder";
    private const string Lane2Mesh = "Lane 2 Mesh Holder";
    private const string DividerMesh = "Divider Mesh Holder";
    private const string Terrain = "Terrain";
    private string targetLane = Lane2Mesh;
    private int triggerLaneChangeMaxCount;
    private int triggerLaneChangeCounter;
    private const int LaneChangePermittedMaxCount = 40;

    private LaneChangeState currentState;
    private LaneChangeState previousState;

    private void Awake()
    {
        carController = GetComponent<CarController>();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        var steerAngle = (int) Math.Round(carController.frontLeftWheelCollider.steerAngle, 0);
        var steerAngleDiscretized = (int) Math.Round(steerAngle + carController.maxSteeringAngle, 0);
        sensor.AddObservation(steerAngleDiscretized);
        sensor.AddObservation(IsChangingLane() && !DidChangeLaneTimeOut() ? 1 : 0);

        // 1. Get distance between ego and object
        // 2. Get relative position of object as Vec3
        //   - Create raycast from ego to object
        //   - relativeDirection = raycast.direction - ego.direction
        //   - May need to send raycast out in direction of ego (transform.forward) to get ego.direction
        // 3. Send data as observation (e.g, [5, 0.5, 0.5, 0.5] means this object is 5 units away, diagonally in all 3 planes)
    }

    public override void OnEpisodeBegin()
    {
        ResetCar();
        ResetLaneChangeStates();
        targetLane = Lane2Mesh;
        currentState = LaneChangeState.Restricted;
        previousState = LaneChangeState.Restricted;
    }

    private void ResetLaneChangeStates()
    {
        triggerLaneChangeCounter = 0;
        triggerLaneChangeMaxCount = new Random().Next(0, 150);
        currentState = LaneChangeState.Restricted;
        previousState = LaneChangeState.Restricted;
    }

    private void ResetCar()
    {
        carController.TryGetComponent(out Rigidbody rigidBody);
        rigidBody.velocity = Vector3.zero;
        transform.localPosition = new Vector3((float)636.1836,(float)537.706,(float)-306.7411);
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
        if (Input.GetAxisRaw("Horizontal").Equals(-1f))
        {
            continuousActions[1] = 10;            
        } else if (Input.GetAxisRaw("Horizontal").Equals(1f))
        {
            continuousActions[2] = 10;            
        }

        if (currentState == LaneChangeState.Restricted
            && triggerLaneChangeCounter < triggerLaneChangeMaxCount
            && Input.GetKey(KeyCode.Space))
        {
            triggerLaneChangeCounter = triggerLaneChangeMaxCount;
        }

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var highestValue = actions.ContinuousActions.Max();
        var highestIndex = actions.ContinuousActions.ToList().FindIndex(a => a.Equals(highestValue));
        carController.SetInput(highestIndex);
        
        // Debug.Log("isChangingLane is " + isChangingLane);
        triggerLaneChangeCounter += 1;
        UpdateLaneChangeState();

        if (currentState == LaneChangeState.ControlledAccess)
        {
            if (IsOnlyTouching(targetLane))
            {
                SetReward(10f);
                ResetLaneChangeStates();
            } else if (IsTouching(Terrain))
            {
                SetReward(-10000f);
                EndEpisode();
            }
            else
            {
                SetReward(1f);
            }
        } else if (currentState == LaneChangeState.Failed)
        {
            SetReward(-10000f);
            EndEpisode();
        } else if (IsTouching(DividerMesh) || IsTouching(Terrain))
        {
            SetReward(-10000f);
            EndEpisode();
        }
        else
        {
            SetReward(1f);
        }
    }

    private void UpdateLaneChangeState()
    {
        currentState = IsChangingLane() ? LaneChangeState.ControlledAccess : LaneChangeState.Restricted;
        currentState = DidChangeLaneTimeOut() ? LaneChangeState.Failed : currentState;
        if (previousState != currentState)
        {
            switch (currentState)
            {
                case LaneChangeState.ControlledAccess:
                    Debug.Log("Triggered change lane!");
                    ToggleTargetLane();
                    Debug.Log("New target lane is " + targetLane);
                    break;
                case LaneChangeState.Failed:
                    Debug.Log("Change lane timed out!");
                    break;
            }
        }
        previousState = currentState;
    }

    private bool IsChangingLane()
    {
        return triggerLaneChangeCounter >= triggerLaneChangeMaxCount;
    }

    private bool DidChangeLaneTimeOut()
    {
        return triggerLaneChangeCounter >= triggerLaneChangeMaxCount + LaneChangePermittedMaxCount;
    }

    private bool IsTouching(String colliderName)
    {
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider?.name == colliderName || rHit.collider?.name == colliderName;
    }
    
    private bool IsOnlyTouching(String colliderName)
    {
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider?.name == colliderName && rHit.collider?.name == colliderName;
    }

    private void ToggleTargetLane()
    {
        targetLane = targetLane.Equals(Lane1Mesh) ? Lane2Mesh : Lane1Mesh;
    }
}
