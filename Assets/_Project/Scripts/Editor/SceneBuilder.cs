using System.Collections.Generic;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Targets;
using RangeControl.UI;
using RangeControl.Weapons;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace RangeControl.Editor
{
    /// <summary>
    /// Lays out the range scene from nothing: environment, player rig, the systems and the booth. The player
    /// stands at the origin facing +Z, and one unit is one metre. Building replaces Range.unity, so use this
    /// to regenerate the blockout, not after the scene has been dressed by hand.
    /// </summary>
    public static partial class SceneBuilder
    {
        const float k_BenchTop = 0.975f;
        const float k_RackX = -0.78f;
        const float k_GripLineZ = -0.05f;
        const float k_RailHeight = 0.15f;
        const float k_RailHalfWidth = 4f;
        const float k_PaperHeight = 1.5f;

        static readonly Vector3 s_PanelPosition = new Vector3(0.6f, 1.1f, 0.3f);
        static readonly Quaternion s_PanelRotation = Quaternion.Euler(32f, 60f, 0f);

        // Where each gun's grip sits relative to its own origin, so the rack can line every grip up in one column.
        static readonly Dictionary<string, float> s_GripZ = new Dictionary<string, float>
        {
            { "m1911", 0.026f }, { "uzi", 0.021f }, { "ak74", -0.039f }, { "benelli_m4", -0.129f }, { "m107", -0.123f },
        };

        static readonly Dictionary<string, float> s_RackHeight = new Dictionary<string, float>
        {
            { "m107", 1.74f }, { "benelli_m4", 1.5f }, { "ak74", 1.26f }, { "uzi", 1.02f }, { "m1911", 0.8f },
        };

        class Built
        {
            public Camera Head;
            public XROrigin Origin;
            public Transform LeftController;
            public Behaviour SnapTurn;
            public GameModeController Controller;
            public TargetSpawner Spawner;
            public PaperCarrier Carrier;
            public AmmoSupply Supply;
            public Transform Rack;
            public Transform Panel;
            public RangeControlPanel PanelUi;
            public BoothLayout Booth;
            public Transform PlatesAnchorPoint;
            public Transform Scoreboard;
        }

        [MenuItem("Tools/Range Control/Build/4. Scene", priority = 23)]
        public static void Build()
        {
            var open = SceneManager.GetActiveScene();
            if (open.isDirty && !string.IsNullOrEmpty(open.path))
                EditorSceneManager.SaveScene(open);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var built = new Built();

            BuildLighting();
            BuildEnvironment();
            BuildRig(built);
            BuildSystems(built);
            BuildBooth(built);
            BuildUi(built);
            WireUp(built);

            EditorAssets.EnsureFolder(ProjectPaths.Scenes);
            EditorSceneManager.SaveScene(scene, ProjectPaths.RangeScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectPaths.RangeScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Range Control: scene built at " + ProjectPaths.RangeScene);
        }

        static Transform Group(string name, Transform parent = null)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        // ---------------------------------------------------------------- lighting

        static void BuildLighting()
        {
            // One directional sun, late afternoon, from behind the shooter's left shoulder so targets are front-lit.
            // No real-time shadows anywhere; the flat ambient keeps unlit sides from going black.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.8f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(36f, 38f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = ArtBuilder.Get("Sky");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.67f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.5f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.3f, 0.25f);
            RenderSettings.fog = false;
        }

        // ---------------------------------------------------------------- environment

        static GameObject Solid(Transform parent, string name, Vector3 position, Vector3 size, string material, SurfaceType surface, bool leavesDecal = true)
        {
            var solid = EditorAssets.Primitive(PrimitiveType.Cube, parent, name, position, size, ArtBuilder.Get(material), true);
            solid.layer = ProjectSetup.EnvironmentLayer;
            solid.isStatic = true;
            EditorAssets.Set(solid.AddComponent<SurfaceMarker>(), ("m_Surface", surface), ("m_LeavesDecal", leavesDecal));
            return solid;
        }

        static GameObject Prop(Transform parent, string name, Vector3 position, Vector3 size, string material)
        {
            var prop = EditorAssets.Primitive(PrimitiveType.Cube, parent, name, position, size, ArtBuilder.Get(material));
            prop.isStatic = true;
            return prop;
        }

        static void BuildEnvironment()
        {
            var environment = Group("Environment");

            // Ground and berms. The berms stop bullets, as on a real range, and cap how far the eye has to draw.
            var ground = Group("Ground", environment);
            Solid(ground, "Sand", new Vector3(0f, -0.1f, 54f), new Vector3(30f, 0.2f, 124f), "Sand", SurfaceType.Dirt, false);
            for (var side = -1; side <= 1; side += 2)
            {
                var berm = Solid(ground, side < 0 ? "BermLeft" : "BermRight", new Vector3(side * 13.5f, 1.2f, 54f), new Vector3(5f, 5f, 124f), "Berm", SurfaceType.Dirt, false);
                berm.transform.localRotation = Quaternion.Euler(0f, 0f, side * 28f);
            }

            var back = Solid(ground, "BermBack", new Vector3(0f, 2f, 112f), new Vector3(34f, 8f, 6f), "Berm", SurfaceType.Dirt, false);
            back.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);

            // Painted distance lines with a sign beside each.
            var markers = Group("DistanceMarkers", environment);
            foreach (var distance in new[] { 5, 10, 15, 25, 50, 100 })
            {
                Prop(markers, "Line_" + distance, new Vector3(0f, 0.004f, distance), new Vector3(10f, 0.008f, 0.12f), "Marker");
                var sign = Prop(markers, "Sign_" + distance, new Vector3(-5.6f, 0.45f, distance), new Vector3(0.9f, 0.5f, 0.04f), "Marker");
                var label = EditorAssets.Child(sign.transform.parent, "Label_" + distance, new Vector3(-5.6f, 0.45f, distance - 0.03f)).AddComponent<TextMeshPro>();
                label.text = distance + " m";
                label.fontSize = 3.2f;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.08f, 0.08f, 0.08f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.rectTransform.sizeDelta = new Vector2(0.9f, 0.5f);
            }

            // The booth: a covered wooden stall, open toward the lanes.
            var booth = Group("Booth", environment);
            // The deck stands 2 cm proud of the sand: two surfaces at the same height would flicker.
            Solid(booth, "Floor", new Vector3(0f, -0.03f, -0.3f), new Vector3(3.3f, 0.1f, 2.7f), "Wood", SurfaceType.Wood);
            Prop(booth, "Roof", new Vector3(0f, 2.75f, -0.2f), new Vector3(3.6f, 0.08f, 3.2f), "WoodDark");
            Solid(booth, "BackWall", new Vector3(0f, 1.35f, -1.62f), new Vector3(3.3f, 2.7f, 0.08f), "Wood", SurfaceType.Wood);
            foreach (var x in new[] { -1.6f, 1.6f })
            {
                Prop(booth, "PostFront", new Vector3(x, 1.35f, 1f), new Vector3(0.12f, 2.7f, 0.12f), "WoodDark");
                Prop(booth, "PostBack", new Vector3(x, 1.35f, -1.6f), new Vector3(0.12f, 2.7f, 0.12f), "WoodDark");
                Solid(booth, "SideWall", new Vector3(x, 0.55f, -0.3f), new Vector3(0.06f, 1.1f, 2.5f), "Wood", SurfaceType.Wood);
            }

            Solid(booth, "BenchTop", new Vector3(0f, k_BenchTop - 0.025f, 0.71f), new Vector3(0.84f, 0.05f, 0.5f), "Wood", SurfaceType.Wood);
            Solid(booth, "BenchFront", new Vector3(0f, 0.47f, 0.94f), new Vector3(0.84f, 0.94f, 0.04f), "WoodDark", SurfaceType.Wood);
            foreach (var x in new[] { -0.4f, 0.4f })
                Prop(booth, "BenchLeg", new Vector3(x, 0.47f, 0.5f), new Vector3(0.05f, 0.94f, 0.05f), "WoodDark");

            // Stands for the steel plates, off to the right of the paper lane.
            var stands = Group("PlateStands", environment);
            PlateStand(stands, 10f, 2.3f, 4.1f, 1.75f);
            PlateStand(stands, 25f, 2.6f, 4.5f, 2.05f);
            PlateStand(stands, 50f, 2.9f, 4.3f, 2.5f);
        }

        static void PlateStand(Transform parent, float z, float left, float right, float height)
        {
            var stand = Group("Stand_" + z, parent);
            Prop(stand, "PostLeft", new Vector3(left, height * 0.5f, z), new Vector3(0.06f, height, 0.06f), "MetalDark");
            Prop(stand, "PostRight", new Vector3(right, height * 0.5f, z), new Vector3(0.06f, height, 0.06f), "MetalDark");
            Prop(stand, "Bar", new Vector3((left + right) * 0.5f, height, z), new Vector3(right - left + 0.06f, 0.05f, 0.05f), "MetalDark");
        }

        // ---------------------------------------------------------------- player rig

        static void BuildRig(Built built)
        {
            var xr = Group("XR");
            new GameObject("XR Interaction Manager", typeof(XRInteractionManager)).transform.SetParent(xr, false);
            new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule)).transform.SetParent(xr, false);

            // Start from XRI's own rig, then strip it down: the player stands in one booth and never travels.
            var rigAsset = EditorAssets.Load<GameObject>(ProjectPaths.StarterAssets + "/Prefabs/XR Origin (XR Rig).prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigAsset, xr);
            PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            rig.name = "XR Origin";
            rig.layer = ProjectSetup.PlayerLayer;

            var origin = rig.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            built.Origin = origin;

            var inputAsset = EditorAssets.Load<InputActionAsset>(ProjectPaths.Input + "/RangeInput.inputactions");
            var actionManager = rig.GetComponent<InputActionManager>();
            if (!actionManager.actionAssets.Contains(inputAsset))
                actionManager.actionAssets.Add(inputAsset);

            var head = origin.Camera;
            head.nearClipPlane = 0.05f;
            head.farClipPlane = 300f;
            head.allowHDR = false;
            var headData = head.GetUniversalAdditionalCameraData();
            headData.renderPostProcessing = false;
            headData.renderShadows = false;
            EditorAssets.Set(head.gameObject.AddComponent<PlayerAnchorsBinder>(), ("m_Anchors", DataBuilder.Anchors));
            var muffle = head.gameObject.AddComponent<AudioLowPassFilter>();
            muffle.cutoffFrequency = 1300f;
            muffle.enabled = false;
            EditorAssets.Set(head.gameObject.AddComponent<ListenerAudio>(), ("m_Comfort", DataBuilder.Comfort), ("m_Muffle", muffle));
            head.gameObject.AddComponent<FoveatedRendering>();
            built.Head = head;

            // No walking, no teleporting. Snap turn stays, switched off until the player asks for it.
            var locomotion = rig.transform.Find("Locomotion");
            foreach (var unused in new[] { "Move", "Grab Move", "Teleportation", "Climb", "Gravity", "Jump" })
                locomotion.Find(unused).gameObject.SetActive(false);
            var turn = locomotion.Find("Turn");
            turn.GetComponent<ContinuousTurnProvider>().enabled = false;
            var snapTurn = turn.GetComponent<SnapTurnProvider>();
            snapTurn.turnAmount = 45f;
            snapTurn.enabled = false;
            built.SnapTurn = snapTurn;

            foreach (var hand in new[] { "Left", "Right" })
            {
                var controller = rig.transform.Find("Camera Offset/" + hand + " Controller");
                controller.Find("Teleport Interactor").gameObject.SetActive(false);
                // The sample's input switcher exists to juggle teleport and movement, which this rig no longer has.
                foreach (var behaviour in controller.GetComponents<MonoBehaviour>())
                {
                    if (behaviour.GetType().Name == "ControllerInputActionManager")
                        behaviour.enabled = false;
                }

                // The sample rig's hands only look for things on the Default layer. Guns and magazines live on
                // Weapons, and the console buttons and the belt pouch on Interaction, so the hand has to look there too.
                var nearFar = controller.Find("Near-Far Interactor");
                var reach = (1 << 0) | (1 << ProjectSetup.WeaponsLayer) | (1 << ProjectSetup.InteractionLayer);
                nearFar.GetComponent<SphereInteractionCaster>().physicsLayerMask = reach;
                nearFar.GetComponent<CurveInteractionCaster>().raycastMask = reach | (1 << UiFactory.UiLayer) | (1 << ProjectSetup.EnvironmentLayer);

                var presence = controller.gameObject.AddComponent<ControllerPresence>();
                EditorAssets.Set(presence,
                    ("m_GrabInteractor", controller.Find("Near-Far Interactor").GetComponent<NearFarInteractor>()),
                    ("m_ControllerVisual", controller.Find(hand + " Controller Visual").gameObject),
                    ("m_PokeInteractor", controller.Find("Poke Interactor").GetComponent<XRPokeInteractor>()));
                if (hand == "Left")
                    built.LeftController = controller;
            }
        }

        // ---------------------------------------------------------------- systems

        static void BuildSystems(Built built)
        {
            var systems = Group("Systems");

            // Rails the moving targets run on. They slide down the lane when the panel distance changes.
            var spawnerObject = Group("TargetSpawner", systems);
            var rails = new[]
            {
                BuildRail(spawnerObject, "RailNear", 1.5f, false),
                BuildRail(spawnerObject, "RailFar", 4f, true),
            };
            var spawner = spawnerObject.gameObject.AddComponent<TargetSpawner>();
            EditorAssets.Set(spawner,
                ("m_Settings", DataBuilder.Settings),
                ("m_SilhouettePrefab", EditorAssets.Load<GameObject>(PrefabBuilder.SilhouettePath).GetComponent<FlipTarget>()),
                ("m_NoShootPrefab", EditorAssets.Load<GameObject>(PrefabBuilder.NoShootPath).GetComponent<FlipTarget>()),
                ("m_Rails", new object[] { rails[0], rails[1] }));
            built.Spawner = spawner;

            // The paper carrier: a track down the centre lane with a motorised cart that holds the paper up on a post.
            // It runs along the ground so nothing crosses the player's view of the scoreboard overhead.
            var carrierObject = Group("PaperCarrier", systems);
            Prop(carrierObject, "Track", new Vector3(0f, 0.012f, 52.5f), new Vector3(0.07f, 0.024f, 103f), "MetalDark");
            var trolley = Group("Trolley", carrierObject);
            trolley.localPosition = new Vector3(0f, 0f, 15f);
            EditorAssets.Primitive(PrimitiveType.Cube, trolley, "Cart", new Vector3(0f, 0.07f, 0.03f), new Vector3(0.28f, 0.09f, 0.36f), ArtBuilder.Get("Metal"));
            EditorAssets.Primitive(PrimitiveType.Cube, trolley, "Post", new Vector3(0f, (k_PaperHeight - 0.33f + 0.11f) * 0.5f, 0.045f), new Vector3(0.03f, k_PaperHeight - 0.33f - 0.11f, 0.03f), ArtBuilder.Get("MetalDark"));
            // The board sits a clear three centimetres behind the paper, so the two never fight for depth far down the lane.
            EditorAssets.Primitive(PrimitiveType.Cube, trolley, "Backing", new Vector3(0f, k_PaperHeight, 0.036f), new Vector3(0.66f, 0.66f, 0.012f), ArtBuilder.Get("WoodDark"));
            var paper = (GameObject)PrefabUtility.InstantiatePrefab(EditorAssets.Load<GameObject>(PrefabBuilder.PaperTargetPath), trolley);
            paper.transform.localPosition = new Vector3(0f, k_PaperHeight, 0f);
            var motor = trolley.gameObject.AddComponent<AudioSource>();
            PrefabBuilder.ConfigureAudio(motor, 2f, 60f, 0.5f);
            motor.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/CarrierMotor.wav");
            motor.loop = true;
            var carrier = carrierObject.gameObject.AddComponent<PaperCarrier>();
            EditorAssets.Set(carrier,
                ("m_Settings", DataBuilder.Settings),
                ("m_Trolley", trolley),
                ("m_Paper", paper.GetComponent<PaperTarget>()),
                ("m_Motor", motor));
            built.Carrier = carrier;

            // Steel plates: three at 10 m for the tutorial, then bigger ones further out.
            var plates = Group("SteelPlates", systems);
            var plateAsset = EditorAssets.Load<GameObject>(PrefabBuilder.SteelPlatePath);
            void Plate(float x, float y, float z, float scale)
            {
                var plate = (GameObject)PrefabUtility.InstantiatePrefab(plateAsset, plates);
                plate.transform.localPosition = new Vector3(x, y, z);
                plate.transform.localScale = Vector3.one * scale;
            }

            Plate(2.7f, 1.72f, 10f, 1f);
            Plate(3.2f, 1.72f, 10f, 1f);
            Plate(3.7f, 1.72f, 10f, 1f);
            Plate(3.1f, 2.02f, 25f, 1.5f);
            Plate(4f, 2.02f, 25f, 1.5f);
            Plate(3.6f, 2.47f, 50f, 2.2f);
            built.PlatesAnchorPoint = Group("PlatesHintAnchor", plates);
            built.PlatesAnchorPoint.localPosition = new Vector3(3.2f, 2.6f, 9.8f);
            built.PlatesAnchorPoint.localScale = Vector3.one * 7f;

            // Effects: pooled tracers, decals and sounds, and one shared particle emitter per surface.
            var effectsObject = Group("RangeEffects", systems);
            var dirt = PrefabBuilder.BurstParticles(effectsObject, "DirtImpact", ArtBuilder.Get("Dust"), 1f, 3.5f, 0.3f, 0.7f, 0.03f, 0.09f, 35f, 1.2f);
            var wood = PrefabBuilder.BurstParticles(effectsObject, "WoodImpact", ArtBuilder.Get("WoodChips"), 1.5f, 4f, 0.3f, 0.6f, 0.015f, 0.04f, 40f, 1.5f);
            var steel = PrefabBuilder.BurstParticles(effectsObject, "SteelImpact", ArtBuilder.Get("Sparks"), 3f, 8f, 0.12f, 0.3f, 0.01f, 0.03f, 50f, 1f);
            var paperBits = PrefabBuilder.BurstParticles(effectsObject, "PaperImpact", ArtBuilder.Get("PaperFlecks"), 0.6f, 2f, 0.3f, 0.6f, 0.01f, 0.025f, 55f, 0.8f);
            var tick = EditorAssets.Child(effectsObject, "HitTick").AddComponent<AudioSource>();
            tick.playOnAwake = false;
            tick.spatialBlend = 0f;
            tick.volume = 0.6f;
            tick.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/HitTick.wav");
            var effects = effectsObject.gameObject.AddComponent<RangeEffects>();
            EditorAssets.Set(effects,
                ("m_Events", DataBuilder.Events),
                ("m_TracerPrefab", EditorAssets.Load<GameObject>(PrefabBuilder.TracerPath).GetComponent<Tracer>()),
                ("m_DirtImpact", dirt),
                ("m_WoodImpact", wood),
                ("m_SteelImpact", steel),
                ("m_PaperImpact", paperBits),
                ("m_DecalPrefab", EditorAssets.Load<GameObject>(PrefabBuilder.BulletDecalPath).transform),
                ("m_SoundPrefab", EditorAssets.Load<GameObject>(PrefabBuilder.ImpactSoundPath).GetComponent<AudioSource>()),
                ("m_DirtClip", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/DirtThud.wav")),
                ("m_WoodClip", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/WoodThud.wav")),
                ("m_SteelClip", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/SteelPing.wav")),
                ("m_PaperClip", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/PaperThwack.wav")),
                ("m_HitTick", tick));

            var supplyObject = Group("AmmoSupply", systems);
            built.Supply = supplyObject.gameObject.AddComponent<AmmoSupply>();
            EditorAssets.Set(built.Supply, ("m_Events", DataBuilder.Events));

            // Outdoor ambience all the time; a music source that plays between rounds once a track is assigned.
            var ambienceObject = Group("Ambience", systems);
            var wind = ambienceObject.gameObject.AddComponent<AudioSource>();
            wind.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Ambience/WindLoop.wav");
            wind.loop = true;
            wind.playOnAwake = true;
            wind.spatialBlend = 0f;
            wind.volume = 0.3f;
            var music = EditorAssets.Child(ambienceObject, "Music").AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            EditorAssets.Set(ambienceObject.gameObject.AddComponent<Ambience>(), ("m_Events", DataBuilder.Events), ("m_Comfort", DataBuilder.Comfort), ("m_Music", music));

            var controllerObject = Group("GameModeController", systems);
            var controller = controllerObject.gameObject.AddComponent<GameModeController>();
            EditorAssets.Set(controller,
                ("m_Events", DataBuilder.Events),
                ("m_Settings", DataBuilder.Settings),
                ("m_Modes", new object[] { DataBuilder.Mode("FreePractice"), DataBuilder.Mode("MovingGallery"), DataBuilder.Mode("Precision") }),
                ("m_Spawner", spawner),
                ("m_Carrier", carrier));
            built.Controller = controller;
        }

        static TargetRail BuildRail(Transform parent, string name, float depthOffset, bool reversed)
        {
            var rail = Group(name, parent);
            rail.localPosition = new Vector3(0f, 0f, 15f + depthOffset);
            var left = EditorAssets.Child(rail, "Left", new Vector3(-k_RailHalfWidth, k_RailHeight, 0f)).transform;
            var right = EditorAssets.Child(rail, "Right", new Vector3(k_RailHalfWidth, k_RailHeight, 0f)).transform;
            EditorAssets.Primitive(PrimitiveType.Cube, rail, "Beam", new Vector3(0f, k_RailHeight - 0.05f, 0.06f), new Vector3(k_RailHalfWidth * 2f + 0.6f, 0.08f, 0.08f), ArtBuilder.Get("Metal"));
            foreach (var x in new[] { -k_RailHalfWidth - 0.3f, 0f, k_RailHalfWidth + 0.3f })
                EditorAssets.Primitive(PrimitiveType.Cube, rail, "Foot", new Vector3(x, 0.04f, 0.06f), new Vector3(0.2f, 0.08f, 0.3f), ArtBuilder.Get("MetalDark"));

            var component = rail.gameObject.AddComponent<TargetRail>();
            EditorAssets.Set(component,
                ("m_Start", reversed ? right : left),
                ("m_End", reversed ? left : right),
                ("m_DepthOffset", depthOffset));
            return component;
        }

        // ---------------------------------------------------------------- booth furniture

        static void BuildBooth(Built built)
        {
            var booth = Group("BoothInteractables");

            // Weapon rack on the player's left. Guns hang barrel-downrange with their grips in one column.
            var rack = Group("WeaponRack", booth);
            rack.localPosition = new Vector3(k_RackX, 0f, 0f);
            built.Rack = rack;
            EditorAssets.Primitive(PrimitiveType.Cube, rack, "Board", new Vector3(-0.09f, 1.25f, 0.1f), new Vector3(0.04f, 1.25f, 1.6f), ArtBuilder.Get("Wood"));
            foreach (var id in DataBuilder.WeaponIds)
            {
                var weaponAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBuilder.WeaponPath(id));
                if (weaponAsset == null)
                    continue;

                var slot = Group("Slot_" + id, rack);
                slot.localPosition = new Vector3(0f, s_RackHeight[id], k_GripLineZ - s_GripZ[id]);
                EditorAssets.Primitive(PrimitiveType.Cube, rack, "Peg_" + id, new Vector3(-0.045f, s_RackHeight[id] - 0.035f, k_GripLineZ + 0.06f), new Vector3(0.09f, 0.012f, 0.012f), ArtBuilder.Get("Metal"));
                PrefabUtility.InstantiatePrefab(weaponAsset, slot);

                // Name plates sit behind the grip, under the stock. The Uzi's grip is wider, so its plate sits further back.
                var plateZ = k_GripLineZ - (id == "uzi" ? 0.25f : 0.2f);
                var plateObject = EditorAssets.Primitive(PrimitiveType.Cube, rack, "Plate_" + id, new Vector3(-0.066f, s_RackHeight[id] - 0.105f, plateZ), new Vector3(0.006f, 0.035f, 0.17f), ArtBuilder.Get("Plate"));
                var label = EditorAssets.Child(rack, "PlateLabel_" + id, new Vector3(-0.062f, s_RackHeight[id] - 0.105f, plateZ), Quaternion.Euler(0f, -90f, 0f)).AddComponent<TextMeshPro>();
                label.fontSize = 0.22f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.rectTransform.sizeDelta = new Vector2(0.17f, 0.035f);
                label.text = DataBuilder.Weapon(id).DisplayName;
                var plate = plateObject.AddComponent<RackPlate>();
                EditorAssets.Set(plate,
                    ("m_Events", DataBuilder.Events),
                    ("m_Controller", built.Controller),
                    ("m_Weapon", DataBuilder.Weapon(id)),
                    ("m_Plate", plateObject.GetComponent<Renderer>()),
                    ("m_Label", label));
            }

            // Ammunition tray on the bench: four slots that restock for whichever gun is in use.
            var tray = Group("AmmoTray", booth);
            tray.localPosition = new Vector3(0.14f, k_BenchTop, 0.7f);
            EditorAssets.Primitive(PrimitiveType.Cube, tray, "Base", new Vector3(0f, 0.008f, 0f), new Vector3(0.34f, 0.016f, 0.14f), ArtBuilder.Get("Metal"));
            var slots = new object[4];
            for (var i = 0; i < slots.Length; i++)
            {
                var slotObject = EditorAssets.Child(tray, "Slot" + i, new Vector3(-0.12f + i * 0.08f, 0.1f, 0f));
                var trigger = slotObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.03f;
                var socket = slotObject.AddComponent<MagazineSocket>();
                socket.attachTransform = EditorAssets.Child(slotObject.transform, "Attach").transform;
                socket.showInteractableHoverMeshes = false;
                socket.recycleDelayTime = 0.5f;
                slots[i] = socket;
            }

            EditorAssets.Set(tray.gameObject.AddComponent<AmmoTray>(), ("m_Supply", built.Supply), ("m_Slots", slots));

            // The magazine pouch on the player's belt.
            var pouch = Group("HipPouch", booth);
            pouch.gameObject.layer = ProjectSetup.InteractionLayer;
            pouch.localPosition = new Vector3(0f, 1f, 0.06f);
            pouch.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.5f, 0.18f, 0.24f);
            pouch.gameObject.AddComponent<XRSimpleInteractable>();
            EditorAssets.Set(pouch.gameObject.AddComponent<HipPouch>(), ("m_Anchors", DataBuilder.Anchors), ("m_Supply", built.Supply));

            // Ear protection, sitting on the left of the bench.
            var muffs = Group("Earmuffs", booth);
            muffs.localPosition = new Vector3(-0.26f, k_BenchTop + 0.06f, 0.7f);
            var muffsVisual = Group("Visual", muffs);
            for (var side = -1; side <= 1; side += 2)
            {
                var cup = EditorAssets.Primitive(PrimitiveType.Cylinder, muffsVisual, side < 0 ? "CupLeft" : "CupRight", new Vector3(side * 0.085f, 0f, 0f), new Vector3(0.09f, 0.02f, 0.11f), ArtBuilder.Get("Earmuff"));
                cup.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                EditorAssets.Primitive(PrimitiveType.Cube, muffsVisual, side < 0 ? "ArmLeft" : "ArmRight", new Vector3(side * 0.085f, 0.07f, 0f), new Vector3(0.012f, 0.1f, 0.02f), ArtBuilder.Get("MetalDark"));
            }

            EditorAssets.Primitive(PrimitiveType.Cube, muffsVisual, "Band", new Vector3(0f, 0.12f, 0f), new Vector3(0.19f, 0.012f, 0.025f), ArtBuilder.Get("MetalDark"));
            var muffsBody = muffs.gameObject.AddComponent<Rigidbody>();
            muffsBody.isKinematic = true;
            muffsBody.useGravity = false;
            var muffsCollider = muffs.gameObject.AddComponent<BoxCollider>();
            muffsCollider.center = new Vector3(0f, 0.04f, 0f);
            muffsCollider.size = new Vector3(0.22f, 0.18f, 0.12f);
            var muffsGrab = muffs.gameObject.AddComponent<XRGrabInteractable>();
            muffsGrab.useDynamicAttach = true;
            muffsGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            muffsGrab.throwOnDetach = false;
            EditorAssets.Set(muffs.gameObject.AddComponent<Earmuffs>(), ("m_Comfort", DataBuilder.Comfort), ("m_Anchors", DataBuilder.Anchors), ("m_Visual", muffsVisual.gameObject));
            EditorAssets.SetLayerRecursively(muffs.gameObject, ProjectSetup.WeaponsLayer);

            // Fits the booth to the player: left-handed swaps the rack and the panel, seated lifts the rig.
            var spot = Group("StandingSpot", booth);
            var layout = booth.gameObject.AddComponent<BoothLayout>();
            built.Booth = layout;
            EditorAssets.Set(layout,
                ("m_Comfort", DataBuilder.Comfort),
                ("m_Origin", built.Origin),
                ("m_StandingSpot", spot),
                ("m_SnapTurn", built.SnapTurn));
        }

        /// <summary>A console button pressed by pushing it into the panel, along its own +Z.</summary>
        static PhysicalButton BuildPhysicalButton(Transform parent, string name, Vector3 localPosition, string capMaterial, float diameter)
        {
            var root = EditorAssets.Child(parent, name, localPosition);
            root.layer = ProjectSetup.InteractionLayer;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, -0.012f);
            collider.size = new Vector3(diameter, diameter, 0.04f);

            var housing = EditorAssets.Primitive(PrimitiveType.Cylinder, root.transform, "Housing", new Vector3(0f, 0f, 0.004f), new Vector3(diameter * 1.25f, 0.008f, diameter * 1.25f), ArtBuilder.Get("MetalDark"));
            housing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var cap = EditorAssets.Primitive(PrimitiveType.Cylinder, root.transform, "Cap", new Vector3(0f, 0f, -0.014f), new Vector3(diameter, 0.012f, diameter), ArtBuilder.Get(capMaterial));
            cap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var click = root.AddComponent<AudioSource>();
            PrefabBuilder.ConfigureAudio(click, 0.5f, 10f, 0.7f);
            click.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/ButtonClick.wav");

            var interactable = root.AddComponent<XRSimpleInteractable>();
            var filter = root.AddComponent<XRPokeFilter>();
            filter.pokeInteractable = interactable;
            filter.pokeCollider = collider;

            var button = root.AddComponent<PhysicalButton>();
            EditorAssets.Set(button, ("m_Cap", cap.transform), ("m_PressedOffset", new Vector3(0f, 0f, 0.01f)), ("m_Click", click));
            return button;
        }
    }
}
