

using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace FUInertiaRedux {
    [BepInPlugin("Mattdokn.FUInertiaRedux", "FUInertiaRedux", "1.0.0")]
    public class Plugin : BaseUnityPlugin {
        void Awake() {
            EFTHardSettings.Load();

            new GetGlobalConfigPatch().Enable();
            new SprintAccelerationPatch().Enable();
            new UpdateWeightLimitsPatch().Enable();

            SetNoInertiaHardSettings();
        }

        void SetNoInertiaHardSettings() {
            EFTHardSettings eftHardSettings = EFTHardSettings.Instance;

            eftHardSettings.JUMP_DELAY_BY_SPEED = AnimationCurve.Constant(0f, 1f, 0f);

            eftHardSettings.IdleStateMotionPreservation = 0f;
            eftHardSettings.DecelerationSpeed = 10f;
            eftHardSettings.DIRECTION_LERP_SPEED = 20f;
            eftHardSettings.POSE_CHANGING_SPEED = 5f;
            eftHardSettings.CHARACTER_SPEED_CHANGING_SPEED = 100f;
            eftHardSettings.TRANSFORM_ROTATION_LERP_SPEED = 100f;
            eftHardSettings.StartingSprintSpeed = 10f;
        }

        void SetDefaultInertiaHardSettings() {
            EFTHardSettings eftHardSettings = EFTHardSettings.Instance;

            eftHardSettings.JUMP_DELAY_BY_SPEED = new AnimationCurve(new Keyframe[] {
                new Keyframe(0.00000f, 0.30000f, Mathf.Infinity, 0.00000f, 0.33333f, 0.33333f),
                new Keyframe(0.30000f, 0.00000f, Mathf.Infinity, Mathf.Infinity, 0.33333f, 0.33333f),
                new Keyframe(0.99958f, -0.00007f, 0.00000f, 0.00000f, 0.33333f, 0.33333f) });

            eftHardSettings.IdleStateMotionPreservation = 0.8f;
            eftHardSettings.DecelerationSpeed = 1.2f;
            eftHardSettings.DIRECTION_LERP_SPEED = 5f;
            eftHardSettings.POSE_CHANGING_SPEED = 3f;
            eftHardSettings.CHARACTER_SPEED_CHANGING_SPEED = 1f;
            eftHardSettings.TRANSFORM_ROTATION_LERP_SPEED = 5f;
            eftHardSettings.StartingSprintSpeed = 0.5f;
        }

        public class GetGlobalConfigPatch : ModulePatch {
            protected override MethodBase GetTargetMethod() => typeof(Class263).GetMethod("GetGlobalConfig");

            [PatchPostfix]
            static void Postfix(ref Task<BackendConfigClass> __result) {
                __result = __result.ContinueWith(task => {
                    BackendConfigClass backendConfig = task.Result;
                    if (backendConfig != null) {
                        UpdateInertia(backendConfig);
                    }
                    return backendConfig;
                });
            }

            private static void UpdateInertia(BackendConfigClass backendConfig) {
                Logger.LogInfo("Updating server-side inertia settings.");
                var inertiaSettings = backendConfig.Config.Inertia;

                // Setting the float values
                inertiaSettings.BaseJumpPenalty = 0.03f;
                inertiaSettings.CrouchSpeedAccelerationRange.x = 4.75f;
                inertiaSettings.CrouchSpeedAccelerationRange.y = 7.5f;
                inertiaSettings.ExitMovementStateSpeedThreshold.x = 0.001f;
                inertiaSettings.ExitMovementStateSpeedThreshold.y = 0.001f;
                inertiaSettings.InertiaLimitsStep = 0.1f;
                inertiaSettings.MaxTimeWithoutInput.x = 0.01f;
                inertiaSettings.MaxTimeWithoutInput.y = 0.03f;
                inertiaSettings.PreSprintAccelerationLimits.x = 8f;
                inertiaSettings.PreSprintAccelerationLimits.y = 4f;
                inertiaSettings.SprintAccelerationLimits.x = 15f;
                inertiaSettings.SprintBrakeInertia.y = 0f;
                inertiaSettings.SprintTransitionMotionPreservation.x = 0.006f;
                inertiaSettings.SprintTransitionMotionPreservation.y = 0.008f;
                inertiaSettings.WalkInertia.x = 0.002f;
                inertiaSettings.WalkInertia.y = 0.025f;
                inertiaSettings.SuddenChangesSmoothness = 1f;
            }
        }
        public class SprintAccelerationPatch : ModulePatch {
            protected override MethodBase GetTargetMethod() => typeof(MovementContext).GetMethod("SprintAcceleration");

            [PatchPrefix]
            private static bool Prefix(MovementContext __instance, float deltaTime, Player ____player, GClass733 ____averageRotationX) {
                bool inRaid = Singleton<AbstractGame>.Instance.InRaid;
                bool flag = ____player.IsYourPlayer && inRaid;
                bool result;
                if (flag) {
                    float num = ____player.Physical.SprintAcceleration * deltaTime;
                    float num2 = (____player.Physical.SprintSpeed * __instance.SprintingSpeed + 1f) * __instance.StateSprintSpeedLimit;
                    float num3 = Mathf.Max(EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(Mathf.Abs((float)____averageRotationX.Average)), EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(2.1474836E+09f) * 2f);
                    num2 = Mathf.Clamp(num2 * num3, 0.1f, num2);
                    __instance.SprintSpeed = Mathf.Clamp(__instance.SprintSpeed + num * Mathf.Sign(num2 - __instance.SprintSpeed), 0.01f, num2);
                    result = false;
                } else {
                    result = true;
                }
                return result;
            }
        }

        public class UpdateWeightLimitsPatch : ModulePatch {
            protected override MethodBase GetTargetMethod() => typeof(GClass681).GetMethod("UpdateWeightLimits");

            [PatchPostfix]
            private static void Postfix(GClass681 __instance) {
                __instance.BaseInertiaLimits = Vector3.zero;
            }
        }
    }
}
