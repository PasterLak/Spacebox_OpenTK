using OpenTK.Mathematics;
using Spacebox.Entities;
using Spacebox.Scenes;

namespace Spacebox.Common
{
    public abstract class Light
    {
        public Vector3 Ambient { get; set; } = new Vector3(0.2f);
        public Vector3 Diffuse { get; set; } = new Vector3(0.5f);
        public Vector3 Specular { get; set; } = new Vector3(1.0f);
    }

    public class DirectionalLight : Light
    {
        public Vector3 Direction { get; set; } = new Vector3(-0.2f, -1.0f, -0.3f);
    }

    public class PointLight : Light
    {
        public Vector3 Position { get; set; }
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;
        
        private Shader shader;
        public PointLight(Vector3 position)
        {
            Position = position;
            
        }

        public void Render(Shader shader, Camera camera)
        {
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());

            shader.SetVector3("viewPos", camera.Position);
/*
            shader.SetVector3($"pointLights[{j}].position", Position);
            shader.SetVector3($"pointLights[{j}].ambient", new Vector3(0, 0, 1));
            shader.SetVector3($"pointLights[{j}].diffuse", new Vector3(0.8f, 0.8f, 0.8f));
            shader.SetVector3($"pointLights[{j}].specular", new Vector3(1.0f, 1.0f, 1.0f));
            shader.SetFloat($"pointLights[{j}].constant", 1.0f);
            shader.SetFloat($"pointLights[{j}].linear", 0.09f);
            shader.SetFloat($"pointLights[{j}].quadratic", 0.032f);*/
        }
    }

    public class SpotLight : Light
    {
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public float CutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(12.5f));
        public float OuterCutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(17.5f));
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        public SpotLight(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}
