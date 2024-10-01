﻿
using OpenTK.Mathematics;


namespace Spacebox.Common
{
    internal class Water : Model, ITransparent
    {
        public Water(Shader shader) : 
            base("Resources/Models/plane.obj", new Material(shader, 
                new Texture2D("Resources/Textures/water.png")))
        {

            Material.Tiling = new Vector2(40, 40);
            Material.Color = new Vector4(1,1,1,0.5f);
        }

        public void DrawTransparent(Camera camera)
        {
            Update();

            Draw(camera);
        }

        public void Update()
        {
            Material.Offset -= new Vector2(0.15f * Time.Delta, 0);
            Material.Shader.SetVector3("ambientColor", Lighting.AmbientColor);

            
        }
    }
}
