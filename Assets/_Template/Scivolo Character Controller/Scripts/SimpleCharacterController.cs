using MenteBacata.ScivoloCharacterController;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using MirraGames.SDK;

namespace MenteBacata.ScivoloCharacterControllerDemo
{
    
    public class SimpleCharacterController : MonoBehaviour
    {
        public event System.Action OnJumped; 
        public event System.Action OnStuck; 

        [Header("Stuck Detection")]
        [SerializeField] private float m_StuckXZThreshold = 0.1f;
        [SerializeField] private float m_StuckYThreshold = 0.1f;
        [SerializeField] private float m_StuckTimeThreshold = 1f;

        private bool m_LookingForStuck = false;
        private float m_PotentialStuckTimeElapsed = 0f;
        private Vector3 m_PotentialStuckPosition = Vector3.zero;

        [Header("Other")]
        public float moveSpeed = 5f;

        public float jumpSpeed = 8f;
        
        public float rotationSpeed = 720f;

        public float gravity = -25f;

        public CharacterCapsule capsule;

        public CharacterMover mover;

        public GroundDetector groundDetector;

        public MeshRenderer groundedIndicator;
        
        
        private const float minVerticalSpeed = -12f;


        // Allowed time before the character is set to ungrounded from the last time he was safely grounded.
        private const float timeBeforeUngrounded = 0.02f;

        // Speed along the character local up direction.
        private float verticalSpeed = 0f;

        // Time after which the character should be considered ungrounded.
        private float nextUngroundedTime = -1f;

        private Transform cameraTransform;

        private Collider[] overlaps = new Collider[5];

        private int overlapCount;

        private MoveContact[] moveContacts = CharacterMover.NewMoveContactArray;

        private int contactCount;

        private bool isOnMovingPlatform = false;

        private MovingPlatform movingPlatform;

        [SerializeField] private Animator animator;

        private Vector3 m_JoystickVector;
        public Vector3 JoystickVector => m_JoystickVector;

        bool isJoystickJump;

        private void Start()
        {
            cameraTransform = Camera.main.transform;
            mover.canClimbSteepSlope = true;

            Assert.IsNotNull(animator);
        }

        private void Update()
        {
            if (MirraSDK.Time.Scale <= 0f)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Vector3 movementInput = GetMovementInput();
            
            movementInput = m_JoystickVector;
            animator.SetFloat("Movement", movementInput.sqrMagnitude);
            

            Vector3 velocity = moveSpeed * movementInput;
            
            HandleOverlaps();

            bool groundDetected = DetectGroundAndCheckIfGrounded(out bool isGrounded, out GroundInfo groundInfo);

            SetGroundedIndicatorColor(isGrounded);

            isOnMovingPlatform = false;

            if ((isGrounded && (Input.GetButtonDown("Jump") || isJoystickJump)))
            {
                animator.SetBool("isJump", true);
                verticalSpeed = jumpSpeed;
                nextUngroundedTime = -1f;
                isGrounded = false;

                OnJumped?.Invoke();
            }

            if (isGrounded)
            {
                animator.SetBool("isJump", false);
                
                mover.mode = CharacterMover.Mode.Walk;
                verticalSpeed = 0f;

                if (groundDetected)
                    isOnMovingPlatform = groundInfo.collider.TryGetComponent(out movingPlatform);
            }
            else
            {
                mover.mode = CharacterMover.Mode.SimpleSlide;

                BounceDownIfTouchedCeiling();

                verticalSpeed += gravity * deltaTime;
                
                if (verticalSpeed < minVerticalSpeed)
                    verticalSpeed = minVerticalSpeed;
                isJoystickJump = false;
                velocity += verticalSpeed * transform.up;
            }

            RotateTowards(velocity);

            mover.Move(velocity * deltaTime, groundDetected, groundInfo, overlapCount, overlaps, moveContacts, out contactCount);
            animator.SetFloat("FallSpeed", verticalSpeed);

            // Stuck detection
            if (isGrounded)
            {
                m_LookingForStuck = false;
                return;
            }

            if (!m_LookingForStuck)
            {
                m_PotentialStuckTimeElapsed = 0f;
                m_PotentialStuckPosition = transform.position;
                m_LookingForStuck = true;

                return;
            }

            // Check XZ first
            Vector3 CurrentXZ = transform.position;
            CurrentXZ.y = 0f;

            Vector3 PreviousXZ = m_PotentialStuckPosition;
            PreviousXZ.y = 0f;

            if ((CurrentXZ - PreviousXZ).sqrMagnitude >= m_StuckXZThreshold * m_StuckXZThreshold)
            {
                m_PotentialStuckTimeElapsed = 0f;
                m_PotentialStuckPosition = transform.position;
                return;
            }

            if (System.MathF.Abs(transform.position.y - m_PotentialStuckPosition.y) >= m_StuckYThreshold)
            {
                m_PotentialStuckTimeElapsed = 0f;
                m_PotentialStuckPosition = transform.position;
                return;
            }

            m_PotentialStuckTimeElapsed += Time.deltaTime;

            if (m_PotentialStuckTimeElapsed >= m_StuckTimeThreshold)
            {
                OnStuck?.Invoke();
                return;
            }
        }

