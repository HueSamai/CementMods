using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using MelonLoader;

[RegisterTypeInIl2Cpp]
public class RemoveKillVolumes : MonoBehaviour
{
    static bool disabled;
    static GameObject[] killVolumes = new GameObject[0];

    public void Awake()
    {
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneChanged);
    }

    private void Update() {
    }

    public void OnSceneChanged(Scene scene, LoadSceneMode _)
    {
        killVolumes = new GameObject[0];
        disabled = false;
    }

    public static void ToggleKillVolumes()
    {
        if (killVolumes.Length == 0)
        {
            killVolumes = GameObject.FindGameObjectsWithTag("Helper (Kill Volume)");
        }

        if (disabled)
        {
            foreach (GameObject killVolume in killVolumes)
            {
                killVolume.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject killVolume in killVolumes)
            {
                killVolume.SetActive(false);
            }
        }

        disabled = !disabled;
    }
}
