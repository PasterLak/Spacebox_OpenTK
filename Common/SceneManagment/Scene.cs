﻿using OpenTK.Windowing.Desktop;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using Spacebox.Common.Audio;

namespace Spacebox.Common.SceneManagment
{
    public abstract class Scene : Node3D, IDisposable
    {
        protected GameWindow Content;
      
        public SceneManager SceneManager { get; private set; }


        public SoundManager SoundManager { get; private set; }
        public CollisionManager CollisionManager { get; private set; }
        public Renderer Renderer { get; private set; }


        public Scene()
        {
            SoundManager = new SoundManager();
            CollisionManager = new CollisionManager();
            Renderer = new Renderer();

            Name = "DefaultName";
            Resizable = false;
        }

        public abstract void LoadContent();

        public abstract void UnloadContent();

        public virtual void Awake() { }
        public virtual void Start() { }

        public abstract void Update();

        public virtual void LateUpdate() { }

        public abstract void Render();

        public abstract void OnGUI();

        public void Dispose()
        {
            SoundManager.Dispose();
            
        }

      
    }
}
