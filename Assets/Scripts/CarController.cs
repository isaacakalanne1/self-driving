using UnityEngine;

public class CarController : MonoBehaviour
{

    private const string Horizontal = "Horizontal";
    private const string Vertical = "Vertical";

    private float turnValue;
    private float verticalInput;
    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isBraking;

    [SerializeField] private float motorForce;
    [SerializeField] private float brakeForce;
    [SerializeField] public float maxSteeringAngle;
    [SerializeField] private float softTurn;
    [SerializeField] private int hardTurn;

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

    public void SetInput(int action)
    {
        verticalInput = 1;
        turnValue = 0;
        switch (action)
        {
            case 1:
                turnValue = -softTurn;
                break;
            case 2:
                turnValue = +softTurn;
                break;
            case 3:
                turnValue = -hardTurn;
                break;
            case 4:
                turnValue = +hardTurn;
                break;
        }
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
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
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
        wheelTransform.localRotation = rot;
        // wheelTransform.localPosition = pos;
    }

}
