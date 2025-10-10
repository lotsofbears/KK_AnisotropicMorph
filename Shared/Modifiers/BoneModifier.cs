using ADV.Commands.Base;
using ADV.Commands.Object;
using Illusion.Extensions;
using KKABMX.Core;
using MessagePack.Decoders;
using System;
using System.Collections.Generic;
using System.Reflection;
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


        // Array with effects to apply, default enum order
        protected readonly bool[] Effects = new bool[Enum.GetNames(typeof(Effect)).Length];

        // Max scale along velocity axis
        //private float _scaleMaxStretch = 0.4f;
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


        #region Linear variables and properties

        private Vector3 _linearGravityForce = new(0f, 0f, 0f);
        private Vector3 _linearLimitPositive;
        private Vector3 _linearLimitNegative;
        private float _linearSpringStrength = 25f;
        private float _linearDamping = 10f;
        private float _linearMassMultiplier = 1f;
        private float _linearMass = 1f;
        private float _linearMaxVelocity = 1f;
        private float _linearMaxSqrVelocity = 1f;
        private bool _linearGravity;

        private float SetLinearMassMultiplier
        {
            set
            {
                if (value <= 0f) value = 0.01f;

                _linearMass = value;
                _linearMassMultiplier = 1f / value;
            }
        }

        private void SetMaxVelocity(float value)
        {
            _linearMaxVelocity = value;
            _linearMaxSqrVelocity = value * value;
        }

        #endregion



        #region Angular variables and properties

        protected Vector3 AngularApplication;
        private float _angularSpringStrength = 30f;
        private float _angularDamping = 5f;
        private float _angularMaxAngle = 45f;

        #endregion



        #region Scale variables and properties

        // How much the scale stretches along velocity direction.
        private float _scaleAccelerationFactor = 40f; //0.01f;
        // How much to squash along deceleration axis
        private float _scaleDecelerationFactor = 0.5f;
        private Vector3 _scaleUnevenDistribution = new Vector3(0.67f, 0.5f, 0.33f);
        // How fast squash reacts
        private float _scaleLerpSpeed = 10f;
        // Max squash on deceleration
        private float _scaleMaxDistortion = 0.4f;
        private bool _scalePreserveVolume;
        private bool _scaleDumbAcceleration;
        private float _scaleAccumulatedAcceleration;
        private float _scaleAccumulatedDeceleration;

        #endregion



        #region Gravity variables and properties

        // When Dot(Bone.up, Vector3.up) points in up/middle/down direction.
        private Vector3 _dotUpUp = Vector3.zero;
        private Vector3 _dotUpMiddle = new Vector3(0f, 0.02f, 0f);
        private Vector3 _dotUpDown = new Vector3(0f, 0.05f, 0f);

        // When Dot(Bone.forward, Vector3.up) points in up/middle/down direction.
        private Vector3 _dotFwdUp = new Vector3(0.075f, 0.075f, -0.15f);
        private Vector3 _dotFwdMiddle;
        // Half of Z volume are perpendicular axes, half is subcutaneous fat from all around.
        private Vector3 _dotFwdDown = new Vector3(-0.05f, -0.05f, 0.2f);

        // When Dot(Bone.right, Vector3.up) points in up/middle/down direction.
        private Vector3 _dotRUp = new Vector3(-0.025f, -0.02f, 0f);
        private Vector3 _dotRMiddle;
        private Vector3 _dotRDown = new Vector3(0.025f, -0.02f, 0f);

        #endregion

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
                AniMorph.Logger.LogError($"{this.GetType().Name} wasn't initialized due to a wrong parameter.");
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
        internal virtual void UpdateModifiers(float deltaTime, float fps)
        {
            var effects = Effects;

            // Apply linear offset, its calculations are necessary to other methods even if the offset itself isn't.
            var positionModifier = GetLinearOffset(deltaTime, out var velocity, out var velocityMagnitude);

            //// Remove linear offset if setting
            if (!effects[(int)RefEffect.Linear])
            {
                positionModifier = Vector3.zero;
            }
            // Apply angular offset
            var rotationModifier = effects[(int)RefEffect.Angular] ? GetAngularOffset(deltaTime) : Vector3.zero;

            // Not allowed axes are multiplied by zero, allowed by one.
            rotationModifier = Vector3.Scale(rotationModifier, AngularApplication);

            // Apply acceleration scale distortion
            var scaleModifier = GetScaleOffset(
                velocity, velocityMagnitude, deltaTime, fps,
                effects[(int)RefEffect.Acceleration],
                effects[(int)RefEffect.Deceleration]
                );

            if (effects[(int)RefEffect.Tethering])
                rotationModifier += Tethering.GetTetheringOffset(velocity, deltaTime);

            var dotUp = Vector3.Dot(Bone.up, Vector3.up);
            var dotR = Vector3.Dot(Bone.right, Vector3.up);
            var dotFwd = Vector3.Dot(Bone.forward, Vector3.up);

            // Apply gravity position offset
            if (effects[(int)RefEffect.GravityLinear])
                positionModifier += GetGravityPositionOffset(dotUp, dotR);
            // Apply gravity scale offset
            if (effects[(int)RefEffect.GravityScale])
                scaleModifier = Vector3.Scale(scaleModifier, GetGravityScaleOffset(dotFwd));
            // Apply gravity rotation offset
            if (effects[(int)RefEffect.GravityAngular])
                rotationModifier += GetGravityAngularOffset(dotFwd, dotR);

            var boneModifierData = BoneModifierData;
            // Write modifiers for ABMX consumption
            boneModifierData.PositionModifier = positionModifier;
            boneModifierData.RotationModifier = rotationModifier;
            boneModifierData.ScaleModifier = scaleModifier;

            // Store current variables as "previous" for the next frame.
            StoreVariables(velocity);
        }


        // Implementation with Hooke's Law 
        /// <param name="deltaTime">Requires unscaled time to work properly on abnormal speed.</param>
        protected Vector3 GetLinearOffset(float deltaTime, out Vector3 velocity, out float velocityMagnitude)
        {
            velocity = _prevVelocity;

            var localDelta = Bone.InverseTransformPoint(_prevPosition);

            _prevPosition = Bone.position;

            var localDeltaMagnitude = localDelta.magnitude;

            // Normalized displacement, avoid division by zero
            var direction = (localDeltaMagnitude == 0f) ? Vector3.zero : (localDelta * (1f / localDeltaMagnitude));

            //var stretch = currentLength - restLength;
            // F_spring = -k * x
            var springForce = -_linearSpringStrength * localDeltaMagnitude * direction;
            // F_damp = -c * v
            var dampingForce = -_linearDamping * velocity;
            // Forces combined
            var totalForce = springForce + dampingForce;
            // Apply gravity force
            if (_linearGravity)
            {
                totalForce += _linearMass * Bone.InverseTransformDirection(_linearGravityForce);
            }
            // a = F / m
            var acceleration = totalForce * (_linearMassMultiplier * deltaTime);

            
            // Limit axes

            for (var i = 0; i < 3; i++)
            {
                if (acceleration[i] > 0f)
                {
                    acceleration[i] *= _linearLimitPositive[i];
                }
                else
                {
                    acceleration[i] *= _linearLimitNegative[i];
                }
            }

            velocity += acceleration;

            // Check if clamp is necessary
            velocityMagnitude = velocity.magnitude;
            var maxVelocity = _linearMaxVelocity;

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

            // Avoid Vector3(Infinity) in axis.
            if (float.IsInfinity(axis.x))
            {
#if DEBUG
                AniMorph.Logger.LogDebug($"Infinity axis detected[{axis}] angle[{angle:F3}], using fallback axis.");
#endif
                //axis = new Vector3(axis.x < 0f ? 1f : 0f, axis.y < 0f ? 1f : 0f, axis.z < 0f ? 1f : 0f);
                axis = Vector3.up;
            }

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
            if (absAngle > _angularMaxAngle)
                newRotation = Quaternion.Slerp(currentRotation, newRotation, _angularMaxAngle / absAngle);

            var result = (Quaternion.Inverse(currentRotation) * newRotation).eulerAngles;
           // _prevRotation = Quaternion.Euler(result) * _prevRotation;
            //AniMorph.Logger.LogDebug($"angle[{angle}] deltaEuler{deltaRotation.eulerAngles} result{result} prevRotation{prevRotation} newRotation{newRotation} currentRotation{currentRotation} angularVelocity{angularVelocity}");
            return result;
        }

        internal void OnChangeAnimator()
        {
            var bone = Bone;

            _prevPosition = bone.position;
            _prevRotation = bone.rotation;
            _prevVelocity = Vector3.zero;
            _prevAngularVelocity = Vector3.zero;
            _prevScale = Vector3.one;
            _scaleAccumulatedAcceleration = 0f;
            _scaleAccumulatedDeceleration = 0f;
            //_prevAccelerationScale = Vector3.one;
            //_prevDecelerationScale = Vector3.one;
        }

        protected void StoreVariables(Vector3 velocity)
        {
            _prevVelocity = velocity;
        }

        //private Vector3 _prevDecelerationScale;
        //private Vector3 _prevAccelerationScale;

        protected Vector3 GetScaleOffset(Vector3 velocity, float velocityMagnitude, float deltaTime, float fps, bool acceleration,  bool deceleration)
        {
            if (!acceleration && !deceleration) return Vector3.one;
            // Avoid division by zero
            if (velocityMagnitude == 0f)
            {
#if DEBUG
                AniMorph.Logger.LogDebug($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}:Undesirable parameter value in 'velocityMagnitude', avoided divisions by zero.");
#endif
                return Vector3.one;
            }

            // Normalize velocity
            var velocityNormalized = velocity * (1f / velocityMagnitude);

            var absVelocityNormalized = new Vector3(Mathf.Abs(velocityNormalized.x), Mathf.Abs(velocityNormalized.y), Mathf.Abs(velocityNormalized.z));

            var accelerationVec = (velocity - _prevVelocity) * fps;
            // Project acceleration onto direction to get deceleration if negative
            var accelerationDot = Vector3.Dot(accelerationVec, velocityNormalized);
            // Initialize distortion as neutral
            var distortion = Vector3.one;

            // Proper acceleration with accumulation or without
            // looks worse then a dumb magnitude based implementation.
            // But hey it's an option.
            if (acceleration)
            {
                var distortionAmount = _scaleAccelerationFactor;

                if (_scaleDumbAcceleration)
                {
                    distortionAmount *= velocityMagnitude * fps;
                }
                else
                {
                    // Accumulate acceleration, deltaTime is a passive drain to avoid awkward accumulations in some idle animations.
                    var totalAcceleration = Mathf.Clamp01(_scaleAccumulatedAcceleration + accelerationDot - (deltaTime * 0.1f));

                    distortionAmount *= totalAcceleration;

                    _scaleAccumulatedAcceleration = totalAcceleration;
                }

                distortionAmount = Mathf.Clamp(distortionAmount, 0f, _scaleMaxDistortion);
                // Apply distortion amount to velocity axes
                distortion += absVelocityNormalized * distortionAmount;
                // Shrink axes perpendicular to the velocity 
                var perpendicularScale = Vector3.one + Vector3.Scale(absVelocityNormalized - Vector3.one, distortionAmount * _scaleUnevenDistribution);
                // Combine vectors
                distortion = Vector3.Scale(distortion, perpendicularScale);
//#if DEBUG
//                AniMorph.Logger.LogDebug($"Acceleration:distortion({distortion.x:F3},{distortion.y:F3},{distortion.z:F3})" +
//                    $"distortionAmount[{distortionAmount:F3}] velocityMagnitude[{(velocityMagnitude * fps):F5}" +
//                   // $"scale({perpendicularScale.x:F3},{perpendicularScale.y:F3},{perpendicularScale.z:F3})" +
//                    //$"absVelocityNorm({absVelocityNormalized.x:F3},{absVelocityNormalized.y:F3},{absVelocityNormalized.z:F3})"
//                    "");
//#endif
            }

            if (deceleration)
            {

                //AniMorph.Logger.LogDebug($"Deceleration:dot[{decelerationDot:F3}] " +
                //    $"totalDeceleration[{_totalDeceleration:F3}" 
                //    //$"velocityDir({velocityDir.x:F3},{velocityDir.y:F3},{velocityDir.z:F3})" +
                //    //$"accelerationVec{accelerationVec} accelerationVecMag{accelerationVec.magnitude:F3}"
                    
                //    );
                // Amplify deceleration as it tends to be too small.
                if (accelerationDot < 0f) accelerationDot *= 2f;

                // Accumulate deceleration, deltaTime is a passive drain to avoid awkward accumulations in some idle animations.
                var totalDeceleration = Mathf.Clamp01(_scaleAccumulatedDeceleration + accelerationDot - (deltaTime * 0.1f));
                // Store for the next frame
                _scaleAccumulatedDeceleration = totalDeceleration;

                if (totalDeceleration > 0f)
                {
                    // How much scale can deviate
                    var distortionAmount = totalDeceleration  * _scaleDecelerationFactor;
                    distortionAmount = Mathf.Clamp(distortionAmount, 0f, _scaleMaxDistortion);
                    // Apply distortion amount to velocity axes
                    var decelerationScale = Vector3.one - (absVelocityNormalized * distortionAmount);
                    // Expand axes perpendicular to the velocity
                    var perpendicularScale = Vector3.one + Vector3.Scale((Vector3.one - absVelocityNormalized), distortionAmount * _scaleUnevenDistribution); // * (squashAmount * 0.5f);
                    // Combine vectors
                    decelerationScale = Vector3.Scale(decelerationScale, perpendicularScale);

                    distortion = Vector3.Scale(distortion, decelerationScale);

//#if DEBUG
//                    AniMorph.Logger.LogDebug($"Deceleration:reverseMoment:dot[{decelerationDot:F3}] squashAmount[{distortionAmount:F3}] totalDeceleration[{totalDeceleration}]" +
//                        $"perpendicularScale({perpendicularScale.x:F3},{perpendicularScale.y:F3},{perpendicularScale.z:F3}) " +
//                        $"decelerationScale({decelerationScale.x:F3},{decelerationScale.y:F3},{decelerationScale.z:F3})" +
//                        $"");
//#endif
                }
                //#if DEBUG
                //                else
                //                {
                //                    AniMorph.Logger.LogDebug($"Deceleration:dot[{decelerationDot}]");
                //                }
                //#endif
            }
            if (_scalePreserveVolume)
            {
                // Preserve original volume
                var stretchVolume = distortion.x * distortion.y * distortion.z;
                var volumeCorrection = Mathf.Pow(_originalVolume / stretchVolume, (1f / 3f));
                distortion *= volumeCorrection;
            }

            var finalScale = Vector3.Lerp(_prevScale, distortion, deltaTime * _scaleLerpSpeed);
//#if DEBUG
//            if (!acceleration && !deceleration)
//            {
//                AniMorph.Logger.LogDebug($"stretch({finalScale.x:F3},{finalScale.y:F3},{finalScale.z:F3})" +
//                //    $"stretchVolume[{stretch.x + stretch.y + stretch.z}] " +
//                //    $"finalScale({finalScale.x:F3},{finalScale.y:F3},{finalScale.z:F3}) " +
//                //    $"finalVolume[{finalScale.x + finalScale.y + finalScale.z}]");
//                "");
//            }
//#endif
            _prevScale = finalScale;
            //_prevAccelerationScale = distortion;
            //_prevDecelerationScale = decelerationScale;

            return finalScale;
        }



        protected Vector3 GetGravityPositionOffset(float dotUp, float dotR)
        {

            //var smoothDotUp = NeatStep(Mathf.Abs(dotUp));

            var result = Vector3.Lerp(_dotUpMiddle, dotUp > 0f ? _dotUpUp : _dotUpDown, Mathf.Abs(dotUp));

            result += Vector3.Lerp(_dotRMiddle, dotR > 0f ? _dotRUp : _dotRDown, Mathf.Abs(dotR));

            //AniMorph.Logger.LogDebug($"GravityPosOffset:dotUp[{dotUp:F3}] dotR[{dotR:F3}] result({result.x:F3},{result.y:F3},{result.z:F3})");

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dotFwd"></param>
        /// <returns>Offset for flat addition to the scale.</returns>
        protected Vector3 GetGravityScaleOffset(float dotFwd)
        {
            var result = Vector3.Lerp(_dotFwdMiddle, dotFwd > 0f ? _dotFwdUp : _dotFwdDown, Mathf.Abs(dotFwd));

            //AniMorph.Logger.LogDebug($"GravityScaleOffset:dotFwd[{dotFwd:F3}] result({result.x:F3},{result.y:F3},{result.z:F3})");

            return result;
        }


        private float _sidewaysAngleLimit = 20f;
        //private Quaternion _upRotation = Quaternion.Euler(90f, 0f, 0f);
        //private Quaternion _downRotation = Quaternion.Euler(-90f, 0f, 0f);


        protected Vector3 GetGravityAngularOffset(float masterDotFwd, float masterDotRight)
        {
            var dotFwd = masterDotFwd; // Vector3.Dot(Bone.forward, Vector3.up);
            var dotRight = masterDotRight; // Vector3.Dot(Bone.right, Vector3.up);

            //var absDotFwd = Math.Abs(dotFwd);
            //var absDotRight = Math.Abs(dotRight);
            var angleLimit = _sidewaysAngleLimit;

            // A way to reduce angle spread when lying face up.
            if (dotFwd > 0f) dotFwd *= 0.5f;

            if (_isLeftPosition) angleLimit = -angleLimit;

            var result = new Vector3(0f, (angleLimit * (dotFwd + dotRight)), 0f);

            //var boneUp = Bone.up;

            ////var boneRotation = Bone.rotation;

            ////var deltaEuler = (boneRotation * Quaternion.Inverse(_upRotation)).eulerAngles;

            ////var deltaAngleY = Mathf.DeltaAngle(0f, deltaEuler.y);

            ////var deviationY = Mathf.Min(_angleLimitRad, Mathf.Abs(deltaAngleY));

            ////if (deltaAngleY < 0f) deviationY = -deviationY;

            //var lookRot = Quaternion.LookRotation(-Vector3.up, boneUp);
            ////var result = new Vector3(0f, deviationY * masterFwdDot, 0f);
            //AniMorph.Logger.LogDebug($"dotFwd[{dotFwd:F3}] dotRight[{dotRight:F3}] dotSum[{dotSum:F3}] result({result.x:F3},{result.y:F3},{result.z:F3})");
            return result;
        }

        
        internal virtual void OnConfigUpdate(AniMorph.Body part)
        {
#if DEBUG
            AniMorph.Logger.LogDebug($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}:Pop");
#endif
            OnChangeAnimator();

            if (part == AniMorph.Body.Breast)
            {
                _linearSpringStrength = AniMorph.BreastLinearSpringStrength.Value;
                _linearDamping = AniMorph.BreastLinearDamping.Value;
                _linearGravityForce = new Vector3(0f, AniMorph.BreastLinearGravity.Value, 0f);
                _linearGravity = AniMorph.BreastLinearGravity.Value != 0f;
                _linearLimitPositive = AniMorph.BreastLinearLimitPositive.Value;
                _linearLimitNegative = AniMorph.BreastLinearLimitNegative.Value;
                SetLinearMassMultiplier = AniMorph.BreastLinearMass.Value;

                _angularSpringStrength = AniMorph.BreastAngularSpringStrength.Value;
                _angularDamping = AniMorph.BreastAngularDamping.Value;
                _angularMaxAngle = AniMorph.BreastAngularMaxAngle.Value;

                _scaleAccelerationFactor = AniMorph.BreastScaleAccelerationFactor.Value;
                _scaleDecelerationFactor = AniMorph.BreastScaleDecelerationFactor.Value;
                _scaleLerpSpeed = AniMorph.BreastScaleLerpSpeed.Value;
                _scaleMaxDistortion = AniMorph.BreastScaleMaxDistortion.Value;
                _scaleUnevenDistribution = AniMorph.BreastScaleUnevenDistribution.Value;
                _scalePreserveVolume = AniMorph.BreastScalePreserveVolume.Value;
                _scaleDumbAcceleration = AniMorph.BreastScaleDumbAcceleration.Value;

                if (Tethering != null)
                {
                    Tethering.multiplier = AniMorph.BreastTetheringMultiplier.Value;
                    Tethering.frequency = AniMorph.BreastTetheringFrequency.Value;
                    Tethering.damping = AniMorph.BreastTetheringDamping.Value;
                    Tethering.maxAngle = AniMorph.BreastTetheringMaxAngle.Value;
                }
                _dotUpUp = AniMorph.BreastGravityUpUp.Value;
                _dotUpMiddle = AniMorph.BreastGravityUpMid.Value;
                _dotUpDown = AniMorph.BreastGravityUpDown.Value;
                // Scale uses vector multiplication rather then addition.
                _dotFwdUp = Vector3.one + AniMorph.BreastGravityFwdUp.Value;
                _dotFwdMiddle = Vector3.one + AniMorph.BreastGravityFwdMid.Value;
                _dotFwdDown = Vector3.one + AniMorph.BreastGravityFwdDown.Value;
                _dotRUp = AniMorph.BreastGravityRightUp.Value;
                _dotRMiddle = AniMorph.BreastGravityRightMid.Value;
                _dotRDown = AniMorph.BreastGravityRightDown.Value;

                UpdateEffects(AniMorph.BreastEffects.Value);
            }
            else if (part == AniMorph.Body.Butt)
            {
                _linearSpringStrength = AniMorph.ButtLinearSpringStrength.Value;
                _linearDamping = AniMorph.ButtLinearDamping.Value;
                _linearGravityForce = new Vector3(0f, AniMorph.ButtLinearGravity.Value, 0f);
                _linearGravity = AniMorph.ButtLinearGravity.Value != 0f;
                _linearLimitPositive = AniMorph.ButtLinearLimitPositive.Value;
                _linearLimitNegative = AniMorph.ButtLinearLimitNegative.Value;
                SetLinearMassMultiplier = AniMorph.ButtLinearMass.Value;

                _angularSpringStrength = AniMorph.ButtAngularSpringStrength.Value;
                _angularDamping = AniMorph.ButtAngularDamping.Value;
                _angularMaxAngle = AniMorph.ButtAngularMaxAngle.Value;

                _scaleAccelerationFactor = AniMorph.ButtScaleAccelerationFactor.Value;
                _scaleDecelerationFactor = AniMorph.ButtScaleDecelerationFactor.Value;
                _scaleLerpSpeed = AniMorph.ButtScaleLerpSpeed.Value;
                _scaleMaxDistortion = AniMorph.ButtScaleMaxDistortion.Value;
                _scaleUnevenDistribution = AniMorph.ButtScaleUnevenDistribution.Value;
                _scalePreserveVolume = AniMorph.ButtScalePreserveVolume.Value;
                _scaleDumbAcceleration = AniMorph.ButtScaleDumbAcceleration.Value;

                if (Tethering != null)
                {
                    Tethering.multiplier = AniMorph.ButtTetheringMultiplier.Value;
                    Tethering.frequency = AniMorph.ButtTetheringFrequency.Value;
                    Tethering.damping = AniMorph.ButtTetheringDamping.Value;
                    Tethering.maxAngle = AniMorph.ButtTetheringMaxAngle.Value;
                }
                _dotUpUp = AniMorph.ButtGravityUpUp.Value;
                _dotUpMiddle = AniMorph.ButtGravityUpMid.Value;
                _dotUpDown = AniMorph.ButtGravityUpDown.Value;
                _dotFwdUp = Vector3.one + AniMorph.ButtGravityFwdUp.Value;
                _dotFwdMiddle = Vector3.one + AniMorph.ButtGravityFwdMid.Value;
                _dotFwdDown = Vector3.one + AniMorph.ButtGravityFwdDown.Value;
                _dotRUp = AniMorph.ButtGravityRightUp.Value;
                _dotRMiddle = AniMorph.ButtGravityRightMid.Value;
                _dotRDown = AniMorph.ButtGravityRightDown.Value;

                UpdateEffects(AniMorph.ButtEffects.Value);
            }

            void UpdateEffects(Effect enumValue)
            {
                foreach (Effect value in Enum.GetValues(typeof(Effect)))
                {
                    Effects[GetPower((int)value)] = (enumValue & value) != 0;
                }
            }
        }

        protected void UpdateAngularApplication(AniMorph.Axis enumValue)
        {
            foreach (AniMorph.Axis value in Enum.GetValues(typeof(AniMorph.Axis)))
            {
                // 1 if true, 0f if false so we can simply multiply it.
                AngularApplication[GetPower((int)value)] = ((enumValue & value) != 0) ? 1f : 0f;
            }
        }

        // Find what power of 2 the number is.
        private int GetPower(int number)
        {
            var result = 0;

            while (number > 1)
            {
                number >>= 1;
                result++;
            }
            return result;
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

        /// <summary>
        /// For indexing purpose of 'Effect' enum.
        /// </summary>
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
