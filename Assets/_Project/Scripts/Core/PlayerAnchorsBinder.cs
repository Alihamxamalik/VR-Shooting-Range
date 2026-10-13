using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>Sits on the headset camera and publishes it through <see cref="PlayerAnchors"/>.</summary>
    public sealed class PlayerAnchorsBinder : MonoBehaviour
    {
        [SerializeField] PlayerAnchors m_Anchors;

        void OnEnable()
        {
            if (m_Anchors != null)
                m_Anchors.Register(transform);
        }

        void OnDisable()
        {
            if (m_Anchors != null)
                m_Anchors.Unregister(transform);
        }
    }
}
