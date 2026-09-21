using System.Collections;
using UnityEngine;

namespace EiraNova
{
    public enum DroneState { Patrol, Suspicious, Chase, Disabled }

    /// <summary>Dron de vigilancia: patrulla, detecta, persigue y puede ser desactivado al acercarte.</summary>
    public class EnemyDrone : Interactable
    {
        public DroneState State { get; private set; } = DroneState.Patrol;
        public Vector3[] waypoints;
        public float patrolSpeed = 2.2f;
        public float chaseSpeed = 6f;
        public float detectRange = 10f;
        public float sightRange = 14f;
        public float patrolY = 1.8f;

        int _wp;
        float _alarmTimer;
        float _attackCooldown;
        bool _wasChasing;
        Transform _visual;
        Material _eyeMat;
        Vector3 _startPos;
        Coroutine _disable;

        bool _warnedOnce;

        public void Init(Vector3[] wps, Material eyeMat)
        {
            waypoints = wps;
            _eyeMat = eyeMat;
            _startPos = waypoints != null && waypoints.Length > 0 ? waypoints[0] : transform.position;
            if (waypoints == null || waypoints.Length == 0)
                waypoints = new[] { _startPos, _startPos + Vector3.forward * 6f };
            PromptText = "[E] Desactivar dron";
        }

        void Start()
        {
            if (waypoints == null) waypoints = new[] { transform.position };

            // tomar un hijo visual como referencia (el OJO) para detectar dirección
            if (_visual == null && transform.childCount > 0) _visual = transform.GetChild(0);
        }

        void Update()
        {
            if (State == DroneState.Disabled) return;
            if (GameDirector.Instance != null && GameDirector.Instance.IntroActive) return;
            var player = PlayerAbilities.Instance != null ? PlayerAbilities.Instance.transform : null;
            if (player == null || GameDirector.Instance == null) return;

            float dist = Vector3.Distance(player.position, transform.position);
            bool visible = HasLOS(player);

            bool crouched = PlayerController.Instance != null && PlayerController.Instance.IsCrouching;
            float effectiveDetect = PlayerController.Instance != null && PlayerController.Instance.IsCrouching ? detectRange * 0.45f : detectRange;

            switch (State)
            {
                case DroneState.Patrol:
                    MoveAlongWaypoints();
                    if (visible && dist < effectiveDetect)
                    {
                        State = DroneState.Suspicious;
                        PromptText = "[E] Desactivar dron";
                        if (GameDirector.Instance) GameDirector.Instance.SetPrompt(PromptText);
                    }
                    break;

                case DroneState.Suspicious:
                    Vector3 look = (player.position - transform.position);
                    look.y = 0f; look.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 3f);
                    if (!visible || dist > effectiveDetect * 1.6f)
                        State = DroneState.Patrol;
                    else if (dist < detectRange * 0.7f && visible)
                        State = DroneState.Chase;
                    break;

                case DroneState.Chase:
                    _wasChasing = true;
                    if (visible && dist < sightRange)
                    {
                        Vector3 dir = player.position - transform.position;
                        dir.Normalize();
                        transform.position += new Vector3(dir.x, 0f, dir.z) * chaseSpeed * Time.deltaTime;
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z)), Time.deltaTime * 6f);
                        if (dist < 1.35f && _attackCooldown <= 0f)
                        {
                            DamagePlayer(28f);
                            _attackCooldown = 2.2f;
                        }
                    }
                    else
                    {
                        _alarmTimer += Time.deltaTime;
                        if (_alarmTimer > 3f)
                        {
                            State = DroneState.Patrol;
                            _alarmTimer = 0f;
                            if (_wasChasing)
                            {
                                _wasChasing = false;
                                if (GameDirector.Instance) GameDirector.Instance.AddPoints(150, "Evitar un enemigo");
                                if (GameDirector.Instance && GameDirector.Instance.Nova != null)
                                    GameDirector.Instance.Nova.Say("Respira... nos ha perdido de vista.", null);
                            }
                        }
                    }
                    break;
            }

            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;

            // Oscilar verticalmente (flotación del dron)
            float bob = Mathf.Sin(Time.time * 3f) * 0.06f;
            transform.position = new Vector3(transform.position.x, patrolY + bob, transform.position.z);
        }

        void MoveAlongWaypoints()
        {
            if (waypoints.Length < 1) return;
            Vector3 target = waypoints[_wp];
            target.y = patrolY;
            float step = patrolSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            if (Vector3.Distance(transform.position, target) < 0.2f)
                _wp = (_wp + 1) % waypoints.Length;
            Vector3 fw = (target - transform.position); fw.y = 0f;
            if (fw.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fw), Time.deltaTime * 3f);
        }

        bool HasLOS(Transform target)
        {
            Vector3 eye = transform.position + Vector3.up * 0.3f;
            Vector3 head = target.position + Vector3.up * 1.2f;
            // Sin obstáculo => línea de visión libre.
            if (!Physics.Linecast(eye, head, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
                return true;
            // Impactar al propio objetivo (o a su compañera NOVA) NO bloquea la visión.
            if (hit.collider.GetComponentInParent<PlayerAbilities>() != null) return true;
            if (hit.collider.GetComponentInParent<NovaCompanion>() != null) return true;
            return false;
        }

        void DamagePlayer(float dmg)
        {
            var a = PlayerAbilities.Instance;
            if (a == null) return;
            a.TakeHeartDamage(dmg);
            if (GameDirector.Instance && !_warnedOnce)
            {
                _warnedOnce = true;
                GameDirector.Instance.ShowDialogue("EIRA", "¡Esos drones! (Puedo huir o acercarme y desactivarlos)", 4f);
            }
        }

        // -------------------------------------------------------------
        // INTERACCIÓN: desactivar el dron
        // -------------------------------------------------------------
        public override void Interact()
        {
            if (State == DroneState.Chase || State == DroneState.Disabled) return;
            if (_disable != null) return;
            _disable = StartCoroutine(DisableSequence());
        }

        IEnumerator DisableSequence()
        {
            IsAvailable = false;
            State = DroneState.Suspicious;
            float t = 0f;
            Vector3 playerPos0 = PlayerAbilities.Instance.transform.position;
            while (t < 2f)
            {
                t += Time.deltaTime;
                if (PlayerAbilities.Instance == null ||
                    Vector3.Distance(playerPos0, PlayerAbilities.Instance.transform.position) > 3f)
                {
                    IfPrompt(); IsAvailable = true; _disable = null; yield break;
                }
                GameDirector.Instance?.SetPrompt("DESACTIVANDO DRON... " + Mathf.CeilToInt(2f - t));
                yield return null;
            }
            GameDirector.Instance?.SetPrompt("");
            State = DroneState.Disabled;
            IsAvailable = false;
            GameDirector.Instance?.AddPoints(100, "Desactivar una máquina");
            if (_eyeMat != null) _eyeMat.color = new Color(0.4f, 0.2f, 0.2f);
            // El dron cae al suelo y chisporrotea
            StartCoroutine(FallAndSparks());
        }

        void IfPrompt()
        {
            GameDirector.Instance?.SetPrompt(PromptText);
        }

        IEnumerator FallAndSparks()
        {
            float y0 = transform.position.y;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(y0, 0.6f, t) + Mathf.Sin(t * 20f) * 0.01f, transform.position.z);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 25f) * 12f);
                yield return null;
            }
        }
    }
}