using UnityEngine;

namespace RangeControl.Core
{
    /// <summary>
    /// Runtime anchor for the player's head. The rig registers itself here, and prefabs that need to know
    /// where the player is (the scope, score popups, the hip pouch) read it, so nothing searches the scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Range Control/Player Anchors", fileName = "PlayerAnchors")]
    public sealed class PlayerAnchors : ScriptableObject
    {
        /// <summary>The headset camera's transform, or null when no rig is in the scene.</summary>
        public Transform Head { get; private set; }

        public void Register(Transform head)
        {
            Head = head;
        }

        public void Unregister(Transform head)
        {
            if (Head == head)
                Head = null;
        }
    }
}
