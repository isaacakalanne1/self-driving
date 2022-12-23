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
    ControlledAccess
}

public enum CameraType
{
    Follow,
    Car
}

public class EpisodeBeginData
{
    public Vector3 position;
    public Quaternion rotation;
    public MeshRenderer initialLane;

    public EpisodeBeginData(Vector3 position, Quaternion rotation, MeshRenderer initialLane)
    {
        this.position = position;
        this.rotation = rotation;
        this.initialLane = initialLane;
    }
}

public class DriveToGoalAgent : Agent
{
    
    [SerializeField] private Material outOfBoundsMaterial;
    [SerializeField] private MeshRenderer lane1Mesh;
    [SerializeField] private MeshRenderer lane2Mesh;
    [SerializeField] private MeshRenderer terrainMesh;
    [SerializeField] private MeshRenderer shouldEndAllEpisodesNotifier;
    
    [SerializeField] private LayerMask lane1Mask;
    [SerializeField] private LayerMask lane2Mask;

    [SerializeField] private Camera carCamera;
    [SerializeField] private Camera followCamera;

    private EpisodeBeginData[] listOfEpisodeBeginData;
    private string currentEpisodeInt = "0";

    private CarController carController;
    private int episodeBeginIndex;
    private MeshRenderer targetLane;
    private int triggerLaneChangeMaxCount;
    private int triggerLaneChangeCounter;

    private CurrentLane currentLane = CurrentLane.Low;

    private LaneChangeState currentState;
    private LaneChangeState previousState;

    private void Awake()
    {
        shouldEndAllEpisodesNotifier.name = "0";
        carController = GetComponent<CarController>();
        episodeBeginIndex = GetIndexFromString(carController.name, "Sedan");
        listOfEpisodeBeginData = CreateListOfEpisodeBeginData();
    }

    private int GetIndexFromString(string inputString, string stringToRemove)
    {
        var indexString = inputString.Replace(stringToRemove, "");
        return int.Parse(indexString);
    }

    private EpisodeBeginData[] CreateListOfEpisodeBeginData()
    {
        var yValue = -7.98f;
        EpisodeBeginData[] list = {
            new (new Vector3((float)13.627,yValue,(float)7.775),
                Quaternion.Euler(0, 205, 0),
                lane2Mesh),
            new (new Vector3((float)3.543,yValue,(float)-11.105),
                Quaternion.Euler(0, 270, 0),
                lane2Mesh),
            new (new Vector3((float)-13.656,yValue,(float)-3.791),
                Quaternion.Euler(0, 20, 0),
                lane2Mesh),
            new (new Vector3((float)-15.018,yValue,(float)19.211),
                Quaternion.Euler(0, 80, 0),
                lane2Mesh),
            new (new Vector3((float)6.143,yValue,(float)22.402),
                Quaternion.Euler(0, 115, 0),
                lane2Mesh),
            
            new (new Vector3((float)18.055,yValue,(float)-4.325),
                Quaternion.Euler(0, 180, 0),
                lane1Mesh),
            new (new Vector3((float)-8.11,yValue,(float)-17.16),
                Quaternion.Euler(0, 235, 0),
                lane1Mesh),
            new (new Vector3((float)-21.33,yValue,(float)15.676),
                Quaternion.Euler(0, 25, 0),
                lane1Mesh),
            new (new Vector3((float)-3.398,yValue,(float)25.562),
                Quaternion.Euler(0, 80, 0),
                lane1Mesh),
            new (new Vector3((float)15.585,yValue,(float)17.432),
                Quaternion.Euler(0, 130, 0),
                lane1Mesh)
        };
        return list;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        var steerAngle = (int) Math.Round(carController.frontLeftWheelCollider.steerAngle, 0);
        var steerAngleDiscretized = (int) Math.Round(steerAngle + carController.maxSteeringAngle, 0);
        sensor.AddObservation(steerAngleDiscretized);
        sensor.AddObservation(IsChangingLane() ? 1 : 0);
        sensor.AddObservation(currentLane == CurrentLane.Low ? 0 : 1);
        carController.TryGetComponent(out Rigidbody rigidBody);
        var localVelocity = transform.InverseTransformDirection(rigidBody.velocity);
        sensor.AddObservation(localVelocity.z);
    }

    public override void OnEpisodeBegin()
    {
        shouldEndAllEpisodesNotifier.material = outOfBoundsMaterial;
        ResetCar();
        ResetLaneChangeStates();
        UpdateMaterials();
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
        
        EpisodeBeginData data = listOfEpisodeBeginData[episodeBeginIndex];
        transform.localPosition = data.position;
        transform.localRotation = data.rotation;
        targetLane = data.initialLane;
        currentLane = targetLane.Equals(lane1Mesh) ? CurrentLane.Low : CurrentLane.High;
        
        carController.SetTurnValue(0);
        var initialVerticalInput = carController.GetInitialVerticalInput(currentLane);
        carController.SetVerticalInput(initialVerticalInput);
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
        
        var highestTurnIndex = GetIndexOfHighestValue(actionsOut);
        carController.SetInput(highestTurnIndex, currentLane);
    }

