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

    [SerializeField] private Material targetLaneMaterial;
    [SerializeField] private Material outOfBoundsMaterial;
    [SerializeField] private MeshRenderer lane1Mesh;
    [SerializeField] private MeshRenderer lane2Mesh;
    [SerializeField] private MeshRenderer terrainMesh;
    
    private CarController carController;
    private MeshRenderer targetLane;
    private int triggerLaneChangeMaxCount;
    private int triggerLaneChangeCounter;
    private const int LaneChangePermittedMaxCount = 50;

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
        targetLane = lane2Mesh;
        UpdateLaneMaterials();
    }

    private void ResetLaneChangeStates()
    {
        triggerLaneChangeCounter = 0;
        triggerLaneChangeMaxCount = new Random().Next(1, 150);
        currentState = LaneChangeState.Restricted;
        previousState = LaneChangeState.Restricted;
    }

    private void ResetCar()
    {
        carController.TryGetComponent(out Rigidbody rigidBody);
        rigidBody.velocity = Vector3.zero;
        carController.frontLeftWheelCollider.steerAngle = 0;
        carController.frontRightWheelCollider.steerAngle = 0;
        transform.localPosition = new Vector3((float)636.1836,(float)537.706,(float)-306.7411);
        transform.localRotation = Quaternion.Euler(0, -120, 0);
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
        // triggerLaneChangeCounter += 1;
        UpdateLaneChangeState();

        switch (currentState)
        {
            case LaneChangeState.ControlledAccess:
                if (IsOnlyTouching(targetLane))
                {
                    SetReward(10f);
                    ResetLaneChangeStates();
                } else if (IsTouching(terrainMesh))
                {
                    SetReward(-10000f);
                    EndEpisode();
                }
                else
                {
                    SetReward(1f);
                }
                break;
            case LaneChangeState.Failed:
                SetReward(-10000f);
                EndEpisode();
                break;
            case LaneChangeState.Restricted:
                if (!IsOnlyTouching(targetLane))
                {
                    SetReward(-10000f);
                    EndEpisode();
                }
                else
                {
                    SetReward(1f);
                }
                break;
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
                    UpdateLaneMaterials();
                    Debug.Log("New target lane is " + targetLane.name);
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

    private bool IsTouching(MeshRenderer mesh)
    {
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider?.name == mesh.name || rHit.collider?.name == mesh.name;
    }
    
    private bool IsOnlyTouching(MeshRenderer mesh)
    {
        carController.frontLeftWheelCollider.GetGroundHit(out WheelHit lHit);
        carController.frontRightWheelCollider.GetGroundHit(out WheelHit rHit);
        return lHit.collider?.name == mesh.name && rHit.collider?.name == mesh.name;
    }

    private void ToggleTargetLane()
    {
        targetLane = targetLane.Equals(lane1Mesh) ? lane2Mesh : lane1Mesh;
    }

    private void UpdateLaneMaterials()
    {
        lane1Mesh.material = targetLane.Equals(lane1Mesh) ? targetLaneMaterial : outOfBoundsMaterial;
        lane2Mesh.material = targetLane.Equals(lane2Mesh) ? targetLaneMaterial : outOfBoundsMaterial;
    }
}
