using System.Collections;
using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Nivel 1 — Fase inicial «Ciudad escaneada»: Eira despierta en el escenario real
    /// (gaussian splatting de Downloads) e inmediatamente se encuentra a NOVA. Después
    /// encuentra el holograma de KAEL y desbloquea la puerta que abre el laboratorio.
    /// Estética limpia: el escaneo da el visual; no hay escombros procedurales.
    /// </summary>
    public class Level1CityApproach : MonoBehaviour
    {
        Material _dark, _metal, _accent, _cyan, _green;
        SplatStage _stage;
        Bounds _bounds;
        float _floorY;

        public void Build(Transform player)
        {
            _dark   = WorldBuilder.Mat("C_Dark", new Color(0.04f, 0.05f, 0.07f), true);
            _metal  = WorldBuilder.Mat("C_Metal", new Color(0.22f, 0.24f, 0.28f));
            _accent = WorldBuilder.Mat("C_Accent", new Color(0.16f, 0.18f, 0.22f));
            _cyan   = WorldBuilder.Mat("C_Cyan", WorldBuilder.CyanGlow, true);
            _green  = WorldBuilder.Mat("C_Green", WorldBuilder.GreenGlow, true);

            var root = new GameObject("Ciudad_Gauss").transform;
            root.SetParent(transform, false);

            // ---- Escenario escaneado (gaussian splatting) ---------------------
            _stage = gameObject.AddComponent<SplatStage>();
            _stage.splatPath = "Env/ciudad/escenario.spz";
            _stage.colliderPath = "Env/ciudad/collider.glb";
            _stage.stagePosition = Vector3.zero;
            _stage.targetSpanX = 70f;
            _stage.wallHeight = 14f;
            _stage.Build();

            if (!_stage.Ready)
            {
                Debug.LogWarning("[Level1CityApproach] La ciudad escaneada no está lista; el suelo continúa disponible.");
            }

            _bounds = _stage.StageBounds;
            _floorY = _stage.GroundY;
            System.Func<float, float, Vector3> P = (nx, nz) =>
                new Vector3(_bounds.min.x + nx * _bounds.size.x, _floorY, _bounds.min.z + nz * _bounds.size.z);

            // ---- Spawn de Eira y NOVA ----------------------------------------
            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.transform.position = P(0.09f, 0.44f);

            player.GetComponent<PlayerController>()?.Teleport(P(0.05f, 0.5f) + Vector3.up * 0.9f);
            player.rotation = Quaternion.Euler(0f, 90f, 0f);
            GameDirector.Instance.SetCheckpoint(P(0.05f, 0.5f) + Vector3.up * 0.5f);

            WorldBuilder.PointLight("LuzSpawn", P(0.07f, 0.5f) + Vector3.up * 2.6f, new Color(0.35f, 0.85f, 1f), 10f, 1.6f).transform.SetParent(root, false);

            // Zona de comentario de NOVA (al despertar junto a ella)
            var midZ = _bounds.center.z;
            var novaZone = new GameObject("Zona_NOVA");
            novaZone.transform.position = P(0.10f, 0.5f);
            novaZone.transform.SetParent(root, false);
            var nzc = novaZone.AddComponent<BoxCollider>();
            nzc.size = new Vector3(_bounds.size.x * 0.10f, 4f, _bounds.size.z * 0.5f);
            nzc.isTrigger = true;
            novaZone.AddComponent<NovaMeetZone>();

            // ---- Terminal de contexto (opcional) -----------------------------
            var t = BuildTerminal(root, P(0.26f, 0.30f));
            t.Setup("LA CIUDAD", new string[]
            {
                "AÑO 3000. Escaneo parcial del registro urbano.",
                "KAEL controla los androides del este.",
                "El laboratorio está al final de esta vía."
            }, t.ScreenMat(), null);

            // ---- Holograma de KAEL -------------------------------------------
            var holo = ProceduralCharacters.BuildKaelHologram(P(0.55f, 0.5f));
            holo.transform.SetParent(root, false);
            WorldBuilder.FlickerLight("LuzHolo", P(0.55f, 0.5f) + Vector3.up * 1f, new Color(0.3f, 0.7f, 1f), 10f, 2.2f).transform.SetParent(root, false);

            var kaelZone = new GameObject("Zona_Kael");
            kaelZone.transform.position = P(0.55f, 0.5f);
            kaelZone.transform.SetParent(root, false);
            var kzc = kaelZone.AddComponent<BoxCollider>();
            kzc.size = new Vector3(_bounds.size.x * 0.10f, 6f, _bounds.size.z * 0.4f);
            kzc.isTrigger = true;
            var enc = kaelZone.AddComponent<KaelCityEncounter>();
            enc.city = this;

            // ---- Puerta del laboratorio (bloqueada hasta ver a KAEL) --------
            var door = BuildLabDoor(root, _bounds, P, midZ);
            enc.door = door;

            // Checkpoint previo a la puerta
            var cpZone = new GameObject("Checkpoint_Puerta");
            cpZone.transform.position = P(0.86f, 0.5f);
            cpZone.transform.SetParent(root, false);
            var cpc = cpZone.AddComponent<BoxCollider>();
            cpc.size = new Vector3(_bounds.size.x * 0.03f, 4f, _bounds.size.z * 0.4f);
            cpc.isTrigger = true;
            var ccz = cpZone.AddComponent<CityCheckpointZone>();
            ccz.position = P(0.86f, 0.5f) + Vector3.up * 0.5f;
            ccz.note = "Punto de control: puerta del laboratorio";

            // Un solo dron de vigilancia inicial (lección de pulso Q)
            SpawnDrone(root, new Vector3[] { P(0.40f, 0.65f) + Vector3.up * 2f, P(0.48f, 0.35f) + Vector3.up * 2f });

            GameDirector.Instance.SetObjective("Encuentra a NOVA: te explicará qué está pasando.");
        }

        /// <summary>Lo invoca <see cref="KaelCityEncounter"/> tras el encuentro: libera a los escoltas.</summary>
        public void OnKaelTriggered()
        {
            var root = transform.Find("Ciudad_Gauss");
            if (root == null) return;
            System.Func<float, float, Vector3> P = (nx, nz) =>
                new Vector3(_bounds.min.x + nx * _bounds.size.x, _floorY, _bounds.min.z + nz * _bounds.size.z);

            SpawnDrone(root, new Vector3[] { P(0.60f, 0.20f) + Vector3.up * 2.2f, P(0.72f, 0.80f) + Vector3.up * 2.2f });
            SpawnDrone(root, new Vector3[] { P(0.50f, 0.85f) + Vector3.up * 2.2f, P(0.78f, 0.15f) + Vector3.up * 2.2f });
            SpawnSoldier(root, new Vector3[] { P(0.48f, 0.22f), P(0.66f, 0.78f) });

            GameDirector.Instance.SetObjective("KAEL te busca. Desbloquea la puerta del laboratorio y cruza.");
            GameDirector.Instance.SetCheckpoint(P(0.86f, 0.5f) + Vector3.up * 0.5f);
        }

        // ---------------------------------------------------------------
        // Puerta del laboratorio
        // ---------------------------------------------------------------
        LabEntranceDoor BuildLabDoor(Transform root, Bounds b, System.Func<float, float, Vector3> P, float centerZ)
        {
            var doorPos = P(0.90f, 0.5f);
            var go = new GameObject("Puerta_Laboratorio");
            go.transform.position = doorPos;
            go.transform.SetParent(root, false);

            float halfSpan = b.size.z * 0.5f;

            // Marco: postes + dintel
            Box(go.transform, new Vector3(0f, 2.6f, -halfSpan - 0.4f), new Vector3(0.8f, 5.2f, 0.8f), _metal);
            Box(go.transform, new Vector3(0f, 2.6f,  halfSpan + 0.4f), new Vector3(0.8f, 5.2f, 0.8f), _metal);
            Box(go.transform, new Vector3(0f, 5.4f, 0f), new Vector3(1.2f, 0.8f, b.size.z + 1.2f), _metal);

            // Doble hoja que se alza
            var slabL = Box(go.transform, new Vector3(0f, 2.6f, -halfSpan * 0.5f), new Vector3(0.5f, 5.2f, halfSpan), _accent);
            var slabR = Box(go.transform, new Vector3(0f, 2.6f,  halfSpan * 0.5f), new Vector3(0.5f, 5.2f, halfSpan), _accent);

            // Franja de luz dedicada (cambia a verde al desbloquear)
            var glowMat = WorldBuilder.Mat("C_DoorGlow", WorldBuilder.CyanGlow, true);
            Quad(go.transform, new Vector3(0.31f, 2.6f, 0f), new Vector3(0.04f, 4.4f, b.size.z * 0.9f), glowMat);

            var d = go.AddComponent<LabEntranceDoor>();
            d.Setup(slabL, slabR, glowMat);
            d.AddProximity(new Vector3(0f, 0f, 0f), new Vector3(3f, 6f, b.size.z * 0.9f));
            return d;
        }

        // ---------------------------------------------------------------
        // Helpers de construcción
        // ---------------------------------------------------------------
        GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material m = null, bool solid = true)
        {
            var go = WorldBuilder.Box("Box", pos, size, m != null ? m : _accent, solid);
            go.transform.SetParent(parent, false);
            return go;
        }

        GameObject Quad(Transform parent, Vector3 pos, Vector3 size, Material m)
        {
            var go = WorldBuilder.Quad("Screen", pos, size, m);
            go.transform.SetParent(parent, false);
            return go;
        }

        LabTerminal BuildTerminal(Transform root, Vector3 pos)
        {
            var go = new GameObject("Terminal_Ciudad");
            go.transform.position = pos;
            go.transform.SetParent(root, false);

            Cyl(go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 0.5f, 0.7f), _accent);
            Box(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.4f, 0.3f, 1f), _accent);
            Quad(go.transform, new Vector3(0f, 1.6f, 0.55f), new Vector3(1.2f, 0.7f, 1f), _cyan);

            var t = go.AddComponent<LabTerminal>();
            t.AddProximity(new Vector3(0f, 1.1f, 0f), new Vector3(2.2f, 2f, 2.2f));
            t.StoreScreen(go.transform.Find("Screen").GetComponent<MeshRenderer>().sharedMaterial);
            return t;
        }

        GameObject Cyl(Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var go = WorldBuilder.Cyl("Cyl", pos, scale, m);
            go.transform.SetParent(parent, false);
            return go;
        }

        void SpawnDrone(Transform root, Vector3[] waypoints)
        {
            var drone = EnemyVisuals.BuildDrone(out Material eye);
            drone.transform.position = waypoints[0];
            drone.transform.SetParent(root, false);
            var ctrl = drone.AddComponent<EnemyDrone>();
            ctrl.patrolY = 2.2f;
            ctrl.Init(waypoints, eye);
        }

        void SpawnSoldier(Transform root, Vector3[] waypoints)
        {
            var soldier = EnemyVisuals.BuildSoldier(out Material eye);
            soldier.transform.position = waypoints[0];
            soldier.transform.SetParent(root, false);
            var ctrl = soldier.AddComponent<AndroidSoldier>();
            ctrl.Init(waypoints, eye);
        }
    }

    // ------------------------------------------------------------------
    // Zona de reencuentro con NOVA
    // ------------------------------------------------------------------
    public class NovaMeetZone : MonoBehaviour
    {
        bool _used;

        void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.GetComponent<PlayerAbilities>() == null) return;
            _used = true;
            var gd = GameDirector.Instance;
            if (gd == null) return;
            gd.ShowDialogue("NOVA", "Esta zona quedó intacta... pero KAEL vigila desde el este. Sígueme.", 4f);
            gd.SetObjective("Sigue a NOVA: encuentra el holograma de KAEL y la puerta del laboratorio.");
            gd.AddPoints(25, "Reencuentro con NOVA");
        }
    }

    // ------------------------------------------------------------------
    // Checkpoint de la ciudad
    // ------------------------------------------------------------------
    public class CityCheckpointZone : MonoBehaviour
    {
        public Vector3 position;
        public string note = "Punto de control";
        bool _used;

        void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.GetComponent<PlayerAbilities>() == null) return;
            _used = true;
            GameDirector.Instance?.SetCheckpoint(position);
            GameDirector.Instance?.ShowSideNote(note);
        }
    }

    // ------------------------------------------------------------------
    // Encuentro con el holograma de KAEL en la ciudad
    // ------------------------------------------------------------------
    public class KaelCityEncounter : MonoBehaviour
    {
        public LabEntranceDoor door;
        public Level1CityApproach city;
        bool _used;

        void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.GetComponent<PlayerAbilities>() == null) return;
            _used = true;

            var gd = GameDirector.Instance;
            if (gd == null) return;

            gd.ShowDialogue("KAEL", "La portadora... te busqué mucho tiempo. El laboratorio guarda la verdad, y no podrás huir de ella.", 4.5f);
            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.Say("KAEL liberó los drones... ¡La puerta del laboratorio quedó desbloqueada! Vamos, Eira.", null);
            gd.SetBigText("KAEL TE VIO", "Los drones de vigilancia ya te localizaron", 3f);
            gd.AddPoints(75, "Evidencia de la amenaza");

            door?.Unlock();
            city?.OnKaelTriggered();
        }
    }

    // ------------------------------------------------------------------
    // La puerta que abre el laboratorio
    // ------------------------------------------------------------------
    public class LabEntranceDoor : Interactable
    {
        GameObject _slabL, _slabR;
        Material _glow;
        bool _unlocked;

        public void Setup(GameObject slabL, GameObject slabR, Material glow)
        {
            _slabL = slabL;
            _slabR = slabR;
            _glow = glow;
            PromptText = "[E] Activar puerta del laboratorio (BLOQUEADA)";
        }

        public void Unlock()
        {
            _unlocked = true;
            PromptText = "[E] Abrir la puerta del laboratorio";
            if (_glow != null) _glow.color = WorldBuilder.GreenGlow;
        }

        public override void Interact()
        {
            var gd = GameDirector.Instance;
            if (gd == null) return;
            if (!_unlocked)
            {
                gd.ShowDialogue("SISTEMA", "CERRADURA SECUNDARIA ACTIVA.\nBusca la fuente de datos: el holograma del este.", 3.5f);
                return;
            }

            IsAvailable = false;
            gd.SetPrompt("");
            gd.SetObjective("Abriendo el laboratorio...");
            gd.AddPoints(150, "Desbloquear la entrada");
            StartCoroutine(OpenDoor(gd));
        }

        IEnumerator OpenDoor(GameDirector gd)
        {
            Vector3 a0 = _slabL.transform.localPosition;
            Vector3 b0 = _slabR.transform.localPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.6f;
                _slabL.transform.localPosition = a0 + Vector3.up * 6f * t;
                _slabR.transform.localPosition = b0 + Vector3.up * 6f * t;
                yield return null;
            }
            _slabL.transform.localPosition = a0 + Vector3.up * 6f;
            _slabR.transform.localPosition = b0 + Vector3.up * 6f;
            yield return new WaitForSeconds(0.9f);
            if (gd != null) gd.BeginLabEscape();
        }
    }
}