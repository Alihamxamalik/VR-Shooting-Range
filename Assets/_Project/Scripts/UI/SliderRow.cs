using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.UI
{
    /// <summary>A line of the control panel with a slider that snaps to the setting's step, such as distance or speed.</summary>
    public sealed class SliderRow : MonoBehaviour
    {
        [SerializeField] TMP_Text m_Label;
        [SerializeField] TMP_Text m_Value;
        [SerializeField] Slider m_Slider;

        Func<float> m_Get;
        Action<float> m_Set;
        Func<float, string> m_Format;
        float m_Min;
        float m_Step = 1f;

        public void Bind(string label, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> format)
        {
            m_Label.text = label;
            m_Get = get;
            m_Set = set;
            m_Format = format;
            m_Step = step;
            m_Slider.wholeNumbers = true;
            SetRange(min, max);
        }

        /// <summary>Changes the ends of the slider, as when a scoped rifle opens the range out to 100 m.</summary>
        public void SetRange(float min, float max)
        {
            m_Min = min;
            m_Slider.minValue = 0f;
            m_Slider.maxValue = Mathf.Round((max - min) / m_Step);
            Refresh();
        }

        public void Refresh()
        {
            if (m_Get == null)
                return;
            var value = m_Get();
            m_Slider.SetValueWithoutNotify(Mathf.Round((value - m_Min) / m_Step));
            m_Value.text = m_Format(value);
        }

        public void SetInteractable(bool interactable)
        {
            m_Slider.interactable = interactable;
        }

        void OnEnable()
        {
            m_Slider.onValueChanged.AddListener(OnSliderChanged);
        }

        void OnDisable()
        {
            m_Slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        void OnSliderChanged(float steps)
        {
            m_Set?.Invoke(m_Min + steps * m_Step);
        }
    }
}
