using UnityEngine;
using UnityEngine.InputSystem;

namespace EiraNova
{
    /// <summary>Habilidades de Eira: energía del corazón y la «Clave de Sangre» obtenida tras la cortadura.</summary>
    public class PlayerAbilities : MonoBehaviour
    {
        public static PlayerAbilities Instance { get; private set; }

        public float MaxEnergy = 100f;
        public float energy;
        public float regenPerSecond = 1.2f;
        public float interactRange = 2.4f;
        public bool HasBloodKey { get; private set; }

        // Pulso defensivo: onda electromagnética que desconecta a los androides.
        public float pulseCost = 25f;
        public float pulseRadius = 9f;
        public float pulseStunSeconds = 4.5f;
        public float pulseCooldownTime = 2.5f;
        float _pulseCooldown;

        Interactable _current;
        float _lastPrompt;

        void Awake()
        {
            Instance = this;
            energy = MaxEnergy;
        }

        void Start()
        {
            if (GameDirector.Instance) GameDirector.Instance.UpdateHeartBar(energy / MaxEnergy);
        }

        void Update()
        {
            // Recuperación lenta
            if (energy < MaxEnergy)
            {
                energy = Mathf.Min(MaxEnergy, energy + regenPerSecond * Time.deltaTime);
                if (GameDirector.Instance) GameDirector.Instance.UpdateHeartBar(energy / MaxEnergy);
            }

            if (GameDirector.Instance == null || GameDirector.Instance.Defeated) return;

            UpdateNearby();
            HandleInteraction();
            HandlePulse();
        }

        void UpdateNearby()
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            Vector3 center = transform.position + new Vector3(0f, 0.8f, 0f);
            Collider[] cols = Physics.OverlapSphere(center, interactRange);
            foreach (var c in cols)
            {
                var it = c.GetComponent<Interactable>();
                if (it == null || !it.IsAvailable) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = it; }
            }

            if (best != _current)
            {
                _current = best;
                UpdatePrompt();
            }
            else if (_lastPrompt + 0.25f < Time.time)
            {
                UpdatePrompt();
            }
        }

        void UpdatePrompt()
        {
            _lastPrompt = Time.time;
            if (GameDirector.Instance == null) return;
            if (_current != null)
                GameDirector.Instance.SetPrompt(_current.PromptText);
            else
                GameDirector.Instance.SetPrompt("");
        }

        void HandleInteraction()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.eKey.wasPressedThisFrame) return;
            if (_current == null) return;
            _current.Interact();
        }

        // -------------------------------------------------------------
        // Pulso defensivo
        // -------------------------------------------------------------
        void HandlePulse()
        {
            if (_pulseCooldown > 0f) _pulseCooldown -= Time.deltaTime;
            var kb = Keyboard.current;
            if (kb == null || !kb.qKey.wasPressedThisFrame) return;
            TryPulse();
        }

        /// <summary>
        /// Libera una onda de energía que desconecta a los androides cercanos.
        /// Es la única forma de detenerlos: no se pueden hackear de cerca.
        /// </summary>
        public bool TryPulse()
        {
            var gd = GameDirector.Instance;
            if (gd == null || gd.Defeated || gd.IntroActive) return false;
            if (_pulseCooldown > 0f) return false;

            if (!SpendEnergy(pulseCost))
            {
                if (gd != null) gd.ShowSideNote("Energía insuficiente para el pulso (Q)");
                return false;
            }

            _pulseCooldown = pulseCooldownTime;

            // Onda visual expansiva.
            var wave = WorldBuilder.Cyl("PulsoDefensa", transform.position + Vector3.up * 0.1f,
                new Vector3(0.6f, 0.06f, 0.6f),
                WorldBuilder.Mat("PulsoDefensa", WorldBuilder.CyanGlow, true, 0.7f), false);
            wave.AddComponent<PulseExpand>();

            // Desconecta a los androides dentro del radio.
            int stunned = 0;
            Collider[] cols = Physics.OverlapSphere(transform.position + Vector3.up * 0.9f, pulseRadius);
            foreach (var c in cols)
            {
                var soldier = c.GetComponentInParent<AndroidSoldier>();
                if (soldier != null)
                {
                    soldier.Stun(pulseStunSeconds);
                    stunned++;
                }
            }

            if (stunned > 0)
            {
                if (gd != null)
                {
                    gd.AddPoints(50, "Desconectar máquinas con el pulso");
                    gd.ShowSideNote("PULSO: androides desconectados (" + stunned + ")");
                }
                if (NovaCompanion.Instance != null)
                    NovaCompanion.Instance.Say("¡Los desconectaste! Solo tú puedes hacer eso.", null);
            }
            else if (NovaCompanion.Instance != null && Random.value < 0.25f)
            {
                NovaCompanion.Instance.Say("El pulso solo afecta a las máquinas cercanas.", null);
            }

            return true;
        }

        // -------------------------------------------------------------
        // Clave de sangre
        // -------------------------------------------------------------
        public void GrantBloodKey()
        {
            HasBloodKey = true;
        }

        public bool SpendEnergy(float amount)
        {
            if (energy >= amount)
            {
                energy -= amount;
                if (GameDirector.Instance) GameDirector.Instance.UpdateHeartBar(energy / MaxEnergy);
                return true;
            }
            return false;
        }

        public void TakeHeartDamage(float amount)
        {
            energy -= amount;
            if (GameDirector.Instance) GameDirector.Instance.UpdateHeartBar(Mathf.Max(0f, energy / MaxEnergy));
            if (energy <= 0f && GameDirector.Instance && !GameDirector.Instance.Defeated)
            {
                GameDirector.Instance.HeartFailure();
            }
        }

        public void RefillEnergy()
        {
            energy = MaxEnergy;
            if (GameDirector.Instance) GameDirector.Instance.UpdateHeartBar(1f);
        }
    }
}