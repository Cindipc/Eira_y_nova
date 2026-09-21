using UnityEngine;

namespace EiraNova
{
    /// <summary>Construye los personajes jugables y NPC con primitivas (estilo «voxel/figura») usando la paleta del juego.</summary>
    public static class ProceduralCharacters
    {
        /// <summary>Quita los colisionadores de un muñeco visual (para que colisione solo el CharacterController).</summary>
        public static void StripColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(c);
        }

        // ------------------------------------------------------------------
        // Modelos importados (Assets/Resources/Models/*.obj)
        // Si están presentes se usan; si no, se cae de vuelta a la figura
        // procedural. Cada .obj es una sola malla sin materiales agrupados,
        // así que el cuerpo recibe un color y el pelo se añade por encima
        // con primitivas (para que cada personaje tenga su color de cabello).
        // ------------------------------------------------------------------
        public static bool UseImportedModels = true;

        public static Color EiraModelColor = new Color(0.30f, 0.42f, 0.55f); // chaqueta azul acero
        public static Color KaelModelColor = new Color(0.42f, 0.28f, 0.17f); // abrigo de cuero
        public static Color NovaModelColor = new Color(0.82f, 0.85f, 0.90f); // carcasa clara

        public static Color EiraHairColor = new Color(0.34f, 0.20f, 0.11f); // castaño ondulado
        public static Color EiraHairLight = new Color(0.46f, 0.29f, 0.17f);
        public static Color KaelHairColor = new Color(0.10f, 0.09f, 0.11f); // oscuro
        public static Color KaelHairLight = new Color(0.16f, 0.15f, 0.18f);

        /// <summary>Carga un modelo humanoide desde Resources/Models/&lt;name&gt;. Devuelve null si no existe.</summary>
        public static GameObject TryBuildModel(string name, Color tint, bool unlit = false, float alpha = 1f,
            float targetHeight = 1.62f, float yaw = 0f)
        {
            if (!UseImportedModels) return null;

            string path = "Models/" + name;

            // 1) Intentar como GameObject (prefab de modelo).
            GameObject model = null;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                model = Object.Instantiate(prefab);
            }
            else
            {
                // 2) Fallback: el asset principal es una Mesh.
                var mesh = Resources.Load<Mesh>(path);
                if (mesh != null)
                {
                    model = new GameObject(name + "_Mesh");
                    model.AddComponent<MeshFilter>().sharedMesh = mesh;
                    model.AddComponent<MeshRenderer>();
                }
            }

            if (model == null)
            {
                string found = "";
                foreach (var o in Resources.LoadAll<Object>("Models"))
                    found += o.name + "(" + o.GetType().Name + ") ";
                Debug.LogWarning("[ProceduralCharacters] No se encontro '" + path + "'. En Resources/Models hay: " + (found.Length == 0 ? "<vacio>" : found));
                return null;
            }

