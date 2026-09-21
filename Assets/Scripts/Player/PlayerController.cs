using UnityEngine;
using UnityEngine.InputSystem;

namespace EiraNova
{
    /// <summary>Control del personaje en tercera persona (WASD, correr, saltar, agacharse).</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float crouchSpeed = 2.6f;
        public float jumpSpeed = 6f;
        public float rotationSpeed = 10f;
        public float gravity = -22f;

        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }

        /// <summary>Bloquea el movimiento durante cinemáticas (intro del Nivel 2).</summary>
        public bool ControlLocked { get; private set; }

        CharacterController _cc;
        Vector3 _velocity;
        Transform _visual;

        void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();
        }

        void Start()
        {
            RefreshVisual();
        }

        void RefreshVisual()
        {
            _visual = transform.Find("Eira");
            if (_visual == null && transform.childCount > 0) _visual = transform.GetChild(0);
        }

        public void SetControlLocked(bool locked)
        {
            ControlLocked = locked;
            if (locked) _velocity = Vector3.zero;
        }

        void Update()
        {
            if (ControlLocked) return;
            if (GameDirector.Instance == null || GameDirector.Instance.Defeated) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

            IsSprinting = kb.leftShiftKey.isPressed;
            IsCrouching = kb.leftCtrlKey.isPressed;

            // Orientación desde la cámara (plano horizontal)
            Vector3 camF = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            camF.y = 0f; camF.Normalize();
            Vector3 camR = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            camR.y = 0f; camR.Normalize();

            Vector3 input = (camF * v + camR * h);
            if (input.magnitude > 1f) input.Normalize();

            float speed = IsCrouching ? crouchSpeed : (IsSprinting ? runSpeed : walkSpeed);

            // Altura de agachado
            float targetHeight = IsCrouching ? 1.0f : 1.6f;
            float targetCenterY = IsCrouching ? 0.5f : 0.8f;
            if (Mathf.Abs(_cc.height - targetHeight) > 0.01f)
            {
                _cc.height = Mathf.Lerp(_cc.height, targetHeight, Time.deltaTime * 10f);
                var c = _cc.center;
                c.y = Mathf.Lerp(c.y, targetCenterY, Time.deltaTime * 10f);
                _cc.center = c;
            }

            _velocity.y += gravity * Time.deltaTime;

            if (_cc.isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            if (_cc.isGrounded && kb.spaceKey.isPressed && !IsCrouching)
                _velocity.y = jumpSpeed;

            Vector3 move = input * speed;
            _cc.Move(new Vector3(move.x, 0f, move.z) * Time.deltaTime + new Vector3(0f, _velocity.y * Time.deltaTime, 0f));

            // Girar el muñeco hacia la dirección del movimiento
            if (input.sqrMagnitude > 0.001f && _visual != null)
            {
                Quaternion targetRot = Quaternion.LookRotation(input);
                _visual.rotation = Quaternion.Slerp(_visual.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        public bool IsMoving { get { return _cc != null && new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).sqrMagnitude > 0.01f; } }

        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _velocity = Vector3.zero;
            _cc.enabled = true;
        }

        public void FlashHit()
        {
            if (GameDirector.Instance) GameDirector.Instance.FlashDamage(0.45f);
        }
    }
}