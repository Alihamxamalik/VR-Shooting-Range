using RangeControl.Core;
using RangeControl.Range;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RangeControl.UI
{
    /// <summary>
    /// A small menu on the inside of the left wrist: Pause, Settings, Recenter and Quit. It appears when the
    /// wrist is turned toward the face, like checking a watch, and is hidden the rest of the time so it never
    /// sits in the sight line. Nothing in this project is locked to the head.
    /// </summary>
    public sealed class WristMenu : MonoBehaviour
    {
        [SerializeField] PlayerAnchors m_Anchors;
        [SerializeField] GameObject m_Content;
        [SerializeField] Button m_Pause;
        [SerializeField] TMP_Text m_PauseLabel;
        [SerializeField] Button m_Settings;
        [SerializeField] Button m_Recenter;
        [SerializeField] Button m_Quit;
        [SerializeField] RangeControlPanel m_Panel;
        [SerializeField] BoothLayout m_Booth;
        [Tooltip("How squarely the menu must face the head before it shows: 1 is dead on, 0 is edge on.")]
        [SerializeField, Range(0f, 1f)] float m_ShowFacing = 0.6f;
        [SerializeField, Range(0f, 1f)] float m_HideFacing = 0.4f;

        bool m_Visible;
        bool m_Paused;

        public bool IsPaused => m_Paused;

        void OnEnable()
        {
            m_Pause.onClick.AddListener(TogglePause);
            m_Settings.onClick.AddListener(OpenSettings);
            m_Recenter.onClick.AddListener(Recenter);
            m_Quit.onClick.AddListener(Quit);
            m_Visible = true;
            SetVisible(false);
        }

        void OnDisable()
        {
            m_Pause.onClick.RemoveListener(TogglePause);
            m_Settings.onClick.RemoveListener(OpenSettings);
            m_Recenter.onClick.RemoveListener(Recenter);
            m_Quit.onClick.RemoveListener(Quit);
            if (m_Paused)
                TogglePause();
        }

        void Update()
        {
            var head = m_Anchors != null ? m_Anchors.Head : null;
            if (head == null)
                return;

            // The canvas reads from its -Z side, so that is the side that has to point at the head.
            var toHead = (head.position - transform.position).normalized;
            var facing = Vector3.Dot(-transform.forward, toHead);
            if (!m_Visible && facing >= m_ShowFacing)
                SetVisible(true);
            else if (m_Visible && facing < m_HideFacing && !m_Paused)
                SetVisible(false);
        }

        public void TogglePause()
        {
            m_Paused = !m_Paused;
            Time.timeScale = m_Paused ? 0f : 1f;
            AudioListener.pause = m_Paused;
            m_PauseLabel.text = m_Paused ? "Resume" : "Pause";
        }

        void OpenSettings()
        {
            if (m_Panel != null)
                m_Panel.ShowTab(RangeControlPanel.Tab.Comfort);
        }

        void Recenter()
        {
            if (m_Booth != null)
                m_Booth.Recenter();
        }

        void Quit()
        {
            if (m_Paused)
                TogglePause();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void SetVisible(bool visible)
        {
            if (m_Visible == visible)
                return;
            m_Visible = visible;
            m_Content.SetActive(visible);
        }
    }
}
