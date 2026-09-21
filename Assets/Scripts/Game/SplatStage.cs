using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Gsplat;

namespace EiraNova
{
    /// <summary>
    /// Escenario escaneado con gaussian splatting cargado en tiempo de ejecución:
    /// - el .spz se decodifica con <see cref="GsplatRuntimeLoader"/> y se asigna a un <see cref="GsplatRenderer"/>;
    /// - el collider.glb del pack se lee (JSON GLB) para obtener el AABB del entorno real;
    /// - sobre ese AABB se construyen un suelo y muros de colisión invisibles y a escala de juego.
    /// El escenario queda centrado en <see cref="stagePosition"/> y escalado para que su ancho X
    /// mida <see cref="targetSpanX"/> unidades de juego.
    /// </summary>
    public class SplatStage : MonoBehaviour
    {
        [Header("Recursos (relativos a StreamingAssets)")]
        public string splatPath = "Env/ciudad/escenario.spz";
        public string colliderPath = "Env/ciudad/collider.glb";

        [Header("Ajuste de escenario")]
        public Vector3 stagePosition = Vector3.zero;
        public float targetSpanX = 60f;
        public float wallHeight = 10f;
        public float floorThickness = 0.5f;
        public bool buildWalls = true;

        [Header("Ajustes del visual (si el escaneo aparece girado/desplazado)")]
        public Vector3 splatEuler = Vector3.zero;
        public Vector3 splatOffset = Vector3.zero;

        /// <summary>El escenario está cargado y tiene suelo listo.</summary>
        public bool Ready { get; private set; }

        /// <summary>AABB del suelo jugable en coordenadas de mundo.</summary>
        public Bounds StageBounds { get; private set; }

        /// <summary>Altura del piso (para posicionar al jugador y los props).</summary>
        public float GroundY { get; private set; }

        public GsplatRenderer Renderer { get; private set; }

        GsplatAsset _asset;
        Transform _env;

        public void Build()
        {
            ClearStage();
            Ready = false;

            var env = new GameObject("Env_Root");
            env.transform.SetParent(transform, false);
            _env = env.transform;

            // 1) Límites del entorno: primero del GLB del collider (autoritativo),
            //    después de los bounds del propio .spz, y al final un escenario genérico.
            Bounds aabb;
            bool haveBounds = TryParseGlbBounds(ColliderFullPath(), out aabb);

            GsplatAsset asset = null;
            string spzFull = SplatFullPath();
            if (File.Exists(spzFull))
            {
                try
                {
                    asset = GsplatRuntimeLoader.LoadFile(spzFull, CompressionMode.Spark);
                    _asset = asset;
                    if (!haveBounds)
                    {
                        var b = asset.Bounds;
                        aabb = new Bounds(b.center, b.size);
                        haveBounds = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SplatStage] No se pudo cargar el splat '" + spzFull + "': " + e.Message);
                }
            }
            else
            {
                Debug.LogWarning("[SplatStage] Falta el .spz: " + spzFull);
            }

            if (!haveBounds)
                aabb = new Bounds(Vector3.zero, new Vector3(targetSpanX, wallHeight, targetSpanX * 0.8f));

            // 2) Escala para alcanzar el ancho pedido (centrado en stagePosition).
            float span = Mathf.Max(aabb.size.x, 0.05f);
            float scale = targetSpanX / span;
            StageBounds = new Bounds(stagePosition, aabb.size * scale);
            GroundY = StageBounds.min.y;

            // 3) Renderer del splat (misma escala que el suelo para que coincidan).
            //    Alineamos el centro de los bounds del splat con el centro del GLB (o de
            //    stagePosition) para que el visual coincida con el suelo de colisión.
            Vector3 alignLocal = Vector3.zero;
            if (asset != null)
            {
                Bounds ab = asset.Bounds;
                if (haveBounds && !(System.Math.Abs(ab.center.x) < 0.001f &&
                                    System.Math.Abs(ab.center.y) < 0.001f &&
                                    System.Math.Abs(ab.center.z) < 0.001f))
                    alignLocal = aabb.center - ab.center;
                else
                    alignLocal = aabb.center;
            }

            var splatRoot = new GameObject("Splat_Visual");
            splatRoot.transform.SetParent(env.transform, false);
            splatRoot.transform.position = stagePosition + alignLocal * scale + splatOffset;
            splatRoot.transform.localScale = Vector3.one * scale;
            if (splatEuler != Vector3.zero)
                splatRoot.transform.localRotation = Quaternion.Euler(splatEuler);

            if (asset != null)
            {
                var rend = splatRoot.AddComponent<GsplatRenderer>();
                rend.GsplatAsset = asset;
                rend.GammaToLinear = true;
                Renderer = rend;
                Debug.Log($"[SplatStage] Splat cargado '{splatPath}' splats={asset.SplatCount} " +
                          $"bounds={asset.Bounds} scale={scale:0.###} alignLocal={alignLocal}");
            }
            else
            {
                Debug.LogWarning("[SplatStage] Sin asset de splat: el escenario solo tendra suelo/muros invisibles.");
            }

            Debug.Log($"[SplatStage] Stage listo '{splatPath}' StageBounds={StageBounds} GroundY={GroundY:0.##}");

            // 4) Suelo + muros de colisión invisibles sobre el AABB.
            BuildField();

            Ready = true;
        }