        private void LateUpdate()
        {
            if (isOnMovingPlatform)
                ApplyPlatformMovement(movingPlatform);
        }

        public void SetJoyStickMovement(Vector3 vector3)
        {
            m_JoystickVector = vector3;
        }

        public void SetJoystickJump()
        {
            isJoystickJump = true;
        }

        public Vector3 GetMovementInput()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            //ДЖойстик
            
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, transform.up).normalized;
            Vector3 right = Vector3.Cross(transform.up, forward);

            return x * right + y * forward;
        }

        private void HandleOverlaps()
        {
            if (capsule.TryResolveOverlap())
            {
                overlapCount = 0;
            }
            else
            {
                overlapCount = capsule.CollectOverlaps(overlaps);
            }
        }

        private bool DetectGroundAndCheckIfGrounded(out bool isGrounded, out GroundInfo groundInfo)
        {
            bool groundDetected = groundDetector.DetectGround(out groundInfo);

            if (groundDetected)
            {
                if (groundInfo.isOnFloor && verticalSpeed < 0.1f)
                    nextUngroundedTime = Time.time + timeBeforeUngrounded;
            }
            else
                nextUngroundedTime = -1f;

            isGrounded = Time.time < nextUngroundedTime;
            return groundDetected;
        }

        private void SetGroundedIndicatorColor(bool isGrounded)
        {
            if (groundedIndicator != null)
            {
                
                groundedIndicator.material.color = isGrounded ? Color.green : Color.blue;
            }
        }

        private void RotateTowards(Vector3 direction)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, transform.up);

            if (flatDirection.sqrMagnitude < 1E-06f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection, transform.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void ApplyPlatformMovement(MovingPlatform movingPlatform)
        {
            GetMovementFromMovingPlatform(movingPlatform, out Vector3 movement, out float upRotation);

            transform.Translate(movement, Space.World);
            transform.Rotate(0f, upRotation, 0f, Space.Self);
        }

        private void GetMovementFromMovingPlatform(MovingPlatform movingPlatform, out Vector3 movement, out float deltaAngleUp)
        {
            movingPlatform.GetDeltaPositionAndRotation(out Vector3 platformDeltaPosition, out Quaternion platformDeltaRotation);
            Vector3 localPosition = transform.position - movingPlatform.transform.position;
            movement = platformDeltaPosition + platformDeltaRotation * localPosition - localPosition;

            platformDeltaRotation.ToAngleAxis(out float platformDeltaAngle, out Vector3 axis);
            float axisDotUp = Vector3.Dot(axis, transform.up);

            if (-0.1f < axisDotUp && axisDotUp < 0.1f)
                deltaAngleUp = 0f;
            else
                deltaAngleUp = platformDeltaAngle * Mathf.Sign(axisDotUp);
        }
        
        private void BounceDownIfTouchedCeiling()
        {
            for (int i = 0; i < contactCount; i++)
            {
                if (Vector3.Dot(moveContacts[i].normal, transform.up) < -0.7f)
                {
                    verticalSpeed = -0.25f * verticalSpeed;
                    break;
                }
            }
        }
    }
}
