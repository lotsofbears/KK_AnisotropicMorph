using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Utilities;
using Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                return scene.Equals(String.Empty) || scene.Equals("HProc");
            }
        }

        protected override void Start()
        {
            base.Start();

            if (!IsProperScene || ChaControl == null)
            {
                Destroy(this);
            }
            else
            {
                StartCoroutine(StartCo());

                // Enable if chara is male and male setting is selected, same for female.
                HandleEnable();
#if DEBUG
                _bust = ChaControl.transform.GetComponentsInChildren<Transform>()
                    .Where(t => t.name.Equals("cf_j_waist02"))
                    .FirstOrDefault();
                var flag = FindObjectOfType<HFlag>();
                _camera = flag.ctrlCamera.transform.parent;
#endif
            }
        }
        internal void HandleEnable()
        {
            var setting = AniMorph.Enable.Value;
            enabled = 
                (ChaControl.sex == 0 && (setting & AniMorph.Gender.Male) != 0) 
                || 
                (ChaControl.sex == 1 && (setting & AniMorph.Gender.Female) != 0);
        }

        private IEnumerator StartCo()
        {
            // Wait for loading-scene-lag to avoid delta time spikes and chara teleportation.
            var endOfFrame = CoroutineUtils.WaitForEndOfFrame;
            while (Time.deltaTime > 1f / 30f) // || count++ < 1000)
            {
#if DEBUG
                AniMorph.Logger.LogDebug("StartCo:deltaTime wait");
#endif
                yield return endOfFrame;
            }

            var boneController = ChaControl.GetComponent<BoneController>();
            if (boneController == null)
            {
                Destroy(this);
            }
            else
            {
                if (_boneEffector == null)
                {
                    _boneEffector = new BoneEffector(ChaControl);
                    boneController.AddBoneEffect(_boneEffector);
                }

            }
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

        protected override void OnReload(GameMode currentGameMode)
        {

        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {

        }
    }
}
