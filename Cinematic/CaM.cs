using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using MelonLoader;

[RegisterTypeInIl2Cpp]
public class CinematicMod : MonoBehaviour 
{
    private bool locked;
    private GameObject virtualCamera;

    private GameObject UI;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;

    private GameObject camera;

    private bool camenabler;

    private float speed = 25f;

    private float mouseSensitivity = 10f;

    private bool usingCustomCam;

    private void Start()
    {
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneChanged);
        SceneManager.add_sceneUnloaded((Action<Scene>)OnSceneUnload);
    }

    private bool _hasSetup = false;
    public void Setup()
    {
        camera = Camera.main.gameObject;
        _hasSetup = camera != null;
    }

    private void OnSceneUnload(Scene _)
    {
        if (!_hasSetup) return;
        cameraPosition = camera.transform.position;
        cameraRotation = camera.transform.rotation;
    }

    private void OnSceneChanged(Scene _, LoadSceneMode __)
    {
        _hasAdjustedCamera = false;
    }
    
    private bool _hasAdjustedCamera = false;
    private void AdjustCamera()
    {
        _hasAdjustedCamera = true;

        if (usingCustomCam)
            EnableCustomCamera();
    }

    private void EnableCustomCamera()
    {
        virtualCamera.SetActive(false);
        UI.SetActive(false);
        camera.transform.position = cameraPosition;
        camera.transform.rotation = cameraRotation;
    }

    private void DisableCustomCamera()
    {
        virtualCamera.SetActive(true);
        UI.SetActive(true);
        cameraPosition = camera.transform.position;
        cameraRotation = camera.transform.rotation;
    }

    private void ToggleLock()
    {
        locked = !locked;
    }

    private void ToggleCam()
    {
        usingCustomCam = !usingCustomCam;
        if (usingCustomCam)
        {
            EnableCustomCamera();
        }
        else
        {
            DisableCustomCamera();
        }   
    }

    public void Update()
    {
        if (!_hasSetup && SceneManager.GetActiveScene().name == "Menu")
            Setup();

        if (Keyboard.current.f3Key.wasPressedThisFrame) {
            RemoveKillVolumes.ToggleKillVolumes();
        }

        if (virtualCamera == null)
        {
            virtualCamera = GameObject.Find("VirtualCamera");
        }

        if (UI == null)
        {
            UI = GameObject.Find("UI");
        }

        if (virtualCamera == null || UI == null) return;

        if (!_hasAdjustedCamera) AdjustCamera();

        if (Keyboard.current.f1Key.wasPressedThisFrame) {
            ToggleCam();
        }

        if (usingCustomCam) {
            if (Keyboard.current.f2Key.wasPressedThisFrame) {
                ToggleLock();
            }
        }

        if (!usingCustomCam || locked) return;

        if (camera == null) {
            MelonLogger.Error("CAMERA IS NULL");
            return;
        }

        try {
            if (Mouse.current.rightButton.isPressed)
            {
                float mouseX = Mouse.current.delta.x.ReadValue();
                float mouseY = -Mouse.current.delta.y.ReadValue();

                float newYRot = camera.transform.eulerAngles.y + mouseX * mouseSensitivity * Time.deltaTime;
                float xRot = camera.transform.eulerAngles.x;
                if (xRot > 180)
                {
                    xRot -= 360;
                }

                float newXRot = Mathf.Clamp(xRot + mouseY * mouseSensitivity * Time.deltaTime, -90f, 90f);

                camera.transform.eulerAngles = new Vector3(newXRot, newYRot, 0);
            }
        }
        catch {
            MelonLogger.Error("ERROR WITH MOUSE BUTTON THINGY");
        }

        Vector3 moveDirection = new Vector3(0, 0, 0);

        if (Keyboard.current.upArrowKey.isPressed) {
            moveDirection += camera.transform.forward;
        }

        if (Keyboard.current.downArrowKey.isPressed) {
            moveDirection -= camera.transform.forward;
        }

        if (Keyboard.current.leftArrowKey.isPressed) {
            moveDirection -= camera.transform.right;
        }

        if (Keyboard.current.rightArrowKey.isPressed) {
            moveDirection += camera.transform.right;
        }

        if (Keyboard.current.qKey.isPressed) {
            moveDirection -= Vector3.up;
        }

        if (Keyboard.current.eKey.isPressed) {
            moveDirection += Vector3.up;
        }

        camera.transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (usingCustomCam)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
