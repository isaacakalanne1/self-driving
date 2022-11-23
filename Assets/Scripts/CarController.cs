using UnityEngine;

public enum CurrentLane
{
    Low,
    High
}

public class CarController : MonoBehaviour
{

    private float turnValue;
    private int verticalInput;
    private const int MinVerticalInput = 6;
    private const int MaxVerticalInput = 10;
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

    public int GetInitialVerticalInput(CurrentLane currentLane)
    {
        return currentLane == CurrentLane.Low ? MinVerticalInput : MaxVerticalInput;
    }

    public void SetTurnValue(int value)
    {
        turnValue = value;
    }

    public void SetVerticalInput(int value)
    {
        verticalInput = value;
    }

    public int GetVerticalInput()
    {
        return verticalInput;
    }

    private void FixedUpdate() {
        // GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    public void SetInput(int turn, CurrentLane currentLane)
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
        switch (currentLane)
        {
            case CurrentLane.Low:
                if (verticalInput > MinVerticalInput)
                {
                    verticalInput -= 1;
                }
                break;  
            case CurrentLane.High:
                if (verticalInput < MaxVerticalInput)
                {
                    verticalInput += 1;
                }
                break;
        }
        Debug.Log("Vertical input is " + verticalInput);
    }

    public float GetReward()
    {
        return verticalInput;
    }

    private void HandleMotor() {
        frontLeftWheelCollider.motorTorque = verticalInput/(float)MaxVerticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput/(float)MaxVerticalInput * motorForce;
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
