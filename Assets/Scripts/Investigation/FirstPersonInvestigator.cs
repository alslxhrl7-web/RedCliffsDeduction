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
        DistanceFootsteps footsteps;
        readonly RaycastHit[] nearbyHits = new RaycastHit[32];

        void Awake() { controller = GetComponent<CharacterController>(); footsteps=GetComponent<DistanceFootsteps>(); }

        void Update()
        {
            if (session == null || viewCamera == null || !session.CanProcessInvestigationInput || !Application.isFocused)
            { held = null; lostAt = -1; if(footsteps!=null && !footsteps.ExternalMotion) footsteps.ResetMotion(); return; }
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
            if(footsteps!=null) footsteps.RecordMovement(controller.isGrounded,session.route!=null && session.route.OnOutdoorGround);
            InvestigationTarget target = FindTarget(held);
            if (target != null) { held = target; lostAt = -1; }
            else if (held != null)
            {
                // A one-frame miss must not blink the prompt off.
                if (lostAt < 0) lostAt = Time.unscaledTime;
                if (Time.unscaledTime - lostAt < PromptGrace) target = held; else { held = null; lostAt = -1; }
            }
            session.SetPrompt(target == null ? "" : target.Prompt);
            if (target != null && keyboard.eKey.wasPressedThisFrame) target.Interact(session);
        }

        const float PromptGrace = .12f;
        // Extra reach and aim margin that only apply to a target that is already selected.
        const float HoldRangeMargin = .35f, HoldAimRadius = .15f;
        InvestigationTarget held;
        float lostAt = -1;

        public InvestigationTarget FindTarget() => Find(out _);

        // Keeps `current` while it is still nearly under the crosshair, so the prompt does not flicker at the
        // range limit, at collider edges, or between neighbouring targets. A target hit dead-centre wins at once.
        public InvestigationTarget FindTarget(InvestigationTarget current)
        {
            var found = Find(out bool exact);
            if (current == null || found == current || (found != null && exact)) return found;
            return StillAimingAt(current) ? current : found;
        }

        bool StillAimingAt(InvestigationTarget current)
        {
            if (viewCamera == null || current == null || !current.isActiveAndEnabled) return false;
            Vector3 origin = viewCamera.transform.position;
            Vector3 direction = viewCamera.transform.forward;
            float reach = interactionDistance + HoldRangeMargin;
            int count = Physics.SphereCastNonAlloc(origin, HoldAimRadius, direction, nearbyHits, reach, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = nearbyHits[i];
                if (hit.distance <= 0 || hit.collider.GetComponentInParent<InvestigationTarget>() != current) continue;
                Vector3 toPoint = hit.point - origin;
                if (Physics.Raycast(origin, toPoint.normalized, out RaycastHit visible, toPoint.magnitude + .01f, ~0, QueryTriggerInteraction.Ignore)
                    && visible.collider.GetComponentInParent<InvestigationTarget>() == current) return true;
            }
            return false;
        }

        InvestigationTarget Find(out bool exactHit)
        {
            exactHit = false;
            if (viewCamera == null) return null;
            Vector3 origin = viewCamera.transform.position;
            Vector3 direction = viewCamera.transform.forward;
            if (Physics.Raycast(origin, direction, out RaycastHit direct, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                var exact = direct.collider.GetComponentInParent<InvestigationTarget>();
                if (exact != null) { exactHit = true; return exact; }
            }

            // Thin tabletop clues allow a small aim margin, but must still be visible.
            int count = Physics.SphereCastNonAlloc(origin, .10f, direction, nearbyHits,
                interactionDistance, ~0, QueryTriggerInteraction.Ignore);
            InvestigationTarget nearest = null;
            float nearestDistance = interactionDistance;
            for (int i = 0; i < count; i++)
            {
                var hit = nearbyHits[i];
                var candidate = hit.collider.GetComponentInParent<InvestigationTarget>();
                if (candidate == null || hit.distance <= 0) continue;
                Vector3 toPoint = hit.point - origin;
                float distance = toPoint.magnitude;
                if (distance > nearestDistance) continue;
                if (!Physics.Raycast(origin, toPoint.normalized, out RaycastHit visible,
                    distance + .01f, ~0, QueryTriggerInteraction.Ignore)
                    || visible.collider.GetComponentInParent<InvestigationTarget>() != candidate) continue;
                nearest = candidate;
                nearestDistance = distance;
            }
            return nearest;
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
            if(footsteps!=null) footsteps.ResetMotion();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (session != null && !session.BlocksMovement) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
