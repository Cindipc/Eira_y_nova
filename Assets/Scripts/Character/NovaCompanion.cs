using UnityEngine;

namespace EiraNova
{
    /// <summary>NOVA sigue a Eira y comenta los sucesos del nivel.</summary>
    public class NovaCompanion : MonoBehaviour
    {
        public static NovaCompanion Instance { get; private set; }

        public float followDistance = 2.8f;
        public float maxLeash = 18f;

        CharacterController _cc;
        Vector3 _velocity;

        void Awake() { Instance = this; _cc = gameObject.AddComponent<CharacterController>(); }

        void Start()
        {
            var c = _cc;
            c.height = 1.6f;
            c.radius = 0.3f;
            c.center = new Vector3(0f, 0.8f, 0f);
        }

        public void Say(string text, float? time)
        {
            if (GameDirector.Instance != null)
                GameDirector.Instance.ShowDialogue("NOVA", text, time ?? (3f + text.Length * 0.09f));
        }

        void Update()
        {
            if (GameDirector.Instance == null || GameDirector.Instance.Defeated)
            {
                _velocity = Vector3.zero;
                return;
            }

            var player = PlayerAbilities.Instance != null ? PlayerAbilities.Instance.transform : null;
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > maxLeash)
            {
                _cc.enabled = false;
                transform.position = player.position + player.forward * followDistance + Vector3.up * 0.2f;
                _cc.enabled = true;
            }
            else if (dist > followDistance)
            {
                Vector3 dir = player.position - transform.position;
                dir.y = 0f; dir.Normalize();
                _cc.Move(dir * 3.4f * Time.deltaTime);
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
            }

            if (_cc.isGrounded) _velocity.y = -2f;
            else _velocity.y -= 22f * Time.deltaTime;
            _cc.Move(_velocity * Time.deltaTime);
        }
    }
}