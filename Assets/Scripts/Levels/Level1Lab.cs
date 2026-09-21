using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Nivel 1 — «El Despertar»: laboratorio abandonado. Construye la geometría completa,
    /// los terminales, el ritual de la cortadura, las puertas genéticas, los drones,
    /// las trampas y la Máquina Guardiana. Objetivo: escapar junto a NOVA.
    /// </summary>
    public class Level1Lab : MonoBehaviour
    {
        Material _floor, _wall, _wallWarm, _accentDark, _cyan, _orange, _red, _dark;
        Material _rust, _pipe, _cableMat, _warnY, _warnK, _glass, _panel, _floorDmg;

        public void Build(Transform player)
        {
            InitMaterials();

            // Modo preferido: el laboratorio escaneado (gaussian splatting) limpio.
            if (TryBuildGaussianLab(player)) return;

            // Respaldo: la geometría procedural original.
            BuildProcedural(player);
        }

        void InitMaterials()
        {
            _floor = WorldBuilder.Mat("L1_Floor", new Color(0.13f, 0.14f, 0.16f));
            _wall  = WorldBuilder.Mat("L1_Wall",  new Color(0.26f, 0.28f, 0.33f));
            _wallWarm = WorldBuilder.Mat("L1_WallWarm", new Color(0.30f, 0.24f, 0.20f));
            _accentDark = WorldBuilder.Mat("L1_Accent", new Color(0.19f, 0.21f, 0.25f));
            _cyan   = WorldBuilder.Mat("L1_Cyan", WorldBuilder.CyanGlow, true);
            _orange = WorldBuilder.Mat("L1_Orange", WorldBuilder.OrangeGlow, true);
            _red    = WorldBuilder.Mat("L1_Red", WorldBuilder.RedGlow, true, 0.9f);
            _dark   = WorldBuilder.Mat("L1_Dark", new Color(0.05f, 0.06f, 0.08f), true);
            _rust   = WorldBuilder.Mat("L1_Rust", new Color(0.34f, 0.21f, 0.13f));
            _pipe   = WorldBuilder.Mat("L1_Pipe", new Color(0.30f, 0.32f, 0.36f));
            _cableMat = WorldBuilder.Mat("L1_Cable", new Color(0.07f, 0.07f, 0.09f));
            _warnY  = WorldBuilder.Mat("L1_WarnY", new Color(0.82f, 0.62f, 0.08f));
            _warnK  = WorldBuilder.Mat("L1_WarnK", new Color(0.06f, 0.06f, 0.07f));
            _glass  = WorldBuilder.Mat("L1_Glass", new Color(0.35f, 0.60f, 0.68f), true, 0.32f);
            _panel  = WorldBuilder.Mat("L1_Panel", new Color(0.22f, 0.26f, 0.33f));
            _floorDmg = WorldBuilder.Mat("L1_FloorDmg", new Color(0.09f, 0.09f, 0.11f));
        }

        void BuildProcedural(Transform player)
        {
            var root = new GameObject("Lab_Geo").transform;
            root.SetParent(transform, false);

            // -----------------------------------------------------------------
            // SALA A — Despertar
            // -----------------------------------------------------------------
            float aX0 = -74f, aX1 = -60f;
            BuildFloor(root, aX0, aX1, -15f, 15f);
            BuildSideWalls(root, aX0, aX1, -15f, 15f);
            // pared trasera
            Box(root, new Vector3(aX0 - 0.5f, 3.25f, 0f), new Vector3(1f, 6.5f, 30f));

            // Plataforma de despertar
            Box(root, new Vector3(-71f, 0.25f, 0f), new Vector3(4f, 0.5f, 6f), _floor);
            Quad(root, new Vector3(-71f, 1.4f, 0f), new Vector3(2.6f, 0.14f, 0.01f), _cyan);

            // Máquinas rotas decorativas
            Box(root, new Vector3(-64f, 0.5f, -8f), new Vector3(3f, 1f, 2.4f), _accentDark);
            Cyl(root, new Vector3(-66f, 1.2f, 9f), new Vector3(0.7f, 1.2f, 0.7f), _accentDark);
            Box(root, new Vector3(-63f, 0.8f, 6f), new Vector3(2f, 1.6f, 2f), _accentDark);
            // Cables por el techo
            for (int i = 0; i < 4; i++)
                Cyl(root, new Vector3(-68f + i * 2.4f, 5.2f, 0f), new Vector3(0.08f, 5.2f, 0.08f), _dark, false);

            WorldBuilder.FlickerLight("LuzA", new Vector3(-67f, 5.6f, 0f), new Color(0.7f, 0.85f, 1f), 9f, 1.6f).transform.SetParent(root, false);

            // NOVA comienza aquí dentro mientras Eira despierta
            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.transform.position = new Vector3(-63f, 0f, 2f);

            // -----------------------------------------------------------------
            // PASILLO B — primera lección de sigilo
            // -----------------------------------------------------------------
            float bX0 = -60f, bX1 = -46f;
            BuildFloor(root, bX0, bX1, -7f, 7f);
            BuildSideWalls(root, bX0, bX1, -7f, 7f);
            for (int i = 0; i < 3; i++)
                Box(root, new Vector3(-57f + i * 5f, 5.45f, 0f), new Vector3(1.2f, 0.4f, 8f), _accentDark, false);
            for (int i = 0; i < 5; i++)
                Cyl(root, new Vector3(-58f + i * 3f, 5.1f, 0f), new Vector3(0.06f, 4.8f, 0.06f), _dark, false);

            WorldBuilder.FlickerLight("LuzB1", new Vector3(-51f, 5.6f, 0f), new Color(1f, 0.8f, 0.5f), 8f, 1.4f).transform.SetParent(root, false);

            // Dron de entrenamiento
            SpawnDrone(root, new Vector3[] { new Vector3(-58f, 1.8f, -3f), new Vector3(-48f, 1.8f, 3f) });

            WorldBuilder.Sphere("CapEnergia_B", new Vector3(-55f, 1.2f, -5.5f), 0.3f, _cyan)
                .AddComponent<EnergyPickup>().amount = 35f;

            // -----------------------------------------------------------------
            // SALA C — Archivos / información
            // -----------------------------------------------------------------
            float cX0 = -46f, cX1 = -28f;
            BuildFloor(root, cX0, cX1, -13f, 13f);
            BuildSideWalls(root, cX0, cX1, -13f, 13f);

            // Terminal 1: AÑO 3000
            int reads = 0;
            var t1 = BuildTerminal(root, new Vector3(-42f, 0f, -8f), _cyan, "SISTEMA");
            t1.Setup("SISTEMA", new string[] {
                "AÑO 3000.",
                "Los humanos desaparecieron hace décadas.",
                "Bienvenidas a las ruinas, portadora."
            }, t1.ScreenMat(), () => { if (++reads >= 2) InfoComplete(); });

            // Terminal 2: LA GUERRA
            var t2 = BuildTerminal(root, new Vector3(-32f, 0f, 8f), _orange, "CATÁLOGO");
            t2.Setup("CATÁLOGO", new string[] {
                "GUERRA HUMANO-ANDROIDE.",
                "Los humanos crearon los androides.",
                "Los androides despertaron conciencia.",
                "La rebelión destruyó el mundo."
            }, t2.ScreenMat(), () => { if (++reads >= 2) InfoComplete(); });

            void InfoComplete()
            {
                GameDirector.Instance.SetObjective("Encuentra la cerradura bio-genética (el filo).");
                if (NovaCompanion.Instance != null)
                    NovaCompanion.Instance.Say("Estos archivos... Ustedes guardaron esto. Hay una cerradura bio-genética al sur.", null);
            }

            // Dron patrullando archivo
            SpawnDrone(root, new Vector3[] { new Vector3(-44f, 1.8f, -10f), new Vector3(-30f, 1.8f, 8f) });

            WorldBuilder.Sphere("CapEnergia_C", new Vector3(-30f, 1.2f, -10f), 0.3f, _orange)
                .AddComponent<EnergyPickup>().amount = 40f;

            // -----------------------------------------------------------------
            // SALA D — La CORTADURA (Clave de Sangre)
            // -----------------------------------------------------------------
            float dX0 = -28f, dX1 = -10f;
            BuildFloor(root, dX0, dX1, -9f, 9f);
            BuildSideWalls(root, dX0, dX1, -9f, 9f);

            // Pedestal + filo + escáner
            Cyl(root, new Vector3(-19f, 0.5f, 0f), new Vector3(1.6f, 0.5f, 1.6f), _accentDark);
            var scannerDisc = Cyl(root, new Vector3(-19f, 1.05f, 0f), new Vector3(1.5f, 0.06f, 1.5f), _red, true);
            var blade = Box(root, new Vector3(-19f, 1.8f, 0.5f), new Vector3(0.14f, 0.9f, 0.14f), WorldBuilder.Mat("Blade", new Color(0.85f, 0.2f, 0.2f), true));
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            Box(root, new Vector3(-20.6f, 1.6f, 0f), new Vector3(0.3f, 1.8f, 0.3f), _accentDark);
            Box(root, new Vector3(-17.4f, 1.6f, 0f), new Vector3(0.3f, 1.8f, 0.3f), _accentDark);
            Box(root, new Vector3(-19f, 2.6f, 0f), new Vector3(3.4f, 0.3f, 0.3f), _accentDark);

            var ritual = new GameObject("Ritual_Cortadura");
            ritual.transform.position = new Vector3(-19f, 0f, 0.5f);
            ritual.transform.SetParent(root, false);
            var ritualComp = ritual.AddComponent<BloodRitualMachine>();
            ritualComp.Setup(scannerDisc.GetComponent<MeshRenderer>().sharedMaterial);
            ritualComp.AddProximity(new Vector3(0f, 1.2f, 0f), new Vector3(3f, 2.6f, 3f));

            // Puerta genética 1 (hacia el pasillo E)
            var door1 = BuildGeneDoor(root, new Vector3(-10f, 2.2f, 0f), _accentDark, _cyan, _wall, new Vector3(-14.6f, 2.2f, 0f));
            door1.onOpened = () =>
            {
                GameDirector.Instance.SetObjective("Avanza por el pasillo. Cuida tu corazón y esquiva el plasma.");
                GameDirector.Instance.SetCheckpoint(new Vector3(-12f, 0.5f, 0f));
            };

            // -----------------------------------------------------------------
            // PASILLO E — trampas de plasma
            // -----------------------------------------------------------------
            float eX0 = -10f, eX1 = 8f;
            BuildFloor(root, eX0, eX1, -7f, 7f);
            BuildSideWalls(root, eX0, eX1, -7f, 7f);
            for (int i = 0; i < 4; i++)
                Box(root, new Vector3(-8f + i * 4f, 5.45f, 0f), new Vector3(1.2f, 0.4f, 14f), _accentDark, false);

            BuildPlasmaTrap(root, new Vector3(-4f, 0.12f, 3f));
            BuildPlasmaTrap(root, new Vector3(2f, 0.12f, -3f));

            // Botiquín tecnológico
            BuildMedkit(root, new Vector3(-6f, 0.8f, -5.5f));

            // Puerta genética 2 (hacia el hangar)
            var door2 = BuildGeneDoor(root, new Vector3(8f, 2.2f, 0f), _accentDark, _orange, _wall, new Vector3(11.6f, 2.2f, 0f));
            door2.onOpened = () =>
            {
                GameDirector.Instance.SetObjective("Entra al hangar y sobrecarga la Máquina Guardiana.");
                GameDirector.Instance.SetCheckpoint(new Vector3(10f, 0.5f, 0f));
            };

            // -----------------------------------------------------------------
            // HANGAR F — Jefe final
            // -----------------------------------------------------------------
            float fX0 = 8f, fX1 = 42f;
            BuildFloor(root, fX0, fX1, -16f, 16f);
            BuildSideWalls(root, fX0, fX1, -16f, 16f);
            // Pared trasera sólida (sin huecos): la salida es una puerta, no un agujero.
            Box(root, new Vector3(42.5f, 3.25f, 0f), new Vector3(1f, 6.5f, 32f));

            // Barras superiores
            for (int i = 0; i < 5; i++)
                Box(root, new Vector3(12f + i * 7f, 5.6f, 0f), new Vector3(1.4f, 0.5f, 32f), _accentDark, false);

            WorldBuilder.FlickerLight("LuzH1", new Vector3(14f, 5.8f, 10f), new Color(1f, 0.6f, 0.3f), 11f, 1.8f).transform.SetParent(root, false);
            WorldBuilder.FlickerLight("LuzH2", new Vector3(34f, 5.8f, -10f), new Color(1f, 0.6f, 0.3f), 11f, 1.8f).transform.SetParent(root, false);

            // La Máquina Guardiana
            var bossGo = EnemyVisuals.BuildGuardian(out Material coreMat, out var flashMats);
            bossGo.transform.position = new Vector3(26f, 0f, 0f);
            bossGo.transform.SetParent(root, false);
            var boss = bossGo.AddComponent<GuardianBoss>();
            boss.Setup(coreMat);
            foreach (var m in flashMats) boss.RegisterFlashMat(m);

            // 3 consolas de sobrecarga
            BuildOverloadConsole(root, new Vector3(16f, 0f, -12f), boss, _orange);
            BuildOverloadConsole(root, new Vector3(36f, 0f, -12f), boss, _orange);
            BuildOverloadConsole(root, new Vector3(36f, 0f, 12f), boss, _orange);

            // Salida principal
            var exitDoor = Box(root, new Vector3(41f, 2.5f, 0f), new Vector3(0.4f, 5f, 6f), _accentDark);
            var exitGo = new GameObject("Salida");
            exitGo.transform.position = new Vector3(38f, 0f, 0f);
            exitGo.transform.SetParent(root, false);
            var exit = exitGo.AddComponent<ExitPoint>();
            exit.Setup(exitDoor);
            exit.requiredBoss = boss;
            exit.AddProximity(new Vector3(0f, 1f, -1f), new Vector3(2.5f, 3f, 4f));

            boss.onDefeated += () =>
            {
                GameDirector.Instance.SetCheckpoint(new Vector3(37f, 0.5f, 0f));
            };

            // -----------------------------------------------------------------
            // Vestuario apocalíptico del laboratorio (cables, tuberías, máquinas
            // destruidas, escombros, luces de emergencia...).
            // -----------------------------------------------------------------
            BuildApocalypticDressing(root);

            // -----------------------------------------------------------------
            // Sellar los muros laterales (quita los «huecos» entre salas anchas
            // y pasillos estrechos) y añadir el mobiliario científico.
            // -----------------------------------------------------------------
            SealRoomJoints(root);
            BuildScientificDressing(root);

            // -----------------------------------------------------------------
            // Soldados androides: solo se detienen con el pulso de Eira (Q).
            // -----------------------------------------------------------------
            SpawnSoldier(root, new Vector3[] { new Vector3(-57f, 0f, -4f), new Vector3(-50f, 0f, 4f) });
            SpawnSoldier(root, new Vector3[] { new Vector3(-44f, 0f, -8f), new Vector3(-34f, 0f, -10f) });
            SpawnSoldier(root, new Vector3[] { new Vector3(14f, 0f, 12f), new Vector3(21f, 0f, 12f) });

            // -----------------------------------------------------------------
            // Posición inicial del jugador
            // -----------------------------------------------------------------
            player.GetComponent<PlayerController>()?.Teleport(new Vector3(-70.5f, 0.9f, 0f));
            player.rotation = Quaternion.Euler(0f, 90f, 0f);

            GameDirector.Instance.SetObjective("Investiga el laboratorio y encuentra información sobre el año 3000. Si chocas con un androide, usa tu pulso (Q).");
        }

        // ---------------------------------------------------------------
        // Vestuario apocalíptico: hace que el laboratorio se vea destruido
        // y con color (cables, tuberías, máquinas rotas, escombros, avisos).
        // ---------------------------------------------------------------
        void BuildApocalypticDressing(Transform root)
        {
            // Límites de las salas (x0, x1) y media profundidad en Z.
            float[] x0s   = { -74f, -60f, -46f, -28f, -10f,   8f };
            float[] x1s   = { -60f, -46f, -28f, -10f,   8f,  42f };
            float[] zhalf = {  15f,   7f,  13f,   9f,   7f,  16f };

            for (int r = 0; r < 6; r++)
            {
                float x0 = x0s[r], x1 = x1s[r], z = zhalf[r];
                float cx = (x0 + x1) * 0.5f, dx = x1 - x0;
                bool warm = (r % 2 == 1);

                // ---- Tuberías a lo largo de ambas paredes
                BuildPipeRun(root, new Vector3(cx, 4.7f,  z - 0.6f), dx - 1f, false);
                BuildPipeRun(root, new Vector3(cx, 4.7f, -z + 0.6f), dx - 1f, false);
                BuildPipeRun(root, new Vector3(cx, 3.4f,  z - 0.55f), dx - 1.4f, true);
                BuildPipeRun(root, new Vector3(cx, 3.4f, -z + 0.55f), dx - 1.4f, true);

                // ---- Paneles y manchas de óxido en las paredes
                for (int i = 0; i < Mathf.Max(2, Mathf.RoundToInt(dx / 6f)); i++)
                {
                    float px = x0 + 3f + i * 6f;
                    if (px > x1 - 2f) break;
                    float pz = (z - 0.52f) * (i % 2 == 0 ? 1f : -1f);
                    var panel = Box(root, new Vector3(px, 2.2f + (i % 3) * 0.8f, pz), new Vector3(2.2f, 1.4f, 0.06f), warm ? _wallWarm : _panel, false);
                    if (i % 3 == 0)
                    {
                        var rust = Box(root, new Vector3(px + 1.1f, 1.6f, pz), new Vector3(1.4f, 0.9f, 0.05f), _rust, false);
                        rust.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    }
                }

                // ---- Cables por el techo (cruzan la sala) y colgantes
                for (int i = 0; i < 5; i++)
                {
                    float cxp = x0 + 2f + i * (dx - 4f) / 5f;
                    var cable = WorldBuilder.Cyl("Cable", new Vector3(cxp, 5.2f, 0f), new Vector3(0.05f, z, 0.05f), _cableMat, false);
                    cable.transform.SetParent(root, false);
                    cable.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    if (i % 2 == 0)
                    {
                        var hang = WorldBuilder.Cyl("CableColgante", new Vector3(cxp + 0.6f, 4.4f, (i - 2) * 1.8f), new Vector3(0.035f, 0.8f, 0.035f), _cableMat, false);
                        hang.transform.SetParent(root, false);
                        hang.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);
                    }
                }

                // ---- Barandilla/banda de aviso en el suelo junto a la pared
                BuildHazardStrip(root, new Vector3(cx, 0.02f, z - 0.9f), dx - 2f, 0f);

                // ---- Escombros dispersos
                Random.InitState(1000 + r);
                int debris = 4 + r;
                for (int i = 0; i < debris; i++)
                {
                    float rx = x0 + 2f + Random.value * (dx - 4f);
                    float rz = (Random.value * 2f - 1f) * (z - 2f);
                    var d = Box(root, new Vector3(rx, 0.12f + Random.value * 0.12f, rz),
                        new Vector3(0.3f + Random.value * 0.7f, 0.2f + Random.value * 0.4f, 0.3f + Random.value * 0.7f), _bldDarkish(), false);
                    d.transform.localRotation = Quaternion.Euler(Random.value * 20f, Random.value * 360f, Random.value * 20f);
                }

                // ---- Luz de emergencia roja en salas alternas
                if (r % 2 == 0)
                    WorldBuilder.FlickerLight("Emergencia", new Vector3(cx, 5.6f, 0f), new Color(1f, 0.15f, 0.12f), 12f, 1.3f).transform.SetParent(root, false);
            }

            // ---- Máquinas destruidas repartidas por las salas
            BrokenMachine(root, new Vector3(-66f, 0f,  10f), 20f);
            BrokenMachine(root, new Vector3(-63f, 0f, -11f), -35f);
            BrokenMachine(root, new Vector3(-52f, 0f,   4f), 10f);
            BrokenMachine(root, new Vector3(-40f, 0f, -10f), 55f);
            BrokenMachine(root, new Vector3(-30f, 0f,   8f), -20f);
            BrokenMachine(root, new Vector3(-16f, 0f,  -7f), 40f);
            BrokenMachine(root, new Vector3(  0f, 0f,   5f), -50f);
            BrokenMachine(root, new Vector3( 20f, 0f,  13f), 15f);
            BrokenMachine(root, new Vector3( 33f, 0f, -13f), -30f);

            // ---- Cristales rotos en el suelo
            Material glass = _glass;
            for (int i = 0; i < 14; i++)
            {
                float gx = -72f + i * 8f;
                var shard = Box(root, new Vector3(gx, 0.03f, (i % 2 == 0 ? 3f : -3f)), new Vector3(0.5f, 0.04f, 0.35f), glass, false);
                shard.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
            }

            // ---- Aviso de peligro antes de las puertas genéticas y el hangar
            BuildHazardStrip(root, new Vector3(-11.2f, 0.02f, 0f), 4f, 90f);
            BuildHazardStrip(root, new Vector3( 9.2f, 0.02f, 0f), 4f, 90f);
            BuildHazardStrip(root, new Vector3( 40f,  0.02f, 0f), 5f, 90f);
        }

        // ---------------------------------------------------------------
        // Sellar los muros: rellena los «huecos» que dejan las salas anchas
        // al conectar con pasillos estrechos (los pasillos ya no caen al vacío).
        // ---------------------------------------------------------------
        void SealRoomJoints(Transform root)
        {
            SealJoint(root, -60f, 15f, 7f);
            SealJoint(root, -46f, 7f, 13f);
            SealJoint(root, -28f, 13f, 9f);
            SealJoint(root, -10f, 9f, 7f);
            SealJoint(root, 8f, 7f, 16f);
        }

        void SealJoint(Transform root, float x, float za, float zb)
        {
            float n = Mathf.Min(za, zb);
            float m = Mathf.Max(za, zb);
            float mid = (n + m) * 0.5f;
            float seg = m - n;
            if (seg < 0.05f) return;
            Box(root, new Vector3(x, 3.25f, mid),  new Vector3(1.2f, 6.5f, seg), _wall);
            Box(root, new Vector3(x, 3.25f, -mid), new Vector3(1.2f, 6.5f, seg), _wall);
        }

        // ---------------------------------------------------------------
        // Mobiliario de laboratorio científico abandonado: cápsulas criogénicas,
        // mesas de análisis, racks de tubos de ensayo, secuenciadores, señales
        // de bioseguridad y rieles/gantry del techo.
        // ---------------------------------------------------------------
        void BuildScientificDressing(Transform root)
        {
            // Cápsulas criogénicas (cámaras de contención) en SALA A y SALA C.
            BuildCryoPod(root, new Vector3(-69.5f, 0f, 9f), 70f);
            BuildCryoPod(root, new Vector3(-66f, 0f, -9f), -60f);
            BuildCryoPod(root, new Vector3(-43f, 0f, 10f), 50f);
            BuildCryoPod(root, new Vector3(-35f, 0f, -11f), -40f);

            // Mesas de análisis con equipo científico (SALA C y SALA D).
            BuildLabBench(root, new Vector3(-36f, 0f, -11f), 20f);
            BuildLabBench(root, new Vector3(-30.5f, 0f, 6f), -30f);
            BuildLabBench(root, new Vector3(-22f, 0f, -7f), 15f);

            // Secuenciador / máquina de análisis (SALA C).
            BuildSequencer(root, new Vector3(-38f, 0f, 4f), -20f);

            // Racks de tubos de ensayo junto a las mesas.
            BuildTubeRack(root, new Vector3(-36.8f, 0f, -9.6f), 6, 40f);
            BuildTubeRack(root, new Vector3(-31.3f, 0f, 7.4f), 6, -10f);

            // Señal de bioseguridad en el pasillo hacia el hangar.
            BuildBiohazardSign(root, new Vector3(-9.5f, 0f, 5.7f), 0f);
            BuildBiohazardSign(root, new Vector3(9.5f, 0f, 5.7f), 0f);

            // Rieles/regueras de techo del laboratorio (gantry) en salas grandes.
            BuildGantryRail(root, -72f, -62f, 0f);
            BuildGantryRail(root, -44f, -30f, 0f);
            BuildGantryRail(root, 12f, 38f, 0f);

            // Luz de emergencia extra sobre las cápsulas.
            WorldBuilder.FlickerLight("LuzCrioA", new Vector3(-68f, 4.6f, 9f), new Color(0.35f, 0.9f, 1f), 9f, 1.5f).transform.SetParent(root, false);
            WorldBuilder.FlickerLight("LuzCrioC", new Vector3(-39f, 4.6f, -11f), new Color(0.35f, 0.9f, 1f), 9f, 1.5f).transform.SetParent(root, false);
        }

        void BuildCryoPod(Transform root, Vector3 p, float yaw)
        {
            var g = new GameObject("CapsulaCrio");
            g.transform.position = p;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box(g.transform, new Vector3(0f, 0.12f, 0f), new Vector3(1.25f, 0.24f, 1.0f), _accentDark);
            Cyl(g.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.5f, 0.95f, 0.5f), _glass, false);
            Box(g.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.24f, 1.35f, 0.22f), _dark, false);
            Cyl(g.transform, new Vector3(0f, 1.82f, 0f), new Vector3(0.52f, 0.06f, 0.52f), _accentDark, false);
            Box(g.transform, new Vector3(0f, 1.95f, 0f), new Vector3(1.0f, 0.12f, 0.7f), _rust, false);
            WorldBuilder.PointLight("LuzCrio", new Vector3(0f, 0.9f, -0.15f), new Color(0.3f, 0.8f, 0.9f), 4f, 1.0f).transform.SetParent(g.transform, false);
        }

        void BuildLabBench(Transform root, Vector3 p, float yaw)
        {
            var g = new GameObject("MesaLab");
            g.transform.position = p;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box(g.transform, new Vector3(0f, 0.85f, 0f), new Vector3(1.7f, 0.08f, 0.75f), _panel);
            for (int i = 0; i < 4; i++)
            {
                float lx = i < 2 ? -0.72f : 0.72f;
                float lz = (i % 2 == 0) ? -0.30f : 0.30f;
                Box(g.transform, new Vector3(lx, 0.42f, lz), new Vector3(0.09f, 0.85f, 0.09f), _accentDark);
            }

            // Matraz de vidrio
            Cyl(g.transform, new Vector3(-0.42f, 1.0f, 0f), new Vector3(0.09f, 0.14f, 0.09f), _glass, false);
            Sphere(g.transform, new Vector3(-0.42f, 1.3f, 0f), 0.09f, _glass, false);
            // Mechero de laboratorio
            Cyl(g.transform, new Vector3(0.0f, 1.0f, 0f), new Vector3(0.08f, 0.13f, 0.08f), _rust, false);
            Sphere(g.transform, new Vector3(0.0f, 1.16f, 0f), 0.035f, _cyan, false);
            // Pequeña pantalla de datos (oscura)
            Box(g.transform, new Vector3(0.45f, 1.05f, 0f), new Vector3(0.3f, 0.26f, 0.04f), _dark, false);
            Quad(g.transform, new Vector3(0.45f, 1.05f, 0.03f), new Vector3(0.22f, 0.14f, 1f), _cyan);
        }

        void BuildSequencer(Transform root, Vector3 p, float yaw)
        {
            var g = new GameObject("Secuenciador");
            g.transform.position = p;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box(g.transform, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 1.2f, 0.9f), _panel);
            Cyl(g.transform, new Vector3(0f, 1.35f, 0f), new Vector3(0.4f, 0.35f, 0.4f), _accentDark);
            Quad(g.transform, new Vector3(0f, 1.55f, 0.0f), new Vector3(0.5f, 0.3f, 1f), _cyan);
            Box(g.transform, new Vector3(0f, 1.9f, 0f), new Vector3(1.5f, 0.12f, 0.6f), _rust, false);
            Cyl(g.transform, new Vector3(0.45f, 0.8f, 0f), new Vector3(0.06f, 0.5f, 0.06f), _orange, false);
        }

        void BuildTubeRack(Transform root, Vector3 p, int count, float yaw)
        {
            var g = new GameObject("RackTubos");
            g.transform.position = p;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Box(g.transform, new Vector3(0f, 0.25f, 0f), new Vector3(count * 0.16f + 0.1f, 0.06f, 0.4f), _accentDark);
            for (int i = 0; i < count; i++)
            {
                float tx = -count * 0.08f + i * 0.16f;
                Cyl(g.transform, new Vector3(tx, 0.45f, 0f), new Vector3(0.035f, 0.2f, 0.035f), _glass, false);
                Sphere(g.transform, new Vector3(tx, 0.66f, 0f), 0.03f, _cyan, false);
            }
        }

        void BuildBiohazardSign(Transform root, Vector3 p, float yaw)
        {
            var g = new GameObject("SenalBio");
            g.transform.position = p;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Cyl(g.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.04f, 0.6f, 0.04f), _accentDark);
            Box(g.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.9f, 0.7f, 0.06f), _warnY, false);
            Box(g.transform, new Vector3(0f, 1.5f, 0.04f), new Vector3(0.5f, 0.2f, 0.02f), _warnK, false);
        }

        void BuildGantryRail(Transform root, float x0, float x1, float z)
        {
            float cx = (x0 + x1) * 0.5f;
            float dx = x1 - x0;
            var rail = Cyl(root, new Vector3(cx, 5.35f, z), new Vector3(0.05f, dx * 0.5f, 0.05f), _pipe, false);
            rail.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int i = -1; i <= 1; i += 2)
            {
                var hanger = Cyl(root, new Vector3(cx + i * dx * 0.28f, 4.7f, z), new Vector3(0.03f, 0.65f, 0.03f), _accentDark, false);
                hanger.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            }
        }

        void SpawnSoldier(Transform root, Vector3[] waypoints)
        {
            var soldier = EnemyVisuals.BuildSoldier(out Material eye);
            soldier.transform.position = waypoints[0];
            soldier.transform.SetParent(root, false);
            var ctrl = soldier.AddComponent<AndroidSoldier>();
            ctrl.Init(waypoints, eye);
        }

        Material _bldDarkishCached;
        Material _bldDarkish()
        {
            if (_bldDarkishCached == null)
                _bldDarkishCached = WorldBuilder.Mat("L1_Debris", new Color(0.17f, 0.16f, 0.15f));
            return _bldDarkishCached;
        }

        void BuildPipeRun(Transform root, Vector3 center, float length, bool rust)
        {
            var m = rust ? _rust : _pipe;
            var pipe = WorldBuilder.Cyl("Tuberia", center, new Vector3(0.13f, length * 0.5f, 0.13f), m);
            pipe.transform.SetParent(root, false);
            pipe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // acostada sobre X

            // Bridas
            for (int i = -1; i <= 1; i += 2)
            {
                var flange = WorldBuilder.Cyl("Brida", center + new Vector3(i * length * 0.35f, 0f, 0f), new Vector3(0.22f, 0.06f, 0.22f), _accentDark);
                flange.transform.SetParent(root, false);
                flange.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        void BuildHazardStrip(Transform root, Vector3 center, float length, float yaw)
        {
            var g = new GameObject("BandaPeligro");
            g.transform.position = center;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            int n = Mathf.Max(3, Mathf.RoundToInt(length / 0.6f));
            for (int i = 0; i < n; i++)
            {
                float lx = -length * 0.5f + (i + 0.5f) * (length / n);
                var seg = WorldBuilder.Box("Seg", Vector3.zero, new Vector3(length / n * 0.55f, 0.02f, 0.5f), i % 2 == 0 ? _warnY : _warnK, false);
                seg.transform.SetParent(g.transform, false);
                seg.transform.localPosition = new Vector3(lx, 0f, 0f);
            }
        }

        void BrokenMachine(Transform root, Vector3 p, float yaw)
        {
            var g = new GameObject("MaquinaDestruida");
            g.transform.position = p;
            g.transform.SetParent(root, false);
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Cuerpo
            var body = WorldBuilder.Box("Cuerpo", p + new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 1.2f), _panel);
            body.transform.SetParent(root, false);
            var top = WorldBuilder.Box("Cabina", p + new Vector3(0f, 1.05f, 0f), new Vector3(1.15f, 0.3f, 0.9f), _accentDark);
            top.transform.SetParent(root, false);

            // Pantalla muerta (con un leve brillo)
            var screen = WorldBuilder.Quad("Pantalla", p + new Vector3(0f, 1.0f, 0.47f), new Vector3(0.8f, 0.34f, 1f), _dark);
            screen.transform.SetParent(root, false);

            // Placa arrancada + óxido
            var tear = WorldBuilder.Box("Desgarro", p + new Vector3(0.5f, 0.55f, 0.62f), new Vector3(0.55f, 0.45f, 0.08f), _rust, false);
            tear.transform.SetParent(root, false);
            tear.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);

            // Cables al suelo
            for (int i = 0; i < 3; i++)
            {
                var c = WorldBuilder.Cyl("Cable", p + new Vector3(-0.35f + i * 0.35f, 0.2f, 0.65f), new Vector3(0.02f, 0.22f, 0.02f), _cableMat, false);
                c.transform.SetParent(root, false);
                c.transform.localRotation = Quaternion.Euler(18f, 0f, (i - 1) * 16f);
            }

            // Chispa / luz parpadeante
            WorldBuilder.FlickerLight("Chispa", p + new Vector3(0.4f, 0.9f, 0.6f), new Color(1f, 0.55f, 0.15f), 5f, 1.4f).transform.SetParent(root, false);
        }

        // ---------------------------------------------------------------
        // Helpers de construcción
        // ---------------------------------------------------------------
        // Helpers de construcción
        // ---------------------------------------------------------------
        void BuildFloor(Transform root, float x0, float x1, float z0, float z1)
        {
            float dx = x1 - x0, dz = z1 - z0;
            Box(root, new Vector3((x0 + x1) / 2f, -0.5f, (z0 + z1) / 2f), new Vector3(dx, 1f, dz), _floor);
        }

        void BuildSideWalls(Transform root, float x0, float x1, float z0, float z1)
        {
            float dx = x1 - x0;
            float zMin = Mathf.Min(z0, z1), zMax = Mathf.Max(z0, z1);
            Box(root, new Vector3((x0 + x1) / 2f, 3.25f, zMin - 0.5f), new Vector3(dx, 6.5f, 1f));
            Box(root, new Vector3((x0 + x1) / 2f, 3.25f, zMax + 0.5f), new Vector3(dx, 6.5f, 1f));
        }

        GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material m = null, bool solid = true)
        {
            var go = WorldBuilder.Box("Box", pos, size, m != null ? m : _wall, solid);
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

        GameObject Sphere(Transform parent, Vector3 pos, float r, Material m, bool solid = true)
        {
            var go = WorldBuilder.Sphere("Sphere", pos, r, m, solid);
            go.transform.SetParent(parent, false);
            return go;
        }

        LabTerminal BuildTerminal(Transform root, Vector3 pos, Material screenMat, string label)
        {
            var go = new GameObject("Terminal_" + label);
            go.transform.position = pos;
            go.transform.SetParent(root, false);

            Cyl(go.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.7f, 0.5f, 0.7f), _accentDark);
            Box(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.4f, 0.3f, 1.0f), _accentDark);
            Quad(go.transform, new Vector3(0f, 1.6f, 0.55f), new Vector3(1.2f, 0.7f, 1f), screenMat);
            Box(go.transform, new Vector3(0f, 1.2f, 0.4f), new Vector3(0.5f, 0.1f, 0.25f), _cyan, false);

            var t = go.AddComponent<LabTerminal>();
            t.AddProximity(new Vector3(0f, 1.1f, 0f), new Vector3(2.2f, 2f, 2.2f));
            t.StoreScreen(go.transform.Find("Screen").GetComponent<MeshRenderer>().sharedMaterial);
            return t;
        }

        GeneDoor BuildGeneDoor(Transform root, Vector3 closedPos, Material dmat, Material glow, Material frameMat, Vector3 openPos)
        {
            var go = new GameObject("PuertaGenetica");
            go.transform.position = closedPos;
            go.transform.SetParent(root, false);

            Box(go.transform, Vector3.zero, new Vector3(6f, 4.4f, 0.4f), dmat);
            Box(go.transform, new Vector3(0f, 0f, 0.25f), new Vector3(2.6f, 0.5f, 0.06f), glow, false);
            Box(go.transform, new Vector3(0f, -2.2f, 0f), new Vector3(0.6f, 0.4f, 0.8f), frameMat);

            var gd = go.AddComponent<GeneDoor>();
            gd.Setup(closedPos, openPos);
            gd.AddProximity(new Vector3(0f, 0f, 1.5f), new Vector3(7f, 5f, 3.5f));
            return gd;
        }

        void BuildMedkit(Transform root, Vector3 pos)
        {
            var med = WorldBuilder.Box("Medkit", pos, new Vector3(0.5f, 0.35f, 0.5f), WorldBuilder.Mat("Med", new Color(0.95f, 0.95f, 0.95f)), false);
            med.transform.SetParent(root, false);
            var cross = WorldBuilder.Box("Cross", pos + Vector3.up * 0.02f, new Vector3(0.14f, 0.28f, 0.06f), WorldBuilder.Mat("Cross", WorldBuilder.RedGlow, true), false);
            cross.transform.SetParent(root, false);
            med.AddComponent<SpinBob>();
            med.AddComponent<MedkitPickup>();
        }

        void BuildPlasmaTrap(Transform root, Vector3 pos)
        {
            var mat = WorldBuilder.Mat("Plasma", new Color(1f, 0.4f, 0.12f), true, 0.7f);
            var trap = WorldBuilder.Box("Plasma", pos, new Vector3(3f, 0.14f, 3f), mat, false);
            trap.transform.SetParent(root, false);
            var col = trap.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3f, 0.6f, 3f);
            var pt = trap.AddComponent<PlasmaTrap>();
            pt.Init(mat);
        }

        void BuildOverloadConsole(Transform root, Vector3 pos, GuardianBoss boss, Material screenMat)
        {
            var go = new GameObject("Consola");
            go.transform.position = pos;
            go.transform.SetParent(root, false);

            Cyl(go.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.45f, 0.9f), _accentDark);
            Box(go.transform, new Vector3(0f, 1.0f, 0f), new Vector3(1.2f, 0.6f, 0.9f), _accentDark);
            var screen = WorldBuilder.Quad("Scr", new Vector3(0f, 1.05f, 0.48f), new Vector3(0.9f, 0.46f, 1f), screenMat);
            screen.transform.SetParent(go.transform, false);
            Cyl(go.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.06f, 0.6f, 0.06f), _cyan, false);

            var oc = go.AddComponent<OverloadConsole>();
            oc.Setup(screen.GetComponent<MeshRenderer>().sharedMaterial);
            oc.boss = boss;
            oc.AddProximity(new Vector3(0f, 0.8f, 0f), new Vector3(2f, 2.4f, 2f));
        }

        void SpawnDrone(Transform root, Vector3[] waypoints)
        {
            var drone = EnemyVisuals.BuildDrone(out Material eye);
            drone.transform.position = waypoints[0];
            drone.transform.SetParent(root, false);
            var ctrl = drone.AddComponent<EnemyDrone>();
            ctrl.Init(waypoints, eye);
        }

        // ---------------------------------------------------------------
        // MODO ESCANEADO (gaussian splatting)
        // El laboratorio es un escaneo real (Documents\pack\500k.spz). El suelo
        // y los muros invisibles se generan sobre el AABB del collider; los
        // props del Nivel 1 se colocan encima. Sin escombros ni vestuario apocalíptico.
        // ---------------------------------------------------------------
        bool TryBuildGaussianLab(Transform player)
        {
            var root = new GameObject("Lab_Gauss").transform;
            root.SetParent(transform, false);

            var stage = gameObject.AddComponent<SplatStage>();
            stage.splatPath = "Env/laboratorio/escenario.spz";
            stage.colliderPath = "Env/laboratorio/collider.glb";
            stage.stagePosition = Vector3.zero;
            stage.targetSpanX = 44f;
            stage.wallHeight = 10f;
            stage.Build();

            if (!stage.Ready)
            {
                Debug.LogWarning("[Level1Lab] El laboratorio escaneado no está listo; se usará el modo procedural.");
                Destroy(stage);
                Destroy(root.gameObject);
                return false;
            }

            var b = stage.StageBounds;
            float floorY = stage.GroundY;
            System.Func<float, float, Vector3> P = (nx, nz) =>
                new Vector3(b.min.x + nx * b.size.x, floorY, b.min.z + nz * b.size.z);
            Vector3 Up = Vector3.up;

            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.transform.position = P(0.07f, 0.44f);

            player.GetComponent<PlayerController>()?.Teleport(P(0.04f, 0.5f) + Up * 0.9f);
            player.rotation = Quaternion.Euler(0f, 90f, 0f);
            GameDirector.Instance.SetCheckpoint(P(0.04f, 0.5f) + Up * 0.5f);

            // ---- Información: AÑO 3000 y la guerra --------------------------
            int reads = 0;
            var t1 = BuildTerminal(root, P(0.16f, 0.32f), _cyan, "SISTEMA");
            t1.Setup("SISTEMA", new string[]
            {
                "AÑO 3000.",
                "Los humanos desaparecieron hace décadas.",
                "Bienvenidas a las ruinas, portadora."
            }, t1.ScreenMat(), () => { if (++reads >= 2) InfoComplete(); });

            var t2 = BuildTerminal(root, P(0.16f, 0.68f), _orange, "CATÁLOGO");
            t2.Setup("CATÁLOGO", new string[]
            {
                "GUERRA HUMANO-ANDROIDE.",
                "Los humanos crearon los androides.",
                "Los androides despertaron conciencia.",
                "La rebelión destruyó el mundo."
            }, t2.ScreenMat(), () => { if (++reads >= 2) InfoComplete(); });

            void InfoComplete()
            {
                GameDirector.Instance.SetObjective("Encuentra la cerradura bio-genética (el filo).");
                if (NovaCompanion.Instance != null)
                    NovaCompanion.Instance.Say("Estos archivos... Hay una cerradura bio-genética al sur.", null);
            }

            SpawnDrone(root, new Vector3[] { P(0.13f, 0.62f) + Up * 2f, P(0.24f, 0.38f) + Up * 2f });

            // ---- La cortadura (Clave de Sangre) ------------------------------
            var ritualPos = P(0.36f, 0.5f);
            Cyl(root, ritualPos + new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.5f, 1.6f), _accentDark);
            var scannerDisc = Cyl(root, ritualPos + new Vector3(0f, 1.05f, 0f), new Vector3(1.5f, 0.06f, 1.5f), _red, true);
            var blade = Box(root, ritualPos + new Vector3(0f, 1.6f, 0.6f), new Vector3(0.14f, 0.9f, 0.14f),
                WorldBuilder.Mat("Blade", new Color(0.85f, 0.2f, 0.2f), true));
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            Box(root, ritualPos + new Vector3(-1.5f, 1.6f, 0f), new Vector3(0.3f, 1.8f, 0.3f), _accentDark);
            Box(root, ritualPos + new Vector3(1.5f, 1.6f, 0f), new Vector3(0.3f, 1.8f, 0.3f), _accentDark);
            Box(root, ritualPos + new Vector3(0f, 2.6f, 0f), new Vector3(3.4f, 0.3f, 0.3f), _accentDark);

            var ritual = new GameObject("Ritual_Cortadura");
            ritual.transform.position = ritualPos + new Vector3(0f, 0f, 0.6f);
            ritual.transform.SetParent(root, false);
            var ritualComp = ritual.AddComponent<BloodRitualMachine>();
            ritualComp.Setup(scannerDisc.GetComponent<MeshRenderer>().sharedMaterial);
            ritualComp.AddProximity(new Vector3(0f, 1.2f, 0f), new Vector3(3f, 2.6f, 3f));

            // ---- Sellos bio-genéticos (bloquean de pared a pared) ------------
            BuildSeal(root, P(0.50f, 0.5f).x, b, () =>
            {
                GameDirector.Instance.SetObjective("Cruza el tramo de plasma. Cuida tu corazón.");
                GameDirector.Instance.SetCheckpoint(P(0.54f, 0.5f) + Up * 0.5f);
            });

            // Tramo de plasma
            BuildPlasmaTrap(root, P(0.57f, 0.36f));
            BuildPlasmaTrap(root, P(0.63f, 0.64f));
            BuildMedkit(root, P(0.55f, 0.22f));

            BuildSeal(root, P(0.73f, 0.5f).x, b, () =>
            {
                GameDirector.Instance.SetObjective("Entra al sector de contención y sobrecarga a la Máquina Guardiana.");
                GameDirector.Instance.SetCheckpoint(P(0.76f, 0.5f) + Up * 0.5f);
            });

            SpawnDrone(root, new Vector3[] { P(0.66f, 0.20f) + Up * 2f, P(0.80f, 0.80f) + Up * 2f });
            SpawnSoldier(root, new Vector3[] { P(0.12f, 0.82f), P(0.28f, 0.20f) });

            // ---- Sector de la Máquina Guardiana ------------------------------
            var bossGo = EnemyVisuals.BuildGuardian(out Material coreMat, out var flashMats);
            bossGo.transform.position = P(0.87f, 0.5f);
            bossGo.transform.SetParent(root, false);
            var boss = bossGo.AddComponent<GuardianBoss>();
            boss.Setup(coreMat);
            foreach (var m in flashMats) boss.RegisterFlashMat(m);

            BuildOverloadConsole(root, P(0.82f, 0.20f), boss, _orange);
            BuildOverloadConsole(root, P(0.92f, 0.16f), boss, _orange);
            BuildOverloadConsole(root, P(0.92f, 0.84f), boss, _orange);

            var exitDoor = Box(root, P(0.965f, 0.5f) + new Vector3(0f, 2.5f, 0f), new Vector3(0.4f, 5f, 6f), _accentDark);
            var exitGo = new GameObject("Salida");
            exitGo.transform.position = P(0.945f, 0.5f);
            exitGo.transform.SetParent(root, false);
            var exit = exitGo.AddComponent<ExitPoint>();
            exit.Setup(exitDoor);
            exit.requiredBoss = boss;
            exit.AddProximity(new Vector3(0f, 1f, -1f), new Vector3(2.5f, 3f, 4f));

            boss.onDefeated += () => GameDirector.Instance.SetCheckpoint(P(0.93f, 0.5f) + Up * 0.5f);

            // ---- Iluminación limpia ------------------------------------------
            WorldBuilder.PointLight("LuzInfo", P(0.16f, 0.5f) + Up * 2.6f, new Color(0.35f, 0.85f, 1f), 12f, 1.4f).transform.SetParent(root, false);
            WorldBuilder.PointLight("LuzRitual", ritualPos + Up * 2.6f, new Color(1f, 0.35f, 0.2f), 10f, 1.6f).transform.SetParent(root, false);
            WorldBuilder.FlickerLight("LuzJefe", P(0.87f, 0.5f) + Up * 2.8f, new Color(1f, 0.6f, 0.3f), 14f, 1.8f).transform.SetParent(root, false);

            GameDirector.Instance.SetObjective("Investiga el laboratorio: lee los archivos, obtén la clave de sangre y escapa con NOVA.");
            return true;
        }

        /// <summary>Un sello a lo ancho del laboratorio que se alza con la llave de sangre.</summary>
        void BuildSeal(Transform root, float gateX, Bounds b, System.Action onOpened)
        {
            float floorY = b.min.y;
            var slab = Box(root, new Vector3(gateX, floorY + 2.2f, b.center.z), new Vector3(0.5f, 4.4f, b.size.z), _accentDark);
            Quad(root, new Vector3(gateX + 0.28f, floorY + 2.2f, b.center.z), new Vector3(0.04f, 3.8f, b.size.z), _cyan);

            var gd = slab.AddComponent<GeneDoor>();
            gd.Setup(slab.transform.position, slab.transform.position + Vector3.up * 5f);
            gd.AddProximity(new Vector3(0f, 0f, 0f), new Vector3(2.4f, 5f, 2f));
            gd.onOpened = onOpened;
        }
    }
}