        void BuildField()
        {
            var field = new GameObject("Campo_Colision");
            field.transform.SetParent(_env != null ? _env : transform, false);

            var b = StageBounds;
            float halfThick = floorThickness * 0.5f;
            BuildColBox("Piso", field.transform,
                new Vector3(b.center.x, b.min.y - halfThick, b.center.z),
                new Vector3(b.size.x, floorThickness, b.size.z));

            if (!buildWalls) return;

            float w = Mathf.Max(0.8f, floorThickness);
            float wallY = b.min.y + wallHeight * 0.5f;
            float h = wallHeight;

            BuildColBox("Muro_PX", field.transform, new Vector3(b.max.x + w * 0.5f, wallY, b.center.z), new Vector3(w, h, b.size.z));
            BuildColBox("Muro_NX", field.transform, new Vector3(b.min.x - w * 0.5f, wallY, b.center.z), new Vector3(w, h, b.size.z));
            BuildColBox("Muro_PZ", field.transform, new Vector3(b.center.x, wallY, b.max.z + w * 0.5f), new Vector3(b.size.x, h, w));
            BuildColBox("Muro_NZ", field.transform, new Vector3(b.center.x, wallY, b.min.z - w * 0.5f), new Vector3(b.size.x, h, w));
        }

        /// <summary>Crea una caja de colisión invisible (sin renderer).</summary>
        public static GameObject BuildColBox(string name, Transform parent, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        // ---------------------------------------------------------------
        // Rutas
        // ---------------------------------------------------------------
        string SplatFullPath() => FullPath(splatPath);
        string ColliderFullPath() => FullPath(colliderPath);

        string FullPath(string rel)
        {
            if (string.IsNullOrWhiteSpace(rel)) return "";
            var clean = rel.Replace('\\', '/');
            return Path.Combine(Application.streamingAssetsPath, clean.Replace('/', Path.DirectorySeparatorChar));
        }

        void ClearStage()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        void OnDestroy()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            if (_asset != null)
            {
                if (Application.isPlaying) Destroy(_asset);
                else DestroyImmediate(_asset);
                _asset = null;
            }
        }

