using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


//IFarAttachProvider
public class FarNearGrab : XRGrabInteractable
{
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
