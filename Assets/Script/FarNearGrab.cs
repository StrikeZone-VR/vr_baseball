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
    
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Ray Interactor로 선택된 경우 => 일단 계속 건드려 보자
        if (args.interactorObject is XRRayInteractor)
        {
            Debug.Log("포지셩" + args.interactorObject.transform.position);
            // attachTransform을 컨트롤러 위치로 리셋
            //attachTransform.position = args.interactorObject.transform.position;
        }
        base.OnSelectEntered(args);

        
        
        
        //
        // // 어느 손으로 잡았는지 확인
        // //string interactorName = args.interactorObject.transform.name.ToLower();
        // string mytag = args.interactorObject.transform.tag;
        //
        //
        // if (mytag.Contains("left") && leftHandGrip != null)
        // {
        //     transform.position = leftHandGrip.position;
        //     Debug.Log("left : " + transform.position );
        // }
        // else if (mytag.Contains("right") && rightHandGrip != null)
        // {
        //     transform.position = rightHandGrip.position;
        //     Debug.Log("right : " + transform.position );
        // }
        
        //Debug.Log($"{interactorName} hold by controller. AttachTransform: {this.attachTransform.name}");
    }
}
