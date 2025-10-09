using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AniMorph
{
    internal class Tethering
    {


        // Multiplier for velocity
        private float _multiplier = -500;
        // Limit of rotation
        private float _maxAngle = 30f;
        // Natural frequency of bounce
        private float _frequency = 3f; // 4f;

        // 0 = undamped, 1 = critically damped
        private float _dampingR = 0.3f;

        private Vector3 _velocity;
        // Current rotation offset
        private Vector3 _position;
        // How much Z velocity influences bone
        private readonly float _influenceZ;
        // How much X velocity influences bone
        private readonly float _influenceX;


        internal Tethering(Transform centeredBone, Vector3 bonePosition)
        {
            var localBonePosition = centeredBone.InverseTransformPoint(bonePosition);
            var divider = Mathf.Abs(localBonePosition.x) + Mathf.Abs(localBonePosition.z);
            _influenceZ = divider == 0f ? (0f) : (localBonePosition.x / divider);
            //_yawInfluence = Mathf.Abs(_yawInfluence);
            _influenceX = 1f - Mathf.Abs(_influenceZ);
        }


        internal Vector3 GetTetheringOffset(Vector3 velocity, float deltaTime)
        {
            var targetEuler = new Vector3(
                -velocity.y,
                (velocity.x * _influenceX) + (-velocity.z * _influenceZ),

                //-velocity.z * _influenceZ,
                0f
                ) * _multiplier;

            targetEuler = Vector3.ClampMagnitude(targetEuler, _maxAngle);

            var result = DampedSpring(_position, targetEuler, ref _velocity, _frequency, _dampingR, deltaTime);
            _position = result;
            return result;
        }
        /// <summary>
        /// Spat out by GPT.
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
        //   (forward)
        //
        //        -Z
        //       |   
        // +X    |    -X
        // <—————|—————>
        //       |
        //       |
        //        +Z
        //
        // R  — together  + separate
        // L  — separate  + together
    }   
}
