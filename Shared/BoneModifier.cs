using ADV.Commands.Base;
using ADV.Commands.Object;
using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KineticShift
{
    internal class BoneModifier
    {
        protected readonly Transform Bone;
        protected readonly float _boneMass;

        // Height / Width ratio
        private readonly float _height;
        // Width / Height ratio
        private readonly float _width;


        private float _posSpring = 300f;
        private float _posDrag = -5f;   
        

        private float _rotSpring = 10f;
        // Resistance for angular rotation
        private float _rotDrag = -2f;

        private float _scaleScalar = 1f;
        // Max scale along velocity axis
        private float _maxStretchScale = 1.5f;
        // Min scale on perpendicular axes
        private float _minSquashScale = 0.5f;
        // Smoothing factor for scale change
        private float _scaleSmoothing = 10f;
        // How strong the deceleration squash is
        private float _squashIntensity = 1.0f;
        // How quickly the squash effect fades
        private float _squashDecaySpeed = 5f;
        private Vector3 _baseScale;
        private readonly float _baseScaleMagnitude;
        // Momentum reversal squash offset
        private Vector3 _squashOffset;
        // Movement speed
        private Vector3 _velocity;
        // Movement direction with magnitude 1f
        private Vector3 _velocityNormalized;
        // Snapshots of previous frame
        private Vector3 _prevVelocity;
        private Vector3 _prevVelocityNormalized;
        private Vector3 _prevAngularVelocity;
        private Vector3 _prevPosition;
        private Vector3 _prevScale;
        // Pseudo previous as we use dead reckoning and have no way to know,
        // Synchronized periodically when animator changes states.
        private Quaternion _prevRotation;

        //private float _velocity;
        //private float _kineticEnergy;
        //private float _damping 
        //private float _dampingLimit = 0.1f;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="halfBoneFatMass">Higher value — higher offset at high velocities.</param>
        internal BoneModifier(Transform bone, SkinnedMeshRenderer skinnedMeshRenderer, float halfBoneFatMass)
        {
            if (bone == null || skinnedMeshRenderer == null || halfBoneFatMass <= 0f)
            {
                KS.Logger.LogError($"{this.GetType().Name} wasn't initialized due to wrong argument");
                return;
            }

            Bone = bone;
            _prevPosition = bone.position;
            _prevRotation = bone.rotation;
            _boneMass = halfBoneFatMass;
            _baseScale = bone.localScale;
            _baseScaleMagnitude = _baseScale.magnitude;
            var bakerMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakerMesh);

            var vertices = bakerMesh.vertices;
            var triangles = bakerMesh.triangles;
            var t = skinnedMeshRenderer.transform;
            Ray[] rays = [
                new Ray(bone.position, bone.position + bone.forward), 
                new Ray(bone.position, bone.position - bone.forward), 
                new Ray(bone.position, bone.position - bone.right), 
                new Ray(bone.position, bone.position + bone.right)
                ];

            float[] closestDist = [Mathf.Infinity, Mathf.Infinity, Mathf.Infinity, Mathf.Infinity];

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var v0 = t.TransformPoint(vertices[triangles[i]]);
                var v1 = t.TransformPoint(vertices[triangles[i + 1]]);
                var v2 = t.TransformPoint(vertices[triangles[i + 2]]);

                for (var j = 0; j < rays.Length; j++)
                {
                    var ray = rays[j];
                    if (IntersectRayTriangle(ray, v0, v1, v2, out var hitPoint))
                    {
                        var dist = Vector3.Distance(ray.origin, hitPoint);
                        if (dist < closestDist[j])
                        {
                            closestDist[j] = dist;
                        }
                    }
                }
            }
            for (var i = 0; i < closestDist.Length; i++)
            {
                if (closestDist[i] == Mathf.Infinity)
                {
                    KS.Logger.LogError($"{this.GetType().Name} couldn't find the intersection point[{i}].");
                }
            }
            _height = closestDist[0] + closestDist[1];
            _width = closestDist[2] + closestDist[3];

            static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hitPoint)
            {
                hitPoint = Vector3.zero;
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;

                Vector3 h = Vector3.Cross(ray.direction, edge2);
                float a = Vector3.Dot(edge1, h);
                if (Mathf.Abs(a) < Mathf.Epsilon)
                    return false; // Ray is parallel to triangle

                float f = 1f / a;
                Vector3 s = ray.origin - v0;
                float u = f * Vector3.Dot(s, h);
                if (u < 0f || u > 1f)
                    return false;

                Vector3 q = Vector3.Cross(s, edge1);
                float v = f * Vector3.Dot(ray.direction, q);
                if (v < 0f || u + v > 1f)
                    return false;

                // At this stage we can compute t to find out where the intersection point is on the line
                float t = f * Vector3.Dot(edge2, q);
                if (t >= 0f) // ray intersection
                {
                    hitPoint = ray.origin + ray.direction * t;
                    return true;
                }

                return false; // Line intersection but not a ray intersection
            }
        }

        internal virtual void UpdateModifiers(float deltaTime, out Vector3 positionModifier, out Vector3 rotationModifier, out Vector3 scaleModifier)
        {
            // Position calculation
            var currentPosition = Bone.position;
            var deltaVecLocal = Bone.InverseTransformPoint(_prevPosition);
            //deltaVector = Bone.TransformVector(deltaVector);

            var velocity = _velocity;
            var springForce = _posSpring * deltaVecLocal;
            var dampingForce = _posDrag * velocity;
            var force = (springForce + dampingForce) * deltaTime;

            velocity = (velocity + force) * deltaTime;
            //positionModifier = deltaVector - velocity * delta;
            //var newPosition = _prevPosition + velocity; // - targetPosition);
            positionModifier = deltaVecLocal - velocity;  //Bone.TransformVector(newPosition - currentPosition);


            // Rotation calculation
            var currentRot = Bone.rotation;
            var prevRot = _prevRotation;
            var deltaRot = currentRot * Quaternion.Inverse(prevRot);

            //var targetRot = currentRot;

            deltaRot.ToAngleAxis(out var angle, out var axis);

            // Due to (probably) the type being float we wont arrive at the target with this method,
            // and instead will get stuck in tiny numbers.
            // To avoid micro glitches/restless behavior we settle down with 0.1f. Epsilon is too big.
            //if (angle > 0.1f)
            //{
                if (angle > 180f) angle -= 360f;

                var torque = axis * (angle * Mathf.Deg2Rad * _rotSpring);

                var angularVelocity = _prevAngularVelocity;
                var dampingTorque = _rotDrag * angularVelocity;
                torque += dampingTorque;

                angularVelocity += torque * deltaTime;
                
                var targetRot = Quaternion.Euler(angularVelocity * (Mathf.Rad2Deg * deltaTime)) * prevRot;
                rotationModifier = (currentRot * Quaternion.Inverse(targetRot)).eulerAngles;

            scaleModifier = _baseScale;
            KS.Logger.LogDebug($"rotMod{rotationModifier} deltaRot{deltaRot.eulerAngles} prevRot{_prevRotation.eulerAngles} angularVelocity{angularVelocity}");
            //}

            //ApplyScaleModification(velocity, deltaTime, out scaleModifier);

            // Using actual position from this LateUpdate before IK has updated
            // Will be problematic when some other bone modifier nearby is in play.
            _prevRotation = targetRot;
            _prevPosition = Bone.TransformPoint(positionModifier);
            _prevVelocity = velocity;
            _prevAngularVelocity = angularVelocity;
        }


        private void ApplyScaleModification(Vector3 velocity, float deltaTime, out Vector3 scaleModifier)
        {
            // Avoid zero vector normalization
            if (velocity == Vector3.zero)
            {
                scaleModifier = Vector3.one;
                return;
            }

            // Apply Deceleration aka MomentumReversal squash
            var bigDelta = _scaleScalar * (1f / deltaTime);

            var acceleration = (velocity - _prevVelocity) / deltaTime;
            var velocityNormalized = velocity.normalized;
            var squashOffset = _squashOffset;

            // Reverse projected acceleration
            var deceleration = -Vector3.Project(acceleration, velocityNormalized);
            // Extract scalar projection (not a cosine)
            var decelMagnitude = Vector3.Dot(deceleration, velocityNormalized);

            var boneRight = Bone.right;
            var boneUp = Bone.up;
            var boneForward = Bone.forward;

            // If strong deceleration
            if (decelMagnitude > 0f) // && velocity.magnitude < prevVelocity.magnitude)
            {
                var dir = _prevVelocityNormalized;
                // Small magnitude influence
                var magnitudeMultiplier = _squashIntensity * decelMagnitude; // * (1f / 10f);
                // Compress along movement axes
                var squash = new Vector3(
                    Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(dir, boneRight)) * magnitudeMultiplier),
                    Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(dir, boneUp)) * magnitudeMultiplier),
                    Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(dir, boneForward)) * magnitudeMultiplier)
                );

                // Invert squash to create perpendicular expansion
                var avg = (squash.x + squash.y + squash.z) * (1f / 3f);

                var deltaX = 1f - squash.x;
                var deltaY = 1f - squash.y;
                var deltaZ = 1f - squash.z;

                var inverseSquash = new Vector3(
                    squash.x + (deltaX) * 0.5f + deltaY * deltaZ,
                    squash.y + (deltaY) * 0.5f + deltaX * deltaZ,
                    squash.z + (deltaZ) * 0.5f + deltaX * deltaY
                );
                // The smaller the average squash, the more inverse squash
                squashOffset = Vector3.Lerp(Vector3.one, inverseSquash, 1f - avg);

            }
            // Smoothly return squash offset to neutral (1,1,1)
            squashOffset = Vector3.Lerp(squashOffset, Vector3.one, deltaTime * _squashDecaySpeed);

            // Get normalized velocity direction
            //var dir = velocity.normalized;

            var speed = velocity.magnitude * bigDelta;

            // Calculate stretch scale (bigger with speed)
            var stretchAmount = Mathf.Clamp(1f + speed, 1f, _maxStretchScale);

            // Perpendicular squash (inverse of stretch to preserve volume feel)
            var squashAmount = Mathf.Clamp(1f - (stretchAmount - 1f) * 0.5f, _minSquashScale, 1f);


            var xStretch = Mathf.Abs(Vector3.Dot(velocityNormalized, boneRight));
            var yStretch = Mathf.Abs(Vector3.Dot(velocityNormalized, boneUp));
            var zStretch = Mathf.Abs(Vector3.Dot(velocityNormalized, boneForward));

            var baseScale = _baseScale;

            // Compute new scale along local axes
            var newScale = new Vector3(
                baseScale.x * (1f + (stretchAmount - 1f) * xStretch),
                baseScale.y * (1f + (stretchAmount - 1f) * yStretch),
                baseScale.z * (1f + (stretchAmount - 1f) * zStretch)
                );

            KS.Logger.LogDebug($"stretchAmount[{stretchAmount:F2}] squashAmount[{squashAmount:F2}] " +
                $"xStretch({xStretch:F2},{yStretch:F2},{zStretch:F2}) " +
                $"newScale({newScale.x:F2},{newScale.y:F2},{newScale.z:F2}) "
                );
            // Normalize to squash perpendicular axes
            //var stretchRatio = newScale.magnitude / _baseScaleMagnitude;
            newScale = new Vector3(
                Mathf.Lerp(newScale.x, baseScale.x * squashAmount, 1f - xStretch),
                Mathf.Lerp(newScale.y, baseScale.y * squashAmount, 1f - yStretch),
                Mathf.Lerp(newScale.z, baseScale.z * squashAmount, 1f - zStretch)
            );

            newScale = Vector3.Scale(newScale, squashOffset);
            // Smoothly interpolate to new scale
            scaleModifier = Vector3.Lerp(_prevScale, newScale, deltaTime * _scaleSmoothing);
            scaleModifier.z = 1f;

            //KS.Logger.LogDebug($"stretchAmount[{stretchAmount:F2}] squashAmount[{squashAmount:F2}] " +
            //    $"xStretch({xStretch:F2},{yStretch:F2},{zStretch:F2}) " +
            //    $"newScale({newScale.x:F2},{newScale.y:F2},{newScale.z:F2}) " +
            //    $"scaleModifier({scaleModifier.x:F2},{scaleModifier.y:F2},{scaleModifier.z:F2}) " +
            //    $"velocity({velocity.x:F2},{velocity.y:F2},{velocity.z:F2})");
            // Store local variables
            _prevVelocityNormalized = velocityNormalized;
            _prevScale = scaleModifier;
            _squashOffset = squashOffset;
        }

        //private void UpdateStretchScale(Vector3 velocity, Vector3 velocityNormalized, out Vector3 scaleModifier)
        //{
        //    // Don't stretch if not moving
        //    //if (velocity.sqrMagnitude < 0.001f)
        //    //{
        //    //scaleModifier = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * scaleSmoothing);

            //    //}


            //}

            /// <summary>
            /// Mathf.SmoothStep but limited to 0..1f.
            /// </summary>
        protected float CheapStep(float t) => t * t * (3 - 2 * t);

        /// <summary>
        /// Mathf.SmoothStep but limited to 0..1f and done through cosine.
        /// </summary>
        protected float NeatStep(float t) => 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

        protected float EaseIn(float t) => t * t * (2f - t);
    }
}
