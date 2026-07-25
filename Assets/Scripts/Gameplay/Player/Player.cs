using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    [SerializeField] protected PlayerComponent _playerComponent;
    [SerializeField] private GameObject shirt;
    
    [SerializeField] protected Baseball ball;

    [Tooltip("이동속도(m/s). NavMeshAgent의 Speed를 이 값으로 덮어쓴다. 인스펙터에서 개별 조정.")]
    [SerializeField] private float moveSpeed = 3.5f;

    protected NavMeshAgent nav;

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        if (nav)
        {
            nav.speed = moveSpeed;
        }
    }
    public void SetActiveRole(PlayerComponent newRole)
    {
        // 기존 역할 끄기
        if (_playerComponent != null)
            _playerComponent.enabled = false;

        // 새 역할 켜기
        _playerComponent = newRole;
        if (_playerComponent != null)
            _playerComponent.enabled = true;
    }

    public void LookAtPlayer(Vector3 target)
    {
        transform.LookAt(target, Vector3.up);

        //x, z => zero because prevent superconductor phenomenon
        transform.rotation = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f);
    }

    /// <summary>
    /// nav의 SetDestination은 여기서만 발동시키게 하자 
    /// </summary>
    public virtual void MovePlayer(Vector3 pos)
    {
        LookAtPlayer(pos);
        nav.SetDestination(pos);
    }
    
    public void StopMove()
    {
        if (!nav)
        {
            Debug.Log("nav is null");
            return;
        }
        if (nav.isActiveAndEnabled && nav.isOnNavMesh)
        {
            nav.ResetPath();
            nav.velocity = Vector3.zero;
        }
        if (!nav.isActiveAndEnabled)
        {
            Debug.Log("isActiveAndEnabled is null");
        }
        if (!nav.isOnNavMesh)
        {
            Debug.Log("isOnNavMesh is null");
        }
    }

    public void SetNavUpdateRotation(bool value)
    {
        if (nav) nav.updateRotation = value;
    }
    
    
    public void SetShirtColor(Color teamColor)
    {
        // 1. 렌더러 가져오기 (보통 캐릭터는 SkinnedMeshRenderer)
        Renderer myRenderer = shirt.GetComponentInChildren<Renderer>();

        // 2. 프로퍼티 블록 생성 (통로 역할)
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        // 3. 현재 렌더러의 상태를 블록에 담기
        myRenderer.GetPropertyBlock(propBlock);

        // 4. 블록에 '팀 색상'만 따로 설정 (셰이더 변수명 확인 필요: _Color 또는 _BaseColor)
        propBlock.SetColor("_Color", teamColor); 

        // 5. 변경된 블록을 다시 렌더러에 덮어씌우기
        myRenderer.SetPropertyBlock(propBlock);
    }
    public PlayerComponent GetPlayerComponent()
    {
        return _playerComponent;
    }

    public void SetBall(Baseball ball)
    {
        this.ball = ball;
    }

    public Baseball GetBaseBall() => ball;
    
    public float GetBallDistance()
    {
        return this.ball.DefenderDis;
    }


    public bool IsPassingBall()
    {
        return ball.CurrentState == BallState.Thrown;
    }

    public DefenderComponent GetBallDefender()
    {
        return ball.MyDefenderComponent;
    }
}
