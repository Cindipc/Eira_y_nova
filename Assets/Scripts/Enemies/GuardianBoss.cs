using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Máquina Guardiana (jefe del Nivel 1). No se «mata» con armas:
    /// hay que sobrecargar sus 3 sistemas con las consolas del hangar esquivando sus ataques.
    /// </summary>
    public class GuardianBoss : MonoBehaviour
    {
        public int MaxShield = 3;
        public int Shield { get; private set; }

        public float detectRadius = 7f;
        public bool Defeated { get; private set; }
        public event System.Action onDefeated;

        Transform _player;
        float _attackTimer = 2.5f;
        bool _fightStarted;
        List<Material> _flashMats = new List<Material>();

        Material _coreMat;

        public void Setup(Material coreMat)
        {
            _coreMat = coreMat;
            Shield = MaxShield;
        }

        void Update()
        {
            if (Defeated) return;

            if (_player == null && PlayerAbilities.Instance != null)
                _player = PlayerAbilities.Instance.transform;

            if (_player == null) return;
            float dist = Vector3.Distance(_player.position, transform.position);

            // Inicio del combate cuando el jugador se acerca
            if (!_fightStarted && dist < detectRadius)
            {
                _fightStarted = true;
                GameDirector.Instance?.ShowBossBar("MÁQUINA GUARDIANA", (float)Shield / MaxShield, true);
                GameDirector.Instance?.SetObjective("Sobrecarga los 3 sistemas de la guardiana con las consolas (E)");
                if (GameDirector.Instance != null && GameDirector.Instance.Nova != null)
                    GameDirector.Instance.Nova.Say("¡La guardiana! Sobrecarga sus sistemas laterales ¡esquiva sus ataques!", null);
            }

            if (!_fightStarted) return;

            // Pulso de vida
            transform.Rotate(0f, 4f * Time.deltaTime, 0f, Space.Self);

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f && dist < 16f)
            {
                _attackTimer = dist < 5f ? Random.Range(2.2f, 3f) : Random.Range(1.4f, 2.2f);
                StartCoroutine(AttackRandom());
            }
        }

        IEnumerator AttackRandom()
        {
            if (Random.value < 0.5f)
                yield return StartCoroutine(Shockwave());
            else
                yield return StartCoroutine(Laser());
        }

        // ---- Onda expansiva
        IEnumerator Shockwave()
        {
            var mat = WorldBuilder.Mat("Wave", WorldBuilder.OrangeGlow, true, 0.85f);
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                var seg = WorldBuilder.Box("WaveSeg", transform.position + Vector3.up * 0.4f, Vector3.zero, mat, true);
                seg.transform.rotation = Quaternion.AngleAxis(i * 360f / n, Vector3.up);
                seg.transform.localScale = Vector3.zero;
                SegExpand sh = seg.AddComponent<SegExpand>();
                sh.speed = 5f;
                sh.origin = transform;
                yield return new WaitForSeconds(0.12f);
            }
            yield return new WaitForSeconds(0.8f);
        }

        public class SegExpand : MonoBehaviour
        {
            public float speed = 6f;
            public Transform origin;
            float _t = 0f;
            Vector3 _dir;

            void Start()
            {
                _dir = origin.forward;
                transform.LookAt(transform.position + _dir);
            }

            void Update()
            {
                _t += Time.deltaTime;
                float len = _t * speed;
                transform.position = origin.position + origin.forward * len + Vector3.up * 0.4f;
                transform.localScale = new Vector3(0.3f, 0.14f, Mathf.Min(7f, len * 4f) + 0.4f);
                transform.forward = origin.forward;

                if (PlayerAbilities.Instance != null &&
                    Vector3.Distance(transform.position, PlayerAbilities.Instance.transform.position) < 1.0f &&
                    _t > 0.2f)
                {
                    Damage(18f);
                }
                if (_t > 1.8f) Destroy(gameObject);
            }

            void Damage(float d) { PlayerAbilities.Instance?.TakeHeartDamage(d); }
        }

        // ---- Rayo láser
        IEnumerator Laser()
        {
            if (PlayerAbilities.Instance == null) yield break;
            var beam = WorldBuilder.Cyl("Laser", transform.position + Vector3.up * 1.2f, new Vector3(0.2f, 0.2f, 0.2f),
                WorldBuilder.Mat("LaserMat", Color.red, true, 0.9f));
            beam.transform.SetParent(transform, false);

            Vector3 dir = PlayerAbilities.Instance.transform.position - transform.position;
            dir.y = 0f; dir.Normalize();
            float dur = 0f;
            bool hit = false;
            while (dur < 1.6f)
            {
                dur += Time.deltaTime;
                float len = Mathf.Min(20f, dur * 12f);
                beam.transform.localPosition = new Vector3(0f, 1.2f, len / 2f);
                beam.transform.localScale = new Vector3(0.24f, 0.24f, len);
                beam.transform.localRotation = Quaternion.identity;
                beam.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
                beam.transform.position = transform.position + Vector3.up * 1.2f + dir * (len / 2f);

                if (PlayerAbilities.Instance != null)
                {
                    Vector3 toPlayer = PlayerAbilities.Instance.transform.position - beam.transform.position;
                    toPlayer.y = 0f;
                    if (dur > 0.6f && toPlayer.magnitude < 0.55f && !hit)
                    {
                        hit = true;
                        PlayerAbilities.Instance.TakeHeartDamage(20f);
                    }
                }
                yield return null;
            }
            Destroy(beam.gameObject);
        }

        // ---- Sobrecarga recibida
        public void ReceiveOverload()
        {
            if (Defeated) return;
            Shield--;
            Flash();
            GameDirector.Instance?.SetBossHp((float)Shield / MaxShield);

            if (Shield <= 0)
            {
                Defeated = true;
                StartCoroutine(DefeatSequence());
            }
            else
            {
                StartCoroutine(Stun(1.5f));
                GameDirector.Instance?.ShowDialogue("GUARDIANA", "SISTEMA " + (MaxShield - Shield) + "/3 SOBRECARGADO", 2.5f);
                if (GameDirector.Instance && GameDirector.Instance.Nova != null)
                    GameDirector.Instance.Nova.Say("¡Quedan " + Shield + " sistemas! ¡Sigue!", null);
            }
        }

        IEnumerator DefeatSequence()
        {
            _attackTimer = float.MaxValue;
            GameDirector.Instance?.ShowBossBar("", 0f, false);
            GameDirector.Instance?.SetObjective("La guardiana cae. Abre la salida e investiga la ciudad.");
            GameDirector.Instance?.AddPoints(200, "Jefe derrotado");
            if (GameDirector.Instance != null && GameDirector.Instance.Nova != null)
                GameDirector.Instance.Nova.Say("¡Lo lograste! La salida está despejada.", null);

            // Destrucción visual
            yield return StartCoroutine(Shake(1.2f));
            float flashT = 0f;
            while (flashT < 3f)
            {
                flashT += Time.deltaTime;
                foreach (var m in _flashMats)
                    if (m != null) m.color = Color.Lerp(WorldBuilder.MetalDark, Color.white, Mathf.PingPong(Time.time * 6f, 1f));
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, flashT / 3f);
                yield return null;
            }
            transform.localScale = Vector3.one * 0.01f;
            yield return new WaitForSeconds(0.5f);
            onDefeated?.Invoke();
        }

        IEnumerator Stun(float s)
        {
            _attackTimer = s + 0.5f;
            yield return new WaitForSeconds(s);
        }

        IEnumerator Shake(float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.position += Random.insideUnitSphere * 0.05f;
                yield return null;
            }
        }

        void Flash()
        {
            foreach (var m in _flashMats)
                if (m != null) m.color = Color.white;
        }

        public void RegisterFlashMat(Material m)
        {
            if (!_flashMats.Contains(m)) _flashMats.Add(m);
        }
    }
}