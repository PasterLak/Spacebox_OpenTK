using Engine.Audio;

namespace Engine.Components
{
    public class BackgroundMusicComponent : Component
    {
        public bool PlayOnStart { get; set; } = true;
        public AudioSource Audio { get; private set; }
        public BackgroundMusicComponent(string path) 
        {
            var clip = Resources.Load<AudioClip>(path);
            Audio = new AudioSource(clip);
            Audio.IsLooped = true;
        }

        public BackgroundMusicComponent(AudioClip clip)
        {
        
            Audio = new AudioSource(clip);
            Audio.IsLooped = true;
        }

        public override void Start()
        {
            base.Start();

            if(PlayOnStart)
            {
                Audio.Play();
            }
          
        }

        public override void OnDetached()
        {
            base.OnDetached();
            Audio.Stop();
            Audio.Dispose();

        }
    }
}
