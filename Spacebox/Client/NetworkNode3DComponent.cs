using Client;
using Engine;
using Engine.Components;
using OpenTK.Mathematics;

namespace Spacebox.Client
{
    public class NetworkNode3DComponent : Component
    {
        public bool IsMine { get; private set; }
        public float SendRate { get; set; } = 0.1f;
        public float MoveThreshold { get; set; } = 0.01f;
        public float AngleThreshold { get; set; } = 0.5f;
        public float SmoothSpeed { get; set; } = 10f;

        private float _lastSendTime;
        private Vector3 _lastSentPosition;
        private Vector3 _lastSentRotation;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        public NetworkNode3DComponent(bool isMine)
        {
            IsMine = isMine;
        }

        public override void Start()
        {
            if (Owner == null) return;

            _lastSentPosition = Owner.Position;
            _lastSentRotation = Owner.Rotation;

            _targetPosition = Owner.Position;
            _targetRotation = Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(Owner.Rotation.X),
                MathHelper.DegreesToRadians(Owner.Rotation.Y),
                MathHelper.DegreesToRadians(Owner.Rotation.Z)
            );
        }

        public override void OnUpdate()
        {
            if (Owner == null) return;
            if(ClientNetwork.Instance==null) return;

            if (IsMine)
            {
                HandleSending();
            }
            else
            {
                HandleInterpolation();
            }
        }

        private void HandleSending()
        {
            if ((float)Time.Total - _lastSendTime < SendRate) return;

            bool hasMoved = Vector3.DistanceSquared(Owner.Position, _lastSentPosition) > (MoveThreshold * MoveThreshold);
            bool hasRotated = Vector3.DistanceSquared(Owner.Rotation, _lastSentRotation) > (AngleThreshold * AngleThreshold);

            if (hasMoved || hasRotated)
            {
                _lastSendTime = (float)Time.Total;
                _lastSentPosition = Owner.Position;
                _lastSentRotation = Owner.Rotation;

                var rotQuat = Quaternion.FromEulerAngles(
                    MathHelper.DegreesToRadians(Owner.Rotation.X),
                    MathHelper.DegreesToRadians(Owner.Rotation.Y),
                    MathHelper.DegreesToRadians(Owner.Rotation.Z)
                );

                ClientNetwork.Instance.SendPosition(Owner.Position, rotQuat);
            }
        }

        private void HandleInterpolation()
        {
            if (Vector3.DistanceSquared(Owner.Position, _targetPosition) > 0.0001f)
            {
                Owner.Position = Vector3.Lerp(Owner.Position, _targetPosition, (float)Time.Delta * SmoothSpeed);
            }

            var currentQuat = Quaternion.FromEulerAngles(
                 MathHelper.DegreesToRadians(Owner.Rotation.X),
                 MathHelper.DegreesToRadians(Owner.Rotation.Y),
                 MathHelper.DegreesToRadians(Owner.Rotation.Z)
            );

            var newQuat = Quaternion.Slerp(currentQuat, _targetRotation, (float)Time.Delta * SmoothSpeed);
            Owner.Rotation = Node3D.QuaternionToEuler(newQuat);
        }

        public void ReceiveState(Vector3 newPos, Quaternion newRot)
        {
            _targetPosition = newPos;
            _targetRotation = newRot;
        }
    }
}
