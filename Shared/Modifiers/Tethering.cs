using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KineticShift
{
    internal class Tethering
    {
        internal Tethering(Transform centeredBone, Vector3 bonePosition)
        {
            var localBonePosition = centeredBone.InverseTransformPoint(bonePosition);
            var divider = Mathf.Abs(localBonePosition.x) + Mathf.Abs(localBonePosition.z);
            _yawInfluence = divider == 0f ? (0f) : (Mathf.Abs(localBonePosition.x) / divider);
            _yawInfluence = Mathf.Abs(_yawInfluence);
        }


        // Multiplier for velocity
        private float _rotationMultiplier = 1000f;
        // Limit of rotation
        private float _maxRotationAngle = 30f;
        // How strongly it springs back
        private float _springStiffness = 5f;
        // How quickly it settles
        private float _damping = 2f;
        // Natural frequency of bounce
        private float _frequency = 4f;
        [Range(0f, 1f)]
        // 0 = undamped, 1 = critically damped
        private float _dampingRatio = 0.3f;
        // How responsive the spring is
        private float _response = 1f;

        private Vector3 _springVelocity;
        private Vector3 _tetherRotationEuler;
        // Current rotation offset
        private Vector3 _springPosition;

        private readonly float _yawInfluence;
     
        //private void Extra()
        //{

        //    // Create desired Euler rotation from inverted movement direction
        //    Vector3 desiredEuler = new Vector3(
        //        -localVelocity.z,                             // Pitch (X) from forward/back movement
        //        localVelocity.z * yawInfluence,              // Yaw (Y) from forward/back with offset influence
        //        localVelocity.x                              // Roll (Z) from side movement
        //    ) * rotationMultiplier;

        //    // Clamp total rotation magnitude
        //    desiredEuler = Vector3.ClampMagnitude(desiredEuler, maxRotationAngle);

        //    // Apply damped spring to each axis
        //    springPosition = DampedSpring(
        //        springPosition,
        //        desiredEuler,
        //        ref springVelocity,
        //        frequency,
        //        dampingRatio,
        //        deltaTime
        //    );

        //    // Apply rotation relative to initial local rotation
        //    Quaternion rotationOffset = Quaternion.Euler(springPosition);
        //    transform.localRotation = defaultLocalRotation * rotationOffset;
        //}
        internal Vector3 ApplySimpleTetheringEffect(Vector3 velocity, float deltaTime)
        {
            var targetEuler = new Vector3(-velocity.y, -velocity.x, 0f) * _rotationMultiplier;

            targetEuler = Vector3.ClampMagnitude(targetEuler, _maxRotationAngle);

            _tetherRotationEuler = Vector3.SmoothDamp(
                _tetherRotationEuler,
                targetEuler,
                ref _springVelocity,
                1f / _springStiffness,
                Mathf.Infinity,
                deltaTime
                );

            _springVelocity *= Mathf.Exp(-_damping * deltaTime);

            KS.Logger.LogDebug($"targetEuler({targetEuler.x:F2},{targetEuler.y:F2},{targetEuler.z:F2})");
            return targetEuler;
        }
        internal Vector3 ApplyAdvancedTetheringEffect(Vector3 velocity, float deltaTime)
        {
            var targetEuler = new Vector3(
                -velocity.y,
                -velocity.x + (-velocity.z * _yawInfluence),
                0f
                ) * _rotationMultiplier;

            targetEuler = Vector3.ClampMagnitude(targetEuler, _maxRotationAngle);

            _springPosition = DampedSpring(_springPosition, targetEuler, ref _springVelocity, _frequency, _dampingRatio, deltaTime);

            return _springPosition;
        }
        /// <summary>
        /// Damped harmonic oscillator for smooth overshoot & bounce behavior.
        /// Based on https://www.youtube.com/watch?v=KPoeNZZ6H4s
        /// </summary>
        private Vector3 DampedSpring(Vector3 current, Vector3 target, ref Vector3 velocity, float freq, float damp, float dt)
        {
            float omega = 2f * Mathf.PI * freq;
            float zeta = damp;
            float omegaZeta = omega * zeta;
            float omegaSq = omega * omega;

            Vector3 f = velocity + omegaZeta * (current - target);
            Vector3 a = -omegaSq * (current - target) - 2f * omegaZeta * f;

            velocity += a * dt;
            return current + velocity * dt;
        }
    }
}
