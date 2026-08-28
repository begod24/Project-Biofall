using UnityEngine;
using Biofall.Core;

namespace Biofall.Gameplay
{
    // A body's view of the shared reader. It holds no reference of its own: the reader outlives
    // scenes, but a field caching it does not survive a run that reloads, so every read goes
    // through InputReader.Instance, which is never null.
    public sealed class PlayerInput : MonoBehaviour
    {
        private static InputReader Reader => InputReader.Instance;

        public Vector2 Move => Reader.Move;

        public Vector2 PointerScreenPosition => Reader.PointerScreenPosition;

        public bool FireHeld => Reader.FireHeld;
        public bool FirePressed => Reader.FirePressed;
        public bool ReloadPressed => Reader.ReloadPressed;

        public bool GrenadePressed => Reader.GrenadePressed;

        public bool InteractPressed => Reader.InteractPressed;

        public bool InteractHeld => Reader.InteractHeld;

        public int WeaponSlot => Reader.WeaponSlot;
    }
}
