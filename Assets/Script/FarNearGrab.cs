using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


//IFarAttachProvider
public class FarNearGrab : XRGrabInteractable
{
    //원거리(레이) 잡기 조건 판정용. 공에 붙어 있으면 Awake에서 자동 연결, 공이 아니면 null(제한 없음).
    private Baseball _baseball;

    protected override void Awake()
    {
        base.Awake();
        _baseball = GetComponentInParent<Baseball>();
    }

    //직접 캐치(Direct, 손이 닿음)는 항상 허용하고, 레이(원거리) 잡기만 조건부로 막는다.
    //- 공중 타구를 때리자마자 레이로 인터셉트하는 것 방지 (플라이는 몸으로 잡아야 = 기존 플라이아웃 유지)
    //- 바운드 후(IsGroundBall) 구른 공은 레이로 회수 허용 (수비 편의)
    //- 수비수가 던져준 송구(Thrown)는 공중이어도 허용 (베이스 커버 스트레스 방지)
    //Hover까지 막아서 "지금은 원거리로 못 잡는다"가 레이 하이라이트로도 보이게 한다.
    public override bool IsHoverableBy(IXRHoverInteractor interactor)
    {
        if (interactor is XRRayInteractor && !CanRangedGrab())
            return false;
        return base.IsHoverableBy(interactor);
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        if (interactor is XRRayInteractor && !CanRangedGrab())
            return false;
        return base.IsSelectableBy(interactor);
    }

    private bool CanRangedGrab()
    {
        if (_baseball == null) return true;                          //공이 아닌 물건이면 제한 없음
        if (_baseball.IsGroundBall) return true;                     //바운드 후 — 구른 공 회수
        if (_baseball.CurrentState == BallState.Thrown) return true; //수비 송구는 공중이어도 받게
        if (_baseball.IsInGamePlay) return false;                    //공중 타구 — 직접 캐치만
        if (_baseball.CurrentState == BallState.Pitched) return false; //투구가 날아오는 중도 금지
        return true; //대기/데드 상태 등은 기존대로 자유
    }

    protected override void OnHoverEntering(HoverEnterEventArgs args)
    {
        base.OnHoverEntering(args);
        
        // Hover 시작될 때 Force Grab 설정
        if (args.interactorObject is XRRayInteractor rayInteractor)
        {
            rayInteractor.useForceGrab = true;
        }
    }
    
    protected override void OnHoverExiting(HoverExitEventArgs args)
    {
        base.OnHoverExiting(args);
        
        // Hover 끝날 때 Force Grab 해제 (선택 사항)
        if (args.interactorObject is XRRayInteractor rayInteractor)
        {
            rayInteractor.useForceGrab = false;
        }
    }

    public List<XRBaseController> GetController()
    {
        List<XRBaseController> list = new List<XRBaseController>();
        
        foreach (IXRSelectInteractor interactor in interactorsSelecting)
        {
            //애초에 부모가 XRController이고 매개변수는 RayInteractor임
            XRBaseController controller = interactor.transform.parent.GetComponent<XRBaseController>();
            if (controller != null)
            {
                list.Add(controller);

            }
        }

        return list;
    }
}