            var root = new GameObject(name);
            model.name = name + "_Mesh";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[ProceduralCharacters] '" + path + "' no tiene Renderers.");
                Object.Destroy(root);
                return null;
            }

            // Escalar a la altura deseada y apoyar los pies en y=0, centrado en x/z.
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            if (b.size.y > 0.001f)
            {
                model.transform.localScale = Vector3.one * (targetHeight / b.size.y);
                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            }
            model.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            var mat = WorldBuilder.Mat(name + "_Mat", tint, unlit, alpha);
            foreach (var r in renderers)
                r.sharedMaterial = mat;

            // Animación de marcha (bob/balanceo) para mallas sin esqueleto.
            root.AddComponent<CharacterWalk>().visual = model.transform;
            Debug.Log("[ProceduralCharacters] Modelo cargado: " + path + " (renderers=" + renderers.Length + ", alto=" + b.size.y.ToString("0.00") + ")");
            return root;
        }

        /// <summary>
        /// Devuelve los límites de la malla en su espacio LOCAL (sin escalar),
        /// para poder colocar cabello/remates en coordenadas 0..altura.
        /// </summary>
        public static Bounds LocalBounds(Transform body)
        {
            var rends = body.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds wb = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) wb.Encapsulate(rends[i].bounds);

            Vector3 c = body.InverseTransformPoint(wb.center);
            Vector3 s = body.InverseTransformVector(wb.size);
            s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            return new Bounds(c, s);
        }

        /// <summary>
        /// Añade cabello (y algunos remates) sobre la malla importada.
        /// El cuerpo pasado es el hijo "&lt;name&gt;_Mesh" y las coordenadas se
        /// calculan desde sus límites reales.
        /// </summary>
        public static void AttachHair(Transform body, string hairName, Color main, Color light, bool shortHair, bool female)
        {
            Material hm = WorldBuilder.Mat(hairName, main);
            Material hl = WorldBuilder.Mat(hairName + "_Light", light);
            hm.SetFloat("_Smoothness", 0.30f);

            Bounds b = LocalBounds(body);
            float hgt   = Mathf.Max(0.5f, b.size.y);
            float topY  = b.max.y;
            float zc    = b.center.z;
            float headR = hgt * 0.062f;
            float headY = topY - headR * 1.15f;

            Transform h = new GameObject(hairName + "_Root").transform;
            h.SetParent(body, false);

            // Casco principal (justo sobre el cráneo)
            var top = sphere(h.gameObject, "HairTop", new Vector3(0f, topY - headR * 0.35f, zc), headR * 0.95f, hm, false);
            top.transform.localScale = new Vector3(headR * 1.35f, headR * 0.95f, headR * 1.45f);

            sphere(h.gameObject, "HairBack", new Vector3(0f, headY, zc - headR * 0.55f), headR * 0.9f, hm, false);
            sphere(h.gameObject, "HairL", new Vector3(-headR * 0.95f, headY - headR * 0.1f, zc - headR * 0.1f), headR * 0.6f, hm, false);
            sphere(h.gameObject, "HairR", new Vector3(headR * 0.95f, headY - headR * 0.1f, zc - headR * 0.1f), headR * 0.6f, hm, false);

            if (shortHair)
            {
                // Flequillo corto
                box(h.gameObject, "Fringe", new Vector3(0f, topY - headR * 0.55f, zc + headR * 0.75f), new Vector3(headR * 1.7f, headR * 0.55f, headR * 0.4f), hm, false);
                sphere(h.gameObject, "HairNape", new Vector3(0f, headY - headR * 0.75f, zc - headR * 0.7f), headR * 0.6f, hm, false);
                sphere(h.gameObject, "HairWave1", new Vector3(-headR * 0.8f, topY - headR * 0.5f, zc + headR * 0.5f), headR * 0.28f, hl, false);
                sphere(h.gameObject, "HairWave2", new Vector3(headR * 0.8f, topY - headR * 0.5f, zc + headR * 0.5f), headR * 0.28f, hl, false);
            }
            else
            {
                // Mechones largos (por si se usa con pelo largo)
                sphere(h.gameObject, "HairLongL", new Vector3(-headR * 1.05f, headY - headR * 1.4f, zc - headR * 0.4f), headR * 0.65f, hm, false);
                sphere(h.gameObject, "HairLongR", new Vector3(headR * 1.05f, headY - headR * 1.4f, zc - headR * 0.4f), headR * 0.65f, hm, false);
            }

            if (female)
            {
                box(h.gameObject, "HairPin", new Vector3(headR * 1.1f, topY - headR * 0.45f, zc + headR * 0.4f), new Vector3(headR * 0.5f, headR * 0.12f, headR * 0.12f), hl, false);
            }
        }

        /// <summary>
        /// Remates de androide para la malla importada de NOVA: ojo central cian,
        /// núcleo de pecho y antena con punta luminosa, colocados por los límites reales.
        /// </summary>
        public static void AttachNovaDetails(Transform body)
        {
            Material glow = WorldBuilder.Mat("Nova_Glow", WorldBuilder.CyanGlow, true);
            Material black = Cap(WorldBuilder.Mat("Nova_Black", new Color(0.10f, 0.11f, 0.13f)));

            Bounds b = LocalBounds(body);
            float hgt = Mathf.Max(0.5f, b.size.y);
            float topY = b.max.y;
            float zc = b.center.z;
            float headR = hgt * 0.06f;
            float headY = topY - headR * 1.1f;

            Transform h = new GameObject("Nova_Detalles").transform;
            h.SetParent(body, false);

            sphere(h.gameObject, "ViseraOjo", new Vector3(0f, headY, zc + headR * 1.25f), headR * 0.75f, glow, false);
            sphere(h.gameObject, "OjoCentral", new Vector3(0f, headY, zc + headR * 1.45f), headR * 0.5f, glow, false);

            sphere(h.gameObject, "NucleoPecho", new Vector3(0f, b.center.y + hgt * 0.05f, zc + hgt * 0.045f), headR * 0.8f, glow, false);

            cyl(h.gameObject, "Antena", new Vector3(0f, topY + headR * 0.9f, zc), new Vector3(headR * 0.16f, headR * 1.0f, headR * 0.16f), black, false);
            sphere(h.gameObject, "PuntaAntena", new Vector3(0f, topY + headR * 1.55f, zc), headR * 0.3f, glow, false);
        }

        static Material Cap(Material m)
        {
            m.SetFloat("_Smoothness", 0.35f);
            return m;
        }

        // Helpers locales para construir muñecos con primitivas
        static GameObject box(GameObject parent, string name, Vector3 pos, Vector3 size, Material m, bool solid = true)
        {
            var go = WorldBuilder.Box(name, pos, size, m, solid);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject cyl(GameObject parent, string name, Vector3 pos, Vector3 scale, Material m, bool solid = true)
        {
            var go = WorldBuilder.Cyl(name, pos, scale, m, solid);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject sphere(GameObject parent, string name, Vector3 pos, float r, Material m, bool solid = true)
        {
            var go = WorldBuilder.Sphere(name, pos, r, m, solid);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ------------------------------------------------------------------
        // EIRA — protagonista humana
        // Superviviente del año 3000: chaqueta táctica oscura, correas cruzadas,
        // guantes sin dedos, botas de combate, cabello corto ondulado castaño
        // hasta los hombros y una pequeña cicatriz en la mejilla.
        // ------------------------------------------------------------------
        public static GameObject BuildEira(Vector3 at)
        {
            // Modelo importado (Resources/Models/eira) si está disponible.
            // La malla es una sola sin huesos, así que la marcha la da CharacterWalk
            // (rebote/inclinación); si el modelo no existe, se cae a la figura procedural.
            var imported = TryBuildModel("eira", EiraModelColor);
            if (imported != null)
            {
                imported.name = "Eira";
                imported.transform.position = at;
                AttachHair(imported.transform.GetChild(0), "Eira_Hair", EiraHairColor, EiraHairLight,
                    shortHair: true, female: true);
                StripColliders(imported);
                return imported;
            }

            var root = new GameObject("Eira");
            root.transform.position = at;

            // Grupo estático del torso/cabeza (se bambolea sobre las piernas).
            var body = new GameObject("Cuerpo");
            body.transform.SetParent(root.transform, false);

            // Pivotes de las extremidades para la marcha articulada.
            var hipL = new GameObject("HipL"); hipL.transform.SetParent(root.transform, false);
            hipL.transform.localPosition = new Vector3(-0.105f, 0.55f, 0f);
            var hipR = new GameObject("HipR"); hipR.transform.SetParent(root.transform, false);
            hipR.transform.localPosition = new Vector3(0.105f, 0.55f, 0f);
            var shL = new GameObject("ShL"); shL.transform.SetParent(root.transform, false);
            shL.transform.localPosition = new Vector3(-0.29f, 1.0f, 0f);
            var shR = new GameObject("ShR"); shR.transform.SetParent(root.transform, false);
            shR.transform.localPosition = new Vector3(0.29f, 1.0f, 0f);

            Material coat    = Cap(WorldBuilder.Mat("Eira_Coat",    new Color(0.16f, 0.17f, 0.20f)));
            Material coatD   = Cap(WorldBuilder.Mat("Eira_CoatDark", new Color(0.10f, 0.11f, 0.13f)));
            Material vest    = Cap(WorldBuilder.Mat("Eira_Vest",    new Color(0.27f, 0.30f, 0.35f)));
            Material strap   = WorldBuilder.Mat("Eira_Strap",       new Color(0.20f, 0.28f, 0.42f), true);
            Material pants   = Cap(WorldBuilder.Mat("Eira_Pants",   new Color(0.19f, 0.21f, 0.25f)));
            Material boot    = Cap(WorldBuilder.Mat("Eira_Boot",    new Color(0.08f, 0.08f, 0.10f)));
            Material skin    = WorldBuilder.Mat("Eira_Skin",        WorldBuilder.Skin);
            Material hair    = WorldBuilder.Mat("Eira_Hair",        new Color(0.33f, 0.19f, 0.11f));
            Material hairL   = WorldBuilder.Mat("Eira_HairLight",   new Color(0.45f, 0.27f, 0.16f));
            Material white   = WorldBuilder.Mat("Eira_White",       new Color(0.92f, 0.95f, 0.98f), true);
            Material scar    = WorldBuilder.Mat("Eira_Scar",        new Color(0.79f, 0.60f, 0.53f));

            // ---- Botas de combate + piernas (cada pierna cuelga de su cadera)
            box(hipL, "BootL", new Vector3(-0.105f, 0.06f, 0f), new Vector3(0.14f, 0.10f, 0.22f), boot);
            box(hipR, "BootR", new Vector3(0.105f, 0.06f, 0f), new Vector3(0.14f, 0.10f, 0.22f), boot);
            box(hipL, "SoleL", new Vector3(-0.105f, 0.012f, 0f), new Vector3(0.16f, 0.03f, 0.26f), coatD, false);
            box(hipR, "SoleR", new Vector3(0.105f, 0.012f, 0f), new Vector3(0.16f, 0.03f, 0.26f), coatD, false);
            cyl(hipL, "LegL", new Vector3(-0.105f, 0.36f, 0f), new Vector3(0.115f, 0.24f, 0.115f), pants);
            cyl(hipR, "LegR", new Vector3(0.105f, 0.36f, 0f), new Vector3(0.115f, 0.24f, 0.115f), pants);

            // ---- Cadera + torso (chaqueta)
            box(body, "Hips", new Vector3(0f, 0.64f, 0f), new Vector3(0.30f, 0.10f, 0.20f), coat);
            box(body, "Body", new Vector3(0f, 0.90f, 0f), new Vector3(0.34f, 0.40f, 0.24f), coat);
            box(body, "Vest", new Vector3(0f, 0.95f, 0f), new Vector3(0.36f, 0.30f, 0.25f), vest);

            // Hombreras
            box(body, "ShL", new Vector3(-0.27f, 1.06f, 0f), new Vector3(0.11f, 0.09f, 0.15f), coatD);
            box(body, "ShR", new Vector3(0.27f, 1.06f, 0f), new Vector3(0.11f, 0.09f, 0.15f), coatD);

            // Correas cruzadas (arnés táctico)
            var s1 = WorldBuilder.Box("StrapA", new Vector3(0f, 0.94f, -0.02f), new Vector3(0.62f, 0.035f, 0.03f), strap, false);
            s1.transform.SetParent(body.transform, false);
            s1.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            var s2 = WorldBuilder.Box("StrapB", new Vector3(0f, 0.94f, -0.03f), new Vector3(0.62f, 0.035f, 0.03f), strap, false);
            s2.transform.SetParent(body.transform, false);
            s2.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            box(body, "Buckle", new Vector3(0f, 0.88f, 0.02f), new Vector3(0.04f, 0.09f, 0.02f), coatD, false);

            // ---- Brazos con chaqueta + guantes sin dedos (cuelgan del hombro)
            cyl(shL, "ArmL", new Vector3(-0.29f, 0.80f, 0f), new Vector3(0.085f, 0.23f, 0.085f), coat);
            cyl(shR, "ArmR", new Vector3(0.29f, 0.80f, 0f), new Vector3(0.085f, 0.23f, 0.085f), coat);
            cyl(shL, "ForeL", new Vector3(-0.29f, 0.60f, 0f), new Vector3(0.07f, 0.15f, 0.07f), coatD);
            cyl(shR, "ForeR", new Vector3(0.29f, 0.60f, 0f), new Vector3(0.07f, 0.15f, 0.07f), coatD);
            // Puño del guante (sin dedos: muñeca cubierta, mano al aire)
            cyl(shL, "CuffL", new Vector3(-0.29f, 0.49f, 0f), new Vector3(0.08f, 0.06f, 0.08f), coatD);
            cyl(shR, "CuffR", new Vector3(0.29f, 0.49f, 0f), new Vector3(0.08f, 0.06f, 0.08f), coatD);
            sphere(shL, "HandL", new Vector3(-0.29f, 0.41f, 0f), 0.06f, skin);
            sphere(shR, "HandR", new Vector3(0.29f, 0.41f, 0f), 0.06f, skin);

            // ---- Cabeza
            sphere(body, "Head", new Vector3(0f, 1.32f, 0f), 0.16f, skin);

            // ---- Pelo corto ondulado hasta los hombros
            var hairBase = WorldBuilder.Sphere("HairBack", new Vector3(0f, 1.36f, 0f), 0.175f, hair);
            hairBase.transform.SetParent(body.transform, false);
            hairBase.transform.localScale = new Vector3(1f, 1.10f, 0.95f);
            sphere(body, "HairSideL", new Vector3(-0.17f, 1.20f, 0f), 0.11f, hair);
            sphere(body, "HairSideR", new Vector3(0.17f, 1.20f, 0f), 0.11f, hair);
            sphere(body, "HairCurlL1", new Vector3(-0.19f, 1.30f, 0.02f), 0.075f, hair);
            sphere(body, "HairCurlR1", new Vector3(0.19f, 1.30f, 0.02f), 0.075f, hair);
            sphere(body, "HairFramL", new Vector3(-0.13f, 1.12f, -0.02f), 0.085f, hair);
            sphere(body, "HairFramR", new Vector3(0.13f, 1.12f, -0.02f), 0.085f, hair);
            sphere(body, "HairLongL", new Vector3(-0.15f, 1.02f, -0.06f), 0.09f, hair);
            sphere(body, "HairLongR", new Vector3(0.15f, 1.02f, -0.06f), 0.09f, hair);
            sphere(body, "HairBack2", new Vector3(0f, 1.26f, -0.16f), 0.12f, hair);
            sphere(body, "HairNape", new Vector3(0f, 1.16f, -0.13f), 0.10f, hair);
            var hairTop = WorldBuilder.Sphere("HairTop", new Vector3(0f, 1.46f, 0f), 0.15f, hair);
            hairTop.transform.SetParent(body.transform, false);
            hairTop.transform.localScale = new Vector3(1.05f, 0.65f, 1f);
            var fringe = WorldBuilder.Box("Fringe", new Vector3(0f, 1.46f, 0.11f), new Vector3(0.30f, 0.05f, 0.04f), hairL, false);
            fringe.transform.SetParent(body.transform, false);
            // Ondas (mechones claros)
            sphere(body, "WaveL", new Vector3(-0.16f, 1.38f, 0.08f), 0.045f, hairL, false);
            sphere(body, "WaveR", new Vector3(0.16f, 1.38f, 0.08f), 0.045f, hairL, false);
            sphere(body, "WaveC", new Vector3(0f, 1.42f, 0.06f), 0.05f, hairL, false);

            // ---- Ojos
            sphere(body, "EyeL", new Vector3(-0.062f, 1.34f, 0.146f), 0.026f, white);
            sphere(body, "EyeR", new Vector3(0.062f, 1.34f, 0.146f), 0.026f, white);

            // ---- Cicatriz en la mejilla derecha
            var scarMark = box(body, "Scar", new Vector3(0.10f, 1.26f, 0.14f), new Vector3(0.035f, 0.012f, 0.005f), scar, false);
            scarMark.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            // ---- Marcha articulada (cuerpo dividido en extremidades)
            var gait = root.AddComponent<CharacterGait>();
            gait.Setup(hipL.transform, hipR.transform, shL.transform, shR.transform, body.transform);

            StripColliders(root);
            return root;
        }

        // ------------------------------------------------------------------
        // NOVA — androide compañero (blanco y negro, dañado, con el nombre grabado
        // en el pecho y un único ojo central azul brillante)
        // ------------------------------------------------------------------
        public static GameObject BuildNova(Vector3 at)
        {
            // Modelo importado (Resources/Models/nova) si está disponible, con los
            // remates de androide (ojo cian, antena, núcleo de pecho) sobre su geometría.
            var imported = TryBuildModel("nova", NovaModelColor);
            if (imported != null)
            {
                imported.name = "NOVA";
                imported.transform.position = at;
                AttachNovaDetails(imported.transform.GetChild(0));
                StripColliders(imported);
                return imported;
            }

            // NOVA también usa el cuerpo articulado en primitivas para poder
            // caminar con la marcha real (como Eira); el .obj es una malla única
            // sin huesos y no puede balancear las piernas.
            var root = new GameObject("NOVA");
            root.transform.position = at;

            var body = new GameObject("Cuerpo");
            body.transform.SetParent(root.transform, false);

            var hipL = new GameObject("HipL"); hipL.transform.SetParent(root.transform, false);
            hipL.transform.localPosition = new Vector3(-0.095f, 0.58f, 0f);
            var hipR = new GameObject("HipR"); hipR.transform.SetParent(root.transform, false);
            hipR.transform.localPosition = new Vector3(0.095f, 0.58f, 0f);
            var shL = new GameObject("ShL"); shL.transform.SetParent(root.transform, false);
            shL.transform.localPosition = new Vector3(-0.23f, 1.1f, 0f);
            var shR = new GameObject("ShR"); shR.transform.SetParent(root.transform, false);
            shR.transform.localPosition = new Vector3(0.23f, 1.1f, 0f);

            Material white  = Cap(WorldBuilder.Mat("Nova_White",  new Color(0.86f, 0.88f, 0.91f)));
            Material whiteD = Cap(WorldBuilder.Mat("Nova_WhiteDark", new Color(0.70f, 0.73f, 0.77f)));
            Material black  = Cap(WorldBuilder.Mat("Nova_Black",  new Color(0.10f, 0.11f, 0.13f)));
            Material glow   = WorldBuilder.Mat("Nova_Glow",       WorldBuilder.CyanGlow, true);
            Material rust   = Cap(WorldBuilder.Mat("Nova_Rust",   WorldBuilder.Rust));
            Material dark   = WorldBuilder.Mat("Nova_Dark",       new Color(0.05f, 0.06f, 0.08f), true);
            Material wireO  = WorldBuilder.Mat("Nova_WireOrange", new Color(1f, 0.5f, 0.15f), true);

            // ---- Piernas (dos tonos: muslo blanco, espinilla negra) cuelgan de la cadera
            box(hipL, "FootL", new Vector3(-0.095f, 0.05f, 0f), new Vector3(0.12f, 0.06f, 0.18f), black);
            box(hipR, "FootR", new Vector3(0.095f, 0.05f, 0f), new Vector3(0.12f, 0.06f, 0.18f), black);
            cyl(hipL, "ShinL", new Vector3(-0.095f, 0.25f, 0f), new Vector3(0.085f, 0.17f, 0.085f), black);
            cyl(hipR, "ShinR", new Vector3(0.095f, 0.25f, 0f), new Vector3(0.085f, 0.17f, 0.085f), black);
            sphere(hipL, "KneeL", new Vector3(-0.095f, 0.43f, 0f), 0.05f, black);
            sphere(hipR, "KneeR", new Vector3(0.095f, 0.43f, 0f), 0.05f, black);
            cyl(hipL, "ThighL", new Vector3(-0.095f, 0.51f, 0f), new Vector3(0.085f, 0.13f, 0.085f), white);
            cyl(hipR, "ThighR", new Vector3(0.095f, 0.51f, 0f), new Vector3(0.085f, 0.13f, 0.085f), white);

            // ---- Cadera + torso blancos, articulaciones negras
            box(body, "Hips", new Vector3(0f, 0.63f, 0f), new Vector3(0.28f, 0.12f, 0.20f), black);
            box(body, "Belly", new Vector3(0f, 0.77f, 0f), new Vector3(0.30f, 0.16f, 0.21f), white);
            box(body, "Torso", new Vector3(0f, 0.97f, 0f), new Vector3(0.32f, 0.30f, 0.23f), white);
            box(body, "Neck", new Vector3(0f, 1.17f, 0f), new Vector3(0.10f, 0.05f, 0.10f), black);
            box(body, "Collar", new Vector3(0f, 1.14f, 0f), new Vector3(0.34f, 0.06f, 0.25f), black);

            // ---- Núcleo del pecho + nombre «NOVA» grabado
            sphere(body, "ChestCore", new Vector3(0f, 0.88f, 0.13f), 0.06f, glow);
            Engrave(body.transform, "NOVA", new Vector3(0f, 1.02f, 0.14f), WorldBuilder.CyanGlow, 0.035f);

            // ---- Daños: placa rota + cableado expuesto
            var broken = WorldBuilder.Box("PlacaRota", new Vector3(0.12f, 0.85f, 0.12f), new Vector3(0.10f, 0.09f, 0.05f), dark, false);
            broken.transform.SetParent(body.transform, false);
            broken.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            var wireA = WorldBuilder.Cyl("CableA", new Vector3(0.14f, 0.76f, 0.12f), new Vector3(0.015f, 0.10f, 0.015f), wireO, false);
            wireA.transform.SetParent(body.transform, false);
            wireA.transform.localRotation = Quaternion.Euler(10f, 0f, 24f);
            var wireB = WorldBuilder.Cyl("CableB", new Vector3(0.09f, 0.73f, 0.115f), new Vector3(0.015f, 0.09f, 0.015f), glow, false);
            wireB.transform.SetParent(body.transform, false);
            wireB.transform.localRotation = Quaternion.Euler(0f, 20f, -18f);
            box(body, "ManchaOxido", new Vector3(-0.24f, 1.06f, -0.04f), new Vector3(0.09f, 0.06f, 0.04f), rust, false);
            box(body, "EspaldaRota", new Vector3(0f, 0.9f, -0.13f), new Vector3(0.10f, 0.08f, 0.03f), dark, false);

            // ---- Brazos (articulados, dos tonos) cuelgan del hombro
            box(shL, "ShL", new Vector3(-0.21f, 1.08f, 0f), new Vector3(0.12f, 0.10f, 0.17f), white);
            box(shR, "ShR", new Vector3(0.21f, 1.08f, 0f), new Vector3(0.12f, 0.10f, 0.17f), white);
            cyl(shL, "ArmL", new Vector3(-0.23f, 0.90f, 0f), new Vector3(0.07f, 0.17f, 0.07f), white);
            cyl(shR, "ArmR", new Vector3(0.23f, 0.90f, 0f), new Vector3(0.07f, 0.17f, 0.07f), white);
            sphere(shL, "ElbowL", new Vector3(-0.23f, 0.72f, 0f), 0.05f, black);
            sphere(shR, "ElbowR", new Vector3(0.23f, 0.72f, 0f), 0.05f, black);
            cyl(shL, "ForeL", new Vector3(-0.23f, 0.62f, 0f), new Vector3(0.06f, 0.15f, 0.06f), black);
            cyl(shR, "ForeR", new Vector3(0.23f, 0.62f, 0f), new Vector3(0.06f, 0.15f, 0.06f), black);
            sphere(shL, "HandL", new Vector3(-0.23f, 0.45f, 0f), 0.055f, black);
            sphere(shR, "HandR", new Vector3(0.23f, 0.45f, 0f), 0.055f, black);

            // ---- Cabeza estilizada con un solo ojo central azul
            box(body, "Head", new Vector3(0f, 1.30f, 0f), new Vector3(0.18f, 0.21f, 0.22f), white);
            box(body, "CaraFrontal", new Vector3(0f, 1.31f, 0.105f), new Vector3(0.14f, 0.15f, 0.04f), whiteD);
            box(body, "Mandibula", new Vector3(0f, 1.19f, 0.02f), new Vector3(0.12f, 0.05f, 0.12f), black);
            sphere(body, "OjoCentral", new Vector3(0f, 1.33f, 0.115f), 0.038f, glow);
            box(body, "AnilloOjo", new Vector3(0f, 1.33f, 0.10f), new Vector3(0.085f, 0.05f, 0.02f), dark, false);

            // Antena
            cyl(body, "Antena", new Vector3(0f, 1.43f, 0f), new Vector3(0.015f, 0.10f, 0.015f), black);
            sphere(body, "PuntaAntena", new Vector3(0f, 1.53f, 0f), 0.022f, glow);

            // ---- Marcha articulada
            var gait = root.AddComponent<CharacterGait>();
            gait.Setup(hipL.transform, hipR.transform, shL.transform, shR.transform, body.transform);

            StripColliders(root);
            return root;
        }

        // ------------------------------------------------------------------
        // KAEL — cyborg (protagonista secundario / villano): cabello oscuro,
        // mitad del rostro cibernética con brillo ámbar, abrigo largo de cuero
        // desgastado con correas/hebillas/bolsillos y guante metálico reforzado
        // en la mano derecha. Con holo=true se renderiza como holograma azul.
        // ------------------------------------------------------------------
        public static GameObject BuildKaelHologram(Vector3 at)
            => BuildKael(at, true);

        public static GameObject BuildKael(Vector3 at, bool holographic)
        {
            // Modelo importado (Resources/Models/kael) si está disponible
            // (tintado de azul si es el holograma del Nivel 2).
            var imported = TryBuildModel("kael",
                holographic ? new Color(0.25f, 0.75f, 1f) : KaelModelColor,
                holographic, holographic ? 0.45f : 1f);
            if (imported != null)
            {
                imported.name = "Kael";
                imported.transform.position = at;
                if (!holographic)
                    AttachHair(imported.transform.GetChild(0), "Kael_Hair", KaelHairColor, KaelHairLight,
                        shortHair: false, female: false);
                StripColliders(imported);
                return imported;
            }

            var root = new GameObject("Kael");
            root.transform.position = at;

            if (holographic)
            {
                var holo = WorldBuilder.Mat("Kael_Holo", new Color(0.25f, 0.75f, 1f), true, 0.40f);
                BuildKaelBody(root, holo, holo, holo, holo, holo, holo, holo, holo, holo, holo);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = holo;
                Transform eye = null;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Kael_Eye") { eye = t; break; }
                if (eye != null)
                {
                    var er = eye.GetComponent<Renderer>();
                    if (er != null)
                        er.sharedMaterial = WorldBuilder.Mat("Kael_EyeHolo", new Color(0.65f, 0.95f, 1f), true, 0.95f);
                }
                // Base del proyector
                cyl(root, "EmisorHolo", Vector3.zero + Vector3.up * 0.02f, new Vector3(0.28f, 0.04f, 0.28f), holo, false);
                StripColliders(root);
                return root;
            }

            Material leather = Cap(WorldBuilder.Mat("Kael_Leather", new Color(0.24f, 0.15f, 0.10f)));
            Material leatherD = Cap(WorldBuilder.Mat("Kael_LeatherDark", new Color(0.15f, 0.09f, 0.06f)));
            Material bone = Cap(WorldBuilder.Mat("Kael_Bone", new Color(0.32f, 0.28f, 0.24f)));
            Material skin = WorldBuilder.Mat("Kael_Skin", WorldBuilder.Skin);
            Material hair = WorldBuilder.Mat("Kael_Hair", new Color(0.10f, 0.09f, 0.10f));
            Material metal = Cap(WorldBuilder.Mat("Kael_Metal", new Color(0.45f, 0.47f, 0.51f)));
            Material metalD = Cap(WorldBuilder.Mat("Kael_MetalDark", new Color(0.24f, 0.25f, 0.29f)));
            Material amber = WorldBuilder.Mat("Kael_Amber", new Color(1f, 0.62f, 0.12f), true);
            Material gold = WorldBuilder.Mat("Kael_Buckle", new Color(0.85f, 0.72f, 0.35f), true);
            Material white = WorldBuilder.Mat("Kael_White", new Color(0.92f, 0.95f, 0.98f), true);

            BuildKaelBody(root, leather, leatherD, bone, skin, hair, metal, metalD, amber, gold, white);
            StripColliders(root);
            return root;
        }

        // Ensambla las piezas del cuerpo de Kael con los materiales dados.
        static void BuildKaelBody(GameObject root, Material leather, Material leatherD, Material bone, Material skin,
            Material hair, Material metal, Material metalD, Material amber, Material gold, Material white)
        {
            // ---- Botas y piernas
            box(root, "BootL", new Vector3(-0.11f, 0.08f, 0f), new Vector3(0.15f, 0.14f, 0.24f), leatherD);
            box(root, "BootR", new Vector3(0.11f, 0.08f, 0f), new Vector3(0.15f, 0.14f, 0.24f), leatherD);
            cyl(root, "LegL", new Vector3(-0.11f, 0.46f, 0f), new Vector3(0.12f, 0.28f, 0.12f), bone);
            cyl(root, "LegR", new Vector3(0.11f, 0.46f, 0f), new Vector3(0.12f, 0.28f, 0.12f), bone);

            // ---- Abrigo largo (faldón hasta media pierna)
            box(root, "FaldonL", new Vector3(-0.17f, 0.58f, 0f), new Vector3(0.13f, 0.30f, 0.22f), leather);
            box(root, "FaldonR", new Vector3(0.17f, 0.58f, 0f), new Vector3(0.13f, 0.30f, 0.22f), leather);
            box(root, "FaldonC", new Vector3(0f, 0.52f, 0f), new Vector3(0.28f, 0.20f, 0.22f), leather);
            box(root, "Hips", new Vector3(0f, 0.76f, 0f), new Vector3(0.34f, 0.10f, 0.24f), leather);

            // ---- Torso (abrigo de cuero)
            box(root, "Torso", new Vector3(0f, 0.96f, 0f), new Vector3(0.38f, 0.44f, 0.26f), leather);

            // Correas tácticas cruzadas con hebillas doradas
            var c1 = WorldBuilder.Box("Correa1", new Vector3(0f, 1.00f, 0.02f), new Vector3(0.44f, 0.05f, 0.025f), leatherD, false);
            c1.transform.SetParent(root.transform, false);
            c1.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            var c2 = WorldBuilder.Box("Correa2", new Vector3(0f, 1.00f, -0.02f), new Vector3(0.44f, 0.05f, 0.025f), leatherD, false);
            c2.transform.SetParent(root.transform, false);
            c2.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            box(root, "Hebilla1", new Vector3(0.20f, 1.09f, 0.04f), new Vector3(0.03f, 0.07f, 0.02f), gold, false);
            box(root, "Hebilla2", new Vector3(-0.20f, 0.89f, 0.04f), new Vector3(0.03f, 0.07f, 0.02f), gold, false);

            // Bolsillos tácticos
            box(root, "BolsilloL", new Vector3(-0.19f, 0.90f, 0.14f), new Vector3(0.09f, 0.10f, 0.05f), leatherD, false);
            box(root, "BolsilloR", new Vector3(0.19f, 0.90f, 0.14f), new Vector3(0.09f, 0.10f, 0.05f), leatherD, false);
            box(root, "TapaL", new Vector3(-0.19f, 0.95f, 0.16f), new Vector3(0.09f, 0.03f, 0.01f), bone, false);
            box(root, "TapaR", new Vector3(0.19f, 0.95f, 0.16f), new Vector3(0.09f, 0.03f, 0.01f), bone, false);

            // Cuello y hombros
            box(root, "Collar", new Vector3(0f, 1.18f, 0f), new Vector3(0.40f, 0.09f, 0.30f), leatherD);
            box(root, "HombroL", new Vector3(-0.28f, 1.14f, 0f), new Vector3(0.13f, 0.11f, 0.18f), leather);
            box(root, "HombroR", new Vector3(0.28f, 1.14f, 0f), new Vector3(0.13f, 0.11f, 0.18f), leather);

            // ---- Brazo izquierdo (cuero)
            cyl(root, "ArmL", new Vector3(-0.27f, 0.96f, 0f), new Vector3(0.085f, 0.22f, 0.085f), leather);
            cyl(root, "ForeL", new Vector3(-0.27f, 0.69f, 0f), new Vector3(0.075f, 0.16f, 0.075f), leatherD);
            sphere(root, "HandL", new Vector3(-0.27f, 0.51f, 0f), 0.06f, skin);

            // ---- Brazo derecho con guante metálico reforzado
            cyl(root, "ArmR", new Vector3(0.27f, 0.96f, 0f), new Vector3(0.085f, 0.22f, 0.085f), leather);
            var gauntlet = WorldBuilder.Cyl("GuanteMetal", new Vector3(0.27f, 0.66f, 0f), new Vector3(0.095f, 0.19f, 0.095f), metal);
            gauntlet.transform.SetParent(root.transform, false);
            box(root, "PulseraR", new Vector3(0.27f, 0.54f, 0f), new Vector3(0.11f, 0.06f, 0.12f), metalD);
            sphere(root, "HandR", new Vector3(0.27f, 0.47f, 0f), 0.065f, metal);
            box(root, "NudillosR", new Vector3(0.27f, 0.49f, 0.075f), new Vector3(0.08f, 0.035f, 0.03f), metalD, false);

            // ---- Cabeza (mitad humana, mitad cibernética)
            sphere(root, "Head", new Vector3(0f, 1.46f, 0f), 0.16f, skin);

            // Pelo oscuro despeinado
            sphere(root, "PeloTop", new Vector3(0f, 1.53f, -0.02f), 0.17f, hair);
            sphere(root, "PeloL", new Vector3(-0.16f, 1.48f, 0f), 0.075f, hair);
            sphere(root, "PeloR", new Vector3(0.16f, 1.48f, -0.05f), 0.08f, hair);
            sphere(root, "PeloNuca", new Vector3(0f, 1.40f, -0.16f), 0.12f, hair);
            var spike1 = WorldBuilder.Box("PeloPico1", new Vector3(-0.07f, 1.62f, 0f), new Vector3(0.03f, 0.09f, 0.03f), hair, false);
            spike1.transform.SetParent(root.transform, false);
            spike1.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            var spike2 = WorldBuilder.Box("PeloPico2", new Vector3(0.08f, 1.61f, -0.02f), new Vector3(0.03f, 0.08f, 0.03f), hair, false);
            spike2.transform.SetParent(root.transform, false);
            spike2.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);

            // Mitad derecha del rostro cibernética (implantes metálicos)
            box(root, "PlacaRostro", new Vector3(0.13f, 1.46f, 0f), new Vector3(0.10f, 0.28f, 0.24f), metal, false);
            box(root, "PlacaMandibula", new Vector3(0.13f, 1.33f, 0.02f), new Vector3(0.05f, 0.12f, 0.18f), metalD, false);
            box(root, "ImplanteMejilla", new Vector3(0.14f, 1.39f, 0.13f), new Vector3(0.02f, 0.05f, 0.04f), metalD, false);
            box(root, "AnilloCuello", new Vector3(0f, 1.27f, 0f), new Vector3(0.22f, 0.05f, 0.09f), metal, false);

            // Ojo cibernético ámbar (lado derecho) y ojo humano (izquierdo)
            var amberMark = WorldBuilder.Sphere("Kael_Eye", new Vector3(0.11f, 1.47f, 0.145f), 0.034f, amber);
            amberMark.transform.SetParent(root.transform, false);
            sphere(root, "EyeL", new Vector3(-0.06f, 1.475f, 0.145f), 0.026f, white);
            box(root, "CejaR", new Vector3(0.13f, 1.53f, 0.11f), new Vector3(0.08f, 0.025f, 0.03f), metalD, false);
        }

        /// <summary>Grabado de texto en 3D (etiqueta «NOVA» del pecho).</summary>
        static void Engrave(Transform parent, string text, Vector3 localPos, Color color, float characterSize)
        {
            var go = new GameObject("Etiqueta_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = characterSize;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (tm.font == null)
                tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}