
namespace Spacebox.Common.Animation
{
    public interface IAnimation
    {
        void Init();

        bool Update(float deltaTime);

        void Apply(Node3D node);

        void Reset();
    }
}
