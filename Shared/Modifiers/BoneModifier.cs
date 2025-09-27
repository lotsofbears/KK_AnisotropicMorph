using ADV.Commands.Base;
using ADV.Commands.Object;
using KKABMX.Core;
using MessagePack.Decoders;
using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace KineticShift
{
    internal class BoneModifier
    {


        protected readonly Transform Bone;

        protected readonly Tethering Tethering;

        // Height / Width ratio
        private readonly float _height;
        // Width / Height ratio
        private readonly float _width;


        private float _posSpring = 1f;
        private float _posDamping = 1f;
        private float _posDrag = -5f;
        private float _maxMagnitude = 0.1f;
        
        // Amplifier for angular rotation
        private float _torqueAmplifier = 10f;
        // Resistance for angular rotation
        private float _torqueDrag = -2f;

        private float _maxRotationAngle = 45f;

        private float _scaleScalar = 1f;
        // Max scale along velocity axis
        private float _scaleMaxStretch = 0.4f;
        //// Min scale on perpendicular axes
        //private float _minSquashScale = 0.67f;
        // Smoothing factor for scale change
        private float _scaleSmoothing = 5f;
        // How strong the deceleration squash is
        private float _squashIntensity = 10000f;
        // How quickly the squash effect fades
        // Bigger => faster
        private float _squashDecaySpeed = 3f;
        // localScale.x * localScale.y * localScale.z
        private float _originalVolume;
        private readonly float _baseScaleMagnitude;
        // Momentum reversal squash offset
        private Vector3 _squashOffset;
        //// Movement speed
        //private Vector3 _velocity;
        // Movement direction with magnitude 1f
        private Vector3 _velocityNormalized;
        // Snapshots of previous frame
        protected Vector3 _prevVelocity;
        protected Vector3 _prevVelocityNormalized;
        protected Vector3 _prevAngularVelocity;
        protected Vector3 _prevPosition;
        protected Vector3 _prevScale;
        // Or aceleration depending on the sign
       // protected float _prevDeceleration;
       // private bool _waitForDeceleration;

        private float _velocityDeltaZ;
        // Pseudo previous as we use dead reckoning and have no way to know,
        // Synchronized periodically when animator changes states.
        private Quaternion _prevRotation;
        protected readonly BoneModifierData BoneModifierData;


        //private float _velocity;
        //private float _kineticEnergy;
        //private float _damping 
        //private float _dampingLimit = 0.1f;
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
                KS.Logger.LogError($"{this.GetType().Name} wasn't initialized due to wrong argument");
                return;
            }

            Bone = bone;
            BoneModifierData = boneModifierData;
            _prevPosition = bone.position;
            _prevRotation = bone.rotation;
            _originalVolume = bone.localScale.x * bone.localScale.y * bone.localScale.z;
            _baseScaleMagnitude = bone.localScale.magnitude;

            if (centeredBone != null)
                Tethering = new Tethering(centeredBone, _prevPosition);

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

        /// <summary>
        /// Meant for access from BoneEffector.
        /// </summary>
        internal virtual void UpdateModifiers(float deltaTime, float unscaledDeltaTime)
        {
            //// Position calculation
            //var currentPosition = Bone.position;
            //var deltaVecLocal = Bone.InverseTransformPoint(_prevPosition);
            ////deltaVector = Bone.TransformVector(deltaVector);

            //var velocity = _velocity;
            //var springForce = _posSpring * deltaVecLocal;
            //var dampingForce = _posDrag * velocity;
            //var force = (springForce + dampingForce) * deltaTime;

            //velocity = (velocity + force) * deltaTime;
            ////positionModifier = deltaVector - velocity * delta;
            ////var newPosition = _prevPosition + velocity; // - targetPosition);
            var boneModifierData = BoneModifierData;
            boneModifierData.PositionModifier = GetLinearVelocity(unscaledDeltaTime, out var velocity, out var velocityMagnitude);
            ////positionModifier = deltaVecLocal - velocity;  //Bone.TransformVector(newPosition - currentPosition);


            //// Rotation calculation
            //var currentRot = Bone.rotation;
            //var prevRot = _prevRotation;
            //var deltaRot = currentRot * Quaternion.Inverse(prevRot);

            ////var targetRot = currentRot;

            //deltaRot.ToAngleAxis(out var angle, out var axis);

            //// Due to (probably) the type being float we wont arrive at the target with this method,
            //// and instead will get stuck in tiny numbers.
            //// To avoid micro glitches/restless behavior we settle down with 0.1f. Epsilon is too big.
            ////if (angle > 0.1f)
            ////{
            //    if (angle > 180f) angle -= 360f;

            //    var torque = axis * (angle * Mathf.Deg2Rad * _rotSpring);

            //    var angularVelocity = _prevAngularVelocity;
            //    var dampingTorque = _rotDrag * angularVelocity;
            //    torque += dampingTorque;

            //    angularVelocity += torque * deltaTime;

            //    var targetRot = Quaternion.Euler(angularVelocity * (Mathf.Rad2Deg * deltaTime)) * prevRot;
            //    rotationModifier = (currentRot * Quaternion.Inverse(targetRot)).eulerAngles;
            var rotMod = GetAngularVelocity(unscaledDeltaTime);

            boneModifierData.RotationModifier = rotMod + Tethering.ApplyAdvancedTethering(velocity, deltaTime);

            //KS.Logger.LogDebug($"rotationModifier({rotationModifier.x:F2},{rotationModifier.y:F2},{rotationModifier.z:F2})");
            boneModifierData.ScaleModifier = GetScaleSquash(velocity, velocityMagnitude, deltaTime);
            //}

            //ApplyScaleModification(velocity, deltaTime, out scaleModifier);

            // Using actual position from this LateUpdate before IK has updated
            //// Will be problematic when some other bone modifier nearby is in play.
            //_prevPosition = Bone.TransformPoint(boneModifierData.PositionModifier);
            //_prevVelocity = velocity;
            StoreVariables(velocity);
        }

        ///// <summary>
        ///// Calculate the axial change and apply it smoothly.
        ///// </summary>
        ///// <param name="velocity">Local variable ready for use by further methods.</param>
        ///// <returns>Vector3 ready for ABMX use</returns>
        //protected Vector3 ApplyAxialVelocityEx(float deltaTime, out Vector3 velocity)
        //{
        //    velocity = _prevVelocity;

        //    var currentPosition = Bone.position;
             
        //    var deltaVecLocal = Bone.InverseTransformPoint(_prevPosition);
        //    // Strip Z axis
        //    _velocityDeltaZ = deltaVecLocal.z;

        //    //deltaVecLocal.z = 0f;

        //    deltaVecLocal = Vector3.ClampMagnitude(deltaVecLocal, _maxMagnitude);


        //    // Scale delta up
        //    var springForce = _posSpring * deltaVecLocal;
        //    // Apply drag to previous velocity (scaled with deltaTime previously)
        //    var dampingForce = _posDrag * velocity;
        //    // Scale with deltaTime
        //    var force = (springForce + dampingForce) * deltaTime;

        //    velocity += force * deltaTime;
        //    velocity *= deltaTime;
        //    //positionModifier = deltaVector - velocity * delta;
        //    //var newPosition = _prevPosition + velocity; // - targetPosition);

        //    // Adjust movement based on current velocity
        //    var result = deltaVecLocal - velocity;

        //    // Store variables for next frame
        //    KS.Logger.LogDebug($"Dot:{Vector3.Dot(_prevVelocityNormalized, Vector3.up)} magnitude:{deltaVecLocal.magnitude}");
        //    _prevPosition = Bone.TransformPoint(result);
        //    return result;
        //}
        //protected Vector3 ApplyAxialVelocityExEx(float deltaTime, out Vector3 velocity)
        //{
        //    velocity = _prevVelocity;

        //    var currentPosition = Bone.position;

        //    var deltaVecLocal = Bone.InverseTransformPoint(_prevPosition);
        //    // Strip Z axis
        //    _velocityDeltaZ = deltaVecLocal.z;

        //    //deltaVecLocal.z = 0f;


        //    var springForce = _posSpring * deltaVecLocal;
        //    var dampingForce = _posDrag * velocity;
        //    var force = (springForce + dampingForce) * deltaTime;

        //    velocity += dampingForce * deltaTime;
        //    //positionModifier = deltaVector - velocity * delta;
        //    //var newPosition = _prevPosition + velocity; // - targetPosition);

        //    // Adjust movement based on current velocity
        //    var result = deltaVecLocal - velocity;
        //    //result = SmoothStep(Vector3.zero, deltaVecLocal, result);

        //    KS.Logger.LogDebug($"magnitude:{result.magnitude} velocity({velocity.x:F4},{velocity.y:F4},{velocity.z:F4})");
        //    result = Vector3.ClampMagnitude(result, _maxMagnitude);

            
        //    // Store variables for next frame
        //    _prevPosition = Bone.TransformPoint(result);
        //    return result;
        //}

        //[Tooltip("Rest length of the spring. 0 means the child tries to stay on the parent.")]
        //public float restLength = 0f;

        // [Tooltip("Spring stiffness: higher values mean a tighter spring.")]
        public float springStrength = 50f;
        [Tooltip("Damping: reduces oscillation over time.")]
        public float damping = 10f;
        private Vector3 _gravityForce = new(0f, 0f, 0f);
        private float _dampCoef = 1f;
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
        private float _massMultiplier = 1f;
        private float _mass = 1f;

        private void SetMaxVelocity(float value)
        {
            _maxVelocity = value;
            _maxSqrVelocity = value * value;
        }
        private float _maxVelocity = 1f;
        private float _maxSqrVelocity = 1f;

        // Implementation with Hooke's Law 
        protected Vector3 GetLinearVelocity(float deltaTime, out Vector3 velocity, out float velocityMagnitude)
        {

            velocity = _prevVelocity;

            var localDelta = Bone.InverseTransformPoint(_prevPosition);

            _prevPosition = Bone.position;

            var localDeltaMagnitude = localDelta.magnitude;

            // Normalized displacement, avoid division by zero
            var direction = (localDeltaMagnitude == 0f) ? Vector3.zero : (localDelta * (1f / localDeltaMagnitude)); //  displacement.normalized;

            //var stretch = currentLength - restLength;
            // F_spring = -k * x
            var springForce = -springStrength * localDeltaMagnitude * direction;
            // F_damp = -c * v
            var dampingForce = -damping * velocity;
            // Apply gravity force
            var gravityForce = _mass * Bone.InverseTransformDirection(_gravityForce);

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

        [Tooltip("Rotational spring stiffness (torque strength).")]
        public float angularSpringStrength = 30f;
        public float angularDamping = 5f;
        protected Vector3 GetAngularVelocity(float deltaTime)
        {
            var currentRotation = Bone.rotation;
            var prevRotation = _prevRotation;

            var deltaRotation = currentRotation * Quaternion.Inverse(_prevRotation);

            deltaRotation.ToAngleAxis(out var angle, out var axis);


            // Convert angle to (-180 .. 180) format
            if (angle > 180f)
                angle -= 360f;

            var angularVelocity = _prevAngularVelocity;

            var torque = angularSpringStrength * angle * axis;
            var damping = -angularDamping * angularVelocity;

            angularVelocity += (torque + damping) * deltaTime;

            var newRotation = Quaternion.Euler(angularVelocity * deltaTime) * prevRotation;
            _prevRotation = newRotation;
            _prevAngularVelocity = angularVelocity;

            var absAngle = Mathf.Abs(angle);
            if (absAngle > _maxRotationAngle)
                newRotation = Quaternion.Slerp(currentRotation, newRotation, _maxRotationAngle / absAngle);

            var result = (Quaternion.Inverse(currentRotation) * newRotation).eulerAngles;
           // _prevRotation = Quaternion.Euler(result) * _prevRotation;
            KS.Logger.LogDebug($"angle[{angle}] delta{deltaRotation.eulerAngles} result{result} adj{_prevRotation.eulerAngles}");
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


        ///// <summary>
        ///// Calculate the angular change and apply it smoothly.
        ///// </summary>
        ///// <returns>Euler Vector3 ready for ABMX use.</returns>
        //protected Vector3 GetAngularVelocityOld(float deltaTime)
        //{
        //    var currentRot = Bone.rotation;
        //    var prevRot = _prevRotation;
        //    var deltaRot = currentRot * Quaternion.Inverse(prevRot);

        //    deltaRot.ToAngleAxis(out var angle, out var axis);

        //    if (angle > 180f) angle -= 360f;

        //    var torque = axis * (angle * Mathf.Deg2Rad * _torqueAmplifier);

        //    var angularVelocity = _prevAngularVelocity;
        //    var dampingTorque = _torqueDrag * angularVelocity;
        //    torque += dampingTorque;

        //    angularVelocity += torque * deltaTime;

        //    var targetRot = Quaternion.Euler(angularVelocity * (Mathf.Rad2Deg * deltaTime)) * prevRot;

        //    var absAngle = Mathf.Abs(angle);
        //    if (absAngle > _maxRotationAngle)
        //        targetRot = Quaternion.Slerp(currentRot, targetRot, _maxRotationAngle / absAngle);

        //    _prevRotation = targetRot;
        //    _prevAngularVelocity = angularVelocity;

        //    var result = (Quaternion.Inverse(currentRot) * targetRot).eulerAngles;

        //    KS.Logger.LogDebug($"angle[{angle}] vel({angularVelocity.x:F4},{angularVelocity.y:F4},{angularVelocity.z:F4}) targetRot({targetRot.x:F4},{targetRot.y:F4},{targetRot.z:F4})");
        //    //var result = (Quaternion.Inverse(currentRot) * targetRot).eulerAngles;
        //    //KS.Logger.LogDebug($"angularVelocity{angularVelocity} torque{torque} targetRot{angularVelocity * (Mathf.Rad2Deg * deltaTime)} result{result} angle{angle}");
        //    return result;
        //   // return (currentRot * Quaternion.Inverse(targetRot)).eulerAngles;
        //}

        //private float _multiplier = 10f;
        //// Limit of rotation
        //private float _maxAngle = 60f;
        //// How strongly it springs back
        //private float _springStiffness = 5f;
        ////// How quickly it settles
        ////private float _damping = 2f;
        //// Natural frequency of bounce
        //private float _frequency = 1.3f; // 2f; // 4f;
        //[Range(0f, 1f)]
        //// 0 = undamped, 1 = critically damped
        //private float _dampingRatio = 0.3f;
        ////// How responsive the spring is
        ////private float _response = 1f;

        //private Vector3 _springVelocity;
        //private Vector3 _tetherRotationEuler;
        //// Current rotation offset
        //private Vector3 _springPosition;
        //protected Vector3 ApplyAngularVelocity(float deltaTime)
        //{
        //    var currentRot = Bone.rotation;
        //    var prevRot = _prevRotation;
        //    var deltaRot = currentRot * Quaternion.Inverse(prevRot);

        //    deltaRot.ToAngleAxis(out var angle, out var axis);

        //    if (angle > 180f) angle -= 360f;

        //    var angularVelocity = axis * angle * _multiplier;

        //    var targetEuler = angularVelocity;

        //    targetEuler = Vector3.ClampMagnitude(targetEuler, _maxAngle);

        //    _springPosition = DampedSpring(_springPosition, targetEuler, ref _springVelocity, _frequency, _dampingRatio, deltaTime);

        //    _prevRotation = Quaternion.Euler(_springPosition) * currentRot;

        //    KS.Logger.LogDebug($"angularVelocity{angularVelocity} targetEuler{targetEuler} angle{angle}");
        //    return _springPosition;
        //}

        //private Vector3 DampedSpring(Vector3 current, Vector3 target, ref Vector3 velocity, float freq, float damp, float dt)
        //{
        //    float omega = 2f * Mathf.PI * freq;
        //    float zeta = damp;
        //    float omegaZeta = omega * zeta;
        //    float omegaSq = omega * omega;

        //    Vector3 f = velocity + omegaZeta * (current - target);
        //    Vector3 a = -omegaSq * (current - target) - 2f * omegaZeta * f;

        //    velocity += a * dt;
        //    return current + velocity * dt;

        //}


        [Tooltip("How much the scale stretches along velocity direction.")]
        public float _scaleStretchFactor = 20f; //0.01f;
        // How much to squash along deceleration axis
        public float _scaleDecelerationSquashFactor = 0.01f;
        // How fast squash reacts
        public float _scaleSquashLerpSpeed = 10f;
        // Max squash on deceleration
        public float _maxSquash = 0.4f;
        private bool _enableDirectionalSquash = false;
        private bool _enableDecelerationSquash = true;
        protected Vector3 GetScaleSquash(Vector3 velocity, float velocityMagnitude, float deltaTime)
        {
            if (velocityMagnitude == 0f) return Vector3.one;

            var prevLocalVelocity = _prevVelocity;

            var velocityDir = velocity * (1f / velocityMagnitude);

            var absVelocityDir = new Vector3(Mathf.Abs(velocityDir.x), Mathf.Abs(velocityDir.y), Mathf.Abs(velocityDir.z));

            // Initialize stretch as neutral
            Vector3 stretch = Vector3.one;

            if (_enableDirectionalSquash)
            {
                stretch += absVelocityDir * (_scaleStretchFactor * velocityMagnitude);
                // Clamp directional stretch
                var floor = 1f - _scaleMaxStretch;
                var ceiling = 1f + _scaleMaxStretch;
                stretch = new Vector3(
                    Mathf.Clamp(stretch.x, floor, ceiling),
                    Mathf.Clamp(stretch.y, floor, ceiling),
                    Mathf.Clamp(stretch.z, floor, ceiling)
                    );
            }
            // Look for decreasing deceleration (check from previous frame)
            // and increasing acceleration for momentum reversal squash.
            if (_enableDecelerationSquash)
            {
                var acceleration = (velocity - prevLocalVelocity) * (1f / deltaTime);

                // Project acceleration onto direction to get deceleration if negative
                var deceleration = Vector3.Dot(acceleration, velocityDir);

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
                if (deceleration > 0f)
                {
                    // Get squash amount
                    var squashAmount = deceleration * _scaleDecelerationSquashFactor;
                    // Clamp squash amount
                    squashAmount = Mathf.Clamp(squashAmount, 0f, _maxSquash);
                    // Apply squash amount to velocity axes
                    var squashScale = Vector3.one + (-absVelocityDir * squashAmount);
                    // Expand perpendicular axes
                    var perpendicularScale = Vector3.one + (Vector3.one - absVelocityDir) * (squashAmount * 0.5f);
                    // Multiply scale vectors
                    squashScale = Vector3.Scale(squashScale, perpendicularScale);

                    stretch = Vector3.Scale(stretch, squashScale);

                    // 
                    //_waitForDeceleration = false;

                    KS.Logger.LogDebug($"reverseMoment[{deceleration < 0f}]:{deceleration:F3} DecelerationStretch({stretch.x:F3},{stretch.y:F3},{stretch.z:F3})");
                }
                else
                {
                    KS.Logger.LogDebug($"reverseMoment[{deceleration < 0f}]:{deceleration:F3}");
                    //_waitForDeceleration = true;
                }
                  
                //_prevDeceleration = deceleration;
            }
            // Preserve original volume
            var stretchVolume = stretch.x * stretch.y * stretch.z;
            var volumeCorrection = Mathf.Pow(_originalVolume / stretchVolume, (1f / 3f));

            var finalScale = Vector3.Lerp(_prevScale, stretch * volumeCorrection, deltaTime * _scaleSquashLerpSpeed);

            //KS.Logger.LogDebug($"stretch({stretch.x:F3},{stretch.y:F3},{stretch.z:F3}) volumeCorrection[{volumeCorrection}] " +
            //    $"stretchVolume[{stretch.x + stretch.y + stretch.z}] " +
            //    $"finalScale({finalScale.x:F3},{finalScale.y:F3},{finalScale.z:F3}) " +
            //    $"finalVolume[{finalScale.x + finalScale.y + finalScale.z}]");
            _prevScale = finalScale;
            return finalScale;
        }

        //protected Vector3 ApplyScaleModification(Vector3 velocity, float deltaTime, float unscaledDeltaTime)
        //{
        //    // Avoid zero vector normalization
        //    if (velocity == Vector3.zero)
        //    {
        //        return Vector3.one;
        //    }

            //    // Apply Deceleration aka MomentumReversal squash
            //    var scaleDeltaTime = _scaleScalar * unscaledDeltaTime;

            //    var acceleration = (velocity - _prevVelocity);
            //    var velocityNormalized = velocity.normalized;
            //    var squashOffset = _squashOffset;

            //    //Reverse projected acceleration
            //    var deceleration = -Vector3.Project(acceleration, velocityNormalized);
            //    // Extract scalar projection (not a cosine)
            //    var decelMagnitude = Vector3.Dot(deceleration, velocityNormalized);

            //    var localRot = Bone.localRotation;

            //    var boneRight = localRot * Vector3.right;
            //    var boneUp = localRot * Vector3.up;
            //    var boneForward = localRot * Vector3.forward;
            //    //If strong deceleration
            //    if (decelMagnitude > 0f) // && velocity.magnitude < prevVelocity.magnitude)
            //    {
            //        var dir = _prevVelocityNormalized;
            //        // Small magnitude influence
            //        var magnitudeMultiplier = _squashIntensity * decelMagnitude * deltaTime; // * (1f / 10f);
            //        // Compress along movement axes
            //        var squash = new Vector3(
            //            Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(dir, boneRight)) * magnitudeMultiplier),
            //            Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(dir, boneUp)) * magnitudeMultiplier),
            //            Mathf.Clamp01(1f - Mathf.Abs(Vector3.Dot(dir, boneForward)) * magnitudeMultiplier)
            //        );

            //        // Invert squash to create perpendicular expansion
            //        var avg = (squash.x + squash.y + squash.z) * (1f / 3f);

            //        var deltaX = 1f - squash.x;
            //        var deltaY = 1f - squash.y;
            //        var deltaZ = 1f - squash.z;

            //        var inverseSquash = new Vector3(
            //            squash.x + deltaX * 0.5f + deltaY + deltaZ,
            //            squash.y + deltaY * 0.5f + deltaX + deltaZ,
            //            squash.z + deltaZ * 0.5f + deltaX + deltaY
            //        );
            //        // The smaller the average squash, the more inverse squash
            //        squashOffset = Vector3.Lerp(Vector3.one, inverseSquash, 1f - avg);

            //        //KS.Logger.LogDebug(
            //        //    $"Dot({Mathf.Abs(Vector3.Dot(dir, boneRight)):F2},{Mathf.Abs(Vector3.Dot(dir, boneUp)):F2},{Mathf.Abs(Vector3.Dot(dir, boneForward)):F2})" +
            //        //    $"magnitudeMultiplier[{magnitudeMultiplier:F2}]" +
            //        //    $"squash({squash.x:F2},{squash.y:F2},{squash.z:F2}) " +
            //        //    $"inverseSquash({inverseSquash.x:F2},{inverseSquash.y:F2},{inverseSquash.z:F2}) " +
            //        //    $"squashOffset({squashOffset.x:F2},{squashOffset.y:F2},{squashOffset.z:F2})"
            //        //    );
            //    }
            //    else
            //    {
            //        // Smoothly return squash offset to neutral (1,1,1)
            //        squashOffset = Vector3.Lerp(squashOffset, Vector3.one, unscaledDeltaTime * _squashDecaySpeed);
            //    }

            //    // Get normalized velocity direction
            //    //var dir = velocity.normalized;

            //    var speed = velocity.magnitude * scaleDeltaTime;

            //    // Calculate stretch scale (bigger with speed)
            //    var stretchAmount = Mathf.Clamp(1f + speed, 1f, _scaleMaxStretch);

            //    // Perpendicular squash (inverse of stretch to preserve volume feel)
            //    var squashAmount = Mathf.Clamp(1f - (stretchAmount - 1f) * 0.5f, _minSquashScale, 1f);


            //    var xStretch = Mathf.Abs(Vector3.Dot(velocityNormalized, boneRight));
            //    var yStretch = Mathf.Abs(Vector3.Dot(velocityNormalized, boneUp));
            //    var zStretch = Mathf.Abs(Vector3.Dot(velocityNormalized, boneForward));

            //    var baseScale = _originalVolume;

            //    // Compute new scale along local axes
            //    var newScale = new Vector3(
            //        baseScale.x * (1f + (stretchAmount - 1f) * xStretch),
            //        baseScale.y * (1f + (stretchAmount - 1f) * yStretch),
            //        baseScale.z * (1f + (stretchAmount - 1f) * zStretch)
            //        );



            //    //KS.Logger.LogDebug($"stretchAmount[{stretchAmount:F2}] squashAmount[{squashAmount:F2}] " +
            //    //    $"xStretch({xStretch:F2},{yStretch:F2},{zStretch:F2}) " +
            //    //    $"newScale({newScale.x:F2},{newScale.y:F2},{newScale.z:F2}) "
            //    //    );
            //    // Normalize to squash perpendicular axes
            //    //var stretchRatio = newScale.magnitude / _baseScaleMagnitude;
            //    newScale = new Vector3(
            //        Mathf.Lerp(newScale.x, baseScale.x * squashAmount, 1f - xStretch),
            //        Mathf.Lerp(newScale.y, baseScale.y * squashAmount, 1f - yStretch),
            //        Mathf.Lerp(newScale.z, baseScale.z * squashAmount, 1f - zStretch)
            //    );

            //    newScale = Vector3.Scale(newScale, squashOffset);
            //    newScale.z += _velocityDeltaZ;
            //    // Smoothly interpolate to new scale
            //    var scaleModifier = Vector3.Lerp(_prevScale, newScale, unscaledDeltaTime * _scaleSmoothing);
            //    //scaleModifier.z = 1f;

            //    //KS.Logger.LogDebug(
            //    //    $"squashOffset({squashOffset.x:F2},{squashOffset.y:F2},{squashOffset.z:F2})" +
            //    //    $"xStretch({xStretch:F2},{yStretch:F2},{zStretch:F2}) " +
            //    //    $"newScale({newScale.x:F2},{newScale.y:F2},{newScale.z:F2}) " +
            //    //    $"scaleModifier({scaleModifier.x:F2},{scaleModifier.y:F2},{scaleModifier.z:F2}) " +
            //    //    $"velocity({velocity.x:F4},{velocity.y:F4},{velocity.z:F4})");

            //    // Store local variables
            //    _prevVelocityNormalized = velocityNormalized;
            //    _prevScale = scaleModifier;
            //    _squashOffset = squashOffset;
            //    return scaleModifier;
            //}

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

        public enum Effect
        {
            None = 1,
            VelocityAxial = 2,
            VelocityAngular = 4,
            Tethering = 8,
            AccelerationScale = 16,
            DecelerationSquash = 32,
        }
    }
}
