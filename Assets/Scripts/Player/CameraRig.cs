using UnityEngine;
using UnityEngine.InputSystem;

namespace EiraNova
{
    /// <summary>Cámara en órbita en tercera persona con el ratón.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public float distance = 7f;
        public float height = 1.7f;
        public float sensitivity = 3f;
        public float pitchMin = -20f;
        public float pitchMax = 45f;
        public float minZoom = 4f;
        public float maxZoom = 16f;
        public float smoothTime = 0.12f;

        float _yaw = 0f;
        float _pitch = 10f;
        Vector3 _vel;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            var kb = Keyboard.current;
            var ms = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (ms != null && ms.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (ms != null)
            {
                Vector2 delta = ms.delta.ReadValue();
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    _yaw += delta.x * sensitivity;
                    _pitch -= delta.y * sensitivity;
                    _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
                }

                float wheel = ms.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.001f)
                    distance = Mathf.Clamp(distance - wheel * 0.5f, minZoom, maxZoom);
            }

            if (target == null)
            {
                if (PlayerAbilities.Instance != null)
                    target = PlayerAbilities.Instance.transform;
                else return;
            }

            Vector3 pivot = target.position + Vector3.up * height;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = pivot - rot * Vector3.forward * distance;

            // Evitar que la cámara atraviese el suelo
            if (desired.y < 0.25f) desired.y = 0.25f;

            if (Physics.Linecast(pivot, desired, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
                desired = hit.point + hit.normal * 0.15f;

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);
            transform.LookAt(pivot + Vector3.up * 0.2f);
        }
    }
}