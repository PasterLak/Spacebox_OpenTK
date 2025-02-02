using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Node3D
    {
        public string PlayerName { get; set; } = "Remote";
        public Vector3 LatestPosition { get; set; }
        public Vector3 LatestRotation { get; set; }

        public void UpdateRemote(float deltaTime)
        {
            Position = Vector3.Lerp(Position, LatestPosition, deltaTime * 5f);
            Rotation = Vector3.Lerp(Rotation, LatestRotation, deltaTime * 5f);
        }

        public void DrawRemote()
        {
            // Здесь можно отрисовать упрощённую модель удалённого игрока
            // Например, вызвать ModelRenderer.DrawModel(this);
        }
    }
}
