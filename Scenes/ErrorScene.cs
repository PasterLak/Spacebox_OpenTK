﻿using OpenTK.Graphics.OpenGL4;
using Spacebox.Common.SceneManagment;
using System.Drawing;

namespace Spacebox.Scenes
{
    internal class ErrorScene : Scene
    {

        public override void LoadContent()
        {
           

            GL.ClearColor(Color.Red);

        }

        public override void Awake()
        {
           
        }
        public override void Start()
        {
            Console.Error.WriteLine("Error Scene!");
        }


        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

           
        }

        public override void OnGUI()
        {
           
        }

        public override void UnloadContent()
        {
           
        }

        public override void Update()
        {
          
        }
    }
}
