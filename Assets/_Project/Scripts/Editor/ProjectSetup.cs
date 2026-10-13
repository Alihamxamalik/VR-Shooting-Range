using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace RangeControl.Editor
{
    /// <summary>
    /// The project settings this game depends on, written down as code: physics layers and which of them
    /// collide, the Quest player settings, the render budget and the OpenXR setup. Running it again is
    /// harmless, and it is the place to look to see how the project is configured and why.
    /// </summary>
    public static class ProjectSetup
    {
        public const int TargetsLayer = 6;
        public const int EnvironmentLayer = 7;
        public const int WeaponsLayer = 8;
        public const int PlayerLayer = 9;
        public const int InteractionLayer = 10;

        public const string MobilePipelineAsset = "Assets/Settings/Mobile_RPAsset.asset";

        /// <summary>What a bullet can hit: targets and the environment, never a weapon or the player.</summary>
        public static LayerMask BulletMask => (1 << TargetsLayer) | (1 << EnvironmentLayer);

        [MenuItem("Tools/Range Control/Configure Project", priority = 0)]
        public static void Configure()
        {
            ConfigureLayers();
            ConfigurePhysics();
            ConfigurePlayer();
            ConfigureRendering();
            ConfigureXR();
            AssetDatabase.SaveAssets();
            Debug.Log("Range Control: project configured for Meta Quest.");
        }

        static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(TargetsLayer).stringValue = "Targets";
            layers.GetArrayElementAtIndex(EnvironmentLayer).stringValue = "Environment";
            layers.GetArrayElementAtIndex(WeaponsLayer).stringValue = "Weapons";
            layers.GetArrayElementAtIndex(PlayerLayer).stringValue = "Player";
            layers.GetArrayElementAtIndex(InteractionLayer).stringValue = "Interaction";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigurePhysics()
        {
            // Targets are only ever hit by raycasts, and interaction zones only ever found by overlap queries,
            // so neither needs to collide with anything.
            for (var layer = 0; layer < 32; layer++)
            {
                Physics.IgnoreLayerCollision(TargetsLayer, layer, true);
                Physics.IgnoreLayerCollision(InteractionLayer, layer, true);
            }

            // Guns and magazines rest on the bench and the floor, but pass through each other and the player,
            // so an ejected magazine never gets kicked out of the gun it is leaving.
            Physics.IgnoreLayerCollision(WeaponsLayer, WeaponsLayer, true);
            Physics.IgnoreLayerCollision(WeaponsLayer, PlayerLayer, true);
            Physics.IgnoreLayerCollision(WeaponsLayer, EnvironmentLayer, false);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Ali Hamza";
            PlayerSettings.productName = "Range Control VR";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.alihamza.rangecontrolvr");
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        }

        static void ConfigureRendering()
        {
            // The budget from the design document: forward rendering, 4x MSAA, no HDR, no real-time shadows,
            // no depth or opaque texture copies. Everything here is paid for twice a frame, once per eye.
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobilePipelineAsset);
            if (pipeline == null)
            {
                Debug.LogWarning("Range Control: no URP asset at " + MobilePipelineAsset + "; rendering settings were left alone.");
                return;
            }

            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 0f;

            var serialized = new SerializedObject(pipeline);
            SetIfPresent(serialized, "m_MainLightShadowsSupported", false);
            SetIfPresent(serialized, "m_AdditionalLightShadowsSupported", false);
            SetIfPresent(serialized, "m_SoftShadowsSupported", false);
            SetIfPresent(serialized, "m_AdditionalLightsRenderingMode", 0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        static void SetIfPresent(SerializedObject serialized, string name, bool value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
                property.boolValue = value;
        }

        static void SetIfPresent(SerializedObject serialized, string name, int value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
                property.intValue = value;
        }

        static void ConfigureXR()
        {
            const BuildTargetGroup android = BuildTargetGroup.Android;

            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(android))
                perTarget.CreateDefaultSettingsForBuildTarget(android);
            if (!perTarget.HasManagerSettingsForBuildTarget(android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(android);

            var manager = perTarget.ManagerSettingsForBuildTarget(android);
            if (!XRPackageMetadataStore.AssignLoader(manager, typeof(OpenXRLoader).FullName, android))
                Debug.LogWarning("Range Control: the OpenXR loader could not be assigned for Android.");
            perTarget.SettingsForBuildTarget(android).InitManagerOnStart = true;
            EditorUtility.SetDirty(perTarget);

            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(android);
            if (openXR == null)
            {
                Debug.LogWarning("Range Control: OpenXR has no settings for Android yet. Open Project Settings > XR Plug-in Management once, then run this again.");
                return;
            }

            // One draw per object for both eyes; no depth submission, which nothing here needs.
            openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXR.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            EditorUtility.SetDirty(openXR);

            FeatureHelpers.RefreshFeatures(android);
            Enable(android, MetaQuestFeature.featureId);
            Enable(android, OculusTouchControllerProfile.featureId);
            Enable(android, MetaQuestTouchPlusControllerProfile.featureId);
            Enable(android, MetaQuestTouchProControllerProfile.featureId);
            Enable(android, FoveatedRenderingFeature.featureId);
            ConfigureQuestTargets(android);
            ConfigureSimulator();
        }

        /// <summary>
        /// Lists the headsets the game is made for and drops the internet permission, which nothing here uses.
        /// Quest Pro stays off the list on purpose: with it on, the foveation feature declares eye tracking as
        /// required in the manifest, and the Quest 2, 3 and 3S have none.
        /// </summary>
        static void ConfigureQuestTargets(BuildTargetGroup group)
        {
            var quest = FeatureHelpers.GetFeatureWithIdForBuildTarget(group, MetaQuestFeature.featureId);
            if (quest == null)
                return;

            var serialized = new SerializedObject(quest);
            var devices = serialized.FindProperty("targetDevices");
            for (var i = 0; devices != null && i < devices.arraySize; i++)
            {
                var device = devices.GetArrayElementAtIndex(i);
                // "eureka" is the manifest name of the Quest 3.
                var name = device.FindPropertyRelative("manifestName").stringValue;
                device.FindPropertyRelative("enabled").boolValue = name == "quest2" || name == "eureka" || name == "quest3s";
            }

            SetIfPresent(serialized, "forceRemoveInternetPermission", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(quest);
        }

        static void ConfigureSimulator()
        {
            // Pressing Play in the Editor brings up XRI's simulated headset and controllers, so the range can
            // be tried with mouse and keyboard. It is Editor-only: a headset build never gets it.
            const string settingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
            var settings = AssetDatabase.LoadMainAssetAtPath(settingsPath);
            var simulator = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.Simulator + "/XR Interaction Simulator.prefab");
            if (settings == null || simulator == null)
            {
                Debug.LogWarning("Range Control: the XR Interaction Simulator sample or its settings asset is missing; skipped simulator setup.");
                return;
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            serialized.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            serialized.FindProperty("m_UseClassic").boolValue = false;
            serialized.FindProperty("m_SimulatorPrefab").objectReferenceValue = simulator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        static void Enable(BuildTargetGroup group, string featureId)
        {
            var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(group, featureId);
            if (feature == null)
            {
                Debug.LogWarning("Range Control: OpenXR feature not found: " + featureId);
                return;
            }

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }
    }
}
