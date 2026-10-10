using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    // Invisible helper that watches for input so idle and wandering time can be measured.
    public sealed class PlaytestObserverRunner : MonoBehaviour
    {
        static PlaytestObserverRunner instance;

        public static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("PlaytestObserver") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PlaytestObserverRunner>();
        }

        void Update()
        {
            if (!PlaytestObserver.Active) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool input = (keyboard != null && keyboard.anyKey.isPressed)
                || (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed
                    || mouse.delta.ReadValue().sqrMagnitude > .25f || mouse.scroll.ReadValue().sqrMagnitude > 0f));
            PlaytestObserver.Tick(input, Application.isFocused);
        }

        void OnApplicationQuit() => PlaytestObserver.End();
        void OnDestroy() { if (instance == this) instance = null; }
    }
}
