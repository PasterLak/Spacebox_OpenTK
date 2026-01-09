using System;
using Engine;
using Spacebox.Game.GUI;

using OpenTK.Mathematics;


namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Astronaut
    {
        public SpaceNetwork.Player NetworkData { get; private set; }

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Quaternion _currentRotation = Quaternion.Identity;

        private GUI.Tag _nameTag;


        public RemoteAstronaut(SpaceNetwork.Player player) : base(player.Position.ToOpenTKVector3(), false)
        {
            NetworkData = player;
            _targetPosition = player.Position.ToOpenTKVector3();
            _targetRotation = new Quaternion(player.Rotation.X, player.Rotation.Y, player.Rotation.Z, player.Rotation.W);

            Position = _targetPosition;
            _currentRotation = _targetRotation;

        }


        public void CreatePlayerVisuals(string color)
        {
            _nameTag = TagManager.Instance.CreateTag($"[{NetworkData.ID}]{NetworkData.Name}", Position, new Color4(NetworkData.Color.X, NetworkData.Color.Y, NetworkData.Color.Z, 1));
            _nameTag.TextAlignment = GUI.Tag.Alignment.Center;

            Name = $"RemoteAstronaut_{NetworkData.ID}";

            CreateModel(NetworkData.ID, color);
            Flashlight.Enabled = true;

            //var uvIndex = GameAssets.AtlasItems.GetUVIndexByName("drill1");
           // _itemModel = ItemModelGenerator.GenerateModelFromAtlas(GameAssets.ItemsTexture, GameAssets.EmissionItems, uvIndex.X, uvIndex.Y, 0.1f, 300f / 500f * 2f, false, false);
           // _itemModel.UseMainCamera = true;
        }

        public void UpdateNetworkData(SpaceNetwork.Player updatedPlayer)
        {
            NetworkData = updatedPlayer;

            _targetPosition = updatedPlayer.Position.ToOpenTKVector3();
            _targetRotation = new Quaternion(updatedPlayer.Rotation.X, updatedPlayer.Rotation.Y, updatedPlayer.Rotation.Z, updatedPlayer.Rotation.W);

            if (_nameTag != null)
            {
                _nameTag.Text = $"[{NetworkData.ID}]{NetworkData.Name}";
                _nameTag.Color = new Color4(NetworkData.Color.X, NetworkData.Color.Y, NetworkData.Color.Z, 1);
            }
        }

        public override void Update()
        {
            Position = Vector3.Lerp(Position, _targetPosition, Time.Delta * 10f);
            _currentRotation = Quaternion.Slerp(_currentRotation, _targetRotation, Time.Delta * 10f);

            Rotation = Node3D.QuaternionToEuler(_currentRotation);

            if (_nameTag != null)
            {
                var up = Vector3.Transform(Vector3.UnitY, _currentRotation);
                _nameTag.WorldPosition = Position + up * 1.8f;
            }

            base.Update();
        }

        public void OnDisconnect()
        {
            if (_nameTag != null)
            {
                TagManager.Instance.ReleaseTag(_nameTag);
                _nameTag = null;
            }
        }

        protected override void UpdateVisuals()
        {
            bool showModel = true;
            if (AstBody != null) AstBody.Enabled = showModel;
            if (AstHelmet != null) AstHelmet.Enabled = showModel;
            if (AstTank != null) AstTank.Enabled = showModel;
        }
    }
}