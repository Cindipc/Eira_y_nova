using System.Collections;
using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Soldado androide de patrulla: camina, ve, persigue y dispara proyectiles lentos.
    /// Solo se detiene cuando Eira usa su PULSO (onda electromagnética): no se puede
    /// hackear de cerca ni detener de otra forma.
    /// </summary>
    public class AndroidSoldier : Interactable
    {
        public Vector3[] waypoints;
        public float patrolSpeed = 1.6f;
        public float chaseSpeed = 4.5f;
        public float detectRange = 10f;
        public float shootRange = 9f;
        public float contactDamage = 20f;

        int _wp;
        bool _chasing;
        bool _stunned;
        float _stunUntil;
        float _shootCd = 1.5f;
        Material _eyeMat;
        Transform _player;
        float _spotCooldown;

        public void Init(Vector3[] wps, Material eyeMat)
        {
            waypoints = wps;
            _eyeMat = eyeMat;
            PromptText = "";
            IsAvailable = false; // no se puede interactuar: solo el pulso lo detiene
        }

        void Start()
        {
            if (waypoints == null || waypoints.Length == 0)
                waypoints = new[] { transform.position };
            _player = PlayerAbilities.Instance != null ? PlayerAbilities.Instance.transform : null;
        }

        public bool IsStunned { get { return _stunned; } }

        /// <summary>El PULSO de Eira desconecta temporalmente al androide.</summary>
        public void Stun(float seconds)
        {
            _stunned = true;
            _stunUntil = Time.time + seconds;
            _chasing = false;
            if (_eyeMat != null) _eyeMat.color = new Color(0.30f, 0.35f, 0.55f);
        }

        void Update()
        {
            if (GameDirector.Instance != null && GameDirector.Instance.IntroActive) return;
            if (_player == null && PlayerAbilities.Instance != null)
                _player = PlayerAbilities.Instance.transform;
            if (_player == null || GameDirector.Instance == null) return;

            if (_stunned)
            {
                if (Time.time >= _stunUntil)
                {
                    _stunned = false;
                    if (_eyeMat != null) _eyeMat.color = WorldBuilder.CyanGlow;
                }
                return;
            }

            float dist = Vector3.Distance(_player.position, transform.position);
            bool visible = HasLOS();

            if (_chasing)
            {
                if (visible && dist < shootRange * 1.5f)
                {
                    Vector3 dir = _player.position - transform.position;
                    dir.y = 0f; dir.Normalize();
                    transform.position += dir * chaseSpeed * Time.deltaTime;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                    if (dist < 1.6f && _shootCd <= 0f)
                    {
                        PlayerAbilities.Instance?.TakeHeartDamage(contactDamage);
                        _shootCd = 2.5f;
                    }
                    if (_shootCd <= 0f && dist < shootRange)
                        Fire();
                }
                else if (!visible || dist > shootRange * 2.2f)
                {
                    _chasing = false;
                }
            }
            else
            {
                Vector3 target = waypoints[_wp];
                transform.position = Vector3.MoveTowards(transform.position, target, patrolSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, target) < 0.3f)
                    _wp = (_wp + 1) % waypoints.Length;
                Vector3 fw = target - transform.position; fw.y = 0f;
                if (fw.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fw), Time.deltaTime * 3f);

                if (visible && dist < detectRange)
                {
                    _chasing = true;
                    if (_spotCooldown <= 0f)
                    {
                        _spotCooldown = 20f;
                        GameDirector.Instance?.ShowDialogue("EIRA", "¡Un androide! No puedo hackearlo... ¡usa tu pulso (Q) para desconectarlo o escapa!", 4.5f);
                        if (GameDirector.Instance != null && GameDirector.Instance.Nova != null)
                            GameDirector.Instance.Nova.Say("No los toques de cerca, Eira. Tu pulso los detiene.", null);
                    }
                }
            }

            if (_spotCooldown > 0f) _spotCooldown -= Time.deltaTime;
            if (_shootCd > 0f) _shootCd -= Time.deltaTime;
        }

        bool HasLOS()
        {
            Vector3 eyes = transform.position + Vector3.up * 1.3f;
            Vector3 head = _player.position + Vector3.up * 1.2f;
            // Sin obstáculo => línea de visión libre.
            if (!Physics.Linecast(eyes, head, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
                return true;
            // Impactar al propio objetivo (o a su compañera NOVA) NO bloquea la visión.
            if (hit.collider.GetComponentInParent<PlayerAbilities>() != null) return true;
            if (hit.collider.GetComponentInParent<NovaCompanion>() != null) return true;
            return false;
        }

        void Fire()
        {
            _shootCd = 2.6f;
            if (_eyeMat != null) _eyeMat.color = Color.red;
            Vector3 dir = (_player.position - transform.position).normalized;
            dir.y = 0f;
            var bolt = WorldBuilder.Sphere("Bolt", transform.position + Vector3.up * 1.4f + dir, 0.12f,
                WorldBuilder.Mat("Bolt", WorldBuilder.OrangeGlow, true));
            var b = bolt.AddComponent<EnemyBolt>();
            b.dir = dir;
            b.speed = 9f;
            b.damage = 25f;
            StartCoroutine(ResetEye());
        }

        IEnumerator ResetEye()
        {
            yield return new WaitForSeconds(0.4f);
            if (!_stunned && _eyeMat != null) _eyeMat.color = WorldBuilder.CyanGlow;
        }

        public override void Interact() { }
    }

    /// <summary>Proyectil del soldado.</summary>
    public class EnemyBolt : MonoBehaviour
    {
        public Vector3 dir;
        public float speed = 9f;
        public float damage = 25f;
        float _life = 4f;

        void Update()
        {
            transform.position += dir * speed * Time.deltaTime;
            if (_life < 0f) Destroy(gameObject);
            _life -= Time.deltaTime;

            if (PlayerAbilities.Instance != null &&
                Vector3.Distance(transform.position, PlayerAbilities.Instance.transform.position) < 0.7f)
            {
                PlayerAbilities.Instance.TakeHeartDamage(damage);
                GameDirector.Instance?.FlashDamage(0.6f);
                Destroy(gameObject);
            }
        }
    }
}