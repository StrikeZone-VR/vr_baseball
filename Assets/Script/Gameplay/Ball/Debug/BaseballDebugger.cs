using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class BaseballDebugger : MonoBehaviour
{
    // ===== Trajectory Gizmos (기존) =====
    [Header("Trajectory")]
    [SerializeField] private TrajectoryBaseBallData _trajectoryData;
    [SerializeField] private StrikeZone strikeZone;

    // ===== Debug Swing (A) =====
    [Header("Debug Swing")]
    [SerializeField] private Key swingKey = Key.Z;
    [SerializeField] private Transform swingPivot;
    [SerializeField] private Vector3 swingStartEuler = new Vector3(-40f, 0f, 0f);
    [SerializeField] private Vector3 swingEndEuler   = new Vector3( 60f, 0f, 0f);
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private AnimationCurve swingEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool autoReleaseOnEnd = true;
    [SerializeField] private XRGrabInteractable ball;
    [SerializeField] private XRInteractionManager interactionManager;

    private Quaternion swingRestRot;

    private void Start()
    {
        ResolveCrossSceneRefs();
        if (swingPivot != null)
            swingRestRot = swingPivot.localRotation;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[swingKey].wasPressedThisFrame)
            StartCoroutine(DebugDoSwing());
    }

    private void ResolveCrossSceneRefs()
    {
        if (interactionManager == null)
            interactionManager = FindObjectOfType<XRInteractionManager>();

        if (ball == null)
        {
            var ballGo = GameObject.FindWithTag("Ball");
            if (ballGo != null)
                ball = ballGo.GetComponent<XRGrabInteractable>();
        }

        if (swingPivot == null)
        {
            var pivotGo = GameObject.Find("DebugSwingPivot");
            if (pivotGo != null)
                swingPivot = pivotGo.transform;
        }
    }

    private IEnumerator DebugDoSwing()
    {
        ResolveCrossSceneRefs();

        if (swingPivot == null)
        {
            Debug.LogWarning("[BaseballDebugger] swingPivot 미설정 — DebugSwingPivot GameObject가 씬에 있는지 확인하세요");
            yield break;
        }
        if (ball != null && !ball.isSelected)
        {
            Debug.LogWarning("[BaseballDebugger] 공이 잡혀있지 않습니다 — trigger로 먼저 잡으세요");
            yield break;
        }

        Quaternion start = Quaternion.Euler(swingStartEuler);
        Quaternion end   = Quaternion.Euler(swingEndEuler);

        swingPivot.localRotation = start;
        yield return null;

        float t = 0f;
        while (t < swingDuration)
        {
            t += Time.deltaTime;
            float u = swingEase.Evaluate(Mathf.Clamp01(t / swingDuration));
            swingPivot.localRotation = Quaternion.Slerp(start, end, u);
            yield return null;
        }
        swingPivot.localRotation = end;

        if (autoReleaseOnEnd
            && ball != null
            && ball.isSelected
            && interactionManager != null
            && ball.interactorsSelecting.Count > 0)
        {
            var holder = ball.interactorsSelecting[0];
            interactionManager.SelectExit(holder, (IXRSelectInteractable)ball);
        }

        yield return new WaitForSeconds(0.5f);
        swingPivot.localRotation = swingRestRot;
    }

    // ===== Trajectory drawing (기존) =====
    private void OnDrawGizmos()
    {
        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        if (_trajectoryData == null) return;

        float dashLength = 0.1f;
        float gapLength  = 0.1f;

        Gizmos.color = Color.yellow;

        float stepLen = dashLength + Mathf.Max(0f, gapLength);
        List<Vector3> list = _trajectoryData.GetPathPoints();
        for (int i = 0; i < list.Count - 1; i++)
        {
            DrawDashedSegment(list[i], list[i + 1], dashLength, stepLen);
        }

        if (_trajectoryData.GetHasPassedStrikeZone())
        {
            DebugDrawSp(_trajectoryData.GetStrikeZonePoint());
        }

        if (_trajectoryData.GetHasLand())
        {
            Gizmos.DrawWireSphere(_trajectoryData.GetLandingPoint(), 0.2f);
        }
    }

    private void DrawDashedSegment(Vector3 a, Vector3 b, float dashLen, float stepLen)
    {
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.00001f) return;

        Vector3 dir = ab / len;

        for (float t = 0f; t < len; t += stepLen)
        {
            float t0 = t;
            float t1 = Mathf.Min(t + dashLen, len);
            Gizmos.DrawLine(a + dir * t0, a + dir * t1);
        }
    }

    private void DebugDrawSp(Vector3 point)
    {
        if (strikeZone == null) return;

        float crossSize = 0.05f;
        Color hitColor = Color.yellow;
        float duration = 100f;

        Vector3 right = strikeZone.transform.right;
        Vector3 up    = strikeZone.transform.up;

        Debug.DrawLine(point - right * crossSize, point + right * crossSize, hitColor, duration);
        Debug.DrawLine(point - up    * crossSize, point + up    * crossSize, hitColor, duration);
    }
}
