using System;
using Il2CppFemur;
using UnityEngine;
using MelonLoader;

[RegisterTypeInIl2Cpp]
public class InfiniteGrip : MonoBehaviour
{
    public InfiniteGrip (IntPtr ptr) : base(ptr) {}

    private void Update()
    {
        foreach (Actor actor in FindObjectsOfType<Actor>())
        {
            if (!actor.controlHandeler.leftGrab && !actor.controlHandeler.rightGrab)
            {
                continue;
            }

            if (actor.bodyHandeler.leftGrabInteractable !=  null)
            {   
                actor.bodyHandeler.leftGrabInteractable.grabModifier = Il2Cpp.InteractableObject.Grab.Perminant;
            }

            if (actor.bodyHandeler.rightGrabInteractable !=  null)
            {   
                actor.bodyHandeler.rightGrabInteractable.grabModifier = Il2Cpp.InteractableObject.Grab.Perminant;
            }
        }
    }
}
