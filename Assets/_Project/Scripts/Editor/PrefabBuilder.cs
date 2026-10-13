using System;
using RangeControl.Core;
using RangeControl.Range;
using RangeControl.Targets;
using RangeControl.UI;
using RangeControl.Weapons;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace RangeControl.Editor
{
    /// <summary>
    /// Builds the prefabs: effects, targets, magazines and the five guns. The guns are assembled around the
    /// models from the Low Poly Weapons pack; where each gun is held, where its muzzle is and how its parts
    /// move is the table in <see cref="s_Guns"/>, measured from the models.
    /// </summary>
    public static class PrefabBuilder
    {
        const string k_Effects = ProjectPaths.Prefabs + "/Effects";
        const string k_Targets = ProjectPaths.Prefabs + "/Targets";
        const string k_Weapons = ProjectPaths.Prefabs + "/Weapons";
        const string k_UI = ProjectPaths.Prefabs + "/UI";

        /// <summary>How far the gun is pitched down in the hand, from the original pistol's hand-tuned grip.</summary>
        const float k_GripPitch = -13f;

        public const string TracerPath = k_Effects + "/Tracer.prefab";
        public const string PaperHolePath = k_Effects + "/PaperHole.prefab";
        public const string BulletDecalPath = k_Effects + "/BulletDecal.prefab";
        public const string ImpactSoundPath = k_Effects + "/ImpactSound.prefab";
        public const string ScorePopupPath = k_UI + "/ScorePopup.prefab";
        public const string SilhouettePath = k_Targets + "/Target_Silhouette.prefab";
        public const string NoShootPath = k_Targets + "/Target_NoShoot.prefab";
        public const string PaperTargetPath = k_Targets + "/Target_Paper.prefab";
        public const string SteelPlatePath = k_Targets + "/Target_SteelPlate.prefab";

        public static string WeaponPath(string id) => k_Weapons + "/Weapon_" + id + ".prefab";
        public static string MagazinePath(string id) => k_Weapons + "/Magazine_" + id + ".prefab";

        /// <summary>
        /// One gun's measurements, in metres, in the gun's own space: origin at the model's origin, +Z along
        /// the barrel, +Y up. The grip is where the controller's tracked point sits.
        /// </summary>
        struct GunSpec
        {
            public string Id;
            public string Model;
            public Vector3 Grip;
            public Vector3 Muzzle;
            public Vector3? Support;
            public string MagazinePart;
            public Vector3? ShellPort;
            public string SlidePart;
            public float SlideTravel;
            public Vector3 Hud;
            public bool Scope;
        }

        static readonly GunSpec[] s_Guns =
        {
            new GunSpec
            {
                Id = "m1911", Model = "M1911", Grip = new Vector3(0f, -0.005f, 0.026f), Muzzle = new Vector3(0f, 0.024f, 0.152f),
                MagazinePart = "Magazin", SlidePart = "Slide", SlideTravel = 0.03f, Hud = new Vector3(-0.035f, 0.065f, -0.07f),
            },
            new GunSpec
            {
                Id = "uzi", Model = "Uzi", Grip = new Vector3(0f, 0.003f, 0.021f), Muzzle = new Vector3(0f, 0.025f, 0.22f),
                Support = new Vector3(0f, -0.01f, 0.09f), MagazinePart = "Magazin", SlidePart = "Bolt", SlideTravel = 0.03f,
                Hud = new Vector3(-0.04f, 0.095f, -0.03f),
            },
            new GunSpec
            {
                Id = "ak74", Model = "AK74", Grip = new Vector3(0f, -0.026f, -0.039f), Muzzle = new Vector3(0f, -0.003f, 0.449f),
                Support = new Vector3(0f, -0.02f, 0.175f), MagazinePart = "Mag", SlidePart = "Bolt", SlideTravel = 0.05f,
                Hud = new Vector3(-0.045f, 0.075f, -0.09f),
            },
            new GunSpec
            {
                Id = "benelli_m4", Model = "Bennelli_M4", Grip = new Vector3(0f, -0.042f, -0.129f), Muzzle = new Vector3(0f, 0.002f, 0.479f),
                Support = new Vector3(0f, -0.03f, 0.14f), ShellPort = new Vector3(0f, -0.044f, -0.069f),
                Hud = new Vector3(-0.045f, 0.055f, -0.17f),
            },
            new GunSpec
            {
                Id = "m107", Model = "M107", Grip = new Vector3(0f, -0.039f, -0.123f), Muzzle = new Vector3(0f, 0.024f, 0.7f),
                Support = new Vector3(0f, -0.005f, 0.25f), MagazinePart = "Magazin", SlidePart = "Bolt", SlideTravel = 0.06f,
                Hud = new Vector3(-0.065f, 0.05f, -0.17f), Scope = true,
            },
        };

        [MenuItem("Tools/Range Control/Build/3. Prefabs", priority = 22)]
        public static void Build()
        {
            BuildEffects();
            BuildTargets();
            foreach (var gun in s_Guns)
                BuildGun(gun);
            AssetDatabase.SaveAssets();
            Debug.Log("Range Control: prefabs built.");
        }

        // ---------------------------------------------------------------- effects

        static void BuildEffects()
        {
            var tracer = new GameObject("Tracer");
            var line = tracer.AddComponent<LineRenderer>();
            ConfigureLine(line, ArtBuilder.Get("Tracer"), 0.006f, 0.022f);
            tracer.AddComponent<Tracer>();
            EditorAssets.SavePrefab(tracer, TracerPath);

            var hole = EditorAssets.Primitive(PrimitiveType.Quad, null, "PaperHole", Vector3.zero, Vector3.one * 0.014f, ArtBuilder.Get("BulletHole"));
            EditorAssets.SavePrefab(hole, PaperHolePath);

            var decal = EditorAssets.Primitive(PrimitiveType.Quad, null, "BulletDecal", Vector3.zero, Vector3.one * 0.04f, ArtBuilder.Get("BulletHole"));
            EditorAssets.SavePrefab(decal, BulletDecalPath);

            var sound = new GameObject("ImpactSound");
            ConfigureAudio(sound.AddComponent<AudioSource>(), 3f, 90f, 0.9f);
            EditorAssets.SavePrefab(sound, ImpactSoundPath);

            var popup = new GameObject("ScorePopup");
            var text = popup.AddComponent<TextMeshPro>();
            text.text = "+100";
            text.fontSize = 4f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(6f, 1.5f);
            EditorAssets.SavePrefab(popup, ScorePopupPath);
        }

        public static void ConfigureLine(LineRenderer line, Material material, float startWidth, float endWidth)
        {
            line.sharedMaterial = material;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        public static void ConfigureAudio(AudioSource source, float minDistance, float maxDistance, float volume)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            source.volume = volume;
        }

        /// <summary>A particle system that only emits when told to: bursts of small opaque chips.</summary>
        public static ParticleSystem BurstParticles(Transform parent, string name, Material material, float speedMin, float speedMax,
            float lifeMin, float lifeMax, float sizeMin, float sizeMax, float coneAngle, float gravity)
        {
            var system = EditorAssets.Child(parent, name).AddComponent<ParticleSystem>();
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.maxParticles = 96;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = 0.005f;

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        // ---------------------------------------------------------------- targets

        static void BuildTargets()
        {
            BuildFlipTarget("Target_Silhouette", SilhouettePath, TargetType.MovingSilhouette, ArtBuilder.Get("TargetBoard"), ArtBuilder.Get("TargetBoardHead"), false);
            BuildFlipTarget("Target_NoShoot", NoShootPath, TargetType.NoShoot, ArtBuilder.Get("NoShootBoard"), ArtBuilder.Get("NoShootBoard"), true);
            BuildPaperTarget();
            BuildSteelPlate();
        }

        static void BuildFlipTarget(string name, string path, TargetType type, Material bodyMaterial, Material headMaterial, bool noShoot)
        {
            // Origin at the hinge on the rail. The board faces -Z, toward the booth, and falls away to +Z.
            var root = new GameObject(name);
            var hinge = EditorAssets.Child(root.transform, "Hinge").transform;
            EditorAssets.Primitive(PrimitiveType.Cube, hinge, "Post", new Vector3(0f, 0.45f, 0.025f), new Vector3(0.05f, 0.9f, 0.03f), ArtBuilder.Get("WoodDark"));
            var body = EditorAssets.Primitive(PrimitiveType.Cube, hinge, "Body", new Vector3(0f, 1.2f, 0f), new Vector3(0.45f, 0.6f, 0.02f), bodyMaterial, true);
            var head = EditorAssets.Primitive(PrimitiveType.Cube, hinge, "Head", new Vector3(0f, 1.61f, 0f), new Vector3(0.2f, 0.22f, 0.02f), headMaterial, true);

            if (noShoot)
            {
                // The X is a shape as well as a colour, so the target reads without relying on colour vision.
                for (var i = 0; i < 2; i++)
                {
                    // Stood well proud of the board, so the mark does not shimmer against it at 50 m.
                    var bar = EditorAssets.Primitive(PrimitiveType.Cube, hinge, "Cross" + i, new Vector3(0f, 1.2f, -0.02f), new Vector3(0.6f, 0.07f, 0.01f), ArtBuilder.Get("NoShootMark"));
                    bar.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 53f : -53f);
                }
            }

            var target = root.AddComponent<FlipTarget>();
            EditorAssets.Set(target,
                ("m_Events", DataBuilder.Events),
                ("m_Type", type),
                ("m_Surface", SurfaceType.Wood),
                ("m_Hinge", hinge),
                ("m_ZoneColliders", new object[] { body.GetComponent<Collider>(), head.GetComponent<Collider>() }),
                ("m_FlashRenderers", new object[] { body.GetComponent<Renderer>(), head.GetComponent<Renderer>() }));

            AddZone(body, target, ScoreRules.SilhouetteBody);
            AddZone(head, target, ScoreRules.SilhouetteHead);
            EditorAssets.SetLayerRecursively(root, ProjectSetup.TargetsLayer);
            EditorAssets.SavePrefab(root, path);
        }

        static void AddZone(GameObject part, FlipTarget target, int points)
        {
            EditorAssets.Set(part.AddComponent<ScoreZone>(), ("m_Target", target), ("m_Points", points));
            EditorAssets.Set(part.AddComponent<SurfaceMarker>(), ("m_Surface", SurfaceType.Wood), ("m_LeavesDecal", false));
        }

        static void BuildPaperTarget()
        {
            // Origin at the bullseye, X to the shooter's right, Y up: the frame the ring scoring works in.
            var root = new GameObject("Target_Paper");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(ArtBuilder.PaperSize, ArtBuilder.PaperSize, 0.01f);
            var sheet = EditorAssets.Child(root.transform, "Sheet").transform;
            EditorAssets.Primitive(PrimitiveType.Quad, sheet, "Face", Vector3.zero, Vector3.one * ArtBuilder.PaperSize, ArtBuilder.Get("Paper"));

            var paper = root.AddComponent<PaperTarget>();
            EditorAssets.Set(paper,
                ("m_Events", DataBuilder.Events),
                ("m_Type", TargetType.PaperBullseye),
                ("m_Surface", SurfaceType.Paper),
                ("m_Sheet", sheet),
                ("m_RingWidth", ArtBuilder.RingWidth),
                ("m_HolePrefab", EditorAssets.Load<GameObject>(PaperHolePath).transform));
            EditorAssets.Set(root.AddComponent<SurfaceMarker>(), ("m_Surface", SurfaceType.Paper), ("m_LeavesDecal", false));
            EditorAssets.SetLayerRecursively(root, ProjectSetup.TargetsLayer);
            EditorAssets.SavePrefab(root, PaperTargetPath);
        }

        static void BuildSteelPlate()
        {
            // Origin at the hinge the plate hangs from. The prefab is 30 cm across; scale it for other sizes.
            const float diameter = 0.3f;
            const float drop = 0.08f;
            var root = new GameObject("Target_SteelPlate");
            var hinge = EditorAssets.Child(root.transform, "Hinge").transform;
            for (var side = -1; side <= 1; side += 2)
                EditorAssets.Primitive(PrimitiveType.Cube, hinge, side < 0 ? "ChainLeft" : "ChainRight", new Vector3(side * 0.08f, -drop * 0.5f, 0f), new Vector3(0.008f, drop + 0.04f, 0.008f), ArtBuilder.Get("MetalDark"));

            var plate = EditorAssets.Primitive(PrimitiveType.Cylinder, hinge, "Plate", new Vector3(0f, -(drop + diameter * 0.5f), 0f), new Vector3(diameter, 0.005f, diameter), ArtBuilder.Get("Steel"));
            plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var collider = plate.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 2f, 1f);

            EditorAssets.Set(plate.AddComponent<SteelPlate>(),
                ("m_Events", DataBuilder.Events),
                ("m_Type", TargetType.SteelPlate),
                ("m_Surface", SurfaceType.Steel),
                ("m_Hinge", hinge));
            EditorAssets.Set(plate.AddComponent<SurfaceMarker>(), ("m_Surface", SurfaceType.Steel), ("m_LeavesDecal", false));
            EditorAssets.SetLayerRecursively(root, ProjectSetup.TargetsLayer);
            EditorAssets.SavePrefab(root, SteelPlatePath);
        }

        // ---------------------------------------------------------------- guns

        static void BuildGun(GunSpec spec)
        {
            var data = DataBuilder.Weapon(spec.Id);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.GunPack + "/Prefabs/" + spec.Model + ".prefab");
            if (modelAsset == null)
            {
                Debug.LogWarning("Range Control: " + spec.Model + " is not in " + ProjectPaths.GunPack + "; skipped " + data.DisplayName + ". Import the Low Poly Weapons pack and build again.");
                return;
            }

            var gunMaterial = ArtBuilder.Get("Gun");
            var root = new GameObject("Weapon_" + spec.Id);

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var grip = EditorAssets.Child(root.transform, "GripAttach", spec.Grip, Quaternion.Euler(k_GripPitch, 0f, 0f)).transform;
            var support = spec.Support.HasValue ? EditorAssets.Child(root.transform, "SupportAttach", spec.Support.Value).transform : null;

            // Everything that should kick with recoil hangs off the pivot, which sits in the hand.
            var pivot = EditorAssets.Child(root.transform, "RecoilPivot", spec.Grip).transform;
            Vector3 InPivot(Vector3 gunSpace) => gunSpace - spec.Grip;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, pivot);
            model.name = "Model";
            // The pack's models point their barrels along -Z; turn them to face +Z.
            model.transform.SetLocalPositionAndRotation(InPivot(Vector3.zero), Quaternion.Euler(0f, 180f, 0f));
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = gunMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var bounds = new Bounds();
            var first = true;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;

            var muzzle = EditorAssets.Child(pivot, "Muzzle", InPivot(spec.Muzzle)).transform;
            var flash = BurstParticles(pivot, "MuzzleFlash", ArtBuilder.Get("MuzzleFlash"), 0.6f, 2.5f, 0.03f, 0.06f, 0.02f, 0.06f, 22f, 0f);
            flash.transform.localPosition = InPivot(spec.Muzzle + Vector3.forward * 0.01f);

            var laserObject = EditorAssets.Child(pivot, "Laser", InPivot(spec.Muzzle));
            ConfigureLine(laserObject.AddComponent<LineRenderer>(), ArtBuilder.Get("Laser"), 0.002f, 0.002f);
            var laser = laserObject.AddComponent<LaserSight>();
            EditorAssets.Set(laser, ("m_Mask", ProjectSetup.BulletMask));

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.attachTransform = grip;
            grab.secondaryAttachTransform = support;
            grab.useDynamicAttach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.smoothPosition = false;
            grab.smoothRotation = false;
            grab.trackScale = false;
            // Measured to the gun's own shape, so a hand anywhere on a gun picks that gun over its neighbour on the rack.
            grab.distanceCalculationMode = XRBaseInteractable.DistanceCalculationMode.ColliderVolume;
            // A second hand on the gun steadies it: the aim then follows the line between the two hands.
            grab.selectMode = support != null ? InteractableSelectMode.Multiple : InteractableSelectMode.Single;
            var transformer = root.AddComponent<XRGeneralGrabTransformer>();
            transformer.allowTwoHandedScaling = false;

            var audio = root.AddComponent<AudioSource>();
            ConfigureAudio(audio, 1.5f, 80f, 1f);

            var shooter = root.AddComponent<HitscanShooter>();
            EditorAssets.Set(shooter, ("m_Events", DataBuilder.Events), ("m_Muzzle", muzzle), ("m_HitMask", ProjectSetup.BulletMask), ("m_Seed", (uint)(Array.IndexOf(s_Guns, spec) + 11)));
            var recoil = root.AddComponent<WeaponRecoil>();
            EditorAssets.Set(recoil, ("m_Pivot", pivot));
            var haptics = root.AddComponent<WeaponHaptics>();
            var weapon = root.AddComponent<Weapon>();
            root.AddComponent<RackReturn>();

            var trigger = FindDeep(model.transform, "Trigger");
            var slide = string.IsNullOrEmpty(spec.SlidePart) ? null : FindDeep(model.transform, spec.SlidePart);

            EditorAssets.SetLayerRecursively(root, ProjectSetup.WeaponsLayer);
            var magwell = BuildMagwell(spec, data, weapon, pivot, model.transform, gunMaterial, InPivot);
            var hud = BuildHud(pivot, InPivot(spec.Hud), weapon);
            if (spec.Scope)
                BuildScope(pivot, InPivot, gunMaterial, spec.Muzzle);

            EditorAssets.Set(weapon,
                ("m_Data", data),
                ("m_Events", DataBuilder.Events),
                ("m_Settings", DataBuilder.Settings),
                ("m_Comfort", DataBuilder.Comfort),
                ("m_Buttons", DataBuilder.Buttons),
                ("m_Anchors", DataBuilder.Anchors),
                ("m_Shooter", shooter),
                ("m_Recoil", recoil),
                ("m_Haptics", haptics),
                ("m_Magwell", magwell),
                ("m_Audio", audio),
                ("m_MuzzleFlash", flash),
                ("m_Laser", laser),
                ("m_Trigger", trigger),
                ("m_Slide", slide),
                // The models face backwards inside the prefab, so "back" for a part is its local +Z.
                ("m_TriggerPull", new Vector3(0f, 0f, 0.006f)),
                ("m_SlideTravel", new Vector3(0f, 0f, spec.SlideTravel)));

            hud.SetActive(true);
            EditorAssets.SavePrefab(root, WeaponPath(spec.Id));
        }

        static MagwellSocket BuildMagwell(GunSpec spec, WeaponData data, Weapon weapon, Transform pivot, Transform model, Material gunMaterial, Func<Vector3, Vector3> inPivot)
        {
            Vector3 position;
            Quaternion rotation;
            Magazine magazinePrefab;

            if (spec.ShellPort.HasValue)
            {
                position = inPivot(spec.ShellPort.Value);
                rotation = Quaternion.identity;
                magazinePrefab = BuildShell(spec.Id, data);
            }
            else
            {
                // The magazine in the model shows where a real one sits: take its mesh for the prefab, then hide it.
                var part = FindDeep(model, spec.MagazinePart);
                var filter = part.GetComponent<MeshFilter>();
                var mesh = filter.sharedMesh;
                position = pivot.InverseTransformPoint(part.TransformPoint(mesh.bounds.center));
                rotation = Quaternion.Inverse(pivot.rotation) * part.rotation;
                magazinePrefab = BuildMagazine(spec.Id, data, mesh, gunMaterial);
                part.GetComponent<MeshRenderer>().enabled = false;
            }

            EditorAssets.Set(data, ("m_MagazinePrefab", magazinePrefab));

            // The socket finds magazines through its trigger, so it sits on a layer that still overlaps them.
            var socketObject = EditorAssets.Child(pivot, "Magwell", position, rotation);
            socketObject.layer = 0;
            var trigger = socketObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.045f;
            var attach = EditorAssets.Child(socketObject.transform, "Attach").transform;

            var socket = socketObject.AddComponent<MagwellSocket>();
            socket.attachTransform = attach;
            socket.showInteractableHoverMeshes = false;
            socket.recycleDelayTime = 0.8f;
            EditorAssets.Set(socket, ("m_Weapon", weapon));
            return socket;
        }

        static Magazine BuildMagazine(string id, WeaponData data, Mesh mesh, Material material)
        {
            // Origin at the middle of the magazine, so it sits in the hand rather than hanging off it.
            var root = new GameObject("Magazine_" + id);
            var visual = EditorAssets.Child(root.transform, "Visual", -mesh.bounds.center);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = Vector3.Max(mesh.bounds.size, Vector3.one * 0.012f);
            return FinishMagazine(root, id, data, data.MagazineSize);
        }

        static Magazine BuildShell(string id, WeaponData data)
        {
            var root = new GameObject("Magazine_" + id);
            var hull = EditorAssets.Primitive(PrimitiveType.Cylinder, root.transform, "Hull", new Vector3(0f, 0f, 0.006f), new Vector3(0.02f, 0.024f, 0.02f), ArtBuilder.Get("ShellRed"));
            hull.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var brass = EditorAssets.Primitive(PrimitiveType.Cylinder, root.transform, "Brass", new Vector3(0f, 0f, -0.023f), new Vector3(0.021f, 0.006f, 0.021f), ArtBuilder.Get("Brass"));
            brass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.022f, 0.022f, 0.062f);
            return FinishMagazine(root, id, data, 1);
        }

        static Magazine FinishMagazine(GameObject root, string id, WeaponData data, int rounds)
        {
            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.useDynamicAttach = true;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.smoothPosition = false;
            grab.smoothRotation = false;
            grab.trackScale = false;

            var magazine = root.AddComponent<Magazine>();
            EditorAssets.Set(magazine, ("m_Weapon", data), ("m_Rounds", rounds));
            EditorAssets.SetLayerRecursively(root, ProjectSetup.WeaponsLayer);
            return EditorAssets.SavePrefab(root, MagazinePath(id)).GetComponent<Magazine>();
        }

        static GameObject BuildHud(Transform pivot, Vector3 position, Weapon weapon)
        {
            // Tilted up toward the eye. TextMeshPro reads from its -Z side, which already faces the shooter.
            var root = EditorAssets.Child(pivot, "Hud", position, Quaternion.Euler(25f, 0f, 0f));
            var ammo = HudText(root.transform, "Ammo", Vector3.zero, 0.14f, "7/7 SEMI");
            var combo = HudText(root.transform, "Combo", new Vector3(0f, 0.02f, 0f), 0.18f, "x1.21");
            combo.color = new Color(1f, 0.85f, 0.2f);

            var hud = pivot.root.gameObject.AddComponent<WeaponHud>();
            EditorAssets.Set(hud, ("m_Events", DataBuilder.Events), ("m_Weapon", weapon), ("m_Root", root), ("m_Ammo", ammo), ("m_Combo", combo));
            return root;
        }

        static TextMeshPro HudText(Transform parent, string name, Vector3 position, float fontSize, string sample)
        {
            var text = EditorAssets.Child(parent, name, position).AddComponent<TextMeshPro>();
            text.text = sample;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(0.2f, 0.03f);
            return text;
        }

        static void BuildScope(Transform pivot, Func<Vector3, Vector3> inPivot, Material gunMaterial, Vector3 muzzle)
        {
            // The scope sits on the top rail. Its tube is a millimetre above its own origin and its rings reach
            // 34 mm below, so the origin goes that far above the rail.
            const float railTop = 0.062f;
            const float axis = railTop + 0.035f;
            var scopeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.GunPack + "/Prefabs/TAN_LR_Scope_01.prefab");
            var scopeRoot = EditorAssets.Child(pivot, "Scope", inPivot(new Vector3(0f, axis - 0.001f, -0.03f)));
            if (scopeAsset != null)
            {
                var scopeModel = (GameObject)PrefabUtility.InstantiatePrefab(scopeAsset, scopeRoot.transform);
                scopeModel.name = "Model";
                scopeModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (var renderer in scopeModel.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sharedMaterial = gunMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            // The eyepiece ends 146 mm behind the scope's origin; the lens sits just proud of it, facing the eye.
            var lens = EditorAssets.Primitive(PrimitiveType.Quad, scopeRoot.transform, "Lens", new Vector3(0f, 0.001f, -0.148f), Vector3.one * 0.034f, ArtBuilder.Get("ScopeLens"));

            var cameraObject = EditorAssets.Child(scopeRoot.transform, "ScopeCamera", new Vector3(0f, 0.001f, 0.15f));
            // The scope sits above the barrel, so it is zeroed like a real one: tipped down until the crosshair
            // and the shot meet at 75 m. Between 50 and 100 m the two then stay within a ring of each other.
            const float zeroDistance = 75f;
            var zeroPoint = pivot.TransformPoint(inPivot(muzzle + Vector3.forward * zeroDistance));
            cameraObject.transform.rotation = Quaternion.LookRotation(zeroPoint - cameraObject.transform.position, pivot.up);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 4f;
            // Nothing closer than the muzzle is worth drawing, and a far-out near plane keeps depth precise at range.
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 600f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.cullingMask = ~((1 << ProjectSetup.WeaponsLayer) | (1 << 5));
            var cameraData = camera.GetUniversalAdditionalCameraData();
            // A scope image is flat: it is rendered once, not once per eye.
            cameraData.allowXRRendering = false;
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;

            var view = scopeRoot.AddComponent<ScopeView>();
            EditorAssets.Set(view,
                ("m_Anchors", DataBuilder.Anchors),
                ("m_Camera", camera),
                ("m_Lens", lens.GetComponent<Renderer>()),
                ("m_IdleTexture", ArtBuilder.Texture("ScopeGlass")));
            EditorAssets.SetLayerRecursively(scopeRoot, ProjectSetup.WeaponsLayer);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            throw new InvalidOperationException("No part named " + name + " under " + root.name);
        }
    }
}
