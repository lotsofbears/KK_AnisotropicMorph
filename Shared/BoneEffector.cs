using ADV.Commands.Base;
using KKABMX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static AniMorph.AniMorph;
using static ExpressionBone;

namespace AniMorph
{
    internal class BoneEffector : BoneEffect
    {
        private const string Thigh1R = "cf_d_thigh01_R";
        private const string Thigh2R = "cf_d_thigh02_R";
        private const string Thigh3R = "cf_d_thigh03_R";

        private const string Bust = "cf_d_bust00";
        private const string Bust1L = "cf_d_bust01_L";
        private const string Bust1R = "cf_d_bust01_R";

        private const string Butt = "cf_j_waist02";
        private const string ButtL = "cf_d_siri_L";
        private const string ButtR = "cf_d_siri_R";

        private readonly ChaControl _chara;

        private readonly Dictionary<BoneName, BoneData> _mainDic = [];
        private readonly List<string> _returnToABMX = [];
        private readonly List<BoneName> _updateList = [];
        private static readonly List<string> _singleList =
            [
#if DEBUG
            Thigh1R,
            Thigh2R,
            Thigh3R,
#endif
        ];
#if DEBUG
        private static readonly List<string> _requiresBaselineUpdate =
            [
            Thigh1R,
            ];
#endif

        // Master comes first
        private static readonly List<List<string>> _tandemList =
            [
            [ Bust, Bust1L, Bust1R ],
            [ Butt, ButtL, ButtR ],
            ];

        // BodyPart + boneNames-defaultMass pairs
        private static readonly Dictionary<Body, BodyPartMeasurement> _bonesToCheckForSizeDic = new()
        {
            { Body.Breast, 
                new (
                    [ "cf_s_bust00_L", "cf_d_bust01_L", "cf_d_bust02_L", "cf_d_bust03_L"], 
                    1.287239f
                    ) 
            },
            { Body.Butt, 
                new (
                    ["cf_s_siri_L" ], 
                    1.457057f
                    ) 
            },
        };

        private readonly Dictionary<Body, float> _bodyPartSizeDic = [];

        private bool _updated;

        internal BoneEffector(ChaControl chara)
        {
            _chara = chara;

            Setup();
            OnConfigUpdate();
        }

        internal void OnUpdate()
        {
            _updated = false;
        }

        private void Setup()
        {
            // Setup bones for effector

            //// Required for mesh measurements
            //var skinnedMesh = (_chara.rendBody.GetComponent<SkinnedMeshRenderer>());

            //if (skinnedMesh == null)
            //{
            //    AniMorph.Logger.LogDebug($"{GetType().Name} couldn't find mesh.");
            //    return;
            //}
            //var bakedMesh = new Mesh();
            //skinnedMesh.BakeMesh(bakedMesh);


            // Set mass for each bone based on the scale
            var allBones = _chara.transform.GetComponentsInChildren<Transform>();

            foreach (var keyValuePair in _bonesToCheckForSizeDic)
            {
                var bonesToCheck = allBones.Where(t => keyValuePair.Value.bonesToMeasure.Contains(t.name));

                var bodyPartScale = 1f;

                if (AdjustForBoneSize(keyValuePair.Key))
                {
                    // Missing bones
                    if (bonesToCheck.Count() != keyValuePair.Value.bonesToMeasure.Length)
                    {
                        AniMorph.Logger.LogWarning($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}:Couldn't find all bones for measurement of {keyValuePair.Key}, falling back to the default.");
                        bodyPartScale = keyValuePair.Value.defaultMass;
                    }
                    else
                    {
                        foreach (var bone in bonesToCheck)
                        {
                            bodyPartScale *= bone.localScale.x * bone.localScale.y * bone.localScale.z;
                        }
                    }
#if DEBUG
                    AniMorph.Logger.LogDebug($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}:bodyPartScale[{bodyPartScale}] defaultMass[{keyValuePair.Value.defaultMass}]");
#endif
                    bodyPartScale /= keyValuePair.Value.defaultMass;
                }

                _bodyPartSizeDic.Add(keyValuePair.Key, bodyPartScale);
            }

            // Iterate through singular items
            foreach (var single in _singleList)
            {
                if (FindBone(_chara, single, out var boneTransform)
                    && GetEnumBoneName(single, out var boneEnum))
                {
                    _returnToABMX.Add(single);
                    _updateList.Add(boneEnum);
                    Transform centeredBoneTransform = null;
                    if (GetCenteredBone(boneEnum, out var centeredBone))
                    {
                        FindBone(_chara, centeredBone, out centeredBoneTransform);
                    }

                    AddToDic(boneEnum, boneTransform, centeredBoneTransform, null, null); // bakedMesh, skinnedMesh);
                }
                /*
                 * An ABMX Plugin that applies a variety of highly customizable effects to a variety of bones to create an illusion of them behaving in a more chaotic–soft way. 
                 * Works both the Main Game and the Studio but not extensively tested in the latter. Currently only the breast and the butt, other bones wait for an update of ABMX.
                 * 
                 * **How it works**
                 * Very simple, it reads (on early LateUpdate) whatever animator wrote for a frame and applies it in a ~~retarded~~, slow, not immediate fashion with some adjustments.
                 * After that some bones are also influenced by the Dynamic Bone (default of the game) which amplifies them by own calculations. 
                 * 
                 * **Requirements**
                 * [ABMX](https://github.com/ManlyMarco/ABMX/)
                 * 
                 * **Download*
                 * https://github.com/lotsofbears/KK_AnisotropicMorph/releases
                 * 
                 * **Readme and source code**
                 * https://github.com/lotsofbears/KK_AnisotropicMorph
                 * 
                 * 
                 */
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
                    // Some masters track one bone but apply to another.
                    _returnToABMX.Add(master);
                    // Master updates his slaves
                    _updateList.Add(masterEnum);

                    // Init master with slaves together
                    AddToDicTandem(masterEnum, slaveEnums, masterTransform, slaveTransforms, null, null); // bakedMesh, skinnedMesh);
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
                var boneModifierSlaves = new BoneModifierSlave[slaves.Length];
                // Bit of an oversight with boneModifierData as modifiers got access
                // to it much later in the development, so we add it twice on init.
                var boneModifierDataSlaves = new BoneModifierData[slaves.Length];
                for (var i = 0; i < slaves.Length; i++)
                {
                    boneModifierDataSlaves[i] = new();
                    boneModifierSlaves[i] = new BoneModifierSlave(slaveTransforms[i], masterTransform, bakedMesh, skinnedMesh, boneModifierDataSlaves[i]);
                    _mainDic.Add(slaves[i], new BoneData(boneModifierSlaves[i], boneModifierDataSlaves[i]));
                }

                // Add master with slaves
                var boneModifierDataMaster = new BoneModifierData();
                _mainDic.Add(master, new BoneData(new BoneModifierMaster(masterTransform, boneModifierSlaves, boneModifierDataMaster), boneModifierDataMaster));

            }
            bool GetCenteredBone(BoneName boneName, out string centeredBone)
            {
                centeredBone = boneName switch
                {
                    BoneName.Bust1L => Bust,
                    BoneName.Bust1R => Bust,
                    BoneName.ButtL => Butt,
                    BoneName.ButtR => Butt,
                    _ => "",
                };
                return !centeredBone.IsNullOrEmpty();
            }
            static bool AdjustForBoneSize(Body body)
            {
                return body switch
                {
                    Body.Breast => BreastAdjustForSize.Value,
                    Body.Butt => ButtAdjustForSize.Value,
                    _ => false,
                };
            }
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
                Butt => _mainDic[BoneName.Butt].boneModifierData,
                ButtL => _mainDic[BoneName.ButtL].boneModifierData,
                ButtR => _mainDic[BoneName.ButtR].boneModifierData,
                Thigh1R => _mainDic[BoneName.Thigh1R].boneModifierData,
                Thigh2R => _mainDic[BoneName.Thigh2R].boneModifierData,
                Thigh3R => _mainDic[BoneName.Thigh3R].boneModifierData,

                _ => null
            };

        }

