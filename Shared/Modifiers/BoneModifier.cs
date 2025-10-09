using ADV.Commands.Base;
using ADV.Commands.Object;
using KKABMX.Core;
using MessagePack.Decoders;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI.CoroutineTween;
using static RootMotion.FinalIK.IKSolver;

namespace AniMorph
{
    internal class BoneModifier
    {


        protected readonly Transform Bone;

        protected readonly Tethering Tethering;

        // Height / Width ratio
        private readonly float _height;
        // Width / Height ratio
        private readonly float _width;
                

        private float _maxRotationAngle = 45f;

        // Max scale along velocity axis
        private float _scaleMaxStretch = 0.4f;
        //// Min scale on perpendicular axes
        //private float _minSquashScale = 0.67f;

        protected bool _isLeftPosition;

        // localScale.x * localScale.y * localScale.z
        private float _originalVolume;
        private readonly float _baseScaleMagnitude;

        // Snapshots of previous frame
        protected Vector3 _prevVelocity;
        protected Vector3 _prevAngularVelocity;
        protected Vector3 _prevPosition;
        protected Vector3 _prevScale;


        // Linear variables and properties

        //public float restLength = 0f;
        private float _linearSpringStrength = 25f;
        private float _linearDamping = 10f;
        private bool _linearGravity;
        private Vector3 _linearGravityForce = new(0f, 0f, 0f);
        private float _massMultiplier = 1f;
        private float _mass = 1f;
        private float _maxVelocity = 1f;
        private float _maxSqrVelocity = 1f;

        private float GetMassMultiplier
        {
            get
            {
                return _mass;
            }
            set
            {
                _massMultiplier = 1f / value;
            }
        }
        private void SetMaxVelocity(float value)
        {
            _maxVelocity = value;
            _maxSqrVelocity = value * value;
        }


        // Angular variables and properties


        private float _angularSpringStrength = 30f;
        private float _angularDamping = 5f;


        // Scale variables and properties


        // How much the scale stretches along velocity direction.
        public float _scaleAccelerationFactor = 20f; //0.01f;
        // How much to squash along deceleration axis
        public float _scaleDecelerationFactor = 0.01f;
        // How fast squash reacts
        public float _scaleLerpSpeed = 10f;
        // Max squash on deceleration
        public float _scaleMaxDistortion = 0.4f;


        // Gravity variables and properties


        private Vector3 _dotUpUp = Vector3.zero;
        private Vector3 _dotUpMiddle = new Vector3(0f, 0.03f, 0f);
        private Vector3 _dotUpDown = new Vector3(0f, 0.05f, 0f);

        private Vector3 _dotFwdUp;
        private Vector3 _dotFwdMiddle;
        private Vector3 _dotFwdDown;

        private Vector3 _dotRUp = new Vector3(-0.025f, -0.02f, 0f);
        private Vector3 _dotRMiddle;
        private Vector3 _dotRDown = new Vector3(0.025f, -0.02f, 0f);


