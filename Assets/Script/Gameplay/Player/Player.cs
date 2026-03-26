using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    [SerializeField] protected PlayerComponent _playerComponent;
    [SerializeField] private GameObject shirt;
    
    protected Baseball ball;

    protected NavMeshAgent nav;

    // Start is called before the first frame update
    protected void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
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
        nav.SetDestination(pos);
        LookAtPlayer(pos);
    }
    
    public void StopMove()
    {
        if (nav && nav.isActiveAndEnabled && nav.isOnNavMesh)
        {
            nav.ResetPath();
        }
        if (!nav)
        {
            Debug.Log("nav is null");
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
    
    
    
    
    
    
    public float GetBallDistance()
    {
        return this.ball.DefenderDis;
    }

    public Vector3 GetBallTargetPosition()
    {
        return ball.GetTargetPosition();
    }

    public bool IsPassingBall()
    {
        return ball.IsPassing;
    }

    public DefenderComponent GetBallDefender()
    {
        return ball.MyDefenderComponent;
    }
    


}
