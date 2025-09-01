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
        base.OnSelectEntered(args);
        //
        // // 어느 손으로 잡았는지 확인
        // //string interactorName = args.interactorObject.transform.name.ToLower();
        // string mytag = args.interactorObject.transform.tag;
        //
        // Debug.Log("으아으아 : " + mytag );
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
