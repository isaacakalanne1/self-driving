using System;
using System.Collections.Generic;
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
    
    [SerializeField] private Material targetLaneMaterial;
    [SerializeField] private Material outOfBoundsMaterial;
    [SerializeField] private MeshRenderer lane1Mesh;
    [SerializeField] private MeshRenderer lane2Mesh;
    [SerializeField] private MeshRenderer terrainMesh;
    [SerializeField] private MeshRenderer shouldEndAllEpisodesNotifier;

    private EpisodeBeginData[] listOfEpisodeBeginData;
    private string currentEpisodeInt = "0";

    private CarController carController;
    private int episodeBeginIndex;
    private MeshRenderer targetLane;
    private int triggerLaneChangeMaxCount;
    private int triggerLaneChangeCounter;
    private const int LaneChangePermittedMaxCount = 50;

    private CurrentLane currentLane = CurrentLane.Low;

    private LaneChangeState currentState;
    private LaneChangeState previousState;

    private void Awake()
    {
        shouldEndAllEpisodesNotifier.name = "0";
        carController = GetComponent<CarController>();
        // episodeBeginIndex = GetIndexFromString(carController.name, "Sedan");
        listOfEpisodeBeginData = CreateListOfEpisodeBeginData();
        Camera.onPreRender += OnPreRenderCallback;
    }
    
    void OnPreRenderCallback(Camera cam)
    {
        string indexString = cam.name.Replace("Car Camera Sedan", "");
        indexString = indexString.Replace("Follow Camera Sedan", "");
        indexString = indexString.Replace("Sedan", "");
        int cameraIndex = int.Parse(indexString);
        
        if (cameraIndex == episodeBeginIndex)
        {
            UpdateLaneMaterials();
        }
    }

    private int GetIndexFromString(string inputString, string stringToRemove)
    {
        var indexString = inputString.Replace(stringToRemove, "");
        return int.Parse(indexString);
    }

    private EpisodeBeginData[] CreateListOfEpisodeBeginData()
    {
        EpisodeBeginData[] list = {
            new (new Vector3((float)11.552,(float)-8.04,(float)20.226),
                Quaternion.Euler(0, 120, 0),
                lane1Mesh),
            new (new Vector3((float)12.262,(float)-8.04,(float)18.901),
                Quaternion.Euler(0, 120, 0),
                lane2Mesh),
            new (new Vector3((float)15.108,(float)-8.04,(float)17.932),
                Quaternion.Euler(0, 120, 0),
                lane1Mesh),
            new (new Vector3((float)15.59,(float)-8.04,(float)16.499),
                Quaternion.Euler(0, 150, 0),
                lane2Mesh),
            new (new Vector3((float)17.129,(float)-8.04,(float)15.065),
                Quaternion.Euler(0, 170, 0),
                lane1Mesh),
            new (new Vector3((float)16.109,(float)-8.04,(float)12.997),
                Quaternion.Euler(0, 195, 0),
                lane2Mesh),
            new (new Vector3((float)16.066,(float)-8.04,(float)10.635),
                Quaternion.Euler(0, 200, 0),
                lane1Mesh),
            new (new Vector3((float)14.76,(float)-8.04,(float)9.95),
                Quaternion.Euler(0, 210, 0),
                lane2Mesh),
            new (new Vector3((float)14.75,(float)-8.04,(float)8.45),
                Quaternion.Euler(0, 210, 0),
                lane1Mesh),
            new (new Vector3((float)13.627,(float)-8.04,(float)7.775),
                Quaternion.Euler(0, 205, 0),
                lane2Mesh)
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
        sensor.AddObservation(carController.GetVerticalInput());
    }

    public override void OnEpisodeBegin()
    {
        shouldEndAllEpisodesNotifier.material = outOfBoundsMaterial;
        ResetCar();
        ResetLaneChangeStates();
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
        
        carController.SetTurnValue(0);
        carController.SetVerticalInput(carController.GetInitialVerticalInput());
        
        Random rnd = new Random();
        episodeBeginIndex = rnd.Next(0, 10);
        EpisodeBeginData data = listOfEpisodeBeginData[episodeBeginIndex];
        transform.localPosition = data.position;
        transform.localRotation = data.rotation;
        targetLane = data.initialLane;
        currentLane = targetLane.Equals(lane1Mesh) ? CurrentLane.Low : CurrentLane.High;
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
                    UpdateLaneMaterials();
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
        SetAllEpisodesToEnd();
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
