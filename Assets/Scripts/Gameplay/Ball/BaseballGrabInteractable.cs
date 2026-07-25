using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// XRGrabInteractable(Instantaneous)은 잡는 순간 SetupRigidbodyGrab에서 isKinematic=true로 바꾸는데,
/// 인플레이 공(Pitched/Thrown)은 ContinuousDynamic이라 그대로면 Unity 에러가 난다.
/// ("Kinematic body only supports Speculative Continuous collision detection")
/// kinematic 전환 '직전'인 이 함수에서 Discrete로 되돌린다.
/// 놓으면 OnRelease → ThrowPlayerBall → Pitched 상태 전이가 SetRigidbodyMode(true)로 CCD를 복원한다.
/// ㄴ XRI 2.6.4엔 selectEntering 같은 전환 전 공개 이벤트가 없어서 서브클래스로 후킹함.
/// </summary>
public class BaseballGrabInteractable : XRGrabInteractable
{
    protected override void SetupRigidbodyGrab(Rigidbody rigidbody)
    {
        if (rigidbody.collisionDetectionMode != CollisionDetectionMode.Discrete)
        {
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        base.SetupRigidbodyGrab(rigidbody);
    }
}
