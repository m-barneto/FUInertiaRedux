﻿

using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace FUInertiaRedux {
    [BepInPlugin("Mattdokn.FUInertiaRedux", "FUInertiaRedux", "1.0.1")]
    public class Plugin : BaseUnityPlugin {

        ConfigEntry<float> accelerationSpeed;
        ConfigEntry<float> decelerationSpeed;
        ConfigEntry<float> directionChangeSpeed;
        ConfigEntry<float> motionPreservation;
        ConfigEntry<float> poseChangeSpeed;
        ConfigEntry<float> rotationSpeed;
        ConfigEntry<float> initialSprintSpeed;
        ConfigEntry<bool> removeJumpDelay;

        static ConfigEntry<bool> enableServersideInertiaSettings;



        void Awake() {
            EFTHardSettings.Load();


            accelerationSpeed = Config.Bind("Hard Settings", "Acceleration Speed", 100f, 
                new ConfigDescription("How fast you approach full speed.\nVanilla Value: 1.0", null, 
                new ConfigurationManagerAttributes { Order = 0 }));
            decelerationSpeed = Config.Bind("Hard Settings", "Deceleration Speed", 10f, 
                new ConfigDescription("Self explanatory.\nVanilla Value: 1.2", null, 
                new ConfigurationManagerAttributes { Order = -1 }));
            directionChangeSpeed = Config.Bind("Hard Settings", "Direction Change Speed", 20f, 
                new ConfigDescription("How fast you change direction, can cause weird snapping effect if value too high when sprinting and pressing A/D.\nVanilla Value: 5.0", null, 
                new ConfigurationManagerAttributes { Order = -2 }));
            motionPreservation = Config.Bind("Hard Settings", "Motion Preservation", 0f, 
                new ConfigDescription("Motion preservation when you let go of all keys.\nVanilla Value: 0.8", null, 
                new ConfigurationManagerAttributes { Order = -3 }));
            poseChangeSpeed = Config.Bind("Hard Settings", "Pose Change Speed", 5f, 
                new ConfigDescription("How fast you can change height (crouching->standing).\nVanilla Value: 3.0", null, 
                new ConfigurationManagerAttributes { Order = -4 }));
            rotationSpeed = Config.Bind("Hard Settings", "Rotation Speed", 100f, 
                new ConfigDescription("How snapped the character is to your camera's facing direction.\nVanilla Value: 5.0", null, 
                new ConfigurationManagerAttributes { Order = -5 }));
            initialSprintSpeed = Config.Bind("Hard Settings", "Starting Sprint Speed", 10f, 
                new ConfigDescription("Initial sprinting speed.\nVanilla Value: 0.5", null, 
                new ConfigurationManagerAttributes { Order = -6 }));
            removeJumpDelay = Config.Bind("Hard Settings", "Remove Jump Delay", true, 
                new ConfigDescription("Removes the delay between landing and your next jump.", null, 
                new ConfigurationManagerAttributes { Order = -7 }));

            enableServersideInertiaSettings = Config.Bind("Server Settings", "Enabled", true, "Overrides server-side inertia settings. REQUIRES EXITING RAID");

            new GetGlobalConfigPatch().Enable();

            ApplyHardSettings();
        }

        void ApplyHardSettings() {
            EFTHardSettings eftHardSettings = EFTHardSettings.Instance;

            eftHardSettings.CHARACTER_SPEED_CHANGING_SPEED = accelerationSpeed.Value;
            eftHardSettings.DecelerationSpeed = decelerationSpeed.Value;
            eftHardSettings.DIRECTION_LERP_SPEED = directionChangeSpeed.Value;
            eftHardSettings.IdleStateMotionPreservation = motionPreservation.Value;
            eftHardSettings.POSE_CHANGING_SPEED = poseChangeSpeed.Value;
            eftHardSettings.TRANSFORM_ROTATION_LERP_SPEED = rotationSpeed.Value;
            eftHardSettings.StartingSprintSpeed = initialSprintSpeed.Value;

            if (removeJumpDelay.Value) {
                eftHardSettings.JUMP_DELAY_BY_SPEED = AnimationCurve.Constant(0f, 1f, 0f);
            } else {
                eftHardSettings.JUMP_DELAY_BY_SPEED = new AnimationCurve(new Keyframe[] {
                new Keyframe(0.00000f, 0.30000f, Mathf.Infinity, 0.00000f, 0.33333f, 0.33333f),
                new Keyframe(0.30000f, 0.00000f, Mathf.Infinity, Mathf.Infinity, 0.33333f, 0.33333f),
                new Keyframe(0.99958f, -0.00007f, 0.00000f, 0.00000f, 0.33333f, 0.33333f) });
            }
        }

        void SetNoInertiaHardSettings() {
            EFTHardSettings eftHardSettings = EFTHardSettings.Instance;

            eftHardSettings.JUMP_DELAY_BY_SPEED = AnimationCurve.Constant(0f, 1f, 0f);

            eftHardSettings.CHARACTER_SPEED_CHANGING_SPEED = 100f;
            eftHardSettings.DecelerationSpeed = 10f;
            eftHardSettings.DIRECTION_LERP_SPEED = 20f;
            eftHardSettings.IdleStateMotionPreservation = 0f;
            eftHardSettings.POSE_CHANGING_SPEED = 5f;
            eftHardSettings.TRANSFORM_ROTATION_LERP_SPEED = 100f;
            eftHardSettings.StartingSprintSpeed = 10f;
        }

        void SetDefaultInertiaHardSettings() {
            EFTHardSettings eftHardSettings = EFTHardSettings.Instance;

            eftHardSettings.JUMP_DELAY_BY_SPEED = new AnimationCurve(new Keyframe[] {
                new Keyframe(0.00000f, 0.30000f, Mathf.Infinity, 0.00000f, 0.33333f, 0.33333f),
                new Keyframe(0.30000f, 0.00000f, Mathf.Infinity, Mathf.Infinity, 0.33333f, 0.33333f),
                new Keyframe(0.99958f, -0.00007f, 0.00000f, 0.00000f, 0.33333f, 0.33333f) });

            eftHardSettings.CHARACTER_SPEED_CHANGING_SPEED = 1f;
            eftHardSettings.DecelerationSpeed = 1.2f;
            eftHardSettings.DIRECTION_LERP_SPEED = 5f;
            eftHardSettings.IdleStateMotionPreservation = 0.8f;
            eftHardSettings.POSE_CHANGING_SPEED = 3f;
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
                if (enableServersideInertiaSettings.Value) {
                    Logger.LogInfo("Updating server-side inertia settings.");
                    var inertiaSettings = backendConfig.Config.Inertia;

                    inertiaSettings.BaseJumpPenalty = 0.03f;
                    // inertiaSettings.CrouchSpeedAccelerationRange.x = 0.95f;
                    // inertiaSettings.CrouchSpeedAccelerationRange.y = 1.5f;
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
                    inertiaSettings.WalkInertia.y = 0.0025f;

                    inertiaSettings.InertiaLimitsStep = 0.1f;
                }
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
