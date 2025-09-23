using ADV.Commands.Base;
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
        private const string Bust1L = "cf_d_bust01_L";
        private const string Bust1R = "cf_d_bust01_R";
        private const float _hugeFrameTime = 1f / 30f;

        private readonly ChaControl _chara;

        private readonly Dictionary<BoneName, BoneData> _mainDic = [];
        private readonly List<string> _returnToABMX = [];
        private readonly List<BoneName> _updateList = [];
        private static readonly List<string> _lonerList =
            [
            //Bust1R,
            //Thigh2R,
            ];

        // Master comes first
        private static readonly List<List<string>> _tandemList =
            [
            [ Bust, Bust1L, Bust1R ],
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
            // Setup bones for effector

            // Required for mesh measurements
            var skinnedMesh = (_chara.rendBody.GetComponent<SkinnedMeshRenderer>());

            if (skinnedMesh == null)
            {
                KS.Logger.LogDebug($"{GetType().Name} couldn't find mesh.");
                return;
            }
            var bakedMesh = new Mesh();
            skinnedMesh.BakeMesh(bakedMesh);

            // Iterate through singular items
            foreach (var loner in _lonerList)
            {
                if (FindBone(_chara, loner, out var boneTransform)
                    && GetEnumBoneName(loner, out var boneEnum))
                {
                    _returnToABMX.Add(loner);
                    _updateList.Add(boneEnum);

                    AddToDic(boneEnum, boneTransform, null, bakedMesh, skinnedMesh);
                }
            }

            // Iterate through tandems
            foreach (var boneNames in _tandemList)
            {
                // Skip if master without slaves
                if (boneNames.Count < 2) continue;

                // Prepare arrays for init under master
                var slaveEnums = new BoneName[boneNames.Count - 1];
                var slaveTransforms = new Transform[boneNames.Count - 1];
                for (var i = 1; i < boneNames.Count; i++)
                {
                    var slave = boneNames[i];
                    if (FindBone(_chara, slave, out var slaveTransform)
                        && GetEnumBoneName(slave, out var slaveEnum))
                    {
                        // Slaves don't get own update
                        _returnToABMX.Add(slave);
                        // Fill in arrays for master
                        slaveEnums[i - 1] = slaveEnum;
                        slaveTransforms[i - 1] = slaveTransform;
                    }
                }

                var master = boneNames[0];
                if (FindBone(_chara, master, out var masterTransform)
                    && GetEnumBoneName(master, out var masterEnum))
                {
                    _returnToABMX.Add(master);
                    // Master updates his slaves
                    _updateList.Add(masterEnum);

                    // Init master with slaves together
                    AddToDicTandem(masterEnum, slaveEnums, masterTransform, slaveTransforms, bakedMesh, skinnedMesh);
                }
            }

            static bool FindBone(ChaControl chara, string boneName, out Transform bone)
            {
                bone = chara.objBodyBone.transform.GetComponentsInChildren<Transform>(includeInactive: true)
                    .Where(t => t.name.Equals(boneName))
                    .FirstOrDefault();

                return bone != null;
            }

            void AddToDic(BoneName boneName, Transform boneTransform, Transform centeredBone, Mesh bakedMesh, SkinnedMeshRenderer skinnedMesh)
            {
                // Perform null checks
                if (_mainDic.ContainsKey(boneName) || boneTransform == null) return;

                var boneModifierData = new BoneModifierData();
                _mainDic.Add(boneName, new BoneData(new BoneModifier(boneTransform, centeredBone, bakedMesh, skinnedMesh, boneModifierData), boneModifierData));
            }

            void AddToDicTandem(BoneName master, BoneName[] slaves, Transform masterTransform, Transform[] slaveTransforms, Mesh bakedMesh, SkinnedMeshRenderer skinnedMesh)
            {
                // Perform null checks
                if (_mainDic.ContainsKey(master) || masterTransform == null) return;
                for (var i = 0;  i < slaves.Length; i++)
                {
                    if (_mainDic.ContainsKey(slaves[i]) || slaveTransforms[i] == null) return;
                }

                // Add and organize slaves
                var boneModifierSlaves = new BoneModifier[slaves.Length];
                // Bit of an oversight with boneModifierData as modifiers got access
                // to it much later in the development, so we add it twice on init.
                var boneModifierDataSlaves = new BoneModifierData[slaves.Length];
                for (var i = 0; i < slaves.Length; i++)
                {
                    boneModifierDataSlaves[i] = new();
                    boneModifierSlaves[i] = new BoneModifier(slaveTransforms[i], masterTransform, bakedMesh, skinnedMesh, boneModifierDataSlaves[i]);
                    _mainDic.Add(slaves[i], new BoneData(boneModifierSlaves[i], boneModifierDataSlaves[i]));
                }

                // Add master with slaves
                var boneModifierDataMaster = new BoneModifierData();
                _mainDic.Add(master, new BoneData(new MasterBoneModifier(masterTransform, boneModifierSlaves, boneModifierDataMaster), boneModifierDataMaster));

            }
            //string GetCenteredBone(BoneName boneName)
            //{
            //    return boneName switch
            //    {
            //        BoneName.Bust1R => Bust,
            //        _ => "",
            //    };

            //}
        }

        public override IEnumerable<string> GetAffectedBones(BoneController origin) => _returnToABMX;

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
                Bust1L => _mainDic[BoneName.Bust1L].boneModifierData,
                Bust1R => _mainDic[BoneName.Bust1R].boneModifierData,
                Thigh2R => _mainDic[BoneName.Thigh2R].boneModifierData,
                _ => null
            };

        }
        private void UpdateModifiers()
        {
            var deltaTime = Time.deltaTime;
            var unscaledDeltaTime = Time.unscaledDeltaTime;
            if (deltaTime > _hugeFrameTime) deltaTime = _hugeFrameTime;

            foreach (var boneNameEnum in _updateList)
            {
                _mainDic[boneNameEnum].boneModifier.UpdateModifiers(deltaTime, unscaledDeltaTime);
            }
        }
        internal void OnConfigUpdate()
        {

        }
        internal void OnChangeAnimator()
        {

        }
        internal void OnSetPlay(string animName)
        {

        }

        private bool GetEnumBoneName(string boneName, out BoneName enumBoneName)
        {
            enumBoneName = boneName switch
            {
                Bust => BoneName.Bust,
                Bust1L => BoneName.Bust1L,
                Bust1R => BoneName.Bust1R,
                Thigh2R => BoneName.Thigh2R,
                _ => BoneName.None
            };
            return enumBoneName != BoneName.None;
        }

        private enum BoneName
        {
            None,
            Bust1L,
            Bust1R,
            Bust,
            Thigh2R,
        }
    }
}
