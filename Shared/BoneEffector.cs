using KKABMX.Core;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ExpressionBone;

namespace KineticShift
{
    internal class BoneEffector : BoneEffect
    {
        private const string Thigh2R = "cf_d_thigh02_R";
        private const string Thigh1R = "cf_d_thigh01_R";
        private const string Bust = "cf_d_bust00";
        private const string Bust0R = "cf_s_bust00_R";
        private const float _hugeFrameTime = 1f / 30f;

        private readonly ChaControl _chara;

        private readonly Dictionary<BoneName, BoneData> _mainDic = [];
        private readonly List<string> _activeNames = [];
        private static readonly List<string> _allNames =
            [
            Bust,
            //Thigh2R,
            ];
        private static readonly List<string> _auxNames =
            [

            ];

        private bool _updated;

        internal BoneEffector(ChaControl chara)
        {
            _chara = chara;

            Setup();
        }

        internal void OnUpdate()
        {
            _updated = false;
        }
        private void Setup()
        {
            // Setup bones for effector.

            var meshBone = (_chara.rendBody.GetComponent<SkinnedMeshRenderer>());

            if (meshBone == null)
            {
                KS.Logger.LogDebug($"{GetType().Name} couldn't find mesh.");
                return;
            }

            for (var i = 0; i < _allNames.Count; i++)
            {
                var boneName = _allNames[i];

                if (FindBone(_chara, boneName, out var boneTransform))
                {
                    _activeNames.Add(boneName);

                    AddToDic((BoneName)i, boneTransform);
                }
            }

            static bool FindBone(ChaControl chara, string boneName, out Transform bone)
            {
                bone = chara.objBodyBone.transform.GetComponentsInChildren<Transform>(includeInactive: true)
                    .Where(t => t.name.Equals(boneName))
                    .FirstOrDefault();

                return bone != null;
            }
            void AddToDic(BoneName boneName, Transform transform)
            {
                if (_mainDic.ContainsKey(boneName) || transform == null) return;

                _mainDic.Add(boneName, new BoneData(new(transform, meshBone, 0.5f)));
            }
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin) => _activeNames;

        public override BoneModifierData GetEffect(string bone, BoneController origin, ChaFileDefine.CoordinateType coordinate)
        {
            if (!_updated)
            {
                _updated = true;
                UpdateModifiers();
            }
            return bone switch
            {
                Bust => _mainDic[BoneName.Bust].boneModifierData,
                Thigh2R => _mainDic[BoneName.Thigh2R].boneModifierData,
                _ => null
            };

        }
        private void UpdateModifiers()
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > _hugeFrameTime) deltaTime = _hugeFrameTime;

            foreach (var value in _mainDic.Values)
            {
                var boneModifierData = value.boneModifierData;

                value.boneModifier.UpdateModifiers(deltaTime, out boneModifierData.PositionModifier, out boneModifierData.RotationModifier, out boneModifierData.ScaleModifier);
            }
        }
        internal void OnConfigUpdate()
        {

        }

        private enum BoneName
        {
            Bust,
            Thigh2R,
        }
    }
}
