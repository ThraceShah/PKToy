using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Silk.NET.Maths;
using Viewer.IContract;
using static Viewer.Math.MathEx;

namespace Viewer.Graphic.Opengl
{

    public enum CameraMovement
    {
        FORWARD,
        BACKWARD,
        LEFT,
        RIGHT
    };

    public class Camera
    {
        // Default camera values
        const float YAW = -90.0f;
        const float PITCH = 0.0f;
        const float SPEED = 2.5f;
        const float SENSITIVITY = 0.1f;
        const float ZOOM = 60f;

        public KeyCode KeyCode = KeyCode.None;

        // camera Attributes
        public Vector3 Position;
        public Vector3 Front;
        public Vector3 Up;
        public Vector3 Right;
        public Vector3 WorldUp;
        // euler Angles
        public float Yaw;
        public float Pitch;
        // camera options
        public float MovementSpeed;
        public float MouseSensitivity;
        public float Zoom;

        // constructor with vectors
        public Camera(Vector3 position = default, Vector3 up = default, float yaw = YAW, float pitch = PITCH)
        {
            Front = new Vector3(0.0f, 0.0f, -1.0f);
            MovementSpeed = SPEED;
            MouseSensitivity = SENSITIVITY;
            Zoom = ZOOM;
            Position = position;
            if (up == default)
            {
                WorldUp = Vector3.UnitY;
            }
            else
            {
                WorldUp = up;
            }
            Yaw = yaw;
            Pitch = pitch;
            UpdateCameraVectors();
        }
        // constructor with scalar values
        public Camera(float posX, float posY, float posZ, float upX, float upY, float upZ, float yaw, float pitch)
        {
            Front = new Vector3(0.0f, 0.0f, -1.0f);
            MovementSpeed = SPEED;
            MouseSensitivity = SENSITIVITY;
            Zoom = ZOOM;
            Position = new Vector3(posX, posY, posZ);
            WorldUp = new Vector3(upX, upY, upZ);
            Yaw = yaw;
            Pitch = pitch;
            UpdateCameraVectors();
        }

        // returns the view matrix calculated using Euler Angles and the LookAt Matrix
        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
        }

        public Matrix4x4 GetViewMatrix(in Vector3 center)
        {
            return Matrix4x4.CreateLookAt(Position, center - Position, Up);
        }


        // processes input received from any keyboard-like input system. Accepts input parameter in the form of camera defined ENUM (to abstract it from windowing systems)
        public void ProcessKeyboard(CameraMovement direction, float deltaTime)
        {
            float velocity = MovementSpeed * deltaTime;
            if (direction == CameraMovement.FORWARD)
                Position += Front * velocity;
            if (direction == CameraMovement.BACKWARD)
                Position -= Front * velocity;
            if (direction == CameraMovement.LEFT)
                Position -= Right * velocity;
            if (direction == CameraMovement.RIGHT)
                Position += Right * velocity;
        }

        public float MouseXOffset { get; set; } = 0;

        public float MouseYOffset { get; set; } = 0;

        public bool IsRotateX { get; set; } = true;


        // processes input received from a mouse input system. Expects the offset value in both the x and y direction.
        public void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true)
        {

            IsRotateX = xoffset > yoffset;

            MouseXOffset += xoffset * 0.8f;
            MouseYOffset += yoffset * 0.8f;

            xoffset *= MouseSensitivity;
            yoffset *= MouseSensitivity;

            Yaw += xoffset;
            Pitch += yoffset;

            // make sure that when pitch is out of bounds, screen doesn't get flipped
            if (constrainPitch)
            {
                if (Pitch > 89.0f)
                    Pitch = 89.0f;
                if (Pitch < -89.0f)
                    Pitch = -89.0f;
            }

            // update Front, Right and Up Vectors using the updated Euler angles
            UpdateCameraVectors();
        }

        // processes input received from a mouse scroll-wheel event. Only requires input on the vertical wheel-axis
        public void ProcessMouseScroll(float yoffset)
        {
            //opengl这里的取值范围和dx是不一样的,opengl不能小于0,但是dx可以,这和二者的投影算法不同有关
            Zoom -= (float)yoffset;
            if (Zoom <= -0.0f)
                Zoom = 0.001f;
            if (Zoom >= 180.0f)
                Zoom = 179.990f;
        }

        private void UpdateCameraVectors()
        {
            // calculate the new Front vector
            Vector3 front = Vector3.Zero;
            front.X = float.Cos(Radians(Yaw)) * float.Cos(Radians(Pitch));
            front.Y = float.Sin(Radians(Pitch));
            front.Z = float.Sin(Radians(Yaw)) * float.Cos(Radians(Pitch));
            Front = Vector3.Normalize(front);
            // also re-calculate the Right and Up vector
            Right = Vector3.Normalize(Vector3.Cross(Front, WorldUp));  // normalize the vectors, because their length gets closer to 0 the more you look up or down which results in slower movement.
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }

    }
}
