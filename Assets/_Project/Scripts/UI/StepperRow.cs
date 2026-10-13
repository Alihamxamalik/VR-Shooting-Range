using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.UI
{
    /// <summary>
    /// One line of the control panel: a label, the current value and a pair of big minus and plus buttons.
    /// Numbers step, choices cycle and switches flip, all through the same two buttons.
    /// </summary>
    public sealed class StepperRow : MonoBehaviour
    {
        [SerializeField] TMP_Text m_Label;
        [SerializeField] TMP_Text m_Value;
        [SerializeField] Button m_Minus;
        [SerializeField] Button m_Plus;

        Func<string> m_Format;
        Action<int> m_Step;

        /// <summary><paramref name="step"/> receives -1 or +1; <paramref name="format"/> returns the text to show.</summary>
        public void Bind(string label, Func<string> format, Action<int> step)
        {
            m_Label.text = label;
            m_Format = format;
            m_Step = step;
            Refresh();
        }

        public void Refresh()
        {
            if (m_Format != null)
                m_Value.text = m_Format();
        }

        public void SetInteractable(bool interactable)
        {
            m_Minus.interactable = interactable;
            m_Plus.interactable = interactable;
        }

        void OnEnable()
        {
            m_Minus.onClick.AddListener(StepDown);
            m_Plus.onClick.AddListener(StepUp);
        }

        void OnDisable()
        {
            m_Minus.onClick.RemoveListener(StepDown);
            m_Plus.onClick.RemoveListener(StepUp);
        }

        void StepDown()
        {
            m_Step?.Invoke(-1);
        }

        void StepUp()
        {
            m_Step?.Invoke(1);
        }
    }
}