        // ---------------------------------------------------------------
        // Parser del collider.glb (solo JSON + AABB del accessor POSITION)
        // ---------------------------------------------------------------
        bool TryParseGlbBounds(string path, out Bounds aabb)
        {
            aabb = default;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                string json = ExtractGlbJson(File.ReadAllBytes(path));
                if (json == null) return false;

                var root = JVal.Parse(json);
                if (root == null || !root.IsObject) return false;

                int posAccessor = -1;
                var meshes = root.Get("meshes");
                if (meshes != null && meshes.IsArray && meshes.Array.Count > 0)
                {
                    var prims = meshes.Array[0].Get("primitives");
                    if (prims != null && prims.IsArray && prims.Array.Count > 0)
                    {
                        var attrs = prims.Array[0].Get("attributes");
                        var pos = attrs?.Get("POSITION");
                        if (pos != null && pos.IsNumber) posAccessor = (int)pos.Number;
                    }
                }
                if (posAccessor < 0) return false;

                var accessors = root.Get("accessors");
                if (accessors == null || !accessors.IsArray || accessors.Array.Count <= posAccessor) return false;
                var acc = accessors.Array[posAccessor];
                var minArr = acc?.Get("min");
                var maxArr = acc?.Get("max");
                if (minArr == null || !minArr.IsArray || minArr.Array.Count < 3) return false;
                if (maxArr == null || !maxArr.IsArray || maxArr.Array.Count < 3) return false;

                var vMin = new Vector3((float)minArr.Array[0].Number, (float)minArr.Array[1].Number, (float)minArr.Array[2].Number);
                var vMax = new Vector3((float)maxArr.Array[0].Number, (float)maxArr.Array[1].Number, (float)maxArr.Array[2].Number);

                // Transformada del nodo (column-major).
                var m = Matrix4x4.identity;
                var nodes = root.Get("nodes");
                if (nodes != null && nodes.IsArray)
                {
                    foreach (var node in nodes.Array)
                    {
                        var meshRef = node?.Get("mesh");
                        if (meshRef != null && meshRef.IsNumber && (int)meshRef.Number == 0)
                        {
                            var mat = node.Get("matrix");
                            if (mat != null && mat.IsArray && mat.Array.Count >= 16)
                            {
                                m = Matrix4x4.zero;
                                for (int i = 0; i < 16; i++)
                                    m[i % 4, i / 4] = (float)mat.Array[i].Number;
                            }
                            break;
                        }
                    }
                }

                Vector3 wMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 wMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (int i = 0; i < 8; i++)
                {
                    float x = (i & 1) == 0 ? vMin.x : vMax.x;
                    float y = (i & 2) == 0 ? vMin.y : vMax.y;
                    float z = (i & 4) == 0 ? vMin.z : vMax.z;
                    Vector3 c = m.MultiplyPoint3x4(new Vector3(x, y, z));
                    wMin = Vector3.Min(wMin, c);
                    wMax = Vector3.Max(wMax, c);
                }

                aabb = new Bounds((wMin + wMax) * 0.5f, wMax - wMin);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SplatStage] No se pudo parsear el collider GLB '" + path + "': " + e.Message);
                return false;
            }
        }

        static string ExtractGlbJson(byte[] bytes)
        {
            if (bytes.Length < 12) return null;
            uint magic = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
            if (magic != 0x46546C67) return null; // glTF
            int offset = 12;
            while (offset + 8 <= bytes.Length)
            {
                int length = bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
                uint type = (uint)(bytes[offset + 4] | (bytes[offset + 5] << 8) | (bytes[offset + 6] << 16) | (bytes[offset + 7] << 24));
                offset += 8;
                if (offset + length > bytes.Length) return null;
                if (type == 0x4E4F534A) // JSON
                    return System.Text.Encoding.UTF8.GetString(bytes, offset, length);
                offset += length;
            }
            return null;
        }

        // ---------------------------------------------------------------
        // Parser JSON mínimo
        // ---------------------------------------------------------------
        class JVal
        {
            public enum K { Null, Bool, Number, String, Array, Object }
            public K Kind = K.Null;
            public bool Bool;
            public double Number;
            public string Str;
            public List<JVal> Array = new List<JVal>();
            public Dictionary<string, JVal> Obj = new Dictionary<string, JVal>();

            public bool IsObject => Kind == K.Object;
            public bool IsArray => Kind == K.Array;
            public bool IsNumber => Kind == K.Number;

            public JVal Get(string key)
            {
                JVal v;
                return Obj.TryGetValue(key, out v) ? v : null;
            }

            static int Pos;

            public static JVal Parse(string text)
            {
                if (string.IsNullOrEmpty(text)) return null;
                Pos = 0;
                try
                {
                    SkipWs(text);
                    var v = Read(text);
                    SkipWs(text);
                    return v;
                }
                catch
                {
                    return null;
                }
            }

            static void SkipWs(string s)
            {
                while (Pos < s.Length && (s[Pos] == ' ' || s[Pos] == '\t' || s[Pos] == '\r' || s[Pos] == '\n'))
                    Pos++;
            }

            static JVal Read(string s)
            {
                if (Pos >= s.Length) throw new Exception("fin");
                char c = s[Pos];
                switch (c)
                {
                    case '{': return ReadObject(s);
                    case '[': return ReadArray(s);
                    case '"': return ReadStringVal(s);
                    case 't': Expect(s, "true"); return new JVal { Kind = K.Bool, Bool = true };
                    case 'f': Expect(s, "false"); return new JVal { Kind = K.Bool, Bool = false };
                    case 'n': Expect(s, "null"); return new JVal { Kind = K.Null };
                    default: return ReadNumber(s);
                }
            }

            static void Expect(string s, string word)
            {
                for (int i = 0; i < word.Length; i++)
                    if (Pos + i >= s.Length || s[Pos + i] != word[i]) throw new Exception("token");
                Pos += word.Length;
            }

            static JVal ReadObject(string s)
            {
                Pos++; // '{'
                var v = new JVal { Kind = K.Object };
                SkipWs(s);
                if (Pos < s.Length && s[Pos] == '}') { Pos++; return v; }
                while (true)
                {
                    SkipWs(s);
                    var key = ReadStringVal(s).Str;
                    SkipWs(s);
                    if (Pos >= s.Length || s[Pos] != ':') throw new Exception("':'");
                    Pos++;
                    SkipWs(s);
                    var val = Read(s);
                    v.Obj[key] = val;
                    SkipWs(s);
                    if (Pos < s.Length && s[Pos] == ',') { Pos++; continue; }
                    if (Pos < s.Length && s[Pos] == '}') { Pos++; return v; }
                    throw new Exception("'}");
                }
            }

            static JVal ReadArray(string s)
            {
                Pos++; // '['
                var v = new JVal { Kind = K.Array };
                SkipWs(s);
                if (Pos < s.Length && s[Pos] == ']') { Pos++; return v; }
                while (true)
                {
                    SkipWs(s);
                    v.Array.Add(Read(s));
                    SkipWs(s);
                    if (Pos < s.Length && s[Pos] == ',') { Pos++; continue; }
                    if (Pos < s.Length && s[Pos] == ']') { Pos++; return v; }
                    throw new Exception("']");
                }
            }

            static JVal ReadStringVal(string s)
            {
                Pos++; // '"'
                var sb = new System.Text.StringBuilder();
                while (Pos < s.Length)
                {
                    char c = s[Pos++];
                    if (c == '"') return new JVal { Kind = K.String, Str = sb.ToString() };
                    if (c == '\\')
                    {
                        if (Pos >= s.Length) throw new Exception("escape");
                        char e = s[Pos++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (Pos + 4 > s.Length) throw new Exception("\\u");
                                sb.Append((char)Convert.ToInt32(s.Substring(Pos, 4), 16));
                                Pos += 4;
                                break;
                            default: throw new Exception("escape");
                        }
                    }
                    else sb.Append(c);
                }
                throw new Exception("string");
            }

            static JVal ReadNumber(string s)
            {
                int start = Pos;
                if (Pos < s.Length && (s[Pos] == '-' || s[Pos] == '+')) Pos++;
                while (Pos < s.Length && (char.IsDigit(s[Pos]) || s[Pos] == '.' || s[Pos] == 'e' || s[Pos] == 'E' || s[Pos] == '-' || s[Pos] == '+'))
                    Pos++;
                if (Pos == start) throw new Exception("numero");
                double d;
                if (!double.TryParse(s.Substring(start, Pos - start), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out d))
                    throw new Exception("numero");
                return new JVal { Kind = K.Number, Number = d };
            }
        }
    }
}