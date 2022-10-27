using UnityEngine;

public class CarController : MonoBehaviour
{

    private const string Horizontal = "Horizontal";
    private const string Vertical = "Vertical";

    public float turnValue;
    public int verticalInput;
    public int minVerticalInput = 8;
    public int initialVerticalInput = 10;
    private int maxVerticalInput = 15;
    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isBraking;

    [SerializeField] private float motorForce;
    [SerializeField] private float brakeForce;
    [SerializeField] public float maxSteeringAngle;
    [SerializeField] private float softTurn;

    [SerializeField] public WheelCollider frontLeftWheelCollider;
    [SerializeField] public WheelCollider frontRightWheelCollider;
    [SerializeField] public WheelCollider backLeftWheelCollider;
    [SerializeField] public WheelCollider backRightWheelCollider;

    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform backLeftWheelTransform;
    [SerializeField] private Transform backRightWheelTransform;
    
    public GameObject person1;

    private void FixedUpdate() {
        // GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    public void SetInput(int turn, int motor)
    {
        turnValue = 0;
        switch (turn)
        {
            case 1:
                turnValue = -softTurn;
                break;
            case 2:
                turnValue = +softTurn;
                break;  
        }
        switch (motor)
        {
            case 1:
                if (verticalInput < maxVerticalInput)
                {
                    verticalInput += 1;
                }
                break;
            case 2:
                if (verticalInput > minVerticalInput)
                {
                    verticalInput -= 1;
                }
                break;  
        }
    }

    public float GetReward()
    {
        return verticalInput;
    }
    
    public float[] GetRelativeDistanceAndDirectionOfPerson()
    {
        person1 = GameObject.Find("Person1");
        var egoPosition = transform.position;
        var personPosition = person1.transform.position;
        var distance = Vector3.Distance (egoPosition, personPosition);
        var relativeDirection = (egoPosition - personPosition) - transform.forward;
        float[] distanceAndDirection = { distance, relativeDirection.x, relativeDirection.y, relativeDirection.z };
        return distanceAndDirection;
    }

    private void HandleMotor() {
        frontLeftWheelCollider.motorTorque = verticalInput/(float)initialVerticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput/(float)initialVerticalInput * motorForce;
        ApplyBraking();
    }

    private void ApplyBraking() {
        isBraking = Input.GetKey(KeyCode.Space);
        currentBrakeForce = isBraking ? brakeForce : 0f;
        
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        backLeftWheelCollider.brakeTorque = currentBrakeForce;
        backRightWheelCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        var newSteerAngle = frontLeftWheelCollider.steerAngle + turnValue;
        if (newSteerAngle < -maxSteeringAngle)
        {
            newSteerAngle = -maxSteeringAngle;
        }
        if (newSteerAngle > maxSteeringAngle)
        {
            newSteerAngle = maxSteeringAngle;
        }
        frontLeftWheelCollider.steerAngle = newSteerAngle;
        frontRightWheelCollider.steerAngle = newSteerAngle;
    }

    private void UpdateWheels() {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(backLeftWheelCollider, backLeftWheelTransform);
        UpdateSingleWheel(backRightWheelCollider, backRightWheelTransform);
    }

    private static void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform) {
        wheelCollider.GetWorldPose(out var pos, out var rot);
        wheelTransform.rotation = rot;
        // wheelTransform.position = pos;
    }

}
