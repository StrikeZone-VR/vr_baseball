using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


//IFarAttachProvider
public class FarNearGrab : XRGrabInteractable
{
    
    [Header("grip settings")]
    public Transform leftHandGrip;   // 왼손용 그립
    public Transform rightHandGrip;  // 오른손용 그립
    
    
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
}
