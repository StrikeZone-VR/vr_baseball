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
    [SerializeField] private Vector3 swingStartEuler;
    [SerializeField] private Vector3 swingEndEuler;
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private AnimationCurve swingEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool autoReleaseOnEnd = true;

    [SerializeField] private XRGrabInteractable ball;
    [SerializeField] private XRInteractionManager interactionManager;

    //MyXROriginManager가 구독해서 스윙 실행
    [SerializeField] private VoidEventSO debugSwingEvent;

    [Header("Hand Lookup")]
    [SerializeField] private string rightHandName;

    // 런타임 참조 (reparent 안 함, TPD와 안 싸움)
    private Transform rightHand;
    private Transform pivotSource;          // 카메라 transform — 매 프레임 world 위치 가져옴
    private bool swingActive;
    private Quaternion currentSwingRot = Quaternion.identity;

    private void Start()
    {
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[swingKey].wasPressedThisFrame)
        {
            Debug.Log("Z 클릭 - 팔 휘두르기");
            if (debugSwingEvent != null) debugSwingEvent.RaiseEvent();
        }
    }

    /// <summary>
    /// TPD가 매 프레임 손 위치를 덮어쓴 직후, 카메라 위치를 pivot으로 한 회전을 손에 적용.
    /// reparent 안 하므로 TPD와 충돌하지 않음.
    /// </summary>
    private void LateUpdate()
    {
    }



    #region TRAJECTORY
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
    #endregion

}
