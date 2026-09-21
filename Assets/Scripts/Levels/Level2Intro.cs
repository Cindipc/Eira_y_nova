using System.Collections;
using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Cinemática de inicio del Capítulo 2 — «La sangre».
    /// Eira descubre sus poderes: una gota de su sangre despierta una máquina
    /// antigua del PROYECTO EIRA, que la reconoce como «PORTADORA DE LA LLAVE».
    /// </summary>
    public class Level2Intro : MonoBehaviour
    {
        Transform _player;
        PlayerAbilities _ab;
        Material _sigilMat;
        Material _dropMat;

        public void Begin(Transform player)
        {
            _player = player;
            _ab = player != null ? player.GetComponent<PlayerAbilities>() : null;
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            var gd = GameDirector.Instance;
            if (gd == null) yield break;

            gd.IntroActive = true;
            var pc = PlayerController.Instance;
            if (pc != null) pc.SetControlLocked(true);

            var machinePos = new Vector3(58f, 0f, 5f);
            BuildMachine(machinePos);

            gd.SetObjective("Un brillo entre los escombros...");
            yield return new WaitForSeconds(0.7f);

            NovaSay("Salimos del laboratorio... pero esta ciudad ya no es humana. Espera... esa máquina.", 4.5f);
            yield return new WaitForSeconds(4.5f);

            NovaSay("Es una consola del Proyecto. Kael lleva décadas intentando despertarla.", 4.5f);
            yield return new WaitForSeconds(4.5f);

            // Alarma: Kael detecta la anomalía
            gd.ShowSideNote("¡SEÑAL BIO-GENÉTICA DETECTADA!");
            gd.ShowDialogue("EIRA", "¡Mi corazón...!", 2.2f);
            gd.FlashDamage(0.6f);
            SpawnWatcher();
            yield return new WaitForSeconds(1.6f);

            // La cortadura y la gota de sangre
            gd.SetBigText("CORTADURA", "Una gota de tu sangre cae sobre la máquina...", 2.6f);
            gd.FlashDamage(0.85f);
            StartCoroutine(DropBlood(machinePos + Vector3.up * 1.15f));
            yield return new WaitForSeconds(2.8f);

            // La máquina reconoce la sangre
            if (_sigilMat != null) _sigilMat.color = WorldBuilder.CyanGlow;
            gd.SetBigText("DNA COMPATIBLE", "PORTADORA DE LA LLAVE: EIRA", 3.2f);
            if (_ab != null) _ab.GrantBloodKey();
            gd.AddPoints(200, "La llave de sangre");
            yield return new WaitForSeconds(3.4f);

            NovaSay("Tu sangre... la máquina te reconoce. Eres la llave del Núcleo Central, Eira.", 5f);
            yield return new WaitForSeconds(5f);

            NovaSay("Tus antepasados participaron en el PROYECTO EIRA. Por eso tu corazón... y por eso te buscan.", 5.5f);
            yield return new WaitForSeconds(5.5f);

            gd.ShowDialogue("EIRA", "Entonces... mi corazón nunca fue una maldición.", 3.5f);
            yield return new WaitForSeconds(3.5f);

            // El poder despierta: las máquinas cercanas responden
            gd.SetBigText("LA LLAVE DESPIERTA", "Las máquinas te obedecen", 2.6f);
            PowerPulse(machinePos);
            yield return new WaitForSeconds(2.8f);

            if (_ab != null) _ab.RefillEnergy();
            gd.SetObjective("Aprende a usar tus habilidades. Los soldados de Kael ya vienen... ¡llega al refugio rebelde!");
            NovaSay("¡Vienen por tu señal! Corre, el refugio rebelde está al este.", 4f);
            gd.SetCheckpoint(new Vector3(60f, 0.6f, 0f));

            gd.IntroActive = false;
            if (pc != null) pc.SetControlLocked(false);
        }

        void NovaSay(string text, float time)
        {
            if (NovaCompanion.Instance != null)
                NovaCompanion.Instance.Say(text, time);
            else
                GameDirector.Instance.ShowDialogue("NOVA", text, time);
        }

        // ------------------------------------------------------------------
        // Escenografía de la cinemática
        // ------------------------------------------------------------------
        void BuildMachine(Vector3 p)
        {
            var root = new GameObject("Maquina_Antigua");
            root.transform.position = p;
            root.transform.SetParent(transform, false);

            Material metal = WorldBuilder.Mat("Ant_Metal", new Color(0.20f, 0.21f, 0.24f));
            Material dark = WorldBuilder.Mat("Ant_Dark", new Color(0.08f, 0.09f, 0.11f), true);
            _sigilMat = WorldBuilder.Mat("Ant_Sigil", new Color(0.30f, 0.34f, 0.40f), true);
            _dropMat = WorldBuilder.Mat("Ant_Blood", new Color(0.70f, 0.05f, 0.05f), true);

            var pedestal = WorldBuilder.Cyl("Pedestal", p + Vector3.up * 0.45f, new Vector3(0.7f, 0.45f, 0.7f), metal);
            pedestal.transform.SetParent(root.transform, false);
            var disc = WorldBuilder.Cyl("Disco", p + Vector3.up * 0.95f, new Vector3(0.75f, 0.05f, 0.75f), _sigilMat);
            disc.transform.SetParent(root.transform, false);
            var core = WorldBuilder.Sphere("Nucleo", p + Vector3.up * 1.1f, 0.16f, dark);
            core.transform.SetParent(root.transform, false);

            WorldBuilder.PointLight("LuzMaquina", p + Vector3.up * 1.3f, new Color(0.2f, 0.6f, 0.8f), 7f, 1.6f)
                .transform.SetParent(root.transform, false);
        }

        IEnumerator DropBlood(Vector3 target)
        {
            var drop = WorldBuilder.Sphere("GotaDeSangre", target + Vector3.up * 1.4f, 0.08f, _dropMat, false);
            drop.transform.SetParent(transform, false);
            Vector3 start = drop.transform.position;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.2f;
                drop.transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            yield return new WaitForSeconds(0.15f);
            Destroy(drop);
        }

        void SpawnWatcher()
        {
            var go = WorldBuilder.Sphere("Dron_Watcher", new Vector3(46f, 6f, -6f), 0.35f,
                WorldBuilder.Mat("Watcher", WorldBuilder.RedGlow, true), false);
            go.transform.SetParent(transform, false);
            go.AddComponent<WatcherFlyby>();
        }

        void PowerPulse(Vector3 center)
        {
            var wave = WorldBuilder.Cyl("Pulso", center + Vector3.up * 0.1f, new Vector3(0.5f, 0.05f, 0.5f),
                WorldBuilder.Mat("Pulso", WorldBuilder.CyanGlow, true, 0.7f), false);
            wave.transform.SetParent(transform, false);
            wave.AddComponent<PulseExpand>();
        }
    }

    /// <summary>Dron de vigilancia que cruza la pantalla y desaparece.</summary>
    public class WatcherFlyby : MonoBehaviour
    {
        float _t;
        void Update()
        {
            _t += Time.deltaTime;
            transform.position += new Vector3(11f, 0f, 2f) * Time.deltaTime;
            if (_t > 3.2f) Destroy(gameObject);
        }
    }

    /// <summary>Onda de energía que crece desde la máquina.</summary>
    public class PulseExpand : MonoBehaviour
    {
        float _t;
        void Update()
        {
            _t += Time.deltaTime;
            float s = 1f + _t * 14f;
            transform.localScale = new Vector3(s, 0.05f, s);
            var r = GetComponent<Renderer>();
            if (r != null)
            {
                var c = r.material.color;
                c.a = Mathf.Clamp01(0.7f - _t * 0.8f);
                r.material.color = c;
            }
            if (_t > 1f) Destroy(gameObject);
        }
    }
}
