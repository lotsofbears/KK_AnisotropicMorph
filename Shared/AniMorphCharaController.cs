using KKABMX.Core;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Utilities;
using Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AniMorph
{
    internal class AniMorphCharaController : CharaCustomFunctionController
    {
        public BoneEffector BoneEffector => _boneEffector;

        private BoneEffector _boneEffector;

        private float _degPerSec = 0f;


        private bool IsHScene
        {
            get
            {
#if KK
                return Scene.Instance.AddSceneName.Equals("HProc");
#else
                return Scene.AddSceneName.Equals("HProc");
#endif
            }
        }

        protected override void Start()
        {
            base.Start();
            if (!IsHScene || ChaControl == null || ChaControl.sex == 0)
            {
                Destroy(this);
            }
            else
            {
                StartCoroutine(StartCo());
            }
        }

        private IEnumerator StartCo()
        {
            var count = 0;
            var endOfFrame = CoroutineUtils.WaitForEndOfFrame;
            while (Time.deltaTime > 1f / 30f) // || count++ < 1000)
            {
                AniMorph.Logger.LogDebug("StartCo:deltaTime wait");
                yield return endOfFrame;
            }

            var boneController = ChaControl.GetComponent<BoneController>();
            if (boneController == null)
            {
                AniMorph.Logger.LogDebug("StartCo:no bone controller");
                Destroy(this);
            }
            else
            {
                if (_boneEffector == null)
                {
                    AniMorph.Logger.LogDebug("StartCo:no boneEffector");
                    _boneEffector = new BoneEffector(ChaControl);
                    boneController.AddBoneEffect(_boneEffector);
                }

            }
            //enabled = AniM.Enable.Value;
        }

        protected override void Update()
        {
            base.Update();

            // DEBUG 
            if (_degPerSec > 0f) ChaControl.transform.rotation = Quaternion.Euler(0f, _degPerSec * Time.deltaTime, 0f) * ChaControl.transform.rotation;

            _boneEffector?.OnUpdate();
        }

        protected override void OnReload(GameMode currentGameMode)
        {

        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {

        }

        internal void OnChangeAnimator()
        {

        }

        internal void OnSetPlay()
        {

        }
    }
}
