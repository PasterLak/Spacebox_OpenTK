using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Player
{
    public class ImpulseSphere : IPoolable<ImpulseSphere>
    {
        private SphereRenderer _sphereRenderer;
        private float _alpha;
        private bool _isActive;
        private const float EXPANSION_SPEED = 800f;
        private const float FADE_SPEED = 0.5f;
        private const float MAX_SCALE = 2000f;

        public Vector3 Position => _sphereRenderer.Position;

        public bool IsActive { get => _isActive; set => _isActive = value; }

        public ImpulseSphere()
        {
            _sphereRenderer = new SphereRenderer(Camera.Main.Position, 0.5f, 8, 8);
           
            _sphereRenderer.Color = new Color4(1, 1, 1, 0.1f);
            var texture = Engine.Resources.Get<Texture2D>("Resources/Textures/arSphere.png");
            texture.FlipY(); 
            texture.UpdateTexture(true);
            _sphereRenderer.Material = new TransparentMaterial(texture);
            _sphereRenderer.Scale = new Vector3(1, 1, 1);
            _sphereRenderer.Enabled = false;
            _alpha = 1f;
            _isActive = false;
        }

        public void Activate(Vector3 position)
        {
            _sphereRenderer.Position = position;
            _sphereRenderer.Scale = new Vector3(1, 1, 1);
            _sphereRenderer.Enabled = true;
            _alpha = 0.3f;
            _isActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!_isActive)
                return;

            Vector3 expansion = new Vector3(EXPANSION_SPEED, EXPANSION_SPEED, EXPANSION_SPEED) * deltaTime;
            _sphereRenderer.Scale += expansion;
            _alpha -= deltaTime * FADE_SPEED;
            if (_alpha < 0)
                _alpha = 0;
            _sphereRenderer.Color = new Color4(1, 1, 1, _alpha);

            if (_sphereRenderer.Scale.X > MAX_SCALE || _alpha == 0)
            {
                _isActive = false;
                _sphereRenderer.Enabled = false;
                _alpha = 1f;
                _sphereRenderer.Scale = new Vector3(1, 1, 1);
            }
        }

        public void Render()
        {
            if (_sphereRenderer.Enabled)
                _sphereRenderer.Render();
        }

        public ImpulseSphere CreateFromPool()
        {
            return new ImpulseSphere();
        }

        public void Reset()
        {
            _isActive = false;
            _sphereRenderer.Enabled = false;
            _alpha = 0;
        }
    }
}
