using Engine;
using Engine.Audio;
using Engine.Components;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player
{
    public enum MoodEvent
    {
        Discovery,
        Combat,
        Building,
        Exploration,
        Survival,
        Achievement
    }

    public class TimedMoodEvent
    {
        public float Interval { get; set; }
        public Func<bool> Condition { get; set; }
        public int MoodChangeOnTrue { get; set; }
        public int MoodChangeOnFalse { get; set; }
        public float Timer { get; set; } = 0f;
        public string Name { get; set; }

        public TimedMoodEvent(string name, float interval, Func<bool> condition, int moodChangeOnTrue, int moodChangeOnFalse = 0)
        {
            Name = name;
            Interval = interval;
            Condition = condition;
            MoodChangeOnTrue = moodChangeOnTrue;
            MoodChangeOnFalse = moodChangeOnFalse;
        }
    }

    public class Mood : Component
    {
        private StatsData _moodData;
        private List<AudioClip> _allEffectClips = new List<AudioClip>();
        private List<AudioClip> _availableEffects = new List<AudioClip>();
        private List<AudioClip> _playedEffects = new List<AudioClip>();
        private AudioSource _audioSource = new AudioSource();
        private List<TimedMoodEvent> _timedEvents = new List<TimedMoodEvent>();
        private Vector3 _lastPosition;
        private readonly Dictionary<MoodEvent, int> _eventValues = new Dictionary<MoodEvent, int>
        {
            { MoodEvent.Discovery, 1 },
            { MoodEvent.Combat, 10 },
            { MoodEvent.Building, 8 },
            { MoodEvent.Exploration, 12 },
            { MoodEvent.Survival, 20 },
            { MoodEvent.Achievement, 25 }
        };

        public StatsData MoodData => _moodData;
        private Random _random = new Random();
        private Astronaut _astronaut;

        public Mood(Astronaut astronaut)
        {
            _moodData = new StatsData("Mood", 100, 0);
            _moodData.OnMaxReached += PlayRandomEffect;
            _astronaut = astronaut;
        }

        public override void Start()
        {
            base.Start();
            LoadMoodEffects();
            SetupTimedEvents();

            if (_audioSource != null)
            {
                _audioSource.Volume = 0.8f;
                _audioSource.Setup3D(50f, 1000f, 0.2f);
            }

            _lastPosition = Owner?.Position ?? Vector3.Zero;
        }

        private void LoadMoodEffects()
        {
            string moodFolderPath = "Resources/Audio/Mood/";
            _allEffectClips.Clear();

            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, moodFolderPath);

                if (!Directory.Exists(fullPath))
                {
                    Debug.Log($"[Mood] Mood folder not found: {fullPath}");
                    return;
                }

                var supportedExtensions = new[] { ".ogg", ".wav" };
                var files = Directory.GetFiles(fullPath)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToArray();

                foreach (var filePath in files)
                {
                    try
                    {
                        string relativePath = moodFolderPath + Path.GetFileNameWithoutExtension(filePath);
                        var clip = Resources.Load<AudioClip>(relativePath);
                        if (clip != null)
                            _allEffectClips.Add(clip);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[Mood] Could not load audio effect: {filePath} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[Mood] Error loading mood effects: {ex.Message}");
            }

            ResetEffectPool();
            Debug.Log($"[Mood] Loaded {_allEffectClips.Count} mood effects");
        }

        private void ResetEffectPool()
        {
            _availableEffects.Clear();
            _availableEffects.AddRange(_allEffectClips);
            _playedEffects.Clear();
        }

        public float CalculateMoodTrend()
        {
            if (_timedEvents.Count == 0) return 0f;

            var intervals = _timedEvents.Select(e => e.Interval).ToArray();
            float lcm = CalculateLCM(intervals);

            float totalMoodChange = 0f;

            foreach (var timedEvent in _timedEvents)
            {
                int executionsInLCM = (int)(lcm / timedEvent.Interval);

                try
                {
                    bool conditionResult = timedEvent.Condition?.Invoke() ?? false;
                    int moodChangePerExecution = conditionResult ? timedEvent.MoodChangeOnTrue : timedEvent.MoodChangeOnFalse;
                    totalMoodChange += moodChangePerExecution * executionsInLCM;
                }
                catch
                {
                }
            }

            return totalMoodChange / lcm;
        }

        public float CalculateTimeToTarget(int targetMood)
        {
            float trend = CalculateMoodTrend();
            if (Math.Abs(trend) < 0.001f) return -1f;

            int currentMood = _moodData.Value;
            int difference = targetMood - currentMood;

            if ((difference > 0 && trend <= 0) || (difference < 0 && trend >= 0))
                return -1f;

            return Math.Abs(difference / trend);
        }

        private float CalculateLCM(float[] intervals)
        {
            if (intervals.Length == 0) return 1f;

            float lcm = intervals[0];
            for (int i = 1; i < intervals.Length; i++)
            {
                lcm = LCM(lcm, intervals[i]);
            }
            return lcm;
        }

        private float LCM(float a, float b)
        {
            return Math.Abs(a * b) / GCD(a, b);
        }

        private float GCD(float a, float b)
        {
            while (b != 0)
            {
                float temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public void RemoveTimedEvent(string name)
        {
            _timedEvents.RemoveAll(e => e.Name == name);
        }

        public void TriggerMoodEvent(MoodEvent moodEvent)
        {
            if (_eventValues.TryGetValue(moodEvent, out int value))
            {
                _moodData.Increment(value);
            }
        }

        public void AddMood(int points)
        {
            if (points > 0)
                _moodData.Increment(points);
        }

        public void RemoveMood(int points)
        {
            if (points > 0)
                _moodData.Decrement(points);
        }

        public void AddMoodRandom(int min, int max)
        {
            int randomValue = _random.Next(min, max + 1);
            if (randomValue > 0)
                _moodData.Increment(randomValue);
        }

        public void RemoveMoodRandom(int min, int max)
        {
            int randomValue = _random.Next(min, max + 1);
            if (randomValue > 0)
                _moodData.Decrement(randomValue);
        }

        public void ModifyMoodRandom(int min, int max)
        {
            int randomValue = _random.Next(min, max + 1);
            if (randomValue > 0)
                _moodData.Increment(randomValue);
            else if (randomValue < 0)
                _moodData.Decrement(Math.Abs(randomValue));
        }

        private void SetupTimedEvents()
        {
            AddTimedEvent("Flashlight", 10f, () =>
            {
                return _astronaut.Flashlight.Enabled == true;
            }, -1, 2);

            AddTimedEvent("Movement", 3f, () =>
            {
                bool isMoving = Vector3.Distance(_astronaut.Position, _lastPosition) > 0.1f;
                _lastPosition = _astronaut.Position;
                return isMoving;
            }, 0, 1);

            AddTimedEvent("LowHealth", 4f, () => _astronaut.HealthBar.StatsData.Percentage < 0.25f, 1);
        }

        public void AddTimedEvent(string name, float interval, Func<bool> condition, int moodChangeOnTrue, int moodChangeOnFalse = 0)
        {
            _timedEvents.Add(new TimedMoodEvent(name, interval, condition, moodChangeOnTrue, moodChangeOnFalse));
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!_astronaut.IsAlive) return;

            for (int i = 0; i < _timedEvents.Count; i++)
            {
                var timedEvent = _timedEvents[i];
                timedEvent.Timer += Time.Delta;

                if (timedEvent.Timer >= timedEvent.Interval)
                {
                    timedEvent.Timer = 0f;

                    try
                    {
                        bool conditionResult = timedEvent.Condition?.Invoke() ?? false;
                        int moodChange = conditionResult ? timedEvent.MoodChangeOnTrue : timedEvent.MoodChangeOnFalse;

                        if (moodChange > 0)
                            AddMood(moodChange);
                        else if (moodChange < 0)
                            RemoveMood(Math.Abs(moodChange));
                    }
                    catch (Exception ex)
                    {
                        Debug.Error($"[Mood] Error in timed event '{timedEvent.Name}': {ex.Message}");
                    }
                }
            }

#if DEBUG
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.K))
            {
                AddMood(1000);
            }
#endif
        }

        private void PlayRandomEffect()
        {
            MoodData.Reset();

            if (_allEffectClips.Count == 0)
            {
                Debug.Log("[Mood] No mood effects available to play");
                return;
            }

            if (_availableEffects.Count == 0)
            {
                ResetEffectPool();
                Debug.Log("[Mood] All effects played, resetting pool");
            }

            int selectedIndex = _random.Next(_availableEffects.Count);
            var selectedClip = _availableEffects[selectedIndex];

            _playedEffects.Add(selectedClip);
            _availableEffects.RemoveAt(selectedIndex);

            if (_audioSource != null && Owner != null)
            {
                var randomPosition = World.GetRandomPointAroundPosition(Owner.Position, 150f, 800f);
                _audioSource.Position = randomPosition;
                _audioSource.Clip = selectedClip;
                _audioSource.Play();

                Debug.Log($"[Mood] Playing mood effect '{selectedClip.Name}' at distance {Vector3.Distance(Owner.Position, randomPosition):F1}, remaining: {_availableEffects.Count}");
            }
        }

        public void SetEventValue(MoodEvent moodEvent, int value)
        {
            _eventValues[moodEvent] = Math.Max(0, value);
        }

        public override void OnDetached()
        {
            _audioSource?.Dispose();
            _allEffectClips.Clear();
            _availableEffects.Clear();
            _playedEffects.Clear();
            _timedEvents.Clear();
            base.OnDetached();
        }
    }
}