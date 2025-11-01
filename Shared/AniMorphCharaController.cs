using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using KKAPI.Studio;
using KKAPI.Utilities;
using Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AniMorph
{
    internal class AniMorphCharaController : CharaCustomFunctionController
    {
        public BoneEffector BoneEffector => _boneEffector;

        private BoneEffector _boneEffector;
#if DEBUG

        private bool _rotate;

        private Quaternion _rotation;

        public Vector3 SetRotation
        {
            get => _rotation.eulerAngles;
            set
            {
                _rotate = value != Vector3.zero;
                _rotation = Quaternion.Euler(value);
            }
        }
        private bool _follow;
        private bool Follow
        {
            get => _follow;
            set  
            { 
                _follow = value; 
                _prevPosition = _bust.position; 
                _prevRotation = _bust.rotation;
            }
        }

        private Transform _bust;
        private Transform _camera;
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
#endif

        private void OnDisable()
        {
            _boneEffector?.OnDisable();
        }

        private bool IsProperScene
        {
            get
            {
                var scene =
#if KK
                 Scene.Instance.AddSceneName;
#elif KKS
                 Scene.AddSceneName;
#endif
#if DEBUG
                AniMorph.Logger.LogDebug($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}:scene[{scene}] insideStudio[{StudioAPI.InsideStudio}] hScene[{GameAPI.InsideHScene}]");
#endif
                return StudioAPI.InsideStudio || scene.Equals("HProc");
            }
        }

        internal void HandleEnable(bool forceStart = false)
        {
            var setting = AniMorph.Enable.Value;

            var wasEnabled = enabled;

            // Enable if chara is male and male setting is selected, same for female.
            enabled = 
                (ChaControl.sex == 0 && (setting & AniMorph.Gender.Male) != 0) 
                || 
                (ChaControl.sex == 1 && (setting & AniMorph.Gender.Female) != 0);

            var boneController = ChaControl.GetComponent<BoneController>();
            if (boneController == null)
            {
                throw new Exception($"No ABMX BoneController on {ChaControl.name}");
            }


            if (forceStart || wasEnabled != enabled)
            {
                StopAllCoroutines();

                if (enabled)
                {
                    if (forceStart) RemoveBoneEffector();

                    if (_boneEffector == null)
                    {
                        StartCoroutine(StartCo(boneController));
                    }
                }
                else
                {
                    RemoveBoneEffector();
                }
            }
            void RemoveBoneEffector()
            {
                if (_boneEffector == null) return;

                boneController.RemoveBoneEffect(_boneEffector);
                _boneEffector = null;
                boneController.NeedsBaselineUpdate = true;
            }

        }

        private IEnumerator StartCo(BoneController boneController)
        {
            // Wait for loading-scene-lag to avoid delta time spikes and chara teleportation.
            // Requires atleast a frame wait because we are ahead of ABMX init, 3 for a better measurement.
            var count = 3;
            var endOfFrame = CoroutineUtils.WaitForEndOfFrame;
            while (count-- > 0 ||Time.deltaTime > 1f / 30f) // || count++ < 1000)
            {
#if DEBUG
                AniMorph.Logger.LogDebug($"StartCo:deltaTime[{Time.deltaTime:F3}]");
#endif
                yield return endOfFrame;
            }
            _boneEffector = new BoneEffector(ChaControl);
            boneController.AddBoneEffect(_boneEffector);
        }

        protected override void Update()
        {
            base.Update();
#if DEBUG
            if (_rotate) ChaControl.transform.rotation *= _rotation;

            if (_follow)
            {

                //_camera.rotation *= Quaternion.Inverse(_prevRotation) * _bust.rotation;
                _camera.position += _bust.position - _prevPosition;

                _prevPosition = _bust.position;
                _prevRotation = _bust.rotation;
            }
#endif

            _boneEffector?.OnUpdate();
        }

        public void OnConfigUpdate()
        {
            HandleEnable();
            _boneEffector?.OnConfigUpdate();
        }

        protected override void OnReload(GameMode currentGameMode)
        {
#if DEBUG
            AniMorph.Logger.LogDebug($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}:Pop");

#endif
            if (!IsProperScene || ChaControl == null)
            {
                Destroy(this);
            }
            else
            {
                HandleEnable(forceStart: true);
                //#if DEBUG
                //                _bust = ChaControl.transform.GetComponentsInChildren<Transform>()
                //                    .Where(t => t.name.Equals("cf_j_waist02"))
                //                    .FirstOrDefault();
                //                var flag = FindObjectOfType<HFlag>();
                //                _camera = flag.ctrlCamera.transform.parent;
                //#endif
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {

        }

    }
}
