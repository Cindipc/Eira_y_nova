using System.Collections.Generic;
using UnityEngine;

namespace EiraNova
{
    /// <summary>Componente que rota un objeto lentamente (hélice/rotor, objetos decorativos).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = new Vector3(0f, 1f, 0f);
        public float speed = 120f;
        void Update() { transform.Rotate(axis, speed * Time.deltaTime, Space.Self); }
    }

    /// <summary>Flota y rota suavemente (recogibles).</summary>
    public class SpinBob : MonoBehaviour
    {
        Vector3 _base;
        public float bobSpeed = 2f;
        public float bobHeight = 0.15f;
        void Start() { _base = transform.position; }
        void Update()
        {
            transform.position = _base + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
        }
    }

    /// <summary>Visuales de los enemigos construidos con primitivas.</summary>
    public static class EnemyVisuals
    {
        static Material Metal() => MatCap(new Color(0.45f, 0.47f, 0.51f));
        static Material MetalDark() => MatCap(new Color(0.25f, 0.26f, 0.30f));
        static Material MatCap(Color c)
        {
            var m = WorldBuilder.Mat("EV_Metal", c);
            m.SetFloat("_Smoothness", 0.4f);
            return m;
        }

        // ------------------------------------------------------------------
        // DRON volador
        // ------------------------------------------------------------------
        public static GameObject BuildDrone(out Material eyeMat)
        {
            var root = new GameObject("Dron");
            eyeMat = WorldBuilder.Mat("Dron_Eye", WorldBuilder.CyanGlow, true);

            var body = WorldBuilder.Cap("Body", Vector3.zero, new Vector3(0.55f, 0.35f, 0.55f), Metal());
            body.transform.SetParent(root.transform, false);

            var rim = WorldBuilder.Cyl("Rim", Vector3.zero + Vector3.down * 0.02f, new Vector3(0.9f, 0.06f, 0.9f), MetalDark());
            rim.transform.SetParent(root.transform, false);

            // Rotor
            var rotor = new GameObject("Rotor");
            rotor.transform.SetParent(root.transform, false);
            var blade1 = WorldBuilder.Box("Blade1", Vector3.zero + Vector3.up * 0.28f, new Vector3(1.2f, 0.03f, 0.18f), Metal(), false);
            blade1.transform.SetParent(rotor.transform, false);
            var blade2 = WorldBuilder.Box("Blade2", Vector3.zero + Vector3.up * 0.28f, new Vector3(0.18f, 0.03f, 1.2f), Metal(), false);
            blade2.transform.SetParent(rotor.transform, false);
            rotor.AddComponent<Spinner>().speed = 320f;

            // Ojo (hacia delante, +Z)
            var eye = WorldBuilder.Sphere("Eye", Vector3.zero + Vector3.forward * 0.3f, 0.09f, eyeMat);
            eye.transform.SetParent(root.transform, false);

            return root;
        }

        // ------------------------------------------------------------------
        // SOLDADO androide
        // ------------------------------------------------------------------
        public static GameObject BuildSoldier(out Material eyeMat)
        {
            var root = new GameObject("Soldado");
            eyeMat = WorldBuilder.Mat("Sold_Eye", WorldBuilder.CyanGlow, true);

            var torso = WorldBuilder.Box("Torso", Vector3.zero + Vector3.up * 0.8f, new Vector3(0.36f, 0.4f, 0.26f), Metal());
            torso.transform.SetParent(root.transform, false);
            var chest = WorldBuilder.Box("Chest", Vector3.zero + Vector3.up * 0.9f + Vector3.forward * 0.14f, new Vector3(0.2f, 0.16f, 0.04f), WorldBuilder.Mat("Sold_Chest", new Color(0.85f, 0.9f, 1f), true, 0.9f), false);
            chest.transform.SetParent(root.transform, false);

            var head = WorldBuilder.Box("Head", Vector3.zero + Vector3.up * 1.24f, new Vector3(0.22f, 0.2f, 0.22f), MetalDark());
            head.transform.SetParent(root.transform, false);
            var visor = WorldBuilder.Box("Visor", Vector3.zero + Vector3.up * 1.26f + Vector3.forward * 0.115f, new Vector3(0.18f, 0.05f, 0.02f), eyeMat);
            visor.transform.SetParent(root.transform, false);

            var armL = WorldBuilder.Cyl("ArmL", Vector3.zero + Vector3.up * 0.98f + Vector3.left * 0.24f, new Vector3(0.09f, 0.3f, 0.09f), Metal());
            armL.transform.SetParent(root.transform, false);
            var armR = WorldBuilder.Cyl("ArmR", Vector3.zero + Vector3.up * 0.98f + Vector3.right * 0.24f, new Vector3(0.09f, 0.3f, 0.09f), Metal());
            armR.transform.SetParent(root.transform, false);

            var legL = WorldBuilder.Cyl("LegL", Vector3.zero + Vector3.up * 0.3f + Vector3.left * 0.08f, new Vector3(0.12f, 0.2f, 0.12f), MetalDark());
            legL.transform.SetParent(root.transform, false);
            var legR = WorldBuilder.Cyl("LegR", Vector3.zero + Vector3.up * 0.3f + Vector3.right * 0.08f, new Vector3(0.12f, 0.2f, 0.12f), MetalDark());
            legR.transform.SetParent(root.transform, false);

            // Rifle
            var rifle = WorldBuilder.Box("Rifle", Vector3.zero + Vector3.up * 0.76f + Vector3.right * 0.18f + Vector3.forward * 0.28f, new Vector3(0.1f, 0.1f, 0.7f), MetalDark());
            rifle.transform.SetParent(root.transform, false);

            return root;
        }

        // ------------------------------------------------------------------
        // MÁQUINA GUARDIANA (jefe)
        // ------------------------------------------------------------------
        public static GameObject BuildGuardian(out Material coreMat, out List<Material> flashMats)
        {
            var root = new GameObject("Maquina_Guardiana");
            flashMats = new List<Material>();
            coreMat = WorldBuilder.Mat("Guard_Core", WorldBuilder.RedGlow, true);
            Material guardMetal = MatCap(new Color(0.34f, 0.35f, 0.39f));
            Material guardDark = MatCap(new Color(0.18f, 0.19f, 0.22f));

            flashMats.Add(guardMetal);
            flashMats.Add(guardDark);

            var basePlat = WorldBuilder.Cyl("Base", Vector3.zero + Vector3.up * 0.3f, new Vector3(2.8f, 0.35f, 2.8f), guardDark);
            basePlat.transform.SetParent(root.transform, false);

            var legL = WorldBuilder.Box("LegL", Vector3.zero + Vector3.up * 1.0f + Vector3.left * 0.8f, new Vector3(0.8f, 1.1f, 0.9f), guardMetal);
            legL.transform.SetParent(root.transform, false);
            var legR = WorldBuilder.Box("LegR", Vector3.zero + Vector3.up * 1.0f + Vector3.right * 0.8f, new Vector3(0.8f, 1.1f, 0.9f), guardMetal);
            legR.transform.SetParent(root.transform, false);

            var body = WorldBuilder.Box("Body", Vector3.zero + Vector3.up * 2.6f, new Vector3(3.2f, 2.2f, 2.4f), guardMetal);
            body.transform.SetParent(root.transform, false);
            var chestCore = WorldBuilder.Sphere("Core", Vector3.zero + Vector3.up * 2.7f + Vector3.forward * 1.3f, 0.4f, coreMat);
            chestCore.transform.SetParent(root.transform, false);

            var head = WorldBuilder.Box("Head", Vector3.zero + Vector3.up * 4.2f, new Vector3(1.4f, 0.9f, 1.4f), guardDark);
            head.transform.SetParent(root.transform, false);
            var eye = WorldBuilder.Sphere("Eye", Vector3.zero + Vector3.up * 4.2f + Vector3.forward * 0.75f, 0.22f, coreMat);
            eye.transform.SetParent(root.transform, false);

            var armL = WorldBuilder.Box("ArmL", Vector3.zero + Vector3.up * 2.9f + Vector3.left * 1.9f, new Vector3(0.7f, 2.2f, 0.8f), guardMetal);
            armL.transform.SetParent(root.transform, false);
            var armR = WorldBuilder.Box("ArmR", Vector3.zero + Vector3.up * 2.9f + Vector3.right * 1.9f, new Vector3(0.7f, 2.2f, 0.8f), guardMetal);
            armR.transform.SetParent(root.transform, false);

            var cannon = WorldBuilder.Cyl("Cannon", Vector3.zero + Vector3.up * 3.6f + Vector3.right * 1.9f, new Vector3(0.4f, 1.0f, 0.4f), guardDark);
            cannon.transform.SetParent(root.transform, false);

            return root;
        }
    }
}