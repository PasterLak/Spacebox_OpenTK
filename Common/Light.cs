using OpenTK.Mathematics;
using Spacebox.Entities;


namespace Spacebox.Common
{
    public abstract class Light : Transform, INotTransparent
    {
        public Vector3 Ambient { get; set; } = new Vector3(0.2f);
        public Vector3 Diffuse { get; set; } = new Vector3(0.5f);
        public Vector3 Specular { get; set; } = new Vector3(1.0f);

        public Shader Shader { get; set; }

        public Light(Shader shader)
        {
            Shader = shader;
        }

        public virtual void Draw(Camera camera)
        {
            
            if (Shader == null) return;
            Shader.Use();

            Shader.SetMatrix4("view", camera.GetViewMatrix());
            Shader.SetMatrix4("projection", camera.GetProjectionMatrix());
          
            Shader.SetVector3("viewPos", camera.Position);
        }
    }

    public class DirectionalLight : Light
    {
        public Vector3 Direction { get; set; } = new Vector3(-0.2f, -1.0f, -0.3f);

        public DirectionalLight(Shader shader) : base(shader)
        {
           
        }
        public DirectionalLight(Shader shader, Vector3 direction) : base(shader)
        {
            Direction = direction;  
        }

        public override void Draw(Camera camera)
        {
            base.Draw(camera);


            Shader.SetVector3("dirLight.direction", new Vector3(-0.2f, -1.0f, -0.3f));
            Shader.SetVector3("dirLight.ambient", Lighting.AmbientColor); // new Vector3(0.05f, 0.05f, 0.05f)
            Shader.SetVector3("dirLight.diffuse", new Vector3(0.4f, 0.4f, 0.4f));
            Shader.SetVector3("dirLight.specular", new Vector3(0.5f, 0.5f, 0.5f));

            Shader.SetFloat("spotLight.constant", 1.0f);
            Shader.SetFloat("spotLight.linear", 0.09f);
            Shader.SetFloat("spotLight.quadratic", 0.032f);
            Shader.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
            Shader.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)));
        }
    }

    public class PointLight : Light
    {
        public Vector3 Position { get; set; }
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;


        public PointLight(Shader shader, Vector3 position) : base(shader)
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
       
        public bool IsActive = true;
        public Vector3 Direction { get; set; }
        public float CutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(12.5f));
        public float OuterCutOff { get; set; } = MathF.Cos(MathHelper.DegreesToRadians(17.5f));
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        public SpotLight(Shader shader, Vector3 direction) : base(null)
        {
            Shader = shader;
            Direction = direction;
        }

        public override void Draw(Camera camera)
        {
            base.Draw(camera);

            Shader.SetVector3("spotLight.position", camera.Position);
            Shader.SetVector3("spotLight.direction", camera.Front);

            if(!IsActive)
            {
                Shader.SetVector3("spotLight.ambient", new Vector3(0.0f, 0.0f, 0.0f));
                Shader.SetVector3("spotLight.diffuse", new Vector3(0f, 0f, 0f));
                Shader.SetVector3("spotLight.specular", new Vector3(0f, 0f, 0f));
            }
            else
            {
                Shader.SetVector3("spotLight.ambient", new Vector3(0.0f, 0.0f, 0.0f));
                Shader.SetVector3("spotLight.diffuse", new Vector3(1.0f, 1.0f, 1.0f));
                Shader.SetVector3("spotLight.specular", new Vector3(1.0f, 1.0f, 1.0f));
            }

            Shader.SetFloat("spotLight.constant", 1.0f);
            Shader.SetFloat("spotLight.linear", 0.09f);
            Shader.SetFloat("spotLight.quadratic", 0.032f);
            Shader.SetFloat("spotLight.cutOff", CutOff);
            Shader.SetFloat("spotLight.outerCutOff", OuterCutOff);

        }


    }
}