    private int GetIndexOfHighestValue(ActionBuffers actions)
    {
        var turnActions = actions.ContinuousActions.ToList().GetRange(0, 3);
        var highestTurnValue = turnActions.Max();
        return turnActions.FindIndex(a => a.Equals(highestTurnValue));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // var rcComponents = this.GetComponents<RayPerceptionSensorComponent3D>();
        //
        // foreach (var rc in rcComponents)
        // {
        //     var r2 = rc.GetRayPerceptionInput();
        //     var r3 = RayPerceptionSensor.Perceive(r2);
        //     {          
        //         foreach(RayPerceptionOutput.RayOutput rayOutput in r3.RayOutputs)
        //         {
        //             if (rayOutput.HitTaggedObject)
        //             {
        //                 Debug.Log("Ray hit! Index is " + rayOutput.HitTagIndex);
        //             }
        //         }
        //     }
        // }
        
        var highestTurnIndex = GetIndexOfHighestValue(actions);
        carController.SetInput(highestTurnIndex, currentLane);

        // Debug.Log("isChangingLane is " + isChangingLane);
        triggerLaneChangeCounter += 1;

        if (currentState == LaneChangeState.Restricted && Input.GetKey(KeyCode.Space))
        {
            triggerLaneChangeCounter = triggerLaneChangeMaxCount + 10;
        }
        UpdateLaneChangeState();

        switch (currentState)
        {
            case LaneChangeState.ControlledAccess:
                if (IsOnlyTouching(targetLane))
                {
                    SetReward(1000f);
                    ResetLaneChangeStates();
                    currentLane = targetLane.Equals(lane1Mesh) ? CurrentLane.Low : CurrentLane.High;
                } else if (IsTouching(terrainMesh))
                {
                    SetReward(-10000f);
                    SetAllEpisodesToEnd();
                }
                else
                {
                    SetReward(carController.GetReward());
                }
                break;
            case LaneChangeState.Restricted:
                if (!IsOnlyTouching(targetLane))
                {
                    SetReward(-10000f);
                    SetAllEpisodesToEnd();
                }
                else
                {
                    SetReward(carController.GetReward());
                }
                break;
        }
        
        if (ShouldEndAllEpisodes())
        {
            currentEpisodeInt = shouldEndAllEpisodesNotifier.name;
            EndEpisode();
        }
    }

    private void UpdateLaneChangeState()
    {
        currentState = IsChangingLane() ? LaneChangeState.ControlledAccess : LaneChangeState.Restricted;
        if (previousState != currentState)
        {
            switch (currentState)
            {
                case LaneChangeState.ControlledAccess:
                    ToggleTargetLane();
                    UpdateMaterials();
                    break;
            }
        }
        previousState = currentState;
    }

    private bool IsChangingLane()
    {
        return triggerLaneChangeCounter >= triggerLaneChangeMaxCount;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == LaneChangeState.ControlledAccess)
        {
            Debug.Log("Cars collided!");
            SetReward(-10000f);            
        }
        SetAllEpisodesToEnd();
    }

    private void ToggleTargetLane()
    {
        targetLane = targetLane.Equals(lane1Mesh) ? lane2Mesh : lane1Mesh;
    }

    private void UpdateMaterials()
    {
        int layerLane1 = LayerMask.NameToLayer("Lane 1");
        int layerLane2 = LayerMask.NameToLayer("Lane 2");
        int layerLane1Prev = LayerMask.NameToLayer("Lane 1 Prev");
        int layerLane2Prev = LayerMask.NameToLayer("Lane 2 Prev");
        if (currentState == LaneChangeState.ControlledAccess)
        {
            carCamera.cullingMask = targetLane.Equals(lane1Mesh) ? (1 << layerLane1) | (1 << layerLane2Prev) : (1 << layerLane2) | (1 << layerLane1Prev);            
        }
        else
        {
            carCamera.cullingMask = targetLane.Equals(lane1Mesh) ? 1 << layerLane1 : 1 << layerLane2;
        }
        // followCamera.cullingMask = targetLane.Equals(lane1Mesh) ? lane1Mask : lane2Mask;
    }

    private bool ShouldEndAllEpisodes()
    {
        return currentEpisodeInt != shouldEndAllEpisodesNotifier.name;
    }

    private void SetAllEpisodesToEnd()
    {
        var currentInt = int.Parse(shouldEndAllEpisodesNotifier.name);
        currentInt += 1;
        shouldEndAllEpisodesNotifier.name = currentInt.ToString();
    }
}
