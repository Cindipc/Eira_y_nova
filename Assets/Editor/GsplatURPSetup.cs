#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EiraNova
{
    /// <summary>
    /// Añade automáticamente el renderer-feature de gaussian splatting (GsplatURPFeature) a
    /// todos los ScriptableRendererData del proyecto. El tipo es interno al paquete UnitySplats
    /// (compilado solo cuando URP define GSPLAT_ENABLE_URP), por eso se inyecta por reflexión.
    /// Ejecuta al recargar dominios e idempotente (no duplica features ya presentes).
    /// </summary>
    [InitializeOnLoad]
    public static class GsplatURPSetup
    {
        static GsplatURPSetup()
        {
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/Eira Nova/Configurar Gsplat URP Feature")]
        public static void Run()
        {
            Type featureType = FindFeatureType();
            if (featureType == null)
            {
                Debug.Log("[GsplatURPSetup] No se encontró Gsplat.GsplatURPFeature. " +
                          "¿Está instalado el paquete UnitySplats?");
                return;
            }

            int updated = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableRendererData"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(assetPath);
                if (data == null) continue;
                if (HasFeature(data, featureType)) continue;

                var feature = ScriptableObject.CreateInstance(featureType);
                if (feature == null) continue;
                feature.name = "GsplatURPFeature";
                AssetDatabase.AddObjectToAsset(feature, data);

                var so = new SerializedObject(data);
                var prop = so.FindProperty("m_RendererFeatures");
                if (prop != null && prop.isArray)
                {
                    prop.arraySize++;
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = feature;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(data);
                updated++;
            }

            if (updated > 0) AssetDatabase.SaveAssets();
            Debug.Log("[GsplatURPSetup] Renderer data configurados con GsplatURPFeature: " + updated);
        }

        static Type FindFeatureType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.Equals("Gsplat", StringComparison.OrdinalIgnoreCase))
                    continue;
                var t = assembly.GetType("Gsplat.GsplatURPFeature");
                if (t != null) return t;
            }
            return null;
        }

        static bool HasFeature(ScriptableRendererData data, Type type)
        {
            if (data.rendererFeatures == null) return false;
            foreach (var f in data.rendererFeatures)
            {
                if (f != null && f.GetType() == type) return true;
            }
            return false;
        }
    }
}
#endif