using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FarNearGrab : XRGrabInteractable
{
    
    [Header("grip settings")]
    public Transform leftHandGrip;   // 왼손용 그립
    public Transform rightHandGrip;  // 오른손용 그립
    
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        
        // 어느 손으로 잡았는지 확인
        string interactorName = args.interactorObject.transform.name.ToLower();
        
        if (interactorName.Contains("left") && leftHandGrip != null)
        {
            transform.position = leftHandGrip.position;
        }
        else if (interactorName.Contains("right") && rightHandGrip != null)
        {
            transform.position = rightHandGrip.position;
        }
        
        //Debug.Log($"{interactorName} hold by controller. AttachTransform: {this.attachTransform.name}");
    }
}
