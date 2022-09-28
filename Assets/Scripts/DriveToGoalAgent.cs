using System;
using System.Linq;
using System.Timers;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = System.Random;

public class DriveToGoalAgent : Agent
{

    private CarController carController;
    private bool isChangingLane;
    private const string Lane1Mesh = "Lane 1 Mesh Holder";
    private const string Lane2Mesh = "Lane 2 Mesh Holder";
    private const string DividerMesh = "Divider Mesh Holder";
    private const string Terrain = "Terrain";
    private string targetLane = Lane2Mesh;
    private Timer triggerLaneChangeTimer = new();
    private Timer laneChangeCountdownTimer = new();

    private const int LaneChangeDuration = 3_000;
    private const int MinLaneChangeTriggerWait = 0;
    private const int MaxLaneChangeTriggerWait = 15_000;

    private void Awake()
    {
        carController = GetComponent<CarController>();
        SetUpTimers();
    }

    private void SetUpTimers()
    {
        laneChangeCountdownTimer.Elapsed += OnLaneChangeFailed;
        triggerLaneChangeTimer.Elapsed += OnLaneChangeTrigger;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var steerAngle = (int) Math.Round(carController.frontLeftWheelCollider.steerAngle, 0);
        var steerAngleDiscretized = (int) Math.Round(steerAngle + carController.maxSteeringAngle, 0);
        sensor.AddObservation(steerAngleDiscretized);
        sensor.AddObservation(isChangingLane ? 1 : 0);

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
        isChangingLane = false;
        targetLane = Lane2Mesh;
        DisableTimers();
        RestartTriggerLaneChangeTimer();
    }

    private void DisableTimers()
    {
        triggerLaneChangeTimer.Stop();
        laneChangeCountdownTimer.Stop();
    }

    private void RestartTriggerLaneChangeTimer()
    {
        triggerLaneChangeTimer.Interval = GetLaneChangeTriggerInterval();
        triggerLaneChangeTimer.Start();
    }

    private int GetLaneChangeTriggerInterval()
    {
        return new Random().Next(MinLaneChangeTriggerWait, MaxLaneChangeTriggerWait);
    }

    private void OnLaneChangeTrigger(object source, ElapsedEventArgs e)
    {
        Debug.Log("Triggered lane change!");
        isChangingLane = true;
        ToggleTargetLane();
        RestartLaneChangeCountdownTimer();
    }

    private void RestartLaneChangeCountdownTimer()
    {
        laneChangeCountdownTimer.Interval = LaneChangeDuration;
        laneChangeCountdownTimer.Start();
    }

    private void OnLaneChangeFailed(object source, ElapsedEventArgs e)
    {
        Debug.Log("Failed to change lane!");
        SetReward(-10000f);
        EndCurrentEpisode();
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

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var highestValue = actions.ContinuousActions.Max();
        var highestIndex = actions.ContinuousActions.ToList().FindIndex(a => a.Equals(highestValue));
        carController.SetInput(highestIndex);
        
        // Debug.Log("isChangingLane is " + isChangingLane);
        if (isChangingLane)
        {
            if (IsOnlyTouching(targetLane))
            {
                laneChangeCountdownTimer.Stop();
                isChangingLane = false;
                SetReward(10f);
            } else if (IsTouching(Terrain))
            {
                SetReward(-10000f);
                EndCurrentEpisode();
            }
        } else if (IsTouching(DividerMesh) || IsTouching(Terrain))
        {
            SetReward(-10000f);
            EndCurrentEpisode();
        }
        else
        {
            SetReward(1f);
        }
    }

    private void EndCurrentEpisode()
    {
        DisableTimers();
        EndEpisode();
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
