using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EiraNova
{
    /// <summary>Controlador central: vidas, puntos, objetivos, HUD, diálogos y flujo de misión.</summary>
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        public int MaxLives = 3;
        public int Lives { get; private set; } = 3;
        public int Points { get; private set; }

        public PlayerController Player;
        public PlayerAbilities Abilities;
        public NovaCompanion Nova;

        public bool Completed = false;
        public bool Defeated = false;

        /// <summary>Durante la cinemática del Nivel 2 (descubrimiento del poder) el control está bloqueado.</summary>
        public bool IntroActive = false;

        // HUD
        Text _pointsTxt, _objTxt, _promptTxt, _dialNameTxt, _dialTxt, _bigTitle, _bigSub;
        Image _heartBar, _vignette, _black, _bossBar, _bossBarFill;
        readonly Text[] _hearts = new Text[3];
        GameObject _dialPanel, _bossPanel, _bigPanel;
        float _dialTimer;

        Font _font;
        Vector3 _checkpoint;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            IntroActive = false;
            Lives = MaxLives;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        void Start()
        {
            if (Instance != this) return;
            _checkpoint = new Vector3(0f, 0.5f, 0f);
            CleanupRuntime();
            StartCoroutine(Startup());
        }

        /// <summary>
        /// Elimina objetos construidos en tiempo de ejecución de sesiones anteriores.
        /// Fundamental si "Enter Play Mode Options > Reload Domain/Scene" está desactivado,
        /// porque esos objetos sobreviven entre sesiones y duplicaban el nivel (pantalla negra o sin nivel).
        /// </summary>
        void CleanupRuntime()
        {
            foreach (var name in new string[]
            {
                "HUD", "Eira_Root", "NOVA_Root", "Main Camera", "Sol_Atmosfera",
                "Level1_El_Despertar", "Level1_El_Despertar_Ciudad", "Level1_El_Despertar_Lab",
                "Level2_La_Caza_Base"
            })
            {
                var found = GameObject.Find(name);
                if (found != null) Destroy(found);
            }
            var bootstrap = GameObject.Find("GameDirector_Bootstrap");
            if (bootstrap != null && bootstrap != gameObject)
                Destroy(bootstrap);
        }

        // -------------------------------------------------------------
        // ARRANQUE
        // -------------------------------------------------------------
        IEnumerator Startup()
        {
            SetupSceneFeel();
            EnsureCamera();
            BuildHUD();
            BuildPlayer();
            BuildNova();

            // Secuencia de despertar (Capítulo 1: El Despertar)
            yield return FadeBlack(0.05f, 1f);
            SetBigText("AÑO 3000", "", 2f);
            yield return new WaitForSeconds(2.2f);
            yield return FadeBlack(1f, 0f);

            ShowDialogue("EIRA", "¿Qué...? ¿Dónde estoy? Mi corazón... vuelve a latir.", 3.8f);
            yield return new WaitForSeconds(2.6f);
            ShowDialogue("NOVA", "No voy a hacerte daño.", 2.6f);
            yield return new WaitForSeconds(2f);
            ShowDialogue("EIRA", "¿Puedes... hablar?", 2.2f);
            yield return new WaitForSeconds(1.8f);
            ShowDialogue("NOVA", "Sí. Pero tú eres la anomalía.", 3f);
            yield return new WaitForSeconds(2.2f);
            ShowDialogue("NOVA", "Los humanos desaparecieron hace décadas. Bienvenida al año 3000.", 4.2f);
            yield return new WaitForSeconds(3.4f);
            SetObjective("Encuentra a NOVA y averigua qué ha pasado en la ciudad.");

            // Nivel 1 — Fase inicial: la ciudad escaneada (NOVA → KAEL → puerta del laboratorio)
            var levelGo = new GameObject("Level1_El_Despertar_Ciudad");
            var city = levelGo.AddComponent<Level1CityApproach>();
            city.Build(Player.transform);
        }

        void SetupSceneFeel()
        {
            // Entorno "limpio": sin niebla para que los escaneos (splats) se vean nítidos.
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.50f);

            var sun = new GameObject("Sol_Atmosfera");
            var dl = sun.AddComponent<Light>();
            dl.type = LightType.Directional;
            dl.color = new Color(0.85f, 0.90f, 1.0f);
            dl.intensity = 0.9f;
            sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
        }

        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraRig>();
        }

        void BuildPlayer()
        {
            var playerRoot = new GameObject("Eira_Root");
            var cc = playerRoot.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.8f, 0f);
            cc.stepOffset = 0.3f;

            Player = playerRoot.AddComponent<PlayerController>();
            Abilities = playerRoot.AddComponent<PlayerAbilities>();

            var visual = ProceduralCharacters.BuildEira(Vector3.zero);
            visual.transform.SetParent(playerRoot.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            playerRoot.transform.position = _checkpoint;
            var cam = Camera.main;
            if (cam != null)
            {
                var rig = cam.GetComponent<CameraRig>();
                if (rig != null) rig.target = playerRoot.transform;
            }
        }

        void BuildNova()
        {
            var novaRoot = new GameObject("NOVA_Root");
            Nova = novaRoot.AddComponent<NovaCompanion>();
            var visual = ProceduralCharacters.BuildNova(Vector3.zero);
            visual.transform.SetParent(novaRoot.transform, false);
        }

        // -------------------------------------------------------------
        // HUD
        // -------------------------------------------------------------
        void BuildHUD()
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // ---- Vidas (corazones)
            for (int i = 0; i < MaxLives; i++)
            {
                _hearts[i] = MakeText("Heart" + i, canvas.transform, new Color(1f, 0.25f, 0.25f), 34, FontStyle.Bold);
                var rt = _hearts[i].rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(28f + i * 40f, -22f);
                _hearts[i].text = "\u2665";
            }

            // ---- Barra de energía del corazón
            var barBg = MakeImage("HeartBarBG", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
            var barRt = barBg.rectTransform;
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.anchoredPosition = new Vector2(28f, -58f);
            barRt.sizeDelta = new Vector2(160f, 12f);
            _heartBar = MakeImage("HeartBarFill", barBg.transform, new Color(0.95f, 0.30f, 0.30f));
            _heartBar.fillCenter = true;
            var hbRt = _heartBar.rectTransform;
            hbRt.anchorMin = Vector2.zero;
            hbRt.anchorMax = Vector2.one;
            hbRt.offsetMin = new Vector2(2f, 2f);
            hbRt.offsetMax = new Vector2(-2f, -2f);
            hbRt.pivot = new Vector2(0f, 0.5f);
            _heartBar.type = Image.Type.Filled;
            _heartBar.fillMethod = Image.FillMethod.Horizontal;
            _heartBar.fillAmount = 1f;
            var heartLabel = MakeText("HeartTag", barBg.transform, new Color(1f, 0.7f, 0.7f), 12, FontStyle.Bold);
            heartLabel.text = "CORAZÓN";
            var hrt = heartLabel.rectTransform;
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.zero;
            hrt.pivot = new Vector2(0f, 1f);
            hrt.anchoredPosition = new Vector2(2f, 4f);
            hrt.sizeDelta = new Vector2(160f, 16f);
            heartLabel.alignment = TextAnchor.MiddleLeft;

            // ---- Puntos
            var ptsLabel = MakeText("PuntosTag", canvas.transform, new Color(0.8f, 0.85f, 0.9f), 14, FontStyle.Bold);
            ptsLabel.text = "PUNTOS";
            var plrt = ptsLabel.rectTransform;
            plrt.anchorMin = new Vector2(1f, 1f); plrt.anchorMax = new Vector2(1f, 1f);
            plrt.pivot = new Vector2(1f, 1f); plrt.anchoredPosition = new Vector2(-28f, -22f);

            _pointsTxt = MakeText("PuntosVal", canvas.transform, WorldBuilder.CyanGlow, 30, FontStyle.Bold);
            _pointsTxt.text = "0";
            _pointsTxt.alignment = TextAnchor.MiddleRight;
            var prt = _pointsTxt.rectTransform;
            prt.anchorMin = new Vector2(1f, 1f); prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 1f); prt.anchoredPosition = new Vector2(-28f, -44f);
            prt.sizeDelta = new Vector2(220f, 34f);

            // ---- Objetivo
            var objPanel = MakeImage("ObjPanel", canvas.transform, new Color(0f, 0f, 0f, 0.5f));
            var opr = objPanel.rectTransform;
            opr.anchorMin = new Vector2(0f, 0f); opr.anchorMax = new Vector2(0f, 0f);
            opr.pivot = new Vector2(0f, 0f); opr.anchoredPosition = new Vector2(28f, 24f);
            opr.sizeDelta = new Vector2(760f, 52f);
            _objTxt = MakeText("ObjText", objPanel.transform, new Color(1f, 1f, 1f), 18, FontStyle.Bold);
            _objTxt.alignment = TextAnchor.MiddleLeft;
            var obrt = _objTxt.rectTransform;
            obrt.anchorMin = Vector2.zero; obrt.anchorMax = Vector2.one;
            obrt.offsetMin = new Vector2(18f, 6f); obrt.offsetMax = new Vector2(-10f, -6f);
            SetObjective("Explora el laboratorio.");

            // ---- Prompt
            _promptTxt = MakeText("Prompt", canvas.transform, new Color(1f, 0.92f, 0.5f), 24, FontStyle.Bold);
            _promptTxt.text = "";
            _promptTxt.alignment = TextAnchor.MiddleCenter;
            var prt2 = _promptTxt.rectTransform;
            prt2.anchorMin = new Vector2(0.5f, 0f); prt2.anchorMax = new Vector2(0.5f, 0f);
            prt2.pivot = new Vector2(0.5f, 0.5f); prt2.anchoredPosition = new Vector2(0f, 130f);
            prt2.sizeDelta = new Vector2(900f, 40f);

            // ---- Diálogo
            _dialPanel = new GameObject("DialPanel");
            _dialPanel.transform.SetParent(canvas.transform, false);
            var dp = _dialPanel.AddComponent<Image>();
            dp.color = new Color(0f, 0f, 0f, 0.55f);
            var dpRt = _dialPanel.GetComponent<RectTransform>();
            dpRt.anchorMin = new Vector2(0.5f, 0f); dpRt.anchorMax = new Vector2(0.5f, 0f);
            dpRt.pivot = new Vector2(0.5f, 0f); dpRt.anchoredPosition = new Vector2(0f, 40f);
            dpRt.sizeDelta = new Vector2(1400f, 92f);
            dpRt.localScale = Vector3.zero;

            _dialNameTxt = MakeText("DialName", _dialPanel.transform, WorldBuilder.CyanGlow, 22, FontStyle.Bold);
            var dntRt = _dialNameTxt.rectTransform;
            dntRt.anchorMin = new Vector2(0f, 1f); dntRt.anchorMax = new Vector2(0f, 1f);
            dntRt.pivot = new Vector2(0f, 1f); dntRt.anchoredPosition = new Vector2(18f, -10f);
            dntRt.sizeDelta = new Vector2(500f, 26f);

            _dialTxt = MakeText("DialText", _dialPanel.transform, new Color(1f, 1f, 1f), 20);
            _dialTxt.alignment = TextAnchor.UpperLeft;
            var dtRt = _dialTxt.rectTransform;
            dtRt.anchorMin = Vector2.zero; dtRt.anchorMax = Vector2.one;
            dtRt.offsetMin = new Vector2(18f, 14f); dtRt.offsetMax = new Vector2(-18f, -34f);

            // ---- Jefe (barra de vida)
            _bossPanel = new GameObject("BossPanel");
            _bossPanel.transform.SetParent(canvas.transform, false);
            var bossBg = _bossPanel.AddComponent<Image>();
            bossBg.color = new Color(0f, 0f, 0f, 0.5f);
            var bRt = _bossPanel.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 1f); bRt.anchorMax = new Vector2(0.5f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f); bRt.anchoredPosition = new Vector2(0f, -22f);
            bRt.sizeDelta = new Vector2(900f, 26f);
            _bossBarFill = MakeImage("BossFill", _bossPanel.transform, new Color(1f, 0.25f, 0.2f));
            var bfRt = _bossBarFill.rectTransform;
            bfRt.anchorMin = Vector2.zero; bfRt.anchorMax = Vector2.one;
            bfRt.offsetMin = new Vector2(2f, 2f); bfRt.offsetMax = new Vector2(-2f, -2f);
            bfRt.pivot = new Vector2(0f, 0.5f);
            _bossBarFill.type = Image.Type.Filled;
            _bossBarFill.fillMethod = Image.FillMethod.Horizontal;
            _bossBarFill.fillAmount = 1f;
            _bossPanel.SetActive(false);

            // ---- Viñeta de daño
            _vignette = MakeImage("Vignette", canvas.transform, new Color(1f, 0f, 0f, 0f));
            var vRt = _vignette.rectTransform;
            vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one;
            vRt.offsetMin = Vector2.zero; vRt.offsetMax = Vector2.zero;

            // ---- Pantalla negra
            _black = MakeImage("Black", canvas.transform, Color.black);
            var bRt2 = _black.rectTransform;
            bRt2.anchorMin = Vector2.zero; bRt2.anchorMax = Vector2.one;
            bRt2.offsetMin = Vector2.zero; bRt2.offsetMax = Vector2.zero;

            // ---- Mensajes grandes
            _bigPanel = new GameObject("BigPanel");
            _bigPanel.transform.SetParent(canvas.transform, false);
            _bigTitle = MakeText("BigTitle", _bigPanel.transform, Color.white, 64, FontStyle.Bold);
            _bigTitle.alignment = TextAnchor.MiddleCenter;
            _bigSub = MakeText("BigSub", _bigPanel.transform, new Color(1f, 0.9f, 0.6f), 26);
            _bigSub.alignment = TextAnchor.MiddleCenter;
            var btRt = _bigTitle.rectTransform;
            btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
            var bsRt = _bigSub.rectTransform;
            bsRt.anchorMin = new Vector2(0f, 0.4f); bsRt.anchorMax = new Vector2(1f, 0.4f);
            bsRt.anchoredPosition = Vector2.zero; bsRt.sizeDelta = new Vector2(0f, 50f);
            _bigPanel.SetActive(false);
        }

        Text MakeText(string name, Transform parent, Color color, int size, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.supportRichText = true;
            return t;
        }

        Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var i = go.AddComponent<Image>();
            i.color = color;
            i.raycastTarget = false;
            return i;
        }

        // -------------------------------------------------------------
        // FLUJO GENERAL
        // -------------------------------------------------------------
        public void SetObjective(string text)
        {
            if (_objTxt != null) _objTxt.text = "OBJETIVO: " + text;
        }

        public void AddPoints(int amount, string why = "")
        {
            Points += amount;
            if (_pointsTxt != null) _pointsTxt.text = Points.ToString();
            if (!string.IsNullOrEmpty(why))
                ShowSideNote("+" + amount + "  " + why);
        }

        public void SetPrompt(string text)
        {
            if (_promptTxt != null) _promptTxt.text = text;
        }

        public void ShowDialogue(string speaker, string text, float seconds = 3f)
        {
            if (_dialNameTxt != null) _dialNameTxt.text = speaker;
            if (_dialTxt != null) _dialTxt.text = text;
            if (_dialPanel != null)
            {
                var s = _dialPanel.transform.localScale;
                _dialPanel.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            _dialTimer = seconds;
            StopCoroutine("AutoHideDialogue");
            StartCoroutine("AutoHideDialogue");
        }

        IEnumerator AutoHideDialogue()
        {
            while (_dialTimer > 0f)
            {
                _dialTimer -= Time.deltaTime;
                yield return null;
            }
            if (_dialPanel != null) _dialPanel.transform.localScale = Vector3.zero;
        }

        public void ShowSideNote(string text)
        {
            if (_promptTxt != null)
                _promptTxt.text = text;
        }

        public void SetBigText(string title, string sub, float seconds)
        {
            _bigTitle.text = title;
            _bigSub.text = sub;
            _bigPanel.SetActive(true);
            StopCoroutine("HideBig");
            StartCoroutine(HideBig(seconds));
        }

        IEnumerator HideBig(float seconds)
        {
            if (seconds <= 0f) yield break;
            yield return new WaitForSeconds(seconds);
            _bigPanel.SetActive(false);
        }

        public void FlashDamage(float strength = 0.5f)
        {
            StopCoroutine("FadeVignette");
            StartCoroutine(FadeVignette(strength));
        }

        IEnumerator FadeVignette(float strength)
        {
            _vignette.color = new Color(1f, 0f, 0f, strength);
            while (_vignette.color.a > 0.02f)
            {
                _vignette.color = new Color(1f, 0f, 0f, Mathf.Lerp(_vignette.color.a, 0f, Time.deltaTime * 4f));
                yield return null;
            }
            _vignette.color = new Color(1f, 0f, 0f, 0f);
        }

        IEnumerator FadeBlack(float from, float to)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                var c = _black.color;
                c.a = Mathf.Lerp(from, to, t);
                _black.color = c;
                yield return null;
            }
            var c2 = _black.color;
            c2.a = to;
            _black.color = c2;
        }

        // -------------------------------------------------------------
        // VIDAS Y RECURSOS
        // -------------------------------------------------------------
        public void UpdateHeartBar(float amount01)
        {
            if (_heartBar != null) _heartBar.fillAmount = Mathf.Clamp01(amount01);
        }

        public void RefillHeartsUI()
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                if (_hearts[i] != null)
                    _hearts[i].color = i < Lives ? new Color(1f, 0.25f, 0.25f) : new Color(0.25f, 0.25f, 0.25f, 0.4f);
            }
        }

        public void GainLife()
        {
            Lives = Mathf.Min(Lives + 1, MaxLives);
            RefillHeartsUI();
        }

        public void HeartFailure()
        {
            LoseLife("Su corazón falló por sobrecarga");
        }

        public void LoseLife(string cause)
        {
            if (Defeated || Completed) return;
            if (Player != null) Player.FlashHit();
            Lives--;
            RefillHeartsUI();
            if (Lives <= 0)
            {
                Defeated = true;
                SetBigText("HAS CAÍDO", "Presiona R para reiniciar el nivel", 0f);
                _bigPanel.SetActive(true);
                _bigSub.text = "Presiona R para reiniciar el nivel";
                if (Nova != null) Nova.Say("¡Eira! ...Eira, despierta.", null);
            }
            else
            {
                ShowSideNote("-1 VIDA · " + cause);
                Respawn();
            }
        }

        public void Respawn()
        {
            if (Player != null)
                Player.Teleport(_checkpoint);
            if (Abilities != null)
                Abilities.RefillEnergy();
        }

        public void SetCheckpoint(Vector3 pos)
        {
            _checkpoint = pos;
        }

        public void ShowBossBar(string name, float hp01, bool visible)
        {
            if (_bossPanel == null) return;
            _bossPanel.SetActive(visible);
            if (visible && _bossBarFill != null)
                _bossBarFill.fillAmount = Mathf.Clamp01(hp01);
        }

        public void SetBossHp(float hp01)
        {
            if (_bossBarFill != null) _bossBarFill.fillAmount = Mathf.Clamp01(hp01);
        }

        // -------------------------------------------------------------
        // FIN DE NIVEL 1 → ESCAPE / FIN DE LA DEMO
        // -------------------------------------------------------------
        /// <summary>
        /// Transición de la ciudad escaneada al laboratorio: la puerta abre el
        /// laboratorio y comienza la fase final del Nivel 1 (escapar junto a NOVA).
        /// </summary>
        public void BeginLabEscape()
        {
            StartCoroutine(LabEscapeRoutine());
        }

        IEnumerator LabEscapeRoutine()
        {
            SetObjective("Entrando al laboratorio...");
            yield return FadeBlack(0f, 1f);
            SetBigText("EL LABORATORIO", "Escapa junto a NOVA", 2.5f);
            yield return new WaitForSeconds(2.4f);

            var oldCity = GameObject.Find("Level1_El_Despertar_Ciudad");
            if (oldCity != null) Destroy(oldCity);

            var labGo = new GameObject("Level1_El_Despertar_Lab");
            var lab = labGo.AddComponent<Level1Lab>();
            lab.Build(Player.transform);

            yield return new WaitForSeconds(0.4f);
            yield return FadeBlack(1f, 0f);
        }

        public void Level1Complete()
        {
            if (Completed) return;
            Completed = true;
            AddPoints(500, "Nivel 1 — El Despertar superado");
            if (Nova != null) Nova.Say("Escapamos, Eira. Esto apenas comienza.", null);
            StartCoroutine(FinishLevel1Routine());
        }

        IEnumerator FinishLevel1Routine()
        {
            SetObjective("Nivel 1 superado...");
            yield return FadeBlack(0f, 1f);
            SetBigText("NIVEL 1 COMPLETO", "Has escapado del laboratorio con NOVA", 3f);
            yield return new WaitForSeconds(3f);
            EndDemo();
        }

        public void CityComplete()
        {
            EndDemo();
        }

        public void EndDemo()
        {
            Defeated = true;
            _bigPanel.SetActive(true);
            _bigTitle.text = "FIN DE LA DEMO";
            _bigSub.text = "Nivel 1 completo: El Despertar. Presiona R para reiniciar.";
        }

        void Update()
        {
            if (Keyboard_Available())
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                if (kb.fKey.wasPressedThisFrame)
                {
                    TeleportToAndroid();
                }
                if (kb.gKey.wasPressedThisFrame)
                {
                    JumpToLabCombat();
                }
                if (kb.hKey.wasPressedThisFrame)
                {
                    TriggerCityAttack();
                }
                if (kb.tabKey.wasPressedThisFrame && _androidTargets != null && _androidTargets.Count > 0)
                {
                    _androidIndex = (_androidIndex + 1) % _androidTargets.Count;
                    ApplyAndroidTarget(_androidIndex);
                }
            }
        }

        // ------------------------------------------------------------
        // TELEPORT A ANDROIDES (test): F salta junto al siguiente androide,
        // Tab cambia de personaje entre todos los presentes en la escena.
        // ------------------------------------------------------------
        List<Transform> _androidTargets;
        int _androidIndex;

        void TeleportToAndroid()
        {
            _androidTargets = new List<Transform>();
            var nova = FindObjectsOfType<NovaCompanion>();
            foreach (var n in nova) if (n != null) _androidTargets.Add(n.transform);
            var drones = FindObjectsOfType<EnemyDrone>();
            foreach (var d in drones) if (d != null) _androidTargets.Add(d.transform);
            var soldiers = FindObjectsOfType<AndroidSoldier>();
            foreach (var s in soldiers) if (s != null) _androidTargets.Add(s.transform);
            var bosses = FindObjectsOfType<GuardianBoss>();
            foreach (var g in bosses) if (g != null) _androidTargets.Add(g.transform);

            if (_androidTargets.Count == 0)
            {
                SetPrompt("No hay androides en esta escena.");
                return;
            }
            _androidIndex = 0;
            ApplyAndroidTarget(0);
        }

        void ApplyAndroidTarget(int index)
        {
            if (_androidTargets == null || index < 0 || index >= _androidTargets.Count) return;
            var t = _androidTargets[index];
            if (t == null || Player == null) return;

            Vector3 pos = t.position + t.forward * 2.2f + Vector3.up * 0.2f;
            Player.Teleport(pos);
            SetPrompt("TELEPORT: " + t.name + "  (" + (index + 1) + "/" + _androidTargets.Count + ")  [Tab] siguiente");
        }

        // ------------------------------------------------------------
        // SALTOS DE TEST: G salta directo al combate del laboratorio (Nivel 1),
        // H libera a los escoltas/androides en la ciudad (sin pasar por KAEL).
        // ------------------------------------------------------------
        void JumpToLabCombat()
        {
            var gd = this;
            if (gd == null) return;
            gd.BeginLabEscape();
            gd.SetPrompt("G: salto directo al laboratorio (combate)  [F] teleport  [Tab] siguiente");
        }

        void TriggerCityAttack()
        {
            var city = FindObjectOfType<Level1CityApproach>();
            if (city == null)
            {
                SetPrompt("No hay ciudad cargada; usa G para ir al laboratorio.");
                return;
            }
            city.OnKaelTriggered();
            SetPrompt("H: androides de la ciudad liberados  [F] teleport  [Tab] siguiente");
        }

        bool Keyboard_Available()
        {
            try { return UnityEngine.InputSystem.Keyboard.current != null; }
            catch { return false; }
        }
    }
}