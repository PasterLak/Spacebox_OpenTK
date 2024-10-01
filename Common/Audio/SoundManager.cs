using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace Spacebox.Common.Audio
{
    public class SoundManager : IDisposable
    {
        private readonly Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, int> audioClipRefCount = new Dictionary<string, int>();
        private readonly object cacheLock = new object();

        private bool isDisposed = false;

        private readonly string baseDirectory;
        private readonly List<string> allowedExtensions;

        public string BaseDirectory => baseDirectory;
        public List<string> AllowedExtensions => allowedExtensions;

        public SoundManager()
        {
            baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Audio");

            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory cannot be null or whitespace.", nameof(baseDirectory));

            if (!Directory.Exists(baseDirectory))
                throw new DirectoryNotFoundException($"The base directory '{baseDirectory}' does not exist.");

            baseDirectory = Path.GetFullPath(baseDirectory);
            allowedExtensions = new List<string> { ".wav", ".ogg" };

            //DisposablesUnloader.Add(this);
        }

        public void AddAudioClip(string name)
        {
            AddAudioClip(name, AudioLoadMode.LoadIntoMemory);
        }

        public void AddAudioClip(string name, AudioLoadMode loadMode)
        {
            lock (cacheLock)
            {
                if (audioClips.ContainsKey(name))
                {
                    throw new InvalidOperationException($"AudioClip with name '{name}' is already loaded.");
                }

                string filePath = AudioPathResolver.ResolvePath(name, baseDirectory, allowedExtensions);
                if (filePath == null)
                {
                    throw new FileNotFoundException($"Audio file for '{name}' not found in '{baseDirectory}' or its subdirectories with allowed extensions.");
                }

                var clip = new AudioClip(filePath, loadMode, this);
                audioClips[name] = clip;
                audioClipRefCount[name] = 1;
            }
        }

        public AudioClip GetClip(string name, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
            lock (cacheLock)
            {
                if (audioClips.TryGetValue(name, out AudioClip clip))
                {
                    audioClipRefCount[name]++;
                    return clip;
                }
                else
                {
                    string filePath = AudioPathResolver.ResolvePath(name, baseDirectory, allowedExtensions);
                    if (filePath == null)
                    {
                        throw new FileNotFoundException($"Audio file for '{name}' not found in '{baseDirectory}' or its subdirectories with allowed extensions.");
                    }

                    clip = new AudioClip(filePath, loadMode, this);
                    audioClips[name] = clip;
                    audioClipRefCount[name] = 1;
                    return clip;
                }
            }
        }

        public void ReleaseAudioClip(string name)
        {
            lock (cacheLock)
            {
                if (audioClips.TryGetValue(name, out AudioClip clip))
                {
                    audioClipRefCount[name]--;
                    if (audioClipRefCount[name] <= 0)
                    {
                        clip.Dispose();
                        audioClips.Remove(name);
                        audioClipRefCount.Remove(name);
                        Console.WriteLine($"AudioClip for '{name}' deleted.");
                    }
                }
                else
                {
                    Console.WriteLine($"Attempted to release AudioClip '{name}' which is not loaded.");
                }
            }
        }

        public bool TryAddAudioClip(string name, AudioLoadMode loadMode)
        {
            lock (cacheLock)
            {
                if (audioClips.ContainsKey(name))
                {
                    return false;
                }

                string filePath = AudioPathResolver.ResolvePath(name, baseDirectory, allowedExtensions);
                if (filePath == null)
                {
                    return false;
                }

                var clip = new AudioClip(filePath, loadMode, this);
                audioClips[name] = clip;
                audioClipRefCount[name] = 1;
                return true;
            }
        }

        public bool TryGetAudioClip(string name, out AudioClip clip, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
            lock (cacheLock)
            {
                if (audioClips.TryGetValue(name, out clip))
                {
                    audioClipRefCount[name]++;
                    return true;
                }
                else
                {
                    string filePath = AudioPathResolver.ResolvePath(name, baseDirectory, allowedExtensions);
                    if (filePath == null)
                    {
                        clip = null;
                        return false;
                    }

                    clip = new AudioClip(filePath, loadMode, this);
                    audioClips[name] = clip;
                    audioClipRefCount[name] = 1;
                    return true;
                }
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;

            lock (cacheLock)
            {
                foreach (var clip in audioClips.Values)
                {
                    clip.Dispose();
                }
                audioClips.Clear();
                audioClipRefCount.Clear();
            }

            isDisposed = true;
            Console.WriteLine("SoundManager disposed.");
        }

        public int LoadAudioClip(string name, out int sampleRate)
        {
            string filePath = AudioPathResolver.ResolvePath(name, baseDirectory, allowedExtensions);
            if (filePath == null)
            {
                throw new FileNotFoundException($"Audio file for '{name}' not found in '{baseDirectory}' or its subdirectories with allowed extensions.");
            }
            return SoundLoader.LoadSound(filePath, out sampleRate);
        }

        public void ReleaseAudioClipBuffer(string filename, int buffer)
        {
            AL.DeleteBuffer(buffer);
            CheckALError($"Releasing buffer for '{filename}'");
        }

        private void CheckALError(string operation)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                throw new InvalidOperationException($"OpenAL error during {operation}: {AL.GetErrorString(error)}");
            }
        }
    }
}