        private void UpdateModifiers()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime == 0f) return;

            // Opt for lesser evil during the lag spike,
            if (deltaTime > 1f / 15f) deltaTime = 1f / 15f;

            var fps = 1f / deltaTime;
            foreach (var key in _updateList)
            {
                _mainDic[key].boneModifier.UpdateModifiers(deltaTime, fps);
            }
        }

        internal void OnConfigUpdate()
        {
            foreach (var keyValuePair in _mainDic)
            {
                var bodyPart = GetBodyPart(keyValuePair.Key);
                keyValuePair.Value.boneModifier.OnConfigUpdate(bodyPart);

                var mass = _bodyPartSizeDic.TryGetValue(bodyPart, out var value) ? value : 1f;

                keyValuePair.Value.boneModifier.SetMass(mass);
            }

            static Body GetBodyPart(BoneName boneName) => boneName switch
            {
                BoneName.Bust or BoneName.Bust1L or BoneName.Bust1R => Body.Breast,
                BoneName.Butt or BoneName.ButtL or BoneName.ButtR => Body.Butt,
                BoneName.Thigh1R or BoneName.Thigh2R or BoneName.Thigh3R => Body.Thigh,
                _ => 0
            };
        }


        internal void OnChangeAnimator()
        {
            foreach (var entry in _updateList)
            {
                _mainDic[entry].boneModifier.OnChangeAnimator();
            }
        }
        internal void OnSetPlay(string animName)
        {

        }

        internal void OnDisable()
        {
            foreach (var value in _mainDic.Values)
            {
                value.boneModifierData.Clear();
            }
            _updated = true;
        }

        private bool GetEnumBoneName(string boneName, out BoneName enumBoneName)
        {
            enumBoneName = boneName switch
            {
                Bust => BoneName.Bust,
                Bust1L => BoneName.Bust1L,
                Bust1R => BoneName.Bust1R,
                Butt  => BoneName.Butt,
                ButtL => BoneName.ButtL,
                ButtR => BoneName.ButtR,
                Thigh1R => BoneName.Thigh1R,
                Thigh2R => BoneName.Thigh2R,
                Thigh3R => BoneName.Thigh3R,
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
            Butt,
            ButtL,
            ButtR,
            Thigh1R,
            Thigh2R,
            Thigh3R,
            
        }
        private readonly struct BodyPartMeasurement(string[] bonesToMeasure, float defaultMass)
        {
            internal readonly string[] bonesToMeasure = bonesToMeasure;
            internal readonly float defaultMass = defaultMass;
        }
    }
}
