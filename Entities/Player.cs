﻿using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using System;

namespace Spacebox.Entities
{
    public class Player
    {
        public Camera Camera { get; private set; }
        public Transform Transform { get; private set; }
        public Collision Collision { get; private set; }

        private float _cameraSpeed = 1.5f;
        private float _sensitivity = 0.2f;
        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public Player(Vector3 position, float aspectRatio)
        {
            Transform = new Transform();
            Transform.Position = position;
            Camera = new Camera(position, aspectRatio);
            var boundingSphere = new BoundingSphere(position, 1);
            Collision = new DynamicBody(Transform, boundingSphere);
        }

        public void Update()
        {
            HandleInput();
            //Debug.DrawBoundingSphere((BoundingSphere)Collision.BoundingVolume, Color4.Green);
            Collision.UpdateBounding();

            Collision.DrawDebug();
        }

        private void HandleInput()
        {
            var mouse = Input.MouseState;

            if (Input.IsKey(Keys.W))
            {
                Camera.Position += Camera.Front * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.S))
            {
                Camera.Position -= Camera.Front * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.A))
            {
                Camera.Position -= Camera.Right * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.D))
            {
                Camera.Position += Camera.Right * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.Space))
            {
                Camera.Position += Camera.Up * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.LeftShift))
            {
                Camera.Position -= Camera.Up * _cameraSpeed * (float)Time.Delta;
            }

            Transform.Position = Camera.Position;
            Collision.BoundingVolume.Center = Camera.Position;

            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastMousePosition.X;
                var deltaY = mouse.Y - _lastMousePosition.Y;
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                Camera.Yaw += deltaX * _sensitivity;
                Camera.Pitch -= deltaY * _sensitivity;
            }
        }

        public void OnCollisionEnter(ICollidable other)
        {
            Console.WriteLine($"Camera collided with {other}");
        }

        public void OnCollisionExit(ICollidable other)
        {
            Console.WriteLine($"Camera stopped colliding with {other}");
        }
    }
}
