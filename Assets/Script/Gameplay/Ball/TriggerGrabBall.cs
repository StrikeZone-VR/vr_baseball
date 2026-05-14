using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class TriggerGrabBall : MonoBehaviour
{
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private XRBaseInteractor[] handInteractors;
    [SerializeField] private XRInteractionManager manager;

    private XRGrabInteractable ball;

    private void Awake() => ball = GetComponent<XRGrabInteractable>();

    private void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<XRInteractionManager>();

        if (handInteractors == null || handInteractors.Length == 0)
            handInteractors = FindObjectsOfType<XRDirectInteractor>();
    }

    private void OnEnable()
    {
        triggerAction.action.performed += OnTriggerPressed;
        triggerAction.action.canceled  += OnTriggerReleased;
        triggerAction.action.Enable();
    }

    private void OnDisable()
    {
        triggerAction.action.performed -= OnTriggerPressed;
        triggerAction.action.canceled  -= OnTriggerReleased;
    }

    private void OnTriggerPressed(InputAction.CallbackContext _)
    {
        Debug.Log("[TriggerGrabBall] trigger pressed");

        if (!EnsureRefs())
        {
            Debug.LogWarning("[TriggerGrabBall] refs missing: manager=" + (manager != null) + ", hands=" + (handInteractors?.Length ?? 0));
            return;
        }
        if (ball.isSelected) { Debug.Log("[TriggerGrabBall] already selected"); return; }

        bool anyHover = false;
        foreach (var hand in handInteractors)
        {
            if (hand == null) continue;
            bool hovering = hand.interactablesHovered.Contains(ball);
            Debug.Log($"[TriggerGrabBall] hand={hand.name} hovering={hovering}");
            if (!hovering) continue;
            anyHover = true;

            manager.SelectEnter((IXRSelectInteractor)hand, (IXRSelectInteractable)ball);
            return;
        }

        if (!anyHover) Debug.Log("[TriggerGrabBall] no hand is hovering the ball");
    }

    private void OnTriggerReleased(InputAction.CallbackContext _)
    {
        if (!ball.isSelected) return;

        var holder = ball.interactorsSelecting.Count > 0
            ? ball.interactorsSelecting[0]
            : null;
        if (holder == null) return;

        manager.SelectExit(holder, (IXRSelectInteractable)ball);
    }

    private bool EnsureRefs()
    {
        if (manager == null)
            manager = FindObjectOfType<XRInteractionManager>();

        if (handInteractors == null || handInteractors.Length == 0)
            handInteractors = FindObjectsOfType<XRDirectInteractor>();

        return manager != null && handInteractors != null && handInteractors.Length > 0;
    }
}
