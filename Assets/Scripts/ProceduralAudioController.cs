using UnityEngine;

namespace MagnetMayhem
{
    public class ProceduralAudioController : MonoBehaviour
    {
        public static ProceduralAudioController Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _humSource;
        private AudioSource _musicSource;

        private AudioClip _clickClip;
        private AudioClip _repelClip;
        private AudioClip _perfectClip;
        private AudioClip _deliveryClip;
        private AudioClip _failClip;
        private AudioClip _winClip;
        private AudioClip _humLoopClip;
        private AudioClip _ambientMusicClip;

        public bool SfxEnabled { get; private set; } = true;
        public bool MusicEnabled { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SfxEnabled = PlayerPrefs.GetInt("MM_SFX", 1) == 1;
            MusicEnabled = PlayerPrefs.GetInt("MM_MUSIC", 1) == 1;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _humSource = gameObject.AddComponent<AudioSource>();
            _humSource.loop = true;
            _humSource.playOnAwake = false;
            _humSource.volume = 0f;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = 0.16f;

            SynthesizeAllClips();

            _humSource.clip = _humLoopClip;
            _musicSource.clip = _ambientMusicClip;

            if (MusicEnabled) _musicSource.Play();
        }

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            PlayerPrefs.SetInt("MM_SFX", enabled ? 1 : 0);
            if (!enabled) _humSource.Stop();
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt("MM_MUSIC", enabled ? 1 : 0);
            if (enabled)
            {
                if (!_musicSource.isPlaying) _musicSource.Play();
            }
            else
            {
                _musicSource.Stop();
            }
        }

        public void UpdateAttractHum(bool active, float proximity01)
        {
            if (!SfxEnabled || !active)
            {
                if (_humSource.isPlaying) _humSource.Stop();
                return;
            }

            if (!_humSource.isPlaying) _humSource.Play();
            _humSource.volume = Mathf.Lerp(0.06f, 0.24f, proximity01);
            _humSource.pitch = Mathf.Lerp(0.85f, 1.55f, proximity01);
        }

        public void PlayClick()
        {
            if (SfxEnabled) _sfxSource.PlayOneShot(_clickClip, 0.55f);
        }

        public void PlayRepel(bool isPerfect)
        {
            if (!SfxEnabled) return;
            _sfxSource.PlayOneShot(_repelClip, 0.85f);
            if (isPerfect) _sfxSource.PlayOneShot(_perfectClip, 0.90f);
        }

        public void PlayDelivery(int combo)
        {
            if (!SfxEnabled) return;
            _sfxSource.pitch = Mathf.Clamp(1.0f + (combo - 1) * 0.08f, 1.0f, 1.6f);
            _sfxSource.PlayOneShot(_deliveryClip, 0.80f);
            _sfxSource.pitch = 1.0f;
        }

        public void PlayFail()
        {
            if (!SfxEnabled) return;
            _humSource.Stop();
            _sfxSource.PlayOneShot(_failClip, 0.85f);
        }

        public void PlayLevelComplete()
        {
            if (!SfxEnabled) return;
            _humSource.Stop();
            _sfxSource.PlayOneShot(_winClip, 0.90f);
        }

        private void SynthesizeAllClips()
        {
            int sampleRate = 44100;

            // 1. UI Click
            _clickClip = CreateClip("ui_click", 0.06f, sampleRate, (t, norm) =>
            {
                float freq = Mathf.Lerp(1400f, 650f, norm);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - norm);
            });

            // 2. Repel Magnetic Pulse
            _repelClip = CreateClip("repel_pulse", 0.24f, sampleRate, (t, norm) =>
            {
                float freq = Mathf.Lerp(420f, 95f, Mathf.Pow(norm, 0.6f));
                float sub = Mathf.Sin(2f * Mathf.PI * freq * t);
                float buzz = Mathf.Sin(2f * Mathf.PI * (freq * 2.5f) * t) * 0.35f;
                return (sub + buzz) * (1f - norm);
            });

            // 3. Perfect Chime
            _perfectClip = CreateClip("perfect_chime", 0.38f, sampleRate, (t, norm) =>
            {
                float f1 = 880f;  // A5
                float f2 = 1108.7f; // C#6
                float f3 = 1318.5f; // E6
                float s = (Mathf.Sin(2f * Mathf.PI * f1 * t) +
                           Mathf.Sin(2f * Mathf.PI * f2 * t) +
                           Mathf.Sin(2f * Mathf.PI * f3 * t)) / 3f;
                return s * Mathf.Pow(1f - norm, 1.4f);
            });

            // 4. Successful Delivery Pop
            _deliveryClip = CreateClip("delivery_pop", 0.22f, sampleRate, (t, norm) =>
            {
                float freq = Mathf.Lerp(520f, 1040f, norm);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - norm);
            });

            // 5. Wrong Delivery / Fail Buzz
            _failClip = CreateClip("fail_buzz", 0.45f, sampleRate, (t, norm) =>
            {
                float freq = (norm < 0.5f) ? 210f : 155f;
                float saw = 2f * ((t * freq) - Mathf.Floor(0.5f + t * freq));
                return saw * 0.55f * (1f - norm * 0.7f);
            });

            // 6. Level Complete Fanfare
            _winClip = CreateClip("level_win", 0.75f, sampleRate, (t, norm) =>
            {
                float noteFreq = 523.25f; // C5
                if (norm > 0.22f) noteFreq = 659.25f; // E5
                if (norm > 0.44f) noteFreq = 783.99f; // G5
                if (norm > 0.66f) noteFreq = 1046.50f; // C6

                float wave = Mathf.Sin(2f * Mathf.PI * noteFreq * t) +
                             0.4f * Mathf.Sin(2f * Mathf.PI * (noteFreq * 2f) * t);
                return wave * 0.55f * (1f - norm * 0.5f);
            });

            // 7. Continuous Magnetic Hum Loop
            _humLoopClip = CreateClip("attract_hum", 1.0f, sampleRate, (t, norm) =>
            {
                float w1 = Mathf.Sin(2f * Mathf.PI * 110f * t);
                float w2 = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.4f;
                float mod = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                return (w1 + w2) * 0.35f * mod;
            });

            // 8. Futuristic Synthwave Ambient Loop
            _ambientMusicClip = CreateClip("synth_ambient", 4.0f, sampleRate, (t, norm) =>
            {
                float bassFreq = (norm < 0.5f) ? 65.41f : 73.42f; // C2 -> D2
                float padFreq = bassFreq * 4f;
                float w = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.45f +
                          Mathf.Sin(2f * Mathf.PI * padFreq * t) * 0.20f +
                          Mathf.Sin(2f * Mathf.PI * (padFreq * 1.5f) * t) * 0.15f;
                return w * 0.45f;
            });
        }

        private delegate float WaveGenerator(float timeSeconds, float normalizedTime);

        private AudioClip CreateClip(string clipName, float duration, int sampleRate, WaveGenerator generator)
        {
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = (float)i / sampleCount;
                data[i] = Mathf.Clamp(generator(t, norm), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
