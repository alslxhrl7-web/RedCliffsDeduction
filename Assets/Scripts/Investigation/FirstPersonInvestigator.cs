using UnityEngine;
using UnityEngine.InputSystem;

namespace RedCliffMystery.Investigation
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonInvestigator : MonoBehaviour
    {
        public Camera viewCamera;
        public InvestigationSession session;
        public float moveSpeed = 3.1f;
        public float mouseSensitivity = 0.09f;
        public float interactionDistance = 3f;
        CharacterController controller;
        float pitch;
        float verticalSpeed;

        void Awake() => controller = GetComponent<CharacterController>();

        void Update()
        {
            if (session == null || viewCamera == null || !session.CanProcessInvestigationInput || !Application.isFocused)
                return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { session.ShowPause(); return; }
            if (keyboard.jKey.wasPressedThisFrame) { session.ShowJournal(); return; }
            // Escape in the Editor may release the cursor before Update sees the key.
            if (Cursor.lockState != CursorLockMode.Locked) { session.ShowPause(); return; }

            if (mouse != null)
            {
                Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0, look.x, 0);
                pitch = Mathf.Clamp(pitch - look.y, -75, 75);
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            Vector2 input = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            Vector3 movement = transform.TransformDirection(new Vector3(input.x, 0, input.y).normalized);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += -18 * Time.deltaTime;
            controller.Move((movement * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
            InvestigationTarget target = FindTarget();
            session.SetPrompt(target == null ? "" : target.Prompt);
            if (target != null && keyboard.eKey.wasPressedThisFrame) target.Interact(session);
        }

        public InvestigationTarget FindTarget()
        {
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                out RaycastHit hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<InvestigationTarget>();
            return null;
        }

        public void TeleportTo(Transform arrival)
        {
            if (arrival == null) return;
            if (controller == null) controller = GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(arrival.position, arrival.rotation);
            pitch = 0;
            verticalSpeed = 0;
            viewCamera.transform.localRotation = Quaternion.identity;
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && session != null && !session.BlocksMovement) session.ShowPause();
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
