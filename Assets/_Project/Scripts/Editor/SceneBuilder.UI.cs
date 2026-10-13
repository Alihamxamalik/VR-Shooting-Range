using RangeControl.Core;
using RangeControl.Range;
using RangeControl.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.Editor
{
    public static partial class SceneBuilder
    {
        const float k_PanelScale = 0.0005f;
        const float k_RowHeight = 76f;
        const float k_RowPitch = 84f;
        const float k_ColumnWidth = 590f;
        const float k_RightColumn = 610f;

        static void BuildUi(Built built)
        {
            BuildControlPanel(built);
            BuildScoreboard(built);
            BuildStatsBoard(built);
            BuildPopups(built);
            BuildTutorial(built);
            BuildWristMenu(built);
        }

        // ---------------------------------------------------------------- control panel

        static void BuildControlPanel(Built built)
        {
            // The console stands at the player's right hand, tilted back so it is read looking down at it.
            var panel = Group("ControlPanel");
            panel.SetPositionAndRotation(s_PanelPosition, s_PanelRotation);
            built.Panel = panel;

            EditorAssets.Primitive(PrimitiveType.Cube, panel, "Housing", new Vector3(0f, -0.03f, 0.03f), new Vector3(0.68f, 0.56f, 0.04f), ArtBuilder.Get("Console"));
            var pedestal = EditorAssets.Primitive(PrimitiveType.Cube, panel, "Pedestal", new Vector3(0f, -0.62f, 0.32f), new Vector3(0.4f, 0.75f, 0.3f), ArtBuilder.Get("Console"));
            pedestal.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);

            var canvas = UiFactory.Canvas(panel, "Screen", new Vector2(1240f, 800f), k_PanelScale, built.Head);
            canvas.localPosition = new Vector3(0f, 0.02f, 0f);
            UiFactory.Stretch(UiFactory.Panel(canvas, "Background", 0f, 0f, 1240f, 800f, UiFactory.Background).rectTransform);

            var status = UiFactory.Text(canvas, "Status", 24f, 10f, 780f, 64f, "FREE PRACTICE · READY", 34f, TextAlignmentOptions.Left, FontStyles.Bold);
            var multiplier = UiFactory.Text(canvas, "Multiplier", 820f, 10f, 396f, 64f, "Score x1.00", 34f, TextAlignmentOptions.Right, FontStyles.Bold, UiFactory.Accent);

            var tabNames = new[] { "Targets", "Gun", "Round", "Comfort" };
            var tabButtons = new object[tabNames.Length];
            var tabPages = new object[tabNames.Length];
            for (var i = 0; i < tabNames.Length; i++)
            {
                tabButtons[i] = UiFactory.Button(canvas, "Tab_" + tabNames[i], 20f + i * 304f, 84f, 296f, 72f, tabNames[i], 34f);
                tabPages[i] = UiFactory.Rect(canvas, "Page_" + tabNames[i], 20f, 168f, 1200f, 616f).gameObject;
                // The scene is saved showing the first tab, as it will open at runtime.
                ((GameObject)tabPages[i]).SetActive(i == 0);
            }

            Transform Page(int index) => ((GameObject)tabPages[index]).transform;
            float Row(int index) => index * k_RowPitch;

            // Targets.
            var distance = UiFactory.SliderRow(Page(0), "Distance", 0f, Row(0), k_ColumnWidth, k_RowHeight);
            var speed = UiFactory.SliderRow(Page(0), "Speed", 0f, Row(1), k_ColumnWidth, k_RowHeight);
            var slow = UiFactory.Button(Page(0), "Slow", 0f, Row(2), 190f, k_RowHeight, "Slow", 30f);
            var medium = UiFactory.Button(Page(0), "Medium", 200f, Row(2), 190f, k_RowHeight, "Medium", 30f);
            var fast = UiFactory.Button(Page(0), "Fast", 400f, Row(2), 190f, k_RowHeight, "Fast", 30f);
            var pattern = UiFactory.Stepper(Page(0), "Pattern", 0f, Row(3), k_ColumnWidth, k_RowHeight);
            var spawnInterval = UiFactory.Stepper(Page(0), "SpawnInterval", 0f, Row(4), k_ColumnWidth, k_RowHeight);
            var exposure = UiFactory.Stepper(Page(0), "Exposure", 0f, Row(5), k_ColumnWidth, k_RowHeight);
            var targetsPerRound = UiFactory.Stepper(Page(0), "TargetsPerRound", k_RightColumn, Row(0), k_ColumnWidth, k_RowHeight);
            var targetSize = UiFactory.Stepper(Page(0), "TargetSize", k_RightColumn, Row(1), k_ColumnWidth, k_RowHeight);
            UiFactory.Text(Page(0), "PreviewLabel", k_RightColumn, Row(2), 200f, k_RowHeight, "Preview", 30f, TextAlignmentOptions.Left, FontStyles.Normal, UiFactory.DimText);
            var track = UiFactory.Panel(Page(0), "PreviewTrack", k_RightColumn + 200f, Row(2) + 8f, k_ColumnWidth - 200f, 60f, UiFactory.Surface);
            // The ghost moves every frame; its own canvas keeps that from re-batching the whole panel each time.
            track.gameObject.AddComponent<Canvas>();
            var ghost = UiFactory.Panel(track.transform, "Ghost", 0f, 0f, 30f, 46f, new Color(0.9f, 0.55f, 0.2f));
            ghost.rectTransform.anchorMin = ghost.rectTransform.anchorMax = ghost.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ghost.rectTransform.anchoredPosition = Vector2.zero;
            UiFactory.Text(Page(0), "PreviewNote", k_RightColumn, Row(3), k_ColumnWidth, 150f,
                "Every change here shows on the range at once: the carrier drives to the new distance and targets take the new speed.",
                26f, TextAlignmentOptions.TopLeft, FontStyles.Normal, UiFactory.DimText);

            // Gun.
            var fireRate = UiFactory.Stepper(Page(1), "FireRate", 0f, Row(0), k_ColumnWidth, k_RowHeight);
            var infiniteAmmo = UiFactory.Stepper(Page(1), "InfiniteAmmo", 0f, Row(1), k_ColumnWidth, k_RowHeight);
            var laser = UiFactory.Stepper(Page(1), "LaserSight", 0f, Row(2), k_ColumnWidth, k_RowHeight);
            var gunSummary = UiFactory.Text(Page(1), "GunSummary", k_RightColumn, Row(0), k_ColumnWidth, 240f, "No gun in hand", 40f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiFactory.Text(Page(1), "GunHelp", k_RightColumn, Row(3), k_ColumnWidth, 250f,
                "B or Y on the gun hand reloads. A or X releases a locked slide, or switches between single shots and automatic fire on guns that have both.",
                26f, TextAlignmentOptions.TopLeft, FontStyles.Normal, UiFactory.DimText);

            // Round.
            var mode = UiFactory.Stepper(Page(2), "Mode", 0f, Row(0), k_ColumnWidth, k_RowHeight);
            var tier = UiFactory.Stepper(Page(2), "Tier", 0f, Row(1), k_ColumnWidth, k_RowHeight);
            var timer = UiFactory.Stepper(Page(2), "Timer", 0f, Row(2), k_ColumnWidth, k_RowHeight);
            var goal = UiFactory.Text(Page(2), "Goal", 0f, Row(3), k_ColumnWidth, 160f, "", 28f, TextAlignmentOptions.TopLeft);
            var newPaper = UiFactory.Button(Page(2), "NewPaper", 0f, Row(5), 280f, k_RowHeight, "New paper", 30f);
            var presetLoad = new object[PresetStore.SlotCount];
            var presetSave = new object[PresetStore.SlotCount];
            var presetLabels = new object[PresetStore.SlotCount];
            for (var i = 0; i < PresetStore.SlotCount; i++)
            {
                UiFactory.Text(Page(2), "PresetName" + i, k_RightColumn, Row(i), 110f, k_RowHeight, "Slot " + (i + 1), 28f, TextAlignmentOptions.Left, FontStyles.Bold);
                presetLabels[i] = UiFactory.Text(Page(2), "PresetLabel" + i, k_RightColumn + 110f, Row(i), 210f, k_RowHeight, "Empty", 22f, TextAlignmentOptions.Left, FontStyles.Normal, UiFactory.DimText);
                presetLoad[i] = UiFactory.Button(Page(2), "PresetLoad" + i, k_RightColumn + 330f, Row(i), 125f, k_RowHeight, "Load", 28f);
                presetSave[i] = UiFactory.Button(Page(2), "PresetSave" + i, k_RightColumn + 465f, Row(i), 125f, k_RowHeight, "Save", 28f);
            }

            // Comfort.
            var seated = UiFactory.Stepper(Page(3), "Seated", 0f, Row(0), k_ColumnWidth, k_RowHeight);
            var leftHanded = UiFactory.Stepper(Page(3), "LeftHanded", 0f, Row(1), k_ColumnWidth, k_RowHeight);
            var snapTurn = UiFactory.Stepper(Page(3), "SnapTurn", 0f, Row(2), k_ColumnWidth, k_RowHeight);
            var quickReload = UiFactory.Stepper(Page(3), "QuickReload", 0f, Row(3), k_ColumnWidth, k_RowHeight);
            var earProtection = UiFactory.Stepper(Page(3), "EarProtection", 0f, Row(4), k_ColumnWidth, k_RowHeight);
            var masterVolume = UiFactory.Stepper(Page(3), "MasterVolume", k_RightColumn, Row(0), k_ColumnWidth, k_RowHeight);
            var gunVolume = UiFactory.Stepper(Page(3), "GunVolume", k_RightColumn, Row(1), k_ColumnWidth, k_RowHeight);
            var musicVolume = UiFactory.Stepper(Page(3), "MusicVolume", k_RightColumn, Row(2), k_ColumnWidth, k_RowHeight);
            var haptics = UiFactory.Stepper(Page(3), "Haptics", k_RightColumn, Row(3), k_ColumnWidth, k_RowHeight);

            // Shown over the bottom of every page.
            var lockBanner = UiFactory.Panel(canvas, "LockBanner", 20f, 712f, 880f, 72f, new Color(0.45f, 0.12f, 0.1f));
            UiFactory.Stretch(UiFactory.Text(lockBanner.transform, "Text", 0f, 0f, 880f, 72f, "CHALLENGE MODE: settings are fixed so scores stay comparable", 26f, TextAlignmentOptions.Center, FontStyles.Bold).rectTransform);
            var retry = UiFactory.Button(canvas, "Retry", 920f, 712f, 300f, 72f, "Retry", 34f, new Color(0.15f, 0.6f, 0.25f));
            lockBanner.gameObject.SetActive(false);
            retry.gameObject.SetActive(false);

            // Physical controls on the sill below the screen, pressed into the console.
            var sill = Group("Controls", panel);
            sill.localPosition = new Vector3(0f, -0.25f, 0f);
            var start = BuildPhysicalButton(sill, "StartButton", new Vector3(-0.2f, 0f, 0f), "ButtonGreen", 0.07f);
            var recall = BuildPhysicalButton(sill, "RecallButton", new Vector3(0.04f, 0f, 0f), "ButtonBlue", 0.055f);
            ConsoleLabel(sill, "StartLabel", new Vector3(-0.2f, 0.058f, -0.012f), "START / STOP");
            ConsoleLabel(sill, "RecallLabel", new Vector3(0.04f, 0.058f, -0.012f), "RECALL TARGET");

            // The lever beside the start button shows at a glance whether the range is live.
            var leverBase = Group("Lever", sill);
            leverBase.localPosition = new Vector3(-0.3f, 0f, 0f);
            EditorAssets.Primitive(PrimitiveType.Cube, leverBase, "Slot", new Vector3(0f, 0f, 0.002f), new Vector3(0.03f, 0.09f, 0.01f), ArtBuilder.Get("MetalDark"));
            var arm = Group("Arm", leverBase);
            EditorAssets.Primitive(PrimitiveType.Cube, arm, "Stick", new Vector3(0f, 0f, -0.035f), new Vector3(0.012f, 0.012f, 0.07f), ArtBuilder.Get("Metal"));
            EditorAssets.Primitive(PrimitiveType.Sphere, arm, "Knob", new Vector3(0f, 0f, -0.075f), Vector3.one * 0.03f, ArtBuilder.Get("ButtonRed"));

            // Scoring monitor: a second small screen on the inboard side of the console.
            var monitorCanvas = UiFactory.Canvas(panel, "ScoringMonitor", new Vector2(440f, 540f), k_PanelScale, null);
            monitorCanvas.localPosition = new Vector3(-0.45f, 0.05f, 0f);
            EditorAssets.Primitive(PrimitiveType.Cube, panel, "MonitorHousing", new Vector3(-0.45f, 0.05f, 0.022f), new Vector3(0.24f, 0.29f, 0.03f), ArtBuilder.Get("Console"));
            UiFactory.Stretch(UiFactory.Panel(monitorCanvas, "Background", 0f, 0f, 440f, 540f, UiFactory.Background).rectTransform);
            UiFactory.Text(monitorCanvas, "Title", 20f, 8f, 400f, 44f, "SHOT MONITOR", 28f, TextAlignmentOptions.Center, FontStyles.Bold, UiFactory.DimText);
            UiFactory.Icon(monitorCanvas, "Target", 20f, 56f, 400f, 400f, ArtBuilder.Sprite("Bullseye"), Color.white);
            // The rings fill five sixths of the sheet, so the plotting area is that much of the picture.
            var face = UiFactory.Rect(monitorCanvas, "Face", 220f - 166.7f, 256f - 166.7f, 333.4f, 333.4f);
            var dots = new object[16];
            for (var i = 0; i < dots.Length; i++)
            {
                var dot = UiFactory.Icon(face, "Dot" + i, 0f, 0f, 18f, 18f, ArtBuilder.Sprite("Disc"), Color.red);
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                dot.rectTransform.anchoredPosition = Vector2.zero;
                dot.enabled = false;
                dots[i] = dot;
            }

            UiFactory.Text(monitorCanvas, "LastLabel", 20f, 462f, 120f, 70f, "Last", 26f, TextAlignmentOptions.Left, FontStyles.Normal, UiFactory.DimText);
            var lastShot = UiFactory.Text(monitorCanvas, "LastShot", 110f, 462f, 90f, 70f, "-", 52f, TextAlignmentOptions.Left, FontStyles.Bold, UiFactory.Accent);
            UiFactory.Text(monitorCanvas, "TotalLabel", 210f, 462f, 90f, 70f, "Total", 26f, TextAlignmentOptions.Left, FontStyles.Normal, UiFactory.DimText);
            var total = UiFactory.Text(monitorCanvas, "Total", 290f, 462f, 140f, 70f, "0 / 0", 34f, TextAlignmentOptions.Right, FontStyles.Bold);
            var monitor = monitorCanvas.gameObject.AddComponent<ScoringMonitor>();
            EditorAssets.Set(monitor, ("m_Events", DataBuilder.Events), ("m_Face", face), ("m_Dots", dots), ("m_LastShot", lastShot), ("m_Total", total));

            var ui = panel.gameObject.AddComponent<RangeControlPanel>();
            built.PanelUi = ui;
            EditorAssets.Set(ui,
                ("m_Events", DataBuilder.Events),
                ("m_Settings", DataBuilder.Settings),
                ("m_Comfort", DataBuilder.Comfort),
                ("m_Controller", built.Controller),
                ("m_TabButtons", tabButtons),
                ("m_TabPages", tabPages),
                ("m_Distance", distance),
                ("m_Speed", speed),
                ("m_SlowPreset", slow),
                ("m_MediumPreset", medium),
                ("m_FastPreset", fast),
                ("m_Pattern", pattern),
                ("m_SpawnInterval", spawnInterval),
                ("m_Exposure", exposure),
                ("m_TargetsPerRound", targetsPerRound),
                ("m_TargetSize", targetSize),
                ("m_PreviewTrack", track.rectTransform),
                ("m_PreviewGhost", ghost.rectTransform),
                ("m_FireRate", fireRate),
                ("m_InfiniteAmmo", infiniteAmmo),
                ("m_LaserSight", laser),
                ("m_GunSummary", gunSummary),
                ("m_Mode", mode),
                ("m_Tier", tier),
                ("m_Timer", timer),
                ("m_Goal", goal),
                ("m_NewPaper", newPaper),
                ("m_PresetLoad", presetLoad),
                ("m_PresetSave", presetSave),
                ("m_PresetLabels", presetLabels),
                ("m_Seated", seated),
                ("m_LeftHanded", leftHanded),
                ("m_SnapTurn", snapTurn),
                ("m_QuickReload", quickReload),
                ("m_EarProtection", earProtection),
                ("m_MasterVolume", masterVolume),
                ("m_GunVolume", gunVolume),
                ("m_MusicVolume", musicVolume),
                ("m_Haptics", haptics),
                ("m_Status", status),
                ("m_Multiplier", multiplier),
                ("m_LockBanner", lockBanner.gameObject),
                ("m_Retry", retry),
                ("m_StartLever", start),
                ("m_RecallButton", recall),
                ("m_LeverArm", arm));
        }

        static void ConsoleLabel(Transform parent, string name, Vector3 position, string content)
        {
            var label = EditorAssets.Child(parent, name, position).AddComponent<TextMeshPro>();
            label.text = content;
            label.fontSize = 0.16f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(0.2f, 0.03f);
        }

        // ---------------------------------------------------------------- scoreboard

        static void BuildScoreboard(Built built)
        {
            // Hung from a gantry about eleven metres out, above the lanes and tipped down toward the booth.
            var board = Group("Scoreboard");
            board.position = new Vector3(0f, 4.7f, 11f);
            board.rotation = Quaternion.LookRotation(new Vector3(0f, 3.1f, 11f));
            built.Scoreboard = board;

            EditorAssets.Primitive(PrimitiveType.Cube, board, "Frame", new Vector3(0f, 0f, 0.07f), new Vector3(5.3f, 2.3f, 0.1f), ArtBuilder.Get("MetalDark"));
            var gantry = Group("ScoreboardGantry");
            foreach (var x in new[] { -6.2f, 6.2f })
                Prop(gantry, "Post", new Vector3(x, 3.1f, 11.2f), new Vector3(0.16f, 6.2f, 0.16f), "MetalDark");
            Prop(gantry, "Beam", new Vector3(0f, 6.1f, 11.2f), new Vector3(12.6f, 0.16f, 0.16f), "MetalDark");
            foreach (var x in new[] { -2.2f, 2.2f })
                Prop(gantry, "Hanger", new Vector3(x, 5.9f, 11.2f), new Vector3(0.05f, 0.5f, 0.05f), "MetalDark");

            var canvas = UiFactory.Canvas(board, "Screen", new Vector2(2000f, 840f), 0.0025f, null);
            UiFactory.Stretch(UiFactory.Panel(canvas, "Background", 0f, 0f, 2000f, 840f, new Color(0.03f, 0.035f, 0.045f), false).rectTransform);

            var live = UiFactory.Rect(canvas, "Live", 0f, 0f, 2000f, 840f);
            var title = UiFactory.Text(live, "Title", 60f, 30f, 1880f, 110f, "FREE PRACTICE", 84f, TextAlignmentOptions.Center, FontStyles.Bold, UiFactory.DimText);
            var headline = UiFactory.Text(live, "Headline", 60f, 150f, 1880f, 360f, "READY", 300f, TextAlignmentOptions.Center, FontStyles.Bold);
            var clock = UiFactory.Text(live, "Clock", 60f, 520f, 1880f, 130f, "Press START on the panel", 100f, TextAlignmentOptions.Center, FontStyles.Bold, UiFactory.Accent);
            var accuracy = UiFactory.Text(live, "Accuracy", 60f, 690f, 620f, 110f, "", 84f, TextAlignmentOptions.Left, FontStyles.Bold);
            var best = UiFactory.Text(live, "Best", 690f, 690f, 620f, 110f, "BEST 0", 84f, TextAlignmentOptions.Center, FontStyles.Bold, UiFactory.DimText);
            var combo = UiFactory.Text(live, "Combo", 1320f, 690f, 620f, 110f, "", 84f, TextAlignmentOptions.Right, FontStyles.Bold, UiFactory.Accent);

            var results = UiFactory.Rect(canvas, "Results", 0f, 0f, 2000f, 840f);
            var resultTitle = UiFactory.Text(results, "Title", 60f, 30f, 1880f, 110f, "RESULTS", 84f, TextAlignmentOptions.Center, FontStyles.Bold, UiFactory.DimText);
            var resultScore = UiFactory.Text(results, "Score", 60f, 150f, 1100f, 300f, "0", 260f, TextAlignmentOptions.Left, FontStyles.Bold);
            var badge = UiFactory.Icon(results, "MedalBadge", 1330f, 170f, 220f, 220f, ArtBuilder.Sprite("Disc"), Color.gray);
            var medal = UiFactory.Text(results, "Medal", 1180f, 400f, 760f, 110f, "NO MEDAL", 84f, TextAlignmentOptions.Center, FontStyles.Bold);
            medal.rectTransform.anchoredPosition = new Vector2(1060f, -400f);
            var details = UiFactory.Text(results, "Details", 60f, 470f, 1250f, 260f, "", 72f, TextAlignmentOptions.TopLeft);
            var resultBest = UiFactory.Text(results, "Best", 60f, 730f, 1880f, 90f, "", 72f, TextAlignmentOptions.Center, FontStyles.Bold, UiFactory.Accent);
            results.gameObject.SetActive(false);

            var ui = board.gameObject.AddComponent<ScoreboardUI>();
            EditorAssets.Set(ui,
                ("m_Events", DataBuilder.Events),
                ("m_Controller", built.Controller),
                ("m_LiveRoot", live.gameObject),
                ("m_Title", title),
                ("m_Headline", headline),
                ("m_Clock", clock),
                ("m_Accuracy", accuracy),
                ("m_Combo", combo),
                ("m_Best", best),
                ("m_ResultsRoot", results.gameObject),
                ("m_ResultTitle", resultTitle),
                ("m_ResultScore", resultScore),
                ("m_ResultDetails", details),
                ("m_ResultMedal", medal),
                ("m_MedalBadge", badge),
                ("m_ResultBest", resultBest));

            // The range's voice comes from the board: countdown beeps and buzzers.
            var speaker = board.gameObject.AddComponent<AudioSource>();
            PrefabBuilder.ConfigureAudio(speaker, 8f, 120f, 0.8f);
            speaker.spatialBlend = 0.6f;
            EditorAssets.Set(board.gameObject.AddComponent<RangeAnnouncer>(),
                ("m_Events", DataBuilder.Events),
                ("m_CountdownBeep", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/CountdownBeep.wav")),
                ("m_StartBuzzer", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/StartBuzzer.wav")),
                ("m_EndBuzzer", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/EndBuzzer.wav")),
                ("m_PenaltyBuzzer", AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectPaths.Audio + "/Range/PenaltyBuzzer.wav")));
        }

        // ---------------------------------------------------------------- stats board, popups, tutorial, wrist menu

        static void BuildStatsBoard(Built built)
        {
            // On the right-hand wall behind the console, turned to face the player.
            var board = Group("StatsBoard");
            board.position = new Vector3(1.3f, 1.5f, -0.6f);
            board.rotation = Quaternion.Euler(0f, 112f, 0f);
            EditorAssets.Primitive(PrimitiveType.Cube, board, "Backing", new Vector3(0f, 0f, 0.02f), new Vector3(1.06f, 0.76f, 0.03f), ArtBuilder.Get("WoodDark"));

            var canvas = UiFactory.Canvas(board, "Screen", new Vector2(1000f, 700f), 0.001f, null);
            UiFactory.Stretch(UiFactory.Panel(canvas, "Background", 0f, 0f, 1000f, 700f, UiFactory.Background, false).rectTransform);
            UiFactory.Text(canvas, "Title", 30f, 16f, 940f, 60f, "RANGE RECORDS", 44f, TextAlignmentOptions.Left, FontStyles.Bold, UiFactory.Accent);
            var weapons = UiFactory.Text(canvas, "Weapons", 30f, 90f, 500f, 590f, "", 28f, TextAlignmentOptions.TopLeft);
            var bests = UiFactory.Text(canvas, "Bests", 550f, 90f, 420f, 590f, "", 28f, TextAlignmentOptions.TopLeft);

            var weaponList = new object[DataBuilder.WeaponIds.Length];
            for (var i = 0; i < weaponList.Length; i++)
                weaponList[i] = DataBuilder.Weapon(DataBuilder.WeaponIds[i]);
            var modeList = new object[DataBuilder.ModeNames.Length];
            for (var i = 0; i < modeList.Length; i++)
                modeList[i] = DataBuilder.Mode(DataBuilder.ModeNames[i]);

            EditorAssets.Set(board.gameObject.AddComponent<StatsBoardUI>(),
                ("m_Events", DataBuilder.Events),
                ("m_Controller", built.Controller),
                ("m_Weapons", weaponList),
                ("m_Modes", modeList),
                ("m_WeaponColumn", weapons),
                ("m_BestColumn", bests));
        }

        static void BuildPopups(Built built)
        {
            var popups = Group("ScorePopups");
            EditorAssets.Set(popups.gameObject.AddComponent<ScorePopups>(),
                ("m_Events", DataBuilder.Events),
                ("m_Anchors", DataBuilder.Anchors),
                ("m_Prefab", EditorAssets.Load<GameObject>(PrefabBuilder.ScorePopupPath).GetComponent<TextMeshPro>()));
        }

        static void BuildTutorial(Built built)
        {
            var tutorial = Group("Tutorial");
            var hint = UiFactory.Canvas(tutorial, "Hint", new Vector2(900f, 300f), k_PanelScale, null);
            UiFactory.Stretch(UiFactory.Panel(hint, "Background", 0f, 0f, 900f, 300f, new Color(0.05f, 0.06f, 0.08f, 0.92f)).rectTransform);
            UiFactory.Panel(hint, "Stripe", 0f, 0f, 14f, 300f, UiFactory.Accent, false);
            var text = UiFactory.Text(hint, "Text", 44f, 20f, 830f, 260f, "", 40f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            // One anchor beside each thing a hint talks about. An anchor's scale sizes the hint for its distance.
            var rackAnchor = Group("RackAnchor", built.Rack);
            rackAnchor.localPosition = new Vector3(0.12f, 1.02f, 0.45f);
            var benchAnchor = Group("BenchAnchor", tutorial);
            benchAnchor.position = new Vector3(-0.05f, 1.42f, 0.95f);
            benchAnchor.localScale = Vector3.one * 1.2f;
            var panelAnchor = Group("PanelAnchor", built.Panel);
            panelAnchor.localPosition = new Vector3(-0.05f, 0.34f, -0.02f);

            EditorAssets.Set(tutorial.gameObject.AddComponent<TutorialHints>(),
                ("m_Events", DataBuilder.Events),
                ("m_Controller", built.Controller),
                ("m_Anchors", DataBuilder.Anchors),
                ("m_Comfort", DataBuilder.Comfort),
                ("m_Hint", hint),
                ("m_Text", text),
                ("m_RackAnchor", rackAnchor),
                ("m_PlatesAnchor", built.PlatesAnchorPoint),
                ("m_BenchAnchor", benchAnchor),
                ("m_PanelAnchor", panelAnchor));
        }

        static void BuildWristMenu(Built built)
        {
            // On the inside of the left wrist, reading toward the thumb side, like a watch face.
            var menu = Group("WristMenu", built.LeftController);
            menu.localPosition = new Vector3(0.035f, 0.01f, -0.11f);
            menu.localRotation = Quaternion.Euler(0f, -90f, 0f);

            var canvas = UiFactory.Canvas(menu, "Content", new Vector2(360f, 300f), 0.0004f, built.Head);
            UiFactory.Stretch(UiFactory.Panel(canvas, "Background", 0f, 0f, 360f, 300f, UiFactory.Background).rectTransform);
            var pause = UiFactory.Button(canvas, "Pause", 12f, 12f, 162f, 132f, "Pause", 34f);
            var settings = UiFactory.Button(canvas, "Settings", 186f, 12f, 162f, 132f, "Settings", 34f);
            var recenter = UiFactory.Button(canvas, "Recenter", 12f, 156f, 162f, 132f, "Recenter", 34f);
            var quit = UiFactory.Button(canvas, "Quit", 186f, 156f, 162f, 132f, "Quit", 34f, new Color(0.5f, 0.15f, 0.12f));

            EditorAssets.Set(menu.gameObject.AddComponent<WristMenu>(),
                ("m_Anchors", DataBuilder.Anchors),
                ("m_Content", canvas.gameObject),
                ("m_Pause", pause),
                ("m_PauseLabel", pause.GetComponentInChildren<TMP_Text>()),
                ("m_Settings", settings),
                ("m_Recenter", recenter),
                ("m_Quit", quit),
                ("m_Panel", built.PanelUi),
                ("m_Booth", built.Booth));
        }

        static void WireUp(Built built)
        {
            // The pieces that swap sides for a left-handed player.
            EditorAssets.Set(built.Booth, ("m_SidePieces", new object[] { built.Rack, built.Panel }));
        }
    }
}
