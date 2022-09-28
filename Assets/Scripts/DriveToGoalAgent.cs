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
    private static String lane1Mesh = "Lane 1 Mesh Holder";
    private static String lane2Mesh = "Lane 2 Mesh Holder";
    private static String dividerMesh = "Divider Mesh Holder";
    private static String terrain = "Terrain";
    private String targetLane = lane2Mesh;
    private Timer triggerLaneChangeTimer = new();
    private Timer laneChangeCountdownTimer = new();

    private int laneChangeDuration = 3_000;
    private int minLaneChangeTriggerWait = 0;
    private int maxLaneChangeTriggerWait = 15_000;

    private void Awake()
    {
        carController = GetComponent<CarController>();
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
        targetLane = lane2Mesh;
        DisableTimers();
        RestartTriggerLaneChangeTimer();
    }

    private void DisableTimers()
    {
        triggerLaneChangeTimer.Enabled = false;
        laneChangeCountdownTimer.Enabled = false;
        triggerLaneChangeTimer = new();
        laneChangeCountdownTimer = new();
    }

    private void RestartTriggerLaneChangeTimer()
    {
        triggerLaneChangeTimer = new();
        triggerLaneChangeTimer.Elapsed += OnLaneChangeTrigger;
        triggerLaneChangeTimer.Interval = GetLaneChangeTriggerInterval();
        triggerLaneChangeTimer.Enabled = true;
    }

    private int GetLaneChangeTriggerInterval()
    {
        return new Random().Next(minLaneChangeTriggerWait, maxLaneChangeTriggerWait);
    }

    private void OnLaneChangeTrigger(object source, ElapsedEventArgs e)
    {
        isChangingLane = true;
        ToggleTargetLane();
        RestartLaneChangeCountdownTimer();
    }

    private void RestartLaneChangeCountdownTimer()
    {
        laneChangeCountdownTimer = new();
        laneChangeCountdownTimer.Elapsed += OnLaneChangeFailed;
        laneChangeCountdownTimer.Interval = laneChangeDuration;
        laneChangeCountdownTimer.Enabled = true;
    }

    private void CancelLaneChangeCountdownTimer()
    {
        laneChangeCountdownTimer.Enabled = false;
    }

    private void OnLaneChangeFailed(object source, ElapsedEventArgs e)
    {
        SetReward(-10000f);
        EndEpisode();
    }

    private void ResetCar()
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
        
        Debug.Log("isChangingLane is " + isChangingLane);
        if (isChangingLane)
        {
            if (IsOnlyTouching(targetLane))
            {
                CancelLaneChangeCountdownTimer();
                isChangingLane = false;
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
