using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Marcha articulada para personajes construidos con primitivas.
    /// El cuerpo se divide en pivotes (cadera y hombro) por extremidad y, al moverse,
    /// las piernas se balancean de forma alternada (left+right), los brazos en
    /// contrafase y el torso hace un vaivén/bamboleo natural, como una persona real.
    /// Se desactiva automáticamente al estar quieto o en el aire.
    /// </summary>
    public class CharacterGait : MonoBehaviour
    {
        public Transform hipL;
        public Transform hipR;
        public Transform shL;
        public Transform shR;

        /// <summary>Grupo del torso/cabeza (se bambolea sobre las piernas).</summary>
        public Transform body;

        public float legSwing = 26f;
        public float armSwing = 16f;
        public float bodyBob = 0.045f;
        public float bodySway = 3.5f;
        public float bodyLean = 4.5f;
        public float minStepRate = 3.5f;
        public float maxStepRate = 10f;

        CharacterController _cc;
        float _phase;
        float _amp;
        Vector3 _bodyBase;
        Quaternion _bodyBaseRot;

        public void Setup(Transform l, Transform r, Transform sl, Transform sr, Transform b)
        {
            hipL = l; hipR = r; shL = sl; shR = sr; body = b;
            CacheBase();
        }

        void Awake() { CacheBase(); }

        void CacheBase()
        {
            if (body != null)
            {
                _bodyBase = body.localPosition;
                _bodyBaseRot = body.localRotation;
            }
        }

        void LateUpdate()
        {
            if (_cc == null) _cc = GetComponentInParent<CharacterController>();

            float speed = _cc != null ? new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude : 0f;
            bool moving = _cc != null && _cc.isGrounded && speed > 0.15f;

            _amp = Mathf.MoveTowards(_amp, moving ? 1f : 0f, Time.deltaTime * 6f);

            if (_amp > 0f)
            {
                float rate = Mathf.Lerp(minStepRate, maxStepRate, Mathf.Clamp01(speed / 5f));
                _phase += Time.deltaTime * rate;
            }

            float s = Mathf.Sin(_phase);
            float c = Mathf.Abs(Mathf.Cos(_phase));

            // Piernas alternando (izquierda delante, derecha detrás y viceversa).
            if (hipL != null) hipL.localRotation = Quaternion.Euler(-s * legSwing * _amp, 0f, 0f);
            if (hipR != null) hipR.localRotation = Quaternion.Euler(s * legSwing * _amp, 0f, 0f);

            // Brazos en contrafase con la pierna del mismo lado.
            if (shL != null) shL.localRotation = Quaternion.Euler(s * armSwing * _amp, 0f, 0f);
            if (shR != null) shR.localRotation = Quaternion.Euler(-s * armSwing * _amp, 0f, 0f);

            if (body != null)
            {
                float bob = c * bodyBob * _amp;
                float sway = Mathf.Sin(_phase * 0.5f) * bodySway * _amp;
                float lean = bodyLean * _amp;
                body.localPosition = _bodyBase + Vector3.up * bob;
                body.localRotation = _bodyBaseRot * Quaternion.Euler(lean, 0f, sway);
            }
        }
    }
}