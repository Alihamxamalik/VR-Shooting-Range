using RangeControl.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace RangeControl.Editor
{
    /// <summary>
    /// Builds world-space UI from code. Everything is placed with a top-left origin in canvas pixels, so the
    /// layout reads like a sketch: x, y, width, height. Sizes follow the design rule that body text is about
    /// 2 cm tall for every metre the player stands from it.
    /// </summary>
    static class UiFactory
    {
        public const int UiLayer = 5;

        public static readonly Color Background = new Color(0.07f, 0.08f, 0.1f, 0.97f);
        public static readonly Color Surface = new Color(0.14f, 0.16f, 0.19f);
        public static readonly Color ButtonColor = new Color(0.24f, 0.27f, 0.32f);
        public static readonly Color Accent = new Color(0.95f, 0.6f, 0.1f);
        public static readonly Color TextColor = new Color(0.93f, 0.94f, 0.95f);
        public static readonly Color DimText = new Color(0.62f, 0.66f, 0.7f);

        static Sprite Rounded => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite Knob => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        /// <summary>A world-space canvas. Pass a camera to make it something hands can press; leave it null for a display.</summary>
        public static RectTransform Canvas(Transform parent, string name, Vector2 pixels, float metresPerPixel, Camera eventCamera)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (eventCamera != null)
            {
                canvas.worldCamera = eventCamera;
                canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = pixels;
            rect.localScale = Vector3.one * metresPerPixel;
            canvasObject.layer = UiLayer;
            return rect;
        }

        public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rectObject = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)rectObject.transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, width, height);
            return rect;
        }

        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static Image Panel(Transform parent, string name, float x, float y, float width, float height, Color colour, bool rounded = true)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static Image Icon(Transform parent, string name, float x, float y, float width, float height, Sprite sprite, Color colour)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float x, float y, float width, float height, string content,
            float size, TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal, Color? colour = null)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.fontStyle = style;
            text.color = colour ?? TextColor;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        public static Button Button(Transform parent, string name, float x, float y, float width, float height, string label, float fontSize, Color? colour = null)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = colour ?? ButtonColor;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colours = button.colors;
            colours.normalColor = new Color(0.86f, 0.86f, 0.86f);
            colours.highlightedColor = Color.white;
            colours.pressedColor = new Color(0.6f, 0.6f, 0.6f);
            colours.selectedColor = new Color(0.86f, 0.86f, 0.86f);
            colours.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
            colours.fadeDuration = 0.05f;
            button.colors = colours;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var text = Text(image.transform, "Label", 0f, 0f, width, height, label, fontSize, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        public static Slider Slider(Transform parent, string name, float x, float y, float width, float height)
        {
            var root = Rect(parent, name, x, y, width, height);
            var slider = root.gameObject.AddComponent<Slider>();
            const float handleSize = 62f;

            var background = Panel(root, "Background", 0f, (height - 16f) * 0.5f, width, 16f, Surface);
            background.raycastTarget = true;

            var fillArea = Rect(root, "Fill Area", 0f, (height - 16f) * 0.5f, width, 16f);
            var fill = Panel(fillArea, "Fill", 0f, 0f, 0f, 16f, Accent);
            Stretch(fill.rectTransform);

            // The slider stretches its handle to the height of the slide area, so the area is made handle-high.
            var handleArea = Rect(root, "Handle Slide Area", handleSize * 0.5f, (height - handleSize) * 0.5f, width - handleSize, handleSize);
            var handle = Rect(handleArea, "Handle", 0f, 0f, handleSize, handleSize).gameObject.AddComponent<Image>();
            handle.sprite = Knob;
            handle.color = TextColor;
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(handleSize, 0f);
            handle.rectTransform.anchoredPosition = Vector2.zero;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        /// <summary>Label, minus button, value, plus button: one line of the control panel.</summary>
        public static StepperRow Stepper(Transform parent, string name, float x, float y, float width, float height)
        {
            var root = Rect(parent, name, x, y, width, height);
            var button = height;
            var valueWidth = width * 0.3f;
            var labelWidth = width - valueWidth - button * 2f - 12f;

            var label = Text(root, "Label", 0f, 0f, labelWidth, height, name, 30f, TextAlignmentOptions.Left, FontStyles.Normal, DimText);
            var minus = Button(root, "Minus", labelWidth, 0f, button, height, "-", 44f);
            var value = Text(root, "Value", labelWidth + button + 6f, 0f, valueWidth, height, "-", 30f, TextAlignmentOptions.Center, FontStyles.Bold);
            var plus = Button(root, "Plus", labelWidth + button + valueWidth + 12f, 0f, button, height, "+", 44f);

            var row = root.gameObject.AddComponent<StepperRow>();
            EditorAssets.Set(row, ("m_Label", label), ("m_Value", value), ("m_Minus", minus), ("m_Plus", plus));
            return row;
        }

        /// <summary>Label, slider, value: a line of the panel for a setting that is dragged.</summary>
        public static SliderRow SliderRow(Transform parent, string name, float x, float y, float width, float height)
        {
            var root = Rect(parent, name, x, y, width, height);
            var labelWidth = width * 0.33f;
            var valueWidth = width * 0.2f;
            var sliderWidth = width - labelWidth - valueWidth - 16f;

            var label = Text(root, "Label", 0f, 0f, labelWidth, height, name, 30f, TextAlignmentOptions.Left, FontStyles.Normal, DimText);
            var slider = Slider(root, "Slider", labelWidth, 0f, sliderWidth, height);
            var value = Text(root, "Value", labelWidth + sliderWidth + 16f, 0f, valueWidth, height, "-", 30f, TextAlignmentOptions.Right, FontStyles.Bold);

            var row = root.gameObject.AddComponent<SliderRow>();
            EditorAssets.Set(row, ("m_Label", label), ("m_Value", value), ("m_Slider", slider));
            return row;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetLayer(Transform root)
        {
            EditorAssets.SetLayerRecursively(root.gameObject, UiLayer);
        }
    }
}
