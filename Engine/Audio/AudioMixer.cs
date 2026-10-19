namespace Engine.Audio
{
    public enum AudioChannel
    {
        Effects,
        Music,
        Ambient,
        UI
    }

    public static class AudioMixer
    {
        private static readonly float[] ChannelVolumes = { 1f, 1f, 1f, 1f };
        private static readonly HashSet<AudioSource> Sources = new HashSet<AudioSource>();
        private static readonly List<AudioSource> Scratch = new List<AudioSource>();
        private static readonly object SourcesLock = new object();
        private static float _master = 1f;
        private static bool _muted;

        public static float Master
        {
            get => _master;
            set
            {
                float clamped = Math.Clamp(value, 0f, 1f);
                if (clamped == _master) return;
                _master = clamped;
                Refresh();
            }
        }

        public static bool Muted
        {
            get => _muted;
            set
            {
                if (value == _muted) return;
                _muted = value;
                Refresh();
            }
        }

        public static float GetVolume(AudioChannel channel)
        {
            return ChannelVolumes[(int)channel];
        }

        public static void SetVolume(AudioChannel channel, float volume)
        {
            float clamped = Math.Clamp(volume, 0f, 1f);
            if (ChannelVolumes[(int)channel] == clamped) return;
            ChannelVolumes[(int)channel] = clamped;
            Refresh();
        }

        public static float GetGain(AudioChannel channel)
        {
            return _muted ? 0f : _master * ChannelVolumes[(int)channel];
        }

        public static void Reset()
        {
            _master = 1f;
            _muted = false;
            for (int i = 0; i < ChannelVolumes.Length; i++) ChannelVolumes[i] = 1f;
            Refresh();
        }

        internal static void Register(AudioSource source)
        {
            lock (SourcesLock) Sources.Add(source);
        }

        internal static void Unregister(AudioSource source)
        {
            lock (SourcesLock) Sources.Remove(source);
        }

        private static void Refresh()
        {
            lock (SourcesLock)
            {
                Scratch.Clear();
                Scratch.AddRange(Sources);
            }

            for (int i = 0; i < Scratch.Count; i++)
                Scratch[i].ApplyGain();
            Scratch.Clear();
        }
    }
}
