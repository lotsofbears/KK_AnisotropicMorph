using HarmonyLib;
using KKAPI.Chara;
using System;
using System.Collections.Generic;
using System.Text;

namespace KineticShift
{
    internal class Hooks
    {
        private static Harmony _patch;

        private static Func<bool> InTransition;


        public static bool CrossFaderInTransition
        {
            get
            {
                if (InTransition == null) return false;

                return InTransition.Invoke();
            }
        }

        public static void ApplyHooks()
        {
            if (_patch != null) return;

            _patch = Harmony.CreateAndPatchAll(typeof(Hooks));

            SetupDelegates();
        }
        private static void SetupDelegates()
        {
            var type = AccessTools.TypeByName("KK_VR.Features.CrossFader");

            if (type == null) return;

            var methodInfo = AccessTools.PropertyGetter(type, "InTransition");

            if (methodInfo != null)
            {
                InTransition = AccessTools.MethodDelegate<Func<bool>>(methodInfo);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.ChangeAnimator))]
        public static void HSceneProc_ChangeAnimator_Postfix(HSceneProc.AnimationListInfo _nextAinmInfo)
        {
            var type = typeof(KSCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    ((KSCharaController)charaController).BoneEffector.OnChangeAnimator();
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(HActionBase), nameof(HActionBase.SetPlay))]
        public static void HActionBase_SetPlay_Postfix(string _nextAnimation)
        {
            var type = typeof(KSCharaController);
            foreach (var charaController in CharacterApi.GetBehaviours())
            {
                if (charaController != null && charaController.GetType() == type)
                {
                    ((KSCharaController)charaController).BoneEffector.OnSetPlay(_nextAnimation);
                }
            }  
        }
    }
}
