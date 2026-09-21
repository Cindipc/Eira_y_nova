using System.Collections;
using UnityEngine;

namespace EiraNova
{
    /// <summary>Base para todo elemento con el que Eira puede interactuar con [E].</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public string PromptText = "[E] Interactuar";
        public bool IsAvailable { get; protected set; } = true;

        public abstract void Interact();

        /// <summary>Añade un collider de proximidad al objeto de visual (triggers).</summary>
        public Collider AddProximity(Vector3 center, Vector3 size)
        {
            var cols = GetComponents<Collider>();
            foreach (var c in cols)
                if (c.isTrigger) return c;
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.center = center;
            bc.size = size;
            bc.isTrigger = true;
            return bc;
        }
    }

    // ------------------------------------------------------------------
    // TERMINAL DE INFORMACIÓN
    // ------------------------------------------------------------------
    public class LabTerminal : Interactable
    {
        public string speaker = "TERMINAL";
        public string[] lines;
        public float lineTime = 3f;
        public int points = 50;
        public System.Action onRead;

        Material _screenMat;

        public void Setup(string title, string[] texts, Material screenMat, System.Action read = null)
        {
            PromptText = "[E] Leer terminal";
            speaker = title;
            lines = texts;
            _screenMat = screenMat;
            onRead = read;
        }

        public void SetScreenColor(Color c)
        {
            if (_screenMat != null) _screenMat.color = c;
            if (_screenMat != null) _screenMat.SetColor("_BaseColor", c);
        }

        public void StoreScreen(Material m) { _screenMat = m; }

        public Material ScreenMat() => _screenMat;

        public override void Interact()
        {
            if (!IsAvailable) return;
            if (lines == null || lines.Length == 0) return;
            var gd = GameDirector.Instance;
            if (gd == null) return;
            string msg = string.Join("\n", lines);
            gd.ShowDialogue(speaker, msg, 3f + lines.Length * 1.6f);
            gd.AddPoints(points, "Información encontrada");
            onRead?.Invoke();
        }
    }

    // ------------------------------------------------------------------
    // PUERTA GENÉTICA (requiere la Clave de Sangre)
    // ------------------------------------------------------------------
    public class GeneDoor : Interactable
    {
        Vector3 _closedPos;
        Vector3 _openPos;
        float _t = 1f; // 1 = cerrada, 0 = abierta
        public float energyCost = 12f;
        public System.Action onOpened;

        public void Setup(Vector3 closedPos, Vector3 openPos)
        {
            _closedPos = closedPos;
            _openPos = openPos;
            PromptText = "[E] Activar cerradura genética";
            gameObject.transform.position = closedPos;
        }

        public override void Interact()
        {
            var gd = GameDirector.Instance;
            if (gd == null) return;
            var abilities = PlayerAbilities.Instance;
            if (abilities == null) return;

            if (!abilities.HasBloodKey)
            {
                gd.ShowDialogue("SISTEMA", "ACCESO DENEGADO.\nCerradura bio-genética. Compatibilidad de sangre requerida.", 3.5f);
                return;
            }

            if (_t < 0.999f) return;

            if (!abilities.SpendEnergy(energyCost))
            {
                gd.ShowDialogue("EIRA", "Mi corazón no aguanta más... Debo esperar.", 2.5f);
                return;
            }

            gd.ShowDialogue("SISTEMA", "LLAVE DE SANGRE ACEPTADA.\nAbriendo...", 2.5f);
            gd.AddPoints(100, "Acertijo resuelto");
            if (gd.Nova != null) gd.Nova.Say("Tu sangre funciona de verdad, Eira. Vamos.", null);
            gd.SetCheckpoint(transform.position + new Vector3(0f, 0f, 3f));
            StartCoroutine(OpenSequence());
        }

        IEnumerator OpenSequence()
        {
            _t = 1f;
            while (_t > 0f)
            {
                _t -= Time.deltaTime * 0.6f;
                transform.position = Vector3.Lerp(_openPos, _closedPos, _t);
                yield return null;
            }
            transform.position = _openPos;
            IsAvailable = false;
            onOpened?.Invoke();
        }
    }

    // ------------------------------------------------------------------
    // LA CORTADURA — Ritual de sangre (como Cap. 2/4)
    // ------------------------------------------------------------------
    public class BloodRitualMachine : Interactable
    {
        public bool Activated { get; private set; }
        public System.Action onActivated;

        Material _scannerMat;
        Coroutine _sequence;

        public void Setup(Material scannerMat)
        {
            _scannerMat = scannerMat;
            PromptText = "[E] Colocar la mano sobre el filo";
        }

        public override void Interact()
        {
            if (Activated) return;
            if (_sequence != null) return;
            _sequence = StartCoroutine(RunSequence());
        }

        IEnumerator RunSequence()
        {
            var gd = GameDirector.Instance;
            if (gd == null) yield break;

            IsAvailable = false;
            gd.SetPrompt("");
            gd.SetObjective("La cortadura: activa tu clave de sangre");

            gd.ShowDialogue("NOVA", "Esto es una cerradura bio-genética. Solo se abre con sangre compatible...", 4f);
            yield return new WaitForSeconds(4f);
            gd.ShowDialogue("NOVA", "Eira... tú eres una anomalía. Atrévete a tocar el filo.", 4f);
            yield return new WaitForSeconds(4f);

            // CORTADURA
            gd.FlashDamage(0.85f);
            gd.SetBigText("CORTADURA", "Una gota de sangre cae sobre la máquina...", 2.4f);
            yield return new WaitForSeconds(2.6f);

            gd.SetBigText("DNA COMPATIBLE", "PORTADORA DE LA LLAVE: EIRA", 3f);
            if (PlayerAbilities.Instance != null)
                PlayerAbilities.Instance.GrantBloodKey();
            gd.AddPoints(100, "Acertijo resuelto");

            if (_scannerMat != null)
                _scannerMat.color = WorldBuilder.CyanGlow;

            yield return new WaitForSeconds(3.2f);

            gd.ShowDialogue("EIRA", "Mi sangre... puede abrir estas máquinas.", 3.5f);
            gd.SetObjective("Activa las puertas genéticas con tu sangre y llega al hangar.");
            if (gd.Nova != null) gd.Nova.Say("Ahora entiendes por qué te buscan. Vamos, la salida está cerca.", null);

            Activated = true;
            onActivated?.Invoke();
        }
    }

    // ------------------------------------------------------------------
    // CONSOLA DE SOBRECARGA (contra la Máquina Guardiana)
    // ------------------------------------------------------------------
    public class OverloadConsole : Interactable
    {
        public GuardianBoss boss;
        float _cooldown = 0f;

        Material _screenMat;

        public void Setup(Material screenMat)
        {
            _screenMat = screenMat;
            PromptText = "[E] Sobrecargar el sistema";
        }

        void Update()
        {
            if (!IsAvailable && _cooldown > 0f)
            {
                _cooldown -= Time.deltaTime;
                if (_cooldown <= 0f)
                {
                    IsAvailable = true;
                    if (_screenMat != null) _screenMat.color = WorldBuilder.OrangeGlow;
                    PromptText = "[E] Sobrecargar el sistema";
                }
            }
        }

        public override void Interact()
        {
            if (boss == null || !IsAvailable) return;
            StartCoroutine(OverloadSequence());
        }

        IEnumerator OverloadSequence()
        {
            IsAvailable = false;
            var gd = GameDirector.Instance;
            if (gd != null) gd.SetPrompt("SOBRECARGANDO... NO TE MUEVAS");
            _screenMatToFill(_screenMat, 1f);
            float t = 0f;
            while (t < 2.2f)
            {
                t += Time.deltaTime;
                yield return null;
                // Cancelar si el jugador se aleja
                if (PlayerAbilities.Instance != null &&
                    Vector3.Distance(transform.position, PlayerAbilities.Instance.transform.position) > 4f)
                {
                    if (gd != null) gd.SetPrompt("");
                    ResetConsole();
                    yield break;
                }
            }
            if (gd != null) gd.SetPrompt("");
            if (boss != null) boss.ReceiveOverload();
            if (gd != null) gd.AddPoints(100, "Desactivar una máquina");
            _screenMatToFill(_screenMat, 0f);
            _cooldown = 12f;
            if (_screenMat != null) _screenMat.color = new Color(0.25f, 0.25f, 0.28f);
            PromptText = "(sobrecargando...)";
        }

        void ResetConsole()
        {
            _screenMatToFill(_screenMat, 0f);
            _cooldown = 3f;
        }

        void _screenMatToFill(Material m, float t)
        {
            if (m == null) return;
            m.color = Color.Lerp(WorldBuilder.OrangeGlow, Color.white, t);
        }
    }

    // ------------------------------------------------------------------
    // PUNTO DE SALIDA / ESCAPE
    // ------------------------------------------------------------------
    public class ExitPoint : Interactable
    {
        GameObject _doorVisual;
        public GuardianBoss requiredBoss;

        public void Setup(GameObject doorVisual)
        {
            _doorVisual = doorVisual;
            PromptText = "[E] Abrir la salida principal";
        }

        public override void Interact()
        {
            var gd = GameDirector.Instance;
            if (gd == null) return;

            if (requiredBoss != null && !requiredBoss.Defeated)
            {
                gd.ShowDialogue("GUARDIANA", "SALIDA BLOQUEADA.\nNIVEL DE AMENAZA ALTO.", 3f);
                gd.ShowSideNote("Necesita sobrecargar a la Máquina Guardiana primero");
                return;
            }

            gd.SetPrompt("");
            gd.SetObjective("Salir del laboratorio con NOVA...");
            if (_doorVisual != null)
                StartCoroutine(OpenExit());
            else
                gd.Level1Complete();
        }

        IEnumerator OpenExit()
        {
            var closed = _doorVisual.transform.position;
            var open = closed + Vector3.up * 4.5f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.7f;
                _doorVisual.transform.position = Vector3.Lerp(closed, open, t);
                yield return null;
            }
            _doorVisual.transform.position = open;
            yield return new WaitForSeconds(0.8f);
            GameDirector.Instance.Level1Complete();
        }
    }

    // ------------------------------------------------------------------
    // RECOGIBLES
    // ------------------------------------------------------------------
    public class MedkitPickup : Interactable
    {
        public override void Interact()
        {
            var gd = GameDirector.Instance;
            if (gd == null) return;
            if (gd.Lives >= gd.MaxLives)
                gd.AddPoints(25, "Recurso extra");
            else
                gd.GainLife();
            gd.ShowSideNote("Botiquín tecnológico");
            Destroy(gameObject);
        }
    }

    public class EnergyPickup : Interactable
    {
        public float amount = 40f;
        public override void Interact()
        {
            var a = PlayerAbilities.Instance;
            if (a != null)
            {
                a.energy = Mathf.Min(a.MaxEnergy, a.energy + amount);
                GameDirector.Instance?.UpdateHeartBar(a.energy / a.MaxEnergy);
            }
            GameDirector.Instance?.ShowSideNote("Cápsula de energía");
            Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------------
    // RESCATE DE ANDROIDE REBELDE (Nivel 2)
    // ------------------------------------------------------------------
    public class RebelRescue : Interactable
    {
        public string rebelName = "REBELDE";
        GameObject _visual;
        public Vector3 shelterPos;
        bool _rescued;
        Material _coreMat;

        public void Setup(GameObject visual, Material coreMat, Vector3 shelter)
        {
            _visual = visual;
            _coreMat = coreMat;
            shelterPos = shelter;
            PromptText = "[E] Reparar y rescatar al rebelde";
        }

        public override void Interact()
        {
            if (_rescued) return;
            var gd = GameDirector.Instance;
            if (gd == null) return;

            _rescued = true;
            IsAvailable = false;
            gd.ShowDialogue(rebelName, "¿E-rribiste...? Gracias. Por años estuve desconectado en esta calle.", 4f);
            gd.AddPoints(200, "Rescatar rebelde");
            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.Say("Un rebelde. Necesitan nuestra ayuda, Eira.", null);

            if (_coreMat != null) _coreMat.color = WorldBuilder.GreenGlow;
            StartCoroutine(WalkToShelter(gd));
        }

        System.Collections.IEnumerator WalkToShelter(GameDirector gd)
        {
            float t = 0f;
            Vector3 start = _visual != null ? _visual.transform.position : transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.5f;
                if (_visual != null)
                {
                    _visual.transform.position = Vector3.Lerp(start, shelterPos, t);
                    _visual.transform.rotation = Quaternion.Slerp(_visual.transform.rotation, Quaternion.LookRotation(shelterPos - start), Time.deltaTime * 3f);
                }
                yield return null;
            }
            gd.ShowSideNote("Rebelde a salvo");
        }
    }
}