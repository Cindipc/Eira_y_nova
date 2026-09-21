using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Base del Nivel 2 — «La Caza»: la ciudad destruida del año 3000.
    /// Demuestra la infraestructura: ciudad, drones y soldados de Kael, holograma del villano,
    /// rescatables rebeldes, archivo del Proyecto Eira y refugio rebelde.
    /// </summary>
    public class Level2CityBase : MonoBehaviour
    {
        Transform _player;
        PlayerAbilities _abilities;
        NovaCompanion _nova;

        Material _asphalt, _bldMat, _bldDark, _cyan, _orange, _red, _dark;

        public void Build(Transform player, PlayerAbilities abilities, NovaCompanion nova)
        {
            _player = player;
            _abilities = abilities;
            _nova = nova;

            _asphalt = WorldBuilder.Mat("L2_Asphalt", new Color(0.11f, 0.12f, 0.14f));
            _bldMat  = WorldBuilder.Mat("L2_Bld",  new Color(0.26f, 0.27f, 0.31f));
            _bldDark = WorldBuilder.Mat("L2_BldDark", new Color(0.16f, 0.17f, 0.20f));
            _cyan    = WorldBuilder.Mat("L2_Cyan", WorldBuilder.CyanGlow, true);
            _orange  = WorldBuilder.Mat("L2_Orange", WorldBuilder.OrangeGlow, true);
            _red     = WorldBuilder.Mat("L2_Red", WorldBuilder.RedGlow, true, 0.85f);
            _dark    = WorldBuilder.Mat("L2_Dark", new Color(0.04f, 0.05f, 0.07f), true);

            var root = new GameObject("Ciudad_Ruinas").transform;
            root.SetParent(transform, false);

            // -----------------------------------------------------------------
            // Suelo de la calle (conecta con la salida del laboratorio)
            // -----------------------------------------------------------------
            Box(root, new Vector3((41.5f + 158f) / 2f, -0.5f, 0f), new Vector3(116.5f, 1f, 76f), _asphalt);

            // -----------------------------------------------------------------
            // Edificios derruidos (dos hileras)
            // -----------------------------------------------------------------
            BuildBuildingRow(root, 56f, 150f, -24f, new float[] { 18f, 26f, 14f, 20f, 30f, 16f, 22f });
            BuildBuildingRow(root, 64f, 146f,  24f, new float[] { 24f, 15f, 28f, 17f, 21f, 32f, 13f });

            // Escombros
            foreach (var pos in new Vector3[] { new Vector3(70f, 0.25f, -14f), new Vector3(88f, 0.3f, 12f),
                new Vector3(104f, 0.2f, -18f), new Vector3(120f, 0.35f, 6f), new Vector3(132f, 0.25f, -8f), new Vector3(60f, 0.25f, 20f) })
            {
                var rub = WorldBuilder.Box("Escombro", pos, new Vector3(Random.Range(1f, 2.6f), Random.Range(0.4f, 1.1f), Random.Range(1f, 2.6f)), _bldDark);
                rub.transform.SetParent(root, false);
            }

            // -----------------------------------------------------------------
            // Paso elevado roto
            // -----------------------------------------------------------------
            Box(root, new Vector3(92f, 6.1f, 0f), new Vector3(8f, 0.6f, 60f), _bldMat, false).transform.SetParent(root, false);
            for (int i = 0; i < 4; i++)
            {
                float z = -24f + i * 16f;
                Cyl(root, new Vector3(92f, 2.9f, z), new Vector3(0.5f, 2.9f, 0.5f), _bldDark);
            }

            // -----------------------------------------------------------------
            // Coches flotantes abandonados
            // -----------------------------------------------------------------
            Material carMat = WorldBuilder.Mat("L2_Car", new Color(0.3f, 0.32f, 0.38f));
            foreach (var pos in new Vector3[] { new Vector3(74f, 1.7f, -16f), new Vector3(96f, 1.7f, 16f), new Vector3(126f, 1.7f, -12f) })
            {
                var car = WorldBuilder.Box("Coche", pos, new Vector3(2.4f, 0.55f, 1.2f), carMat, false);
                car.transform.SetParent(root, false);
                car.AddComponent<SpinBob>().bobSpeed = 1.4f;
            }

            // Farolas
            for (float x = 60f; x < 150f; x += 14f)
            {
                Vector3 mid = new Vector3(x, 0f, 6f);
                Cyl(root, mid + Vector3.up * 1.7f, new Vector3(0.12f, 1.7f, 0.12f), _bldDark);
                var lamp = WorldBuilder.Sphere("LampGlow", mid + Vector3.up * 3.4f, 0.18f, _dark);
                lamp.transform.SetParent(root, false);
                lamp.name = "Lamp";
            }

            // Luz ambiental cálida de atardecer
            var sun = new GameObject("Sol_Nivel2");
            var dl = sun.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.color = new Color(1f, 0.6f, 0.35f);
            dl.intensity = 0.55f;
            sun.transform.rotation = Quaternion.Euler(20f, 30f, 0f);

            // -----------------------------------------------------------------
            // Drones de patrulla de Kael
            // -----------------------------------------------------------------
            SpawnDrone(root, new Vector3[] { new Vector3(62f, 2.2f, 0f), new Vector3(95f, 2.2f, 0f), new Vector3(128f, 2.2f, 0f) });
            SpawnDrone(root, new Vector3[] { new Vector3(85f, 2.2f, -18f), new Vector3(135f, 2.2f, -18f) });

            // Soldado androide patrullando la calle
            SpawnSoldier(root, new Vector3[] { new Vector3(80f, 0f, 16f), new Vector3(122f, 0f, 16f) });

            // -----------------------------------------------------------------
            // Holograma de KAEL
            // -----------------------------------------------------------------
            var holo = ProceduralCharacters.BuildKaelHologram(new Vector3(78f, 0f, -8f));
            holo.transform.SetParent(root, false);
            WorldBuilder.FlickerLight("LuzHolo", new Vector3(78f, 1f, -8f), new Color(0.3f, 0.7f, 1f), 10f, 2.2f).transform.SetParent(root, false);

            var kaelZone = new GameObject("Zona_Kael");
            kaelZone.transform.position = new Vector3(78f, 2f, -8f);
            kaelZone.transform.SetParent(root, false);
            var kc = kaelZone.AddComponent<BoxCollider>();
            kc.size = new Vector3(24f, 8f, 24f);
            kc.isTrigger = true;
            var enc = kaelZone.AddComponent<KaelEncounter>();
            enc.Init(this);

            // -----------------------------------------------------------------
            // Archivo del Proyecto Eira
            // -----------------------------------------------------------------
            var t = BuildTerminal(root, new Vector3(104f, 0f, -30f));
            t.points = 200;
            t.Setup("ARCHIVO", new string[] {
                "PROYECTO EIRA — REGISTRO CLASIFICADO.",
                "Los antepasados de la portadora formaron parte de un proyecto.",
                "OBJETIVO: crear humanos capaces de interactuar con IA.",
                "La enfermedad era un efecto secundario de la modificación."
            }, t.ScreenMat(), null);

            // -----------------------------------------------------------------
            // Rebelde caído para rescatar
            // -----------------------------------------------------------------
            BuildFallenRebel(root, new Vector3(122f, 0f, 24f));

            // -----------------------------------------------------------------
            // Refugio rebelde (fin de demo)
            // -----------------------------------------------------------------
            var door = Box(root, new Vector3(143f, 2.5f, 0f), new Vector3(0.5f, 5f, 6f), _bldDark);
            Quad(root, new Vector3(143f, 2.5f, 1f), new Vector3(2.5f, 3f, 0.02f), _cyan);
            WorldBuilder.PointLight("LuzRefugio", new Vector3(142f, 3f, 0f), new Color(0.3f, 0.8f, 1f), 8f, 2.6f).transform.SetParent(root, false);

            var shelterGo = new GameObject("Refugio_Rebelde");
            shelterGo.transform.position = new Vector3(140f, 0f, 0f);
            shelterGo.transform.SetParent(root, false);
            var shelter = shelterGo.AddComponent<ShelterEntrance>();
            shelter.Setup(door, _cyan);

            // Zona de checkpoint previa al refugio
            var cp = new GameObject("Checkpoint_Refugio");
            cp.transform.position = new Vector3(134f, 1f, 0f);
            cp.transform.SetParent(root, false);
            var cpCol = cp.AddComponent<BoxCollider>();
            cpCol.size = new Vector3(10f, 4f, 14f);
            cpCol.isTrigger = true;
            cp.AddComponent<CheckpointZone>();
            // -----------------------------------------------------------------
            // Posición del jugador al llegar a la ciudad
            // -----------------------------------------------------------------
            _player.GetComponent<PlayerController>()?.Teleport(new Vector3(52f, 1f, -2f));

            // Cinemática de inicio del Capítulo 2: Eira descubre sus poderes con la gota de sangre.
            var intro = gameObject.AddComponent<Level2Intro>();
            intro.Begin(_player);
        }

        // ---------------------------------------------------------------
        void BuildBuildingRow(Transform root, float x0, float x1, float z, float[] heights)
        {
            float step = (x1 - x0) / heights.Length;
            for (int i = 0; i < heights.Length; i++)
            {
                float h = heights[i];
                float cx = x0 + step * (i + 0.5f);
                float sw = step * 0.72f;
                var b = WorldBuilder.Box("Edificio", new Vector3(cx, h / 2f, z), new Vector3(sw, h, 9f), i % 2 == 0 ? _bldMat : _bldDark);
                b.transform.SetParent(root, false);
                // Ruina superior
                var top = WorldBuilder.Box("Ruina", new Vector3(cx + 1f, h + 0.4f, z + 0.5f), new Vector3(sw * 0.5f, 0.9f, 4f), _bldDark);
                top.transform.SetParent(root, false);
                // ventanas
                int nW = Mathf.RoundToInt(sw / 2.6f);
                for (int w = 0; w < nW; w++)
                    Quad(root, new Vector3(cx + (w - nW / 2f) * 2.6f, 2.5f, z + (i % 2 == 0 ? 4.6f : -4.6f)), new Vector3(1.3f, 1.1f, 0.02f), _dark);
            }
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

        GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material m = null, bool solid = true)
        {
            var go = WorldBuilder.Box("Box", pos, size, m != null ? m : _bldDark, solid);
            go.transform.SetParent(parent, false);
            return go;
        }

        GameObject Cyl(Transform parent, Vector3 pos, Vector3 scale, Material m, bool solid = true)
        {
            var go = WorldBuilder.Cyl("Cyl", pos, scale, m, solid);
            go.transform.SetParent(parent, false);
            return go;
        }

        GameObject Quad(Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var go = WorldBuilder.Quad("Screen", pos, scale, m);
            go.transform.SetParent(parent, false);
            return go;
        }

        LabTerminal BuildTerminal(Transform root, Vector3 pos)
        {
            var go = new GameObject("Terminal_Archivo");
            go.transform.position = pos;
            go.transform.SetParent(root, false);

            Cyl(go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 0.5f, 0.7f), _bldDark);
            Box(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.4f, 0.3f, 1f), _bldDark);
            WorldBuilder.Quad("Screen", new Vector3(0f, 1.6f, 0.55f), new Vector3(1.2f, 0.7f, 1f), _cyan).transform.SetParent(go.transform, false);

            var t = go.AddComponent<LabTerminal>();
            t.AddProximity(new Vector3(0f, 1.1f, 0f), new Vector3(2.2f, 2f, 2.2f));
            t.StoreScreen(go.transform.Find("Screen").GetComponent<MeshRenderer>().sharedMaterial);
            return t;
        }

        void BuildFallenRebel(Transform root, Vector3 pos)
        {
            var go = new GameObject("Rebelde_Caido");
            go.transform.position = pos;
            go.transform.SetParent(root, false);

            var body = WorldBuilder.Box("Body", new Vector3(0f, 0.7f, 0f), new Vector3(0.55f, 0.8f, 0.3f), _bldDark);
            body.transform.SetParent(go.transform, false);
            var head = WorldBuilder.Sphere("Head", new Vector3(0.55f, 0.65f, 0f), 0.16f, _bldMat);
            head.transform.SetParent(go.transform, false);
            var core = WorldBuilder.Sphere("Core", new Vector3(0f, 0.8f, 0.2f), 0.09f, _cyan);
            core.transform.SetParent(go.transform, false);

            var rr = go.AddComponent<RebelRescue>();
            rr.Setup(go, core.GetComponent<MeshRenderer>().sharedMaterial, new Vector3(140f, 0f, 24f));
            rr.AddProximity(new Vector3(0f, 0.8f, 0f), new Vector3(2.2f, 2f, 2.2f));
        }

        // Llamado por KaelEncounter al ver al jugador cerca del holograma
        public void KaelTriggered()
        {
            var cityRoot = transform.Find("Ciudad_Ruinas");
            if (cityRoot == null) return;
            SpawnDrone(cityRoot, new Vector3[] { new Vector3(70f, 2.2f, -2f), new Vector3(102f, 2.2f, -2f) });
            SpawnDrone(cityRoot, new Vector3[] { new Vector3(88f, 2.2f, 8f), new Vector3(116f, 2.2f, 8f) });
            GameDirector.Instance.SetObjective("¡Te buscan! Evita a los soldados y llega al refugio rebelde.");
            GameDirector.Instance.SetCheckpoint(new Vector3(82f, 0.6f, 0f));
        }
    }

    // ------------------------------------------------------------------
    /// <summary>Dispara el encuentro con el holograma de Kael.</summary>
    public class KaelEncounter : MonoBehaviour
    {
        Level2CityBase _city;
        bool _used;

        public void Init(Level2CityBase city) { _city = city; }

        void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.GetComponent<PlayerAbilities>() == null) return;
            _used = true;

            var gd = GameDirector.Instance;
            if (gd == null) return;

            gd.ShowDialogue("KAEL", "Entonces es cierto... nadie me lo puede ocultar. La portadora despertó.", 4.5f);
            gd.ShowSideNote("KAEL ha detectado una anomalía");
            gd.AddPoints(50, "Evidencia de la amenaza");
            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.Say("Es KAEL... Vio tu señal bio-genética. ¡Tenemos que huir! ¡El refugio está al este!", null);

            gd.SetBigText("KAEL TE BUSCA", "Los drones de vigilancia ya te localizaron", 3f);

            if (_city != null) _city.KaelTriggered();
        }
    }

    /// <summary>Registra el checkpoint más próximo al refugio.</summary>
    public class CheckpointZone : MonoBehaviour
    {
        bool _done;
        void OnTriggerEnter(Collider other)
        {
            if (_done) return;
            if (other.GetComponent<PlayerAbilities>() == null) return;
            _done = true;
            GameDirector.Instance?.SetCheckpoint(new Vector3(134f, 0.6f, 0f));
            GameDirector.Instance?.ShowSideNote("Punto de control: Refugio rebelde");
        }
    }

    /// <summary>Entrada al refugio rebelde = fin de la demo del Nivel 2.</summary>
    public class ShelterEntrance : Interactable
    {
        GameObject _door;
        public void Setup(GameObject door, Material glow)
        {
            _door = door;
            PromptText = "[E] Entrar al refugio rebelde";
        }

        public override void Interact()
        {
            var gd = GameDirector.Instance;
            if (gd == null) return;
            gd.SetPrompt("");
            gd.SetObjective("Entras al refugio rebelde...");
            gd.AddPoints(1000, "Nivel 2 (demo) completado");
            gd.CityComplete();
        }
    }
}