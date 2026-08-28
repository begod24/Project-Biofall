using UnityEngine;

namespace Biofall.Data
{
    // One operative the squad can field. Deliberately empty of stats for now: all four are the
    // same soldier, and the class exists so that giving them different loadouts later is an
    // asset edit rather than a code change.
    [CreateAssetMenu(menuName = "Biofall/Operative", fileName = "OP_Operative")]
    public sealed class OperativeData : ScriptableObject
    {
        [Tooltip("Stable id. Saved in PlayerPrefs, so renaming it resets the player's pick.")]
        public string id = "lead";

        [Tooltip("Shown on the selection card.")]
        public string displayName = "LEAD";

        [TextArea(2, 4)]
        [Tooltip("One or two lines under the name.")]
        public string description = "Squad commander. Balanced kit.";

        [Header("Loadout (all identical for now)")]
        [Tooltip("Left empty = the player prefab's authored loadout is used unchanged.")]
        public GameObject bodyPrefabOverride;
    }
}
