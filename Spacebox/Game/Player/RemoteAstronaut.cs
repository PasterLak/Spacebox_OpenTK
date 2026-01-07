using System;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Astronaut
    {
        private SpaceNetwork.Player _playerData;
        public Vector3 LatestPosition { get; set; }
        public Quaternion LatestRotation { get; set; }

        private GUI.Tag _nameTag;
        private Quaternion _currentRotation = Quaternion.Identity;
        private ItemModel _itemModel;

        public RemoteAstronaut(SpaceNetwork.Player player) : base(player.Position.ToOpenTKVector3(), false)
        {
            _playerData = player;
            LatestPosition = player.Position.ToOpenTKVector3();
            //CameraActive = false;

            CreateModel(player.ID);

            _nameTag = TagManager.Instance.CreateTag($"[{_playerData.ID}]{_playerData.Name}", LatestPosition, new Color4(_playerData.Color.X, _playerData.Color.Y, _playerData.Color.Z, 1));
            _nameTag.TextAlignment = GUI.Tag.Alignment.Center;

            var uvIndex = GameAssets.AtlasItems.GetUVIndexByName("drill1");
            _itemModel = ItemModelGenerator.GenerateModelFromAtlas(GameAssets.ItemsTexture, GameAssets.EmissionItems, uvIndex.X, uvIndex.Y, 0.1f, 300f / 500f * 2f, false, false);
            _itemModel.UseMainCamera = true;

            Name = "RemoteAstronaut";
        }

        public override void Update()
        {
            Position = Vector3.Lerp(Position, LatestPosition, Time.Delta * 5f);
            _currentRotation = Quaternion.Slerp(_currentRotation, LatestRotation, Time.Delta * 5f);
            Rotation = Node3D.QuaternionToEuler(_currentRotation);

            var up = Vector3.Transform(Vector3.UnitY, _currentRotation);

            if (_nameTag != null)
            {
                _nameTag.WorldPosition = Position + up * 1f;
            }

            base.Update();
        }

        public void OnDisconnect()
        {
            if (_nameTag != null)
            {
                TagManager.Instance.ReleaseTag(_nameTag);
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