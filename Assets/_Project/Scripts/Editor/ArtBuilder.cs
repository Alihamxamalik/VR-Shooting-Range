using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RangeControl.Editor
{
    /// <summary>
    /// Generates the project's own textures and materials. The look is flat-shaded low poly to sit with the
    /// gun models: muted sand and wood everywhere, with the targets the only high-contrast things in view.
    /// The bullseye texture is drawn from the same ring width the scoring uses, so the picture cannot drift
    /// from the rules.
    /// </summary>
    public static class ArtBuilder
    {
        public const float PaperSize = 0.6f;
        public const float RingWidth = 0.025f;

        const string k_SimpleLit = "Universal Render Pipeline/Simple Lit";
        const string k_Unlit = "Universal Render Pipeline/Unlit";
        const string k_ParticlesUnlit = "Universal Render Pipeline/Particles/Unlit";

        public static Material Get(string name)
        {
            return EditorAssets.Load<Material>(ProjectPaths.Materials + "/" + name + ".mat");
        }

        public static Texture2D Texture(string name)
        {
            return EditorAssets.Load<Texture2D>(ProjectPaths.Textures + "/" + name + ".png");
        }

        public static Sprite Sprite(string name)
        {
            return EditorAssets.Load<Sprite>(ProjectPaths.Textures + "/" + name + ".png");
        }

        [MenuItem("Tools/Range Control/Build/1. Art", priority = 20)]
        public static void Build()
        {
            EditorAssets.EnsureFolder(ProjectPaths.Materials);
            EditorAssets.EnsureFolder(ProjectPaths.Textures);

            var bullseye = WriteTexture("Bullseye", 512, DrawBullseye, importer =>
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 512;
            });
            var hole = WriteTexture("BulletHole", 64, DrawBulletHole, importer =>
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 64;
            });
            var glass = WriteTexture("ScopeGlass", 64, DrawScopeGlass, importer =>
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 64;
            });
            WriteTexture("Disc", 128, DrawDisc, importer =>
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 128;
            });

            // Environment: muted, so nothing competes with the targets.
            Lit("Sand", new Color(0.76f, 0.68f, 0.5f));
            Lit("Berm", new Color(0.52f, 0.44f, 0.31f));
            Lit("Wood", new Color(0.55f, 0.4f, 0.25f));
            Lit("WoodDark", new Color(0.36f, 0.26f, 0.17f));
            Lit("Metal", new Color(0.42f, 0.44f, 0.47f));
            Lit("MetalDark", new Color(0.16f, 0.17f, 0.19f));
            Lit("Marker", new Color(0.92f, 0.92f, 0.88f));
            Lit("Console", new Color(0.2f, 0.22f, 0.25f));
            Lit("ButtonGreen", new Color(0.15f, 0.7f, 0.25f));
            Lit("ButtonRed", new Color(0.8f, 0.15f, 0.12f));
            Lit("ButtonBlue", new Color(0.15f, 0.4f, 0.85f));
            Lit("Earmuff", new Color(0.85f, 0.45f, 0.1f));
            Lit("Brass", new Color(0.78f, 0.62f, 0.25f));
            Lit("ShellRed", new Color(0.7f, 0.12f, 0.1f));
            Lit("Plate", new Color(0.2f, 0.2f, 0.22f));

            // Targets: white paper, orange steel, tan boards, black-on-white no-shoots.
            Lit("Paper", Color.white, bullseye);
            Lit("TargetBoard", new Color(0.9f, 0.55f, 0.2f));
            Lit("TargetBoardHead", new Color(0.8f, 0.42f, 0.14f));
            Lit("NoShootBoard", new Color(0.96f, 0.96f, 0.96f));
            Lit("NoShootMark", new Color(0.05f, 0.05f, 0.05f));
            Lit("Steel", new Color(1f, 0.45f, 0.05f));

            var gunTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectPaths.GunPack + "/Textures_Guns.png");
            if (gunTexture == null)
                Debug.LogWarning("Range Control: the Low Poly Weapons pack is not in Assets/ThirdParty, so guns will be untextured.");
            Lit("Gun", Color.white, gunTexture);

            // Effects. Particles are small opaque chips in keeping with the low-poly look: no blending, no overdraw.
            Make("BulletHole", k_Unlit, Color.white, hole, Cutout);
            Make("Tracer", k_Unlit, new Color(1f, 0.92f, 0.6f));
            Make("Laser", k_Unlit, new Color(1f, 0.1f, 0.1f));
            Make("MuzzleFlash", k_ParticlesUnlit, new Color(1f, 0.85f, 0.45f));
            Make("Sparks", k_ParticlesUnlit, new Color(1f, 0.8f, 0.35f));
            Make("Dust", k_ParticlesUnlit, new Color(0.6f, 0.52f, 0.38f));
            Make("WoodChips", k_ParticlesUnlit, new Color(0.62f, 0.47f, 0.3f));
            Make("PaperFlecks", k_ParticlesUnlit, new Color(0.95f, 0.95f, 0.9f));

            var scopeShader = Shader.Find("RangeControl/Scope Lens");
            if (scopeShader == null)
                throw new InvalidOperationException("The Scope Lens shader has not been imported yet.");
            Make("ScopeLens", "RangeControl/Scope Lens", Color.white, glass);

            var sky = Make("Sky", "Skybox/Procedural", Color.white, null, material =>
            {
                material.SetFloat("_SunSize", 0.035f);
                material.SetFloat("_SunSizeConvergence", 6f);
                material.SetFloat("_AtmosphereThickness", 1.15f);
                material.SetColor("_SkyTint", new Color(0.6f, 0.62f, 0.68f));
                material.SetColor("_GroundColor", new Color(0.5f, 0.44f, 0.36f));
                material.SetFloat("_Exposure", 1.1f);
            });
            EditorUtility.SetDirty(sky);

            AssetDatabase.SaveAssets();
            Debug.Log("Range Control: art built.");
        }

        static Material Lit(string name, Color colour, Texture texture = null)
        {
            return Make(name, k_SimpleLit, colour, texture, material =>
            {
                // No specular highlight: cheaper per pixel, and right for sand, wood and paper. URP reads this
                // property as its SpecularSource enum, where 1 means "no specular" and 0 turns the highlight on.
                material.SetFloat("_SpecularHighlights", 1f);
                material.SetFloat("_Smoothness", 0f);
                material.DisableKeyword("_SPECULAR_COLOR");
                material.DisableKeyword("_SPECGLOSSMAP");
            });
        }

        static void Cutout(Material material)
        {
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }

        static Material Make(string name, string shaderName, Color colour, Texture texture = null, Action<Material> configure = null)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException("Shader not found: " + shaderName);

            var path = ProjectPaths.Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", colour);
            if (texture != null && material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            material.enableInstancing = true;
            configure?.Invoke(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D WriteTexture(string name, int size, Func<float, float, Color> draw, Action<TextureImporter> configure)
        {
            var path = ProjectPaths.Textures + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                    pixels[y * size + x] = draw((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
            }

            texture.SetPixels(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            configure(importer);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>The ten-ring face, with rings 7 to 10 in black as on a competition target. Coordinates run -0.5 to 0.5 across the sheet.</summary>
        static Color DrawBullseye(float x, float y)
        {
            var paper = new Color(0.96f, 0.95f, 0.9f);
            var ink = new Color(0.07f, 0.07f, 0.07f);
            var radius = Mathf.Sqrt(x * x + y * y) * PaperSize;
            var inBlack = radius <= RingWidth * 4f;
            var colour = inBlack ? ink : paper;

            const float lineHalfWidth = 0.0011f;
            for (var ring = 1; ring <= 10; ring++)
            {
                var onLine = 1f - Ramp(lineHalfWidth * 0.6f, lineHalfWidth * 1.4f, Mathf.Abs(radius - ring * RingWidth));
                if (onLine <= 0f)
                    continue;
                // The edge of the black is its own boundary, so the fourth line is not drawn twice.
                var lineColour = ring < 4 ? paper : ink;
                if (ring != 4)
                    colour = Color.Lerp(colour, lineColour, onLine);
            }

            var onInnerX = 1f - Ramp(lineHalfWidth * 0.4f, lineHalfWidth * 1.1f, Mathf.Abs(radius - RingWidth * 0.5f));
            colour = Color.Lerp(colour, paper, onInnerX);
            colour.a = 1f;
            return colour;
        }

        /// <summary>0 below <paramref name="from"/>, 1 above <paramref name="to"/>, and a smooth ramp in between.</summary>
        static float Ramp(float from, float to, float value)
        {
            var t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        /// <summary>A dark hole with a pale torn rim, so it shows on the black of the bull as well as on white paper.</summary>
        static Color DrawBulletHole(float x, float y)
        {
            var radius = Mathf.Sqrt(x * x + y * y) * 2f;
            var rim = Ramp(0.5f, 0.62f, radius);
            var shade = Mathf.Lerp(0.02f, 0.8f, rim);
            return new Color(shade, shade, shade * 0.95f, 1f - Ramp(0.78f, 0.95f, radius));
        }

        static Color DrawScopeGlass(float x, float y)
        {
            var radius = Mathf.Sqrt(x * x + y * y) * 2f;
            var glass = Color.Lerp(new Color(0.16f, 0.24f, 0.34f), new Color(0.03f, 0.05f, 0.08f), radius);
            var glint = Mathf.Clamp01(1f - new Vector2(x + 0.18f, y - 0.2f).magnitude * 6f) * 0.35f;
            return new Color(glass.r + glint, glass.g + glint, glass.b + glint, 1f);
        }

        static Color DrawDisc(float x, float y)
        {
            var radius = Mathf.Sqrt(x * x + y * y) * 2f;
            return new Color(1f, 1f, 1f, 1f - Ramp(0.9f, 0.98f, radius));
        }
    }
}
