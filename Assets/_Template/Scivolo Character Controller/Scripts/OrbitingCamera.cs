using UnityEngine;
using MirraGames.SDK;

namespace MenteBacata.ScivoloCharacterControllerDemo
{
    public class OrbitingCamera : MonoBehaviour
    {
        public Transform target;

        public float verticalOffset = 0f;

        public float distance = 5f;

        [SerializeField] private float m_ConstSensitivityModifier = 10f;
        private float sensitivity = 1f;

        private float yRot = 0f;

        private float xRot = 20f;

        Vector2 touchStart, touchEnd;
        private int m_LastFrameLookStamp = 0;

        Rect blockZone1;
        Rect blockZone2;
        [SerializeField] Transform joystickRect;
        [SerializeField] Transform jumpButtonRect;
        [SerializeField] float mobileCameraSens = 12f;
        bool isTouch;
        bool isMobile;
        
        private void Start()
        {
            SetSensitivity(sensitivity);
        }

        private void FixedUpdate()
        {
            var joystickRectTransform = joystickRect.GetComponent<RectTransform>();
            blockZone1 = new Rect(new Vector2(0, 0), joystickRectTransform.sizeDelta + new Vector2(50, 50));
            blockZone1.center = new Vector2(joystickRect.position.x, joystickRect.position.y);

            var jumpButtonRectTransform = jumpButtonRect.GetComponent<RectTransform>();
            blockZone2 = new Rect(new Vector2(0, 0), jumpButtonRectTransform.sizeDelta + new Vector2(50, 50));
            blockZone2.center = new Vector2(jumpButtonRect.position.x, jumpButtonRect.position.y);
        }

        private void LateUpdate()
        {
            if (MirraSDK.Time.Scale <= 0f)
            {
                return;
            }

            xRot = Mathf.Clamp(xRot, 0f, 85f);

            Quaternion worldRotation = transform.parent != null ? transform.parent.rotation : Quaternion.FromToRotation(Vector3.up, target.up);
            Quaternion cameraRotation = worldRotation * Quaternion.Euler(xRot, yRot, 0f);
            Vector3 targetToCamera = cameraRotation * new Vector3(0f, 0f, -distance);

            transform.SetPositionAndRotation(target.TransformPoint(0f, verticalOffset, 0f) + targetToCamera, cameraRotation);
        }

        public void UpdateMouse(Vector2 AxisLook)
        {
            xRot -= AxisLook.y * (sensitivity);
            yRot += AxisLook.x * (sensitivity);
        }

        public void UpdateTouchpad(Vector2 AxisLook, int FrameStamp)
        {
            if (FrameStamp != m_LastFrameLookStamp)
            {
                m_LastFrameLookStamp = FrameStamp;

                xRot -= AxisLook.y * (sensitivity * mobileCameraSens);
                yRot += AxisLook.x * (sensitivity * mobileCameraSens);
            }
        }

        public void SetSensitivity(float Value)
        {
            sensitivity = Value * m_ConstSensitivityModifier;
        }

        public void SetMobile(bool state)
        {
            isMobile = state;
        }

        public void h0()
        {
            // Touch touch = Input.GetTouch(0);
        }
    }
    
}
