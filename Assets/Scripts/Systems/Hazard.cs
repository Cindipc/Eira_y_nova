using UnityEngine;

namespace EiraNova
{
    /// <summary>Trampa de plasma en el suelo: daña a Eira mientras permanezca dentro.</summary>
    public class PlasmaTrap : MonoBehaviour
    {
        public float damagePerSecond = 22f;
        public float tick = 0.5f;
        float _timer;

        Material _mat;

        public void Init(Material mat) { _mat = mat; }

        void OnTriggerStay(Collider other)
        {
            if (other.GetComponent<PlayerAbilities>() == null) return;
            _timer += Time.deltaTime;
            if (_timer >= tick)
            {
                _timer = 0f;
                var a = other.GetComponent<PlayerAbilities>();
                if (a != null) a.TakeHeartDamage(damagePerSecond * tick);
                GameDirector.Instance?.FlashDamage(0.22f);
            }
        }

        void Update()
        {
            if (_mat != null)
                _mat.color = Color.Lerp(new Color(1f, 0.35f, 0.1f), new Color(1f, 0.9f, 0.4f), Mathf.PingPong(Time.time * 3f, 1f));
        }
    }
}