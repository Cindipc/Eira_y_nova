using UnityEngine;

namespace EiraNova
{
    /// <summary>
    /// Garantiza que el nivel 1 («El Despertar») siempre se construya al entrar a cualquier escena.
    /// Si la escena abierta no contiene un GameDirector, se crea uno automáticamente.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureDirector()
        {
            if (GameDirector.Instance != null) return;
            var go = new GameObject("GameDirector_Bootstrap");
            go.AddComponent<GameDirector>();
        }
    }
}