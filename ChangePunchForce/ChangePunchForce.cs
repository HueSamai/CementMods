using MelonLoader;
using Il2CppFemur;
using UnityEngine;
using UnityEngine.InputSystem;
using Il2CppCinemachine;

[assembly: MelonInfo(typeof(ChangePunchForce), "ChangePunchForce", "0.0.1", "dotpy")]
public class ChangePunchForce : MelonMod
{
    private float _multiplier = 1.0f;
    private string _entered = "";
    private Rect _windowRect = new Rect(Screen.width / 2 - 200, 0, 400, 0);
    private bool _windowIsEnabled = false;

    public override void OnUpdate()
    {
        if (Keyboard.current.shiftKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame)
        {
            _windowIsEnabled = !_windowIsEnabled;
        }
        
        foreach (Actor actor in Actor.CachedActors)
        {
            if (actor.ControlledBy == Actor.ControlledTypes.Human)
            {
                actor._punchForceModifer = _multiplier;
            }
        }
    }

    public override void OnGUI()
    {
        if (_windowIsEnabled)
            GUILayout.Window(0, _windowRect, (GUI.WindowFunction)HandleWindow, "Change Punch Force Multiplier");
    }

    public void HandleWindow(int windowID)
    {
        GUILayout.Label("Enter punch force multiplier");
        _entered = GUILayout.TextField(_entered);
        if (GUILayout.Button("Set punch force"))
        {
            if (!float.TryParse(_entered, out _multiplier))
            {
                _multiplier = 1.0f;
            }
        }
    }
}