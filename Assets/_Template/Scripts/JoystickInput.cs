using MenteBacata.ScivoloCharacterControllerDemo;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using TouchControlsKit;
using MirraGames.SDK;

public class JoystickInput : MonoBehaviour
{
    [SerializeField] SimpleCharacterController simpleCharacterController;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform playerTransform;
    //[SerializeField] YandexSDK yandexSDK;
    [SerializeField] bool isMobile;
    [SerializeField] GameObject mobileControl;
    [SerializeField] private OrbitingCamera m_Camera;

    Vector3 movementVector;
    float x, y;

    private void Awake()
    {
        Assert.IsNotNull(m_Camera);
    }

    private void Start()
    {
        // Initialize mobile controls if needed
        //yandexSDK = FindObjectOfType<YandexSDK>();
    }

    public void SetMobile(bool Mobile)
    {
        isMobile = Mobile;
        if (mobileControl != null)
        {
            mobileControl.SetActive(isMobile);
        }
    }

    public void OnJumpButton()
    {
        simpleCharacterController.SetJoystickJump();
    }

    private void Update()
    {
        if (MirraSDK.Time.Scale <= 0f)
        {
            return;
        }

        x = 0f;
        y = 0f;

        if(isMobile)
        {
            // Use TCK input system for mobile
            Vector2 move = TCKInput.GetAxis("Joystick");

            const float DeadZone = 0.02f;
            if (Mathf.Abs(move.x) > DeadZone)
            {
                x = move.x;
            }
            if (Mathf.Abs(move.y) > DeadZone)
            {
                y = move.y;
            }

            if (TCKInput.GetAction("Jump", EActionEvent.Down))
            {
                OnJumpButton();
            }

            int FrameStamp;
            Vector2 AxisLook;
            (AxisLook, FrameStamp) = TCKInput.GetRelativeAxis("Touchpad");
            m_Camera.UpdateTouchpad(AxisLook, FrameStamp);
        }           
        else
        {
            // Use keyboard input for desktop
            x = Input.GetAxis("Horizontal");
            y = Input.GetAxis("Vertical");
        }

        // Calculate movement direction relative to camera
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, playerTransform.up).normalized;
        Vector3 right = Vector3.Cross(playerTransform.up, forward);
        movementVector = x * right + y * forward;

        // Apply movement
        simpleCharacterController.SetJoyStickMovement(movementVector);
    }

    public bool isJoystickMobile()
    {
        return isMobile;
    }
    
}



