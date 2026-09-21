using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Animación de marcha para mallas <b>sin esqueleto</b> (los .obj base no traen
    /// huesos, así que no se pueden mover solo las piernas). Simula el caminar con
    /// un ciclo de rebote vertical, balanceo lateral e inclinación al avanzar; en
    /// reposo aplica una respiración sutil.
    /// </summary>
    public class CharacterWalk : MonoBehaviour
    {
        /// <summary>Transform de la malla a animar (hijo del root).</summary>
        public Transform visual;

        public float bobHeight = 0.05f;
        public float swayAngle = 6f;
        public float leanAngle = 7f;
        public float yawSway = 4f;
        public float stepFrequency = 1.6f;
        public float idleBob = 0.006f;

        float _phase;
        float _amp;
        Vector3 _basePos;
        Quaternion _baseRot;

        void Start()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual == null) visual = transform;
            _basePos = visual.localPosition;
            _baseRot = visual.localRotation;
        }

        void LateUpdate()
        {
            if (visual == null) return;

            bool moving = false;
            var pc = GetComponentInParent<PlayerController>();
            if (pc != null && !pc.ControlLocked) moving = pc.IsMoving;

            _amp = Mathf.MoveTowards(_amp, moving ? 1f : 0f, Time.deltaTime * 6f);
            _phase += Time.deltaTime * stepFrequency * Mathf.PI * 2f;

            float bob = Mathf.Abs(Mathf.Sin(_phase)) * bobHeight * _amp;
            float breathe = Mathf.Sin(Time.time * 1.6f) * idleBob;
            float roll = Mathf.Sin(_phase) * swayAngle * _amp;
            float lean = leanAngle * _amp;
            float yaw = Mathf.Sin(_phase * 0.5f) * yawSway * _amp;

            visual.localPosition = _basePos + new Vector3(0f, bob + breathe, 0f);
            visual.localRotation = _baseRot * Quaternion.Euler(lean, yaw, roll);
        }
    }
}