        // Pseudo previous as we use dead reckoning and have no way to know,
        // Synchronized periodically when animator changes states.
        private Quaternion _prevRotation;
        protected readonly BoneModifierData BoneModifierData;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="centeredBone">A centered bone with normal orientation on the body. Required for setup only.</param>
        /// <param name="bone">Bone that will be modified.</param>
        /// <param name="bakedMesh">Baked skinned mesh</param>
        /// <param name="skinnedMesh"></param>
        internal BoneModifier(Transform bone, Transform centeredBone, Mesh bakedMesh, SkinnedMeshRenderer skinnedMesh, BoneModifierData boneModifierData)
        {
            if (bone == null)
            {
                AniMorph.Logger.LogError($"{this.GetType().Name} wasn't initialized due to wrong argument");
                return;
            }

            Bone = bone;
            BoneModifierData = boneModifierData;
            _prevPosition = bone.position;
            _prevRotation = bone.rotation;
            _originalVolume = bone.localScale.x * bone.localScale.y * bone.localScale.z;
            _baseScaleMagnitude = bone.localScale.magnitude;

            if (centeredBone != null)
            {
                Tethering = new Tethering(centeredBone, _prevPosition);

                var localBonePosition = centeredBone.InverseTransformPoint(bone.position);
                var divider = Mathf.Abs(localBonePosition.x) + Mathf.Abs(localBonePosition.z);
                var result = divider == 0f ? (0f) : (localBonePosition.x / divider);
                _isLeftPosition = result < 0f;
            }

            // Skip mesh measurements
            if (bakedMesh == null || skinnedMesh == null) return;

            var vertices = bakedMesh.vertices;
            var triangles = bakedMesh.triangles;
            var t = skinnedMesh.transform;
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
                    AniMorph.Logger.LogError($"{this.GetType().Name} couldn't find the intersection point[{i}].");
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

        /// <summary>
        /// Meant for access from BoneEffector.
        /// </summary>
        internal virtual void UpdateModifiers(float deltaTime, float unscaledDeltaTime)
        {
            var boneModifierData = BoneModifierData;
            boneModifierData.PositionModifier = GetLinearOffset(unscaledDeltaTime, out var velocity, out var velocityMagnitude);
           
            var rotMod = GetAngularOffset(unscaledDeltaTime);

            boneModifierData.RotationModifier = rotMod + Tethering.GetTetheringOffset(velocity, deltaTime);

            //boneModifierData.ScaleModifier = GetScaleDistortion(velocity, velocityMagnitude, deltaTime);
            StoreVariables(velocity);
        }


        // Implementation with Hooke's Law 
        protected Vector3 GetLinearOffset(float deltaTime, out Vector3 velocity, out float velocityMagnitude)
        {
            velocity = _prevVelocity;

            var localDelta = Bone.InverseTransformPoint(_prevPosition);

            _prevPosition = Bone.position;

            var localDeltaMagnitude = localDelta.magnitude;

            // Normalized displacement, avoid division by zero
            var direction = (localDeltaMagnitude == 0f) ? Vector3.zero : (localDelta * (1f / localDeltaMagnitude)); //  displacement.normalized;

            //var stretch = currentLength - restLength;
            // F_spring = -k * x
            var springForce = -_linearSpringStrength * localDeltaMagnitude * direction;
            // F_damp = -c * v
            var dampingForce = -_linearDamping * velocity;
            // Apply gravity force
            var gravityForce = _mass * Bone.InverseTransformDirection(_linearGravityForce);

            // Forces combined
            var totalForce = springForce + dampingForce + gravityForce;
            // a = F / m
            var acceleration = totalForce * _massMultiplier;

            velocity += acceleration * deltaTime;

            // Check if clamp is necessary
            velocityMagnitude = velocity.magnitude;
            var maxVelocity = _maxVelocity;

            if (velocityMagnitude > maxVelocity)
            {
                velocity = velocity.normalized * maxVelocity;
                velocityMagnitude = maxVelocity;
            }

                //localVelocity = Vector3.ClampMagnitude(currentVelocity, 1f);

                //KS.Logger.LogDebug($"displacement{localDelta} magnitude:{localDeltaMagnitude} velocity({velocity.x:F4},{velocity.y:F4},{velocity.z:F4})");

            var result = localDelta - velocity;

            return result;
        }

        protected Vector3 GetAngularOffset(float deltaTime)
        {
            var currentRotation = Bone.rotation;
            var prevRotation = _prevRotation;

            var deltaRotation = currentRotation * Quaternion.Inverse(_prevRotation);

            deltaRotation.ToAngleAxis(out var angle, out var axis);


            // Convert angle to (-180 .. 180) format
            if (angle > 180f)
                angle -= 360f;

            var angularVelocity = _prevAngularVelocity;

            var torque = _angularSpringStrength * angle * axis;
            var damping = -_angularDamping * angularVelocity;

            angularVelocity += (torque + damping) * deltaTime;

            var newRotation = Quaternion.Euler(angularVelocity * deltaTime) * prevRotation;
            _prevRotation = newRotation;
            _prevAngularVelocity = angularVelocity;

            var absAngle = Mathf.Abs(angle);
            if (absAngle > _maxRotationAngle)
                newRotation = Quaternion.Slerp(currentRotation, newRotation, _maxRotationAngle / absAngle);

            var result = (Quaternion.Inverse(currentRotation) * newRotation).eulerAngles;
           // _prevRotation = Quaternion.Euler(result) * _prevRotation;
            AniMorph.Logger.LogDebug($"angle[{angle}] delta{deltaRotation.eulerAngles} result{result} adj{_prevRotation.eulerAngles}");
            return result;
        }

        internal void OnChangeAnimator()
        {
            var bone = Bone;

            _prevPosition = bone.position;
            _prevRotation = bone.rotation;
            _prevVelocity = Vector3.zero;
            _prevAngularVelocity = Vector3.zero;
        }

        protected void StoreVariables(Vector3 velocity)
        {
            _prevVelocity = velocity;
        }



        protected Vector3 GetScaleDistortion(Vector3 velocity, float velocityMagnitude, float deltaTime, bool acceleration,  bool deceleration)
        {
            if (velocityMagnitude == 0f) return Vector3.one;

            var prevLocalVelocity = _prevVelocity;

            var velocityDir = velocity * (1f / velocityMagnitude);

            var absVelocityDir = new Vector3(Mathf.Abs(velocityDir.x), Mathf.Abs(velocityDir.y), Mathf.Abs(velocityDir.z));

            // Initialize stretch as neutral
            Vector3 distortion = Vector3.one;

            if (acceleration)
            {
                distortion += absVelocityDir * (_scaleAccelerationFactor * velocityMagnitude);
                // Clamp directional stretch
                var floor = 1f - _scaleMaxStretch;
                var ceiling = 1f + _scaleMaxStretch;
                distortion = new Vector3(
                    Mathf.Clamp(distortion.x, floor, ceiling),
                    Mathf.Clamp(distortion.y, floor, ceiling),
                    Mathf.Clamp(distortion.z, floor, ceiling)
                    );
                AniMorph.Logger.LogDebug($"Acceleration:distortion{distortion}");
            }
            // Look for decreasing deceleration (check from previous frame)
            // and increasing acceleration for momentum reversal squash.
            if (deceleration)
            {
                var accelerationVec = (velocity - prevLocalVelocity) * (1f / deltaTime);

                // Project acceleration onto direction to get deceleration if negative
                var decelerationDot = Vector3.Dot(accelerationVec, velocityDir);

                // If deceleration – look for decreasing velocity, if acceleration – look for increasing
                //var prevAcceleration = _prevAcceleration;
                //var reverseMoment = deceleration > prevAcceleration
                // Either acceleration increases or deceleration decreases
                //var isDeceleration = deceleration < 0f;
                //var reverseMoment = (deceleration > _prevDeceleration)
                //    // Way to commit to reverse momentum on deceleration, as input on high framerates is ass
                //    || (isDeceleration && !_waitForDeceleration);

                // We are after the last moments of deceleration and early moments of acceleration
                // after that the cycle repeats
                if (decelerationDot > 0f)
                {
                    // Get squash amount
                    var squashAmount = decelerationDot * _scaleDecelerationFactor;
                    // Clamp squash amount
                    squashAmount = Mathf.Clamp(squashAmount, 0f, _scaleMaxDistortion);
                    // Apply squash amount to velocity axes
                    var squashScale = Vector3.one + (-absVelocityDir * squashAmount);
                    // Expand perpendicular axes
                    var perpendicularScale = Vector3.one + (Vector3.one - absVelocityDir) * (squashAmount * 0.5f);
                    // Combine vectors
                    squashScale = Vector3.Scale(squashScale, perpendicularScale);

                    distortion = Vector3.Scale(distortion, squashScale);

                    // 
                    //_waitForDeceleration = false;

                    AniMorph.Logger.LogDebug($"Deceleration:reverseMoment:dot[{decelerationDot:F3}] squashAmount[{squashAmount:F3}] squashScale({squashScale.x:F3},{squashScale.y:F3},{squashScale.z:F3}) distortion({distortion.x:F3},{distortion.y:F3},{distortion.z:F3})");
                }
                else
                {
                    AniMorph.Logger.LogDebug($"Deceleration:dot[{decelerationDot}]");
                    //_waitForDeceleration = true;
                }
                  
                //_prevDeceleration = deceleration;
            }
            // Preserve original volume
            var stretchVolume = distortion.x * distortion.y * distortion.z;
            var volumeCorrection = Mathf.Pow(_originalVolume / stretchVolume, (1f / 3f));

            var finalScale = Vector3.Lerp(_prevScale, distortion * volumeCorrection, deltaTime * _scaleLerpSpeed);

            //KS.Logger.LogDebug($"stretch({stretch.x:F3},{stretch.y:F3},{stretch.z:F3}) volumeCorrection[{volumeCorrection}] " +
            //    $"stretchVolume[{stretch.x + stretch.y + stretch.z}] " +
            //    $"finalScale({finalScale.x:F3},{finalScale.y:F3},{finalScale.z:F3}) " +
            //    $"finalVolume[{finalScale.x + finalScale.y + finalScale.z}]");
            _prevScale = finalScale;
            return finalScale;
        }



        protected Vector3 GetGravityPositionOffset(out float dotUp, out float dotR)
        {
            dotUp = Vector3.Dot(Bone.up, Vector3.up);
            dotR = Vector3.Dot(Bone.right, Vector3.up);

            //var smoothDotUp = NeatStep(Mathf.Abs(dotUp));

            var result = Vector3.Lerp(_dotUpMiddle, dotUp > 0f ? _dotUpUp : _dotUpDown, Mathf.Abs(dotUp));

            result += Vector3.Lerp(_dotRMiddle, dotR > 0f ? _dotRUp : _dotRDown, Mathf.Abs(dotR));

            return result;
        }

        protected Vector3 GetGravityScaleOffset(out float dotFwd)
        {
            dotFwd = Vector3.Dot(Bone.forward, Vector3.up);

            return Vector3.Lerp(_dotFwdMiddle, dotFwd > 0f ? _dotFwdUp : _dotFwdDown, Mathf.Abs(dotFwd));
        }

        protected static bool[] Effects = new bool[Enum.GetNames(typeof(Effect)).Length];  

        internal static void UpdateSettings()
        {
            // Cache setting into a static array
            Effects[GetPower((int)Effect.Linear)] = (AniMorph.Effects.Value & Effect.Linear) != 0;
            Effects[GetPower((int)Effect.Angular)] = (AniMorph.Effects.Value & Effect.Angular) != 0;
            Effects[GetPower((int)Effect.Tethering)] = (AniMorph.Effects.Value & Effect.Tethering) != 0;
            Effects[GetPower((int)Effect.Acceleration)] = (AniMorph.Effects.Value & Effect.Acceleration) != 0;
            Effects[GetPower((int)Effect.Deceleration)] = (AniMorph.Effects.Value & Effect.Deceleration) != 0;
            Effects[GetPower((int)Effect.GravityLinear)] = (AniMorph.Effects.Value & Effect.GravityLinear) != 0;
            Effects[GetPower((int)Effect.GravityAngular)] = (AniMorph.Effects.Value & Effect.GravityAngular) != 0;

            // Find what power of 2 a number is
            static int GetPower(int number)
            {
                var result = 0;

                while (number > 1)
                {
                    number >>= 1;
                    result++;
                }
                return result;
            }
        }
        
        internal void OnConfigUpdate()
        {
            _prevVelocity = Vector3.zero;
            _prevAngularVelocity = Vector3.zero;
            _prevScale = Vector3.one;
            _prevRotation = Bone.rotation;
        }

        /// <summary>
        /// Mathf.SmoothStep but limited to 0..1f.
        /// </summary>
        protected float CheapStep(float t) => t * t * (3f - 2f * t);

        /// <summary>
        /// Mathf.SmoothStep but limited to 0..1f and done through cosine.
        /// </summary>
        protected float NeatStep(float t) => 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

        protected float EaseIn(float t) => t * t * (2f - t);
        protected Vector3 CheapStep(Vector3 t) => new(CheapStep(t.x), CheapStep(t.y), CheapStep(t.z));
        protected Vector3 NeatStep(Vector3 t) => new(NeatStep(t.x), NeatStep(t.y), NeatStep(t.z));
        protected Vector3 EaseIn(Vector3 t) => new(EaseIn(t.x), EaseIn(t.y), EaseIn(t.z));
        float EaseOutQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return 1 - (1 - t) * (1 - t); // Ease out quad
        }
        float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1 - Mathf.Pow(1 - t, 3); // Smoother than quad
        }
        protected Vector3 SmoothStep(Vector3 from, Vector3 to, Vector3 t) => new(
            Mathf.SmoothStep(from.x, to.x, t.x), 
            Mathf.SmoothStep(from.y, to.y, t.y), 
            Mathf.SmoothStep(from.z, to.z, t.z)
            );

        [Flags]
        internal enum Effect
        {
            Linear = 1,
            Angular = 2,
            Tethering = 4,
            Acceleration = 8,
            Deceleration = 16,
            GravityLinear = 32,
            GravityAngular = 64,
            GravityScale = 128,
        }

        // For indexing purpose
        protected enum RefEffect
        {
            // Follow the position as if attached by a rubber spring.
            Linear,
            // Follow the rotation as if attached by a rubber spring.
            Angular,
            // Adjust the rotation based on the linear offset as if connected by tether.
            Tethering,
            // Increase the scale along the axis of acceleration, while decreasing perpendicular ones.
            Acceleration,
            // Decrease the scale along the axis of deceleration, while increasing perpendicular ones,
            // when the momentum reversal is in critical state.
            Deceleration,
            // Apply a position offset based on the rotation of the bone and the correlating gravity force.
            GravityLinear,
            // Apply a rotation offset based on the rotation of the bone and the correlating gravity force.
            GravityAngular,
            // Apply a scale offset based on the rotation of the bone and the correlating gravity force.
            GravityScale
        }

    }
}
