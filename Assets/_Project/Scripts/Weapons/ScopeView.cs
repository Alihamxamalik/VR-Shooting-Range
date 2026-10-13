using RangeControl.Core;
using UnityEngine;

namespace RangeControl.Weapons
{
    /// <summary>
    /// A working magnified scope. A second camera with a narrow field of view renders into a small texture
    /// shown on the lens, but only while the eye is close enough to look through it. The rest of the time the
    /// camera is off and the lens shows a still glass texture, which protects frame time on the headset.
    /// </summary>
    public sealed class ScopeView : MonoBehaviour
    {
        static readonly int s_BaseMap = Shader.PropertyToID("_BaseMap");

        [SerializeField] PlayerAnchors m_Anchors;
        [SerializeField] Camera m_Camera;
        [SerializeField] Renderer m_Lens;
        [Tooltip("Shown on the lens while the scope camera is off.")]
        [SerializeField] Texture m_IdleTexture;
        [Tooltip("The scope renders only while the headset is within this many metres of the eyepiece.")]
        [SerializeField] float m_ActiveDistance = 0.25f;
        [SerializeField] int m_Resolution = 512;

        RenderTexture m_Texture;
        MaterialPropertyBlock m_Block;
        bool m_Live;

        public bool IsLive => m_Live;

        void OnEnable()
        {
            m_Block ??= new MaterialPropertyBlock();
            // A 24-bit depth buffer: at 100 m a 16-bit one cannot tell a paper target from the board behind it.
            m_Texture = new RenderTexture(m_Resolution, m_Resolution, 24) { name = "Scope View", antiAliasing = 2 };
            m_Camera.targetTexture = m_Texture;
            m_Live = true;
            SetLive(false);
        }

        void OnDisable()
        {
            m_Camera.enabled = false;
            m_Camera.targetTexture = null;
            if (m_Texture != null)
            {
                m_Texture.Release();
                Destroy(m_Texture);
                m_Texture = null;
            }
        }

        void Update()
        {
            var head = m_Anchors != null ? m_Anchors.Head : null;
            var near = head != null && (head.position - m_Lens.transform.position).sqrMagnitude <= m_ActiveDistance * m_ActiveDistance;
            if (near != m_Live)
                SetLive(near);
        }

        void SetLive(bool live)
        {
            if (m_Live == live)
                return;
            m_Live = live;
            m_Camera.enabled = live;
            m_Block.SetTexture(s_BaseMap, live ? m_Texture : (m_IdleTexture != null ? m_IdleTexture : Texture2D.blackTexture));
            m_Lens.SetPropertyBlock(m_Block);
        }
    }
}
