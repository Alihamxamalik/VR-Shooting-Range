using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RangeControl.Core
{
    /// <summary>
    /// Turns on fixed foveated rendering once the headset display is running: full resolution where the eye
    /// looks, less toward the edge of the lens where it cannot tell. On a standalone headset this is one of
    /// the cheapest ways to buy frame time. It does nothing in the Editor, where there is no XR display.
    /// </summary>
    public sealed class FoveatedRendering : MonoBehaviour
    {
        [Tooltip("0 is off, 1 is the strongest foveation the headset offers.")]
        [SerializeField, Range(0f, 1f)] float m_Level = 0.7f;

        static readonly List<XRDisplaySubsystem> s_Displays = new List<XRDisplaySubsystem>();
        bool m_Applied;

        void Update()
        {
            // The display subsystem starts a few frames after the scene loads, so keep trying until it is there.
            if (m_Applied)
                return;
            SubsystemManager.GetSubsystems(s_Displays);
            foreach (var display in s_Displays)
            {
                if (!display.running)
                    continue;
                display.foveatedRenderingLevel = m_Level;
                display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;
                m_Applied = true;
            }

            if (m_Applied)
                enabled = false;
        }
    }
}
