using UnityEngine;

namespace GungeonFPS.Core
{
    public enum SfxType
    {
        Pistol,
        Shotgun,
        Bounce,
        Hit,
        EnemyDie,
        Dash,
        Blank,
        Hurt,
        Pickup,
        DoorOpen
    }

    public class RetroAudio : MonoBehaviour
    {
        public static RetroAudio Instance { get; private set; }

        private AudioSource _source;
        private AudioClip _pistolClip;
        private AudioClip _shotgunClip;
        private AudioClip _bounceClip;
        private AudioClip _hitClip;
        private AudioClip _dieClip;
        private AudioClip _dashClip;
        private AudioClip _blankClip;
        private AudioClip _hurtClip;
        private AudioClip _pickupClip;
        private AudioClip _doorClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _source = gameObject.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
            _source.volume = 0.35f;

            GenerateClips();
        }

        public void Play(SfxType type, float volumeScale = 1f)
        {
            if (_source == null) return;
            AudioClip clip = type switch
            {
                SfxType.Pistol => _pistolClip,
                SfxType.Shotgun => _shotgunClip,
                SfxType.Bounce => _bounceClip,
                SfxType.Hit => _hitClip,
                SfxType.EnemyDie => _dieClip,
                SfxType.Dash => _dashClip,
                SfxType.Blank => _blankClip,
                SfxType.Hurt => _hurtClip,
                SfxType.Pickup => _pickupClip,
                SfxType.DoorOpen => _doorClip,
                _ => null
            };
            if (clip == null) return;

            _source.pitch = Random.Range(0.93f, 1.07f);
            _source.PlayOneShot(clip, volumeScale);
        }

        private void GenerateClips()
        {
            _pistolClip = CreateNoiseSweep(0.09f, 520f, 140f, 0.7f);
            _shotgunClip = CreateNoiseSweep(0.18f, 260f, 65f, 0.95f);
            _bounceClip = CreateToneSweep(0.11f, 880f, 420f, 0.55f);
            _hitClip = CreateToneSweep(0.05f, 680f, 340f, 0.45f);
            _dieClip = CreateNoiseSweep(0.22f, 340f, 55f, 0.85f);
            _dashClip = CreateNoiseSweep(0.14f, 180f, 620f, 0.5f);
            _blankClip = CreateNoiseSweep(0.32f, 900f, 80f, 1f);
            _hurtClip = CreateToneSweep(0.18f, 220f, 95f, 0.8f);
            _pickupClip = CreateArpeggio(0.16f, 523.25f, 783.99f, 0.5f);
            _doorClip = CreateToneSweep(0.22f, 150f, 310f, 0.5f);
        }

        private static AudioClip CreateToneSweep(float duration, float startFreq, float endFreq, float amp)
        {
            int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / sampleRate;
                float env = 1f - t;
                float wave = Mathf.Sign(Mathf.Sin(phase)); // crisp square wave
                data[i] = wave * env * amp;
            }

            var clip = AudioClip.Create("sfx_tone", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateNoiseSweep(float duration, float startFreq, float endFreq, float amp)
        {
            int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[count];
            float phase = 0f;
            float heldNoise = 0f;
            int holdCounter = 0;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / sampleRate;

                if (holdCounter <= 0)
                {
                    heldNoise = Random.Range(-1f, 1f);
                    holdCounter = Mathf.Max(1, Mathf.RoundToInt(sampleRate / Mathf.Max(80f, freq * 2f)));
                }
                holdCounter--;

                float env = (1f - t) * (1f - t);
                float tone = Mathf.Sin(phase) * 0.35f;
                data[i] = (heldNoise * 0.65f + tone) * env * amp;
            }

            var clip = AudioClip.Create("sfx_noise", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArpeggio(float duration, float freqA, float freqB, float amp)
        {
            int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float freq = t < 0.5f ? freqA : freqB;
                phase += (2f * Mathf.PI * freq) / sampleRate;
                float env = 1f - t * 0.7f;
                data[i] = Mathf.Sign(Mathf.Sin(phase)) * env * amp;
            }

            var clip = AudioClip.Create("sfx_arp", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
