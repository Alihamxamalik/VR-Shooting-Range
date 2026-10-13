using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RangeControl.Editor
{
    /// <summary>Where the project keeps things.</summary>
    static class ProjectPaths
    {
        public const string Root = "Assets/_Project";
        public const string Data = Root + "/Data";
        public const string Weapons = Data + "/Weapons";
        public const string Modes = Data + "/Modes";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";
        public const string Materials = Root + "/Art/Materials";
        public const string Textures = Root + "/Art/Textures";
        public const string Shaders = Root + "/Art/Shaders";
        public const string Audio = Root + "/Audio";
        public const string Input = Root + "/Input";
        public const string RangeScene = Scenes + "/Range.unity";

        public const string GunPack = "Assets/ThirdParty/Low Poly Weapons VOL.1";
        public const string StarterAssets = "Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets";
        public const string Simulator = "Assets/Samples/XR Interaction Toolkit/3.3.2/XR Interaction Simulator";
    }

    /// <summary>Small helpers the content builders share: folders, assets, prefabs and serialized fields.</summary>
    static class EditorAssets
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Loads the asset at <paramref name="path"/>, creating it first if it is not there yet.</summary>
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException("Expected a " + typeof(T).Name + " at " + path);
            return asset;
        }

        /// <summary>Saves the object as a prefab asset, removes it from the scene and returns the asset.</summary>
        public static GameObject SavePrefab(GameObject instance, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        /// <summary>
        /// Sets serialized fields by name, private ones included, and fails loudly on a name that does not
        /// exist, so a renamed field breaks the build step rather than silently leaving a reference empty.
        /// </summary>
        public static void Set(UnityEngine.Object target, params (string name, object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var (name, value) in fields)
            {
                var property = serialized.FindProperty(name);
                if (property == null)
                    throw new ArgumentException(target.GetType().Name + " has no serialized field named " + name);
                Assign(property, value);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static void Assign(SerializedProperty property, object value)
        {
            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case UnityEngine.Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case Enum choice:
                    property.intValue = Convert.ToInt32(choice);
                    break;
                case int whole:
                    if (property.propertyType == SerializedPropertyType.Float)
                        property.floatValue = whole;
                    else
                        property.intValue = whole;
                    break;
                case uint unsigned:
                    property.uintValue = unsigned;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case Vector2 vector2:
                    property.vector2Value = vector2;
                    break;
                case Vector3 vector3:
                    property.vector3Value = vector3;
                    break;
                case Color colour:
                    property.colorValue = colour;
                    break;
                case LayerMask mask:
                    property.intValue = mask.value;
                    break;
                case Array array:
                    property.arraySize = array.Length;
                    for (var i = 0; i < array.Length; i++)
                        Assign(property.GetArrayElementAtIndex(i), array.GetValue(i));
                    break;
                default:
                    throw new ArgumentException("Cannot assign a " + value.GetType().Name + " to " + property.propertyPath);
            }
        }

        public static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        public static GameObject Child(Transform parent, string name, Vector3 localPosition = default, Quaternion? localRotation = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.SetLocalPositionAndRotation(localPosition, localRotation ?? Quaternion.identity);
            return child;
        }

        /// <summary>A primitive with a material and, unless asked for, no collider.</summary>
        public static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider = false)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            var renderer = primitive.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (!keepCollider)
                UnityEngine.Object.DestroyImmediate(primitive.GetComponent<Collider>());
            return primitive;
        }
    }
}
