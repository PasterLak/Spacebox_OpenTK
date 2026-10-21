namespace Spacebox.Game.Player.Footsteps;

public enum FootstepGait : byte
{
    Crouch,
    Walk,
    Run
}

public sealed class FootstepCadence
{
    public const float MinSpeed = 0.4f;
    public const float FirstStepDistance = 0.3f;

    private float _distance;
    private bool _isMoving;

    public static float Stride(FootstepGait gait) => gait switch
    {
        FootstepGait.Crouch => 1.4f,
        FootstepGait.Run => 2.4f,
        _ => 2f
    };

    public bool Advance(float speed, float deltaTime, FootstepGait gait)
    {
        float stride = Stride(gait);
        if (speed < MinSpeed)
        {
            _isMoving = false;
            return false;
        }

        if (!_isMoving)
        {
            _isMoving = true;
            _distance = stride - FirstStepDistance;
        }

        _distance += speed * deltaTime;
        if (_distance < stride) return false;

        _distance %= stride;
        return true;
    }

    public void Reset()
    {
        _isMoving = false;
        _distance = 0f;
    }
}
