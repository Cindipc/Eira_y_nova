using UnityEngine;
using UnityEngine.Rendering;

namespace EiraNova
{
    /// <summary>Helpers para construir el mundo con primitivas y materiales en tiempo de ejecución.</summary>
    public static class WorldBuilder
    {
        // Paleta básica del mundo
        public static readonly Color MetalDark   = new Color(0.13f, 0.14f, 0.16f);
        public static readonly Color MetalGrey   = new Color(0.35f, 0.37f, 0.40f);
        public static readonly Color MetalLight  = new Color(0.60f, 0.62f, 0.65f);
        public static readonly Color Rust        = new Color(0.38f, 0.25f, 0.18f);
        public static readonly Color CyanGlow    = new Color(0.20f, 0.85f, 0.90f);
        public static readonly Color OrangeGlow  = new Color(1.00f, 0.55f, 0.15f);
        public static readonly Color RedGlow     = new Color(1.00f, 0.20f, 0.20f);
        public static readonly Color GreenGlow   = new Color(0.35f, 1.00f, 0.45f);
        public static readonly Color Skin        = new Color(0.95f, 0.76f, 0.64f);
        public static readonly Color Sky         = new Color(0.16f, 0.20f, 0.28f);

        public static Material Mat(string name, Color c, bool unlit = false, float alpha = 1f)
        {
            string shaderName = unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit";
            Shader s = Shader.Find(shaderName);
            if (s == null)
                s = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            var m = new Material(s) { name = name };
            m.SetColor("_BaseColor", c);
            m.color = c;
            if (alpha < 0.999f)
                MakeTransparent(m, new Color(c.r, c.g, c.b, alpha));
            return m;
        }

        static void MakeTransparent(Material m, Color c)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetColor("_BaseColor", c);
        }

        static GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = m;
            if (!collider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            return go;
        }

        public static GameObject Box(string name, Vector3 pos, Vector3 size, Material m, bool solid = true)
            => Primitive(name, PrimitiveType.Cube, pos, size, m, solid);

        public static GameObject Cyl(string name, Vector3 pos, Vector3 scale, Material m, bool collider = true)
            => Primitive(name, PrimitiveType.Cylinder, pos, scale, m, collider);

        public static GameObject Cap(string name, Vector3 pos, Vector3 scale, Material m)
            => Primitive(name, PrimitiveType.Capsule, pos, scale, m, true);

        public static GameObject Sphere(string name, Vector3 pos, float r, Material m, bool collider = true)
            => Primitive(name, PrimitiveType.Sphere, pos, Vector3.one * r, m, collider);

        public static GameObject Quad(string name, Vector3 pos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        /// <summary>Crea un cubo sólido decorativo sin colisiones (utilizado para cables, vigas, etc.).</summary>
        public static GameObject DecoBox(string name, Vector3 pos, Vector3 size, Material m)
            => Box(name, pos, size, m, false);

        public static GameObject PointLight(string name, Vector3 pos, Color c, float range = 8f, float intensity = 2.5f)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            return go;
        }

        /// <summary>Hace que una luz parpadee suavemente (para iluminación de laboratorio).</summary>
        public static GameObject FlickerLight(string name, Vector3 pos, Color c, float range = 8f, float intensity = 2.5f)
        {
            var go = PointLight(name, pos, c, range, intensity);
            var fl = go.AddComponent<FlickerLightSource>();
            fl.baseIntensity = intensity;
            return go;
        }

        public static Transform Parent(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go.transform;
        }
    }

    public class FlickerLightSource : MonoBehaviour
    {
        public float baseIntensity = 2.5f;
        public float minIntensity = 0.4f;
        public float speed = 12f;
        Light _light;

        void Awake() { _light = GetComponent<Light>(); }

        void Update()
        {
            if (_light != null)
                _light.intensity = minIntensity + (baseIntensity - minIntensity) * (0.5f + 0.5f * Mathf.Sin(Time.time * speed + transform.position.x * 3.0f));
        }
    }
}