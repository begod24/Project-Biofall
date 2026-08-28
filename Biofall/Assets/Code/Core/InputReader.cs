using UnityEngine;
using UnityEngine.InputSystem;

namespace Biofall.Core
{
    // The one keyboard/mouse poll in the project. Everything that reads input goes through the
    // single instance this class owns.
    //
    // Ownership matters here, and getting it wrong is what broke every run: the reader used to
    // be an ordinary scene component sitting on the same GameObject as Bootstrap. Bootstrap
    // destroys duplicate roots, so the moment a run scene loaded on top of the boot root, the
    // scene's Bootstrap took its own GameObject down -- and the reader on it. From then on
    // FindAnyObjectByType found nothing and every consumer read Vector2.zero: no movement, no
    // fire, no interact.
    //
    // So the reader is no longer scene-owned. The composition root creates one on its own
    // (DontDestroyOnLoad) GameObject, a scene copy defers to whoever claimed the slot first, and
    // the accessor below creates one on demand if nothing did. There is always exactly one and
    // it always outlives the scene.
    [DefaultExecutionOrder(-100)]
    public class InputReader : MonoBehaviour
    {
        private static InputReader s_instance;

        // Never null. Consumers must call this every time rather than caching in Awake -- a
        // cached reference to a destroyed reader is the bug this class exists to prevent.
        public static InputReader Instance
        {
            get
            {
                if (s_instance != null) return s_instance;

                s_instance = FindAnyObjectByType<InputReader>(FindObjectsInactive.Exclude);
                if (s_instance != null) return s_instance;

                var go = new GameObject(nameof(InputReader));
                DontDestroyOnLoad(go);
                s_instance = go.AddComponent<InputReader>();
                return s_instance;
            }
        }

        public Vector2 Move { get; private set; }

        public Vector2 PointerScreenPosition { get; private set; }

        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool ReloadPressed { get; private set; }

        public bool GrenadePressed { get; private set; }

        public bool InteractPressed { get; private set; }

        public bool InteractHeld { get; private set; }

        public int WeaponSlot { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_instance = null;

        private void Awake()
        {
            // First one wins. A copy left in a run scene stays inert and dies with its scene,
            // which is exactly what happened to the only reader before.
            if (s_instance == null) s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null || mouse == null)
            {
                Move = Vector2.zero;
                FireHeld = FirePressed = ReloadPressed = GrenadePressed = InteractPressed = InteractHeld = false;
                WeaponSlot = 0;
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Move = new Vector2(x, y);

            PointerScreenPosition = mouse.position.ReadValue();
            FireHeld = mouse.leftButton.isPressed;
            FirePressed = mouse.leftButton.wasPressedThisFrame;
            ReloadPressed = keyboard.rKey.wasPressedThisFrame;
            GrenadePressed = keyboard.gKey.wasPressedThisFrame;
            InteractPressed = keyboard.eKey.wasPressedThisFrame;
            InteractHeld = keyboard.eKey.isPressed;

            WeaponSlot = keyboard.digit1Key.wasPressedThisFrame ? 1
                       : keyboard.digit2Key.wasPressedThisFrame ? 2
                       : keyboard.digit3Key.wasPressedThisFrame ? 3
                       : 0;
        }
    }
}
