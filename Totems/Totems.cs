using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Collections.Generic;
using Audio;
using System.Collections;
using CementTools;
using System.IO;
using CementTools.Modules.SceneModule;
using System;
using Random = UnityEngine.Random;
using BetterMapSelection;
using GB.UI.Menu;

public class Totems : CementMod 
{
    // Stores custom scene so that the name can be used in update
    CustomScene customScene;
    LightingSettings lightingSettings;
    OpaqueSurfaceFogRendererFeature.OpaqueSurfaceFogSettings originalSettings;
    OpaqueSurfaceFogRendererFeature.OpaqueSurfaceFogSettings moaiSettings;
    PropertyInfo rendererFeatures;
    ScriptableRenderer scriptableRenderer;

    public static GameObject fallSoundPrefab;
    public static GameObject thiccMoyaiDust;
    public static GameObject slimMoyaiDust;

    AudioClip totemsMusic;

    public void Start()
    {
        StartCoroutine(WaitToStart());
    }

    void ReimportMap()
    {
        AssetBundle moai = AssetBundle.LoadFromFile(Path.Combine(modDirectoryPath, "totems"));
        GameObject scene = moai.LoadAsset<GameObject>("Totems");
        totemsMusic = moai.LoadAsset<AudioClip>("MoaiMusic");
        fallSoundPrefab = moai.LoadAsset<GameObject>("MoyaiFallSound");
        thiccMoyaiDust = moai.LoadAsset<GameObject>("ThiccMoyaiDustParticles");
        slimMoyaiDust = moai.LoadAsset<GameObject>("SlimMoyaiDustParticles");
        moai.Unload(false);

        // Makes a new custom scene with the name CustomScene
        customScene.RemoveAllObjects();
        customScene.AddObject(scene);
    }

    IEnumerator WaitToStart()
    {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Menu");
        try{
    
        Cement.Log("MOAI MOD HAS AWOKEN");
        // Load asset of type GameObject called "Level" from the bundle "customlevel.unity3d",
        // which is in the Assets folder
        AssetBundle moai = AssetBundle.LoadFromFile(Path.Combine(modDirectoryPath, "totems"));
        GameObject scene = moai.LoadAsset<GameObject>("Totems");
        totemsMusic = moai.LoadAsset<AudioClip>("totems");
        fallSoundPrefab = moai.LoadAsset<GameObject>("MoyaiFallSound");
        thiccMoyaiDust = moai.LoadAsset<GameObject>("ThiccMoyaiDustParticles");
        slimMoyaiDust = moai.LoadAsset<GameObject>("SlimMoyaiDustParticles");

        // Makes a new custom scene with the name CustomScene
        customScene = new CustomScene("Totems")
            .AddObject(scene)
            .InvokeOnLoad(OnLoad);

        BetterMapSelectionMod.AddMap("Totems", moai.LoadAsset<Sprite>("Totems"));

        moai.Unload(false);

        rendererFeatures = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
        Cement.Log(rendererFeatures);

        scriptableRenderer = Camera.main.gameObject.GetComponent<UniversalAdditionalCameraData>().scriptableRenderer;

        // settings
        originalSettings = new OpaqueSurfaceFogRendererFeature.OpaqueSurfaceFogSettings();
        originalSettings.skyColor = new Color(0, 0.1448f, 0.695f, 1);
        originalSettings.fogColor = new Color(3.4416f, 2.0722f, 2.2343f, 1);
        originalSettings.fogLightScatterIntensity = 0.1f;
        originalSettings.skyFogMaxDepth = 1213.4f;
        originalSettings.skyFogElevation = 168.5f;
        originalSettings.groundFogMaxDepth = 323.4f;
        originalSettings.groundFogElevation = -14.1f;

        moaiSettings = new OpaqueSurfaceFogRendererFeature.OpaqueSurfaceFogSettings();
        moaiSettings.skyColor = new Color(0, 0.1448f, 0.695f, 1);
        moaiSettings.fogColor = new Color(3.4416f, 2.0722f, 2.2343f, 1);
        moaiSettings.fogLightScatterIntensity = 0.1f;
        moaiSettings.skyFogMaxDepth = 2000f;
        moaiSettings.skyFogElevation = 168.5f;
        moaiSettings.groundFogMaxDepth = 323.4f;
        moaiSettings.groundFogElevation = -14.1f;
        }
        catch(Exception e)
        {
            Cement.Log(e);
        }
    }

    public void OnLoad()
    {
        List<ScriptableRendererFeature> fogRendererFeatures =
            ((List<ScriptableRendererFeature>)rendererFeatures.GetValue(scriptableRenderer));

        ((OpaqueSurfaceFogRendererFeature)fogRendererFeatures[0]).UpdateSettings(moaiSettings);

        for (int i = 0; i < 5; i++) // five is the constant amount of music tracks
        {
            AudioController.Instance.StopMusic(i, 0f);
        }
        for (int i = 0; i < 2; i++) // two is the constant amount of ambience tracks
        {
            AudioController.Instance.StopAmbience(i, 0f);
        }
        AudioResourceLoader.CurrentSceneAudioConfig.musicData.maxVolume = 1f;
        // plays the moai music
        AudioSource totemsAudioSource = AudioController.Instance.PlayMusic(
            totemsMusic,
            6f,
            0,
            false,
            1f
        );
        SceneManager.sceneLoaded += ResetPipelineAsset;

        try
        {
            Cement.Log("Finding moai.");
            Transform moai = GameObject.Find("Totems").transform;
            if (moai == null)
            {
                Cement.Log("Moai is null!");
            }
            Cement.Log("Thicc moai.");
            moai.Find("LeftTiki").gameObject.AddComponent<FallBehaviour>();
            Cement.Log("Slim moai");
            moai.Find("RightTiki").gameObject.AddComponent<FallBehaviour>();
            moai.Find("BambooBridges/bamboo_bridge_1").gameObject.AddComponent<BambooManager>();
            moai.gameObject.AddComponent<FallManager>().totemsAudioSource = totemsAudioSource;
        }
        catch(Exception e)
        {
            Cement.Log(e);
        }

        PauseManager.OnPauseToggled += OnPauseToggled;
    }

    private void OnDestroy()
    {
        PauseManager.OnPauseToggled -= OnPauseToggled;
    }

    private void OnPauseToggled()
    {
        if (PauseManager.Instance.IsPaused)
        {
            AudioController.Instance.PauseMusic(0, 0f);
        }
        else
        {
            AudioController.Instance.ResumeMusic(0, 0f);
        }
    }

    public void ResetPipelineAsset(Scene scene, LoadSceneMode _)
    {   
        if (scene.name == "Menu")
        {
            List<ScriptableRendererFeature> fogRendererFeatures =
                ((List<ScriptableRendererFeature>)rendererFeatures.GetValue(scriptableRenderer));

            ((OpaqueSurfaceFogRendererFeature)fogRendererFeatures[0]).UpdateSettings(originalSettings);
        }
        SceneManager.sceneLoaded -= ResetPipelineAsset;
    }

    public void Update()
    {
        if (SceneManager.GetActiveScene().name != "Menu") return;

        if (Keyboard.current.rightAltKey.wasPressedThisFrame)
        {
            ReimportMap();
        }
    }
}

public class BambooManager : MonoBehaviour
{
    void OnJointBreak()
    {
        transform.parent.GetComponent<AudioSource>().Play();
    }
}

public class CentreOfMass : MonoBehaviour
{
    Vector3 offset = new Vector3(0f, 0f, -2f);

    private void Awake()
    {
        GetComponent<Rigidbody>().centerOfMass = offset;
    }
}

public class FallManager : MonoBehaviour
{
    float firstFallTime = 60f;
    float secondFallTime = 90f;

    FallBehaviour thiccMoyai;
    FallBehaviour slimMoyai;

    bool thiccMoyaiFirst;

    public AudioSource totemsAudioSource;

    FallBehaviour[] moyais = new FallBehaviour[2];

    private void Start()
    {
        thiccMoyai = GameObject.Find("LeftTiki").GetComponent<FallBehaviour>();
        slimMoyai = GameObject.Find("RightTiki").GetComponent<FallBehaviour>();

        thiccMoyai.dustPrefab = Totems.thiccMoyaiDust;
        slimMoyai.dustPrefab = Totems.slimMoyaiDust;

        thiccMoyaiFirst = Random.Range(0, 2) == 0;

        if (thiccMoyaiFirst)
        {
            moyais[0] = thiccMoyai;
            moyais[1] = slimMoyai;
        }
        else
        {
            moyais[1] = thiccMoyai;
            moyais[0] = slimMoyai;
        }
    }

    private void Update()
    {
        if (totemsAudioSource.time >= secondFallTime)
        {
            moyais[1].Fall();
        }
        else if (totemsAudioSource.time >= firstFallTime && !moyais[0].hasFallen)
        {
            moyais[0].Fall();
        }
    }
}


public class FallBehaviour : MonoBehaviour
{
    Vector3[] randomDirections;
    GameObject fallSoundPrefab;
    public bool hasFallen = false;
    bool particlesHaveBeenPlayed = false;
    public GameObject dustPrefab;

    private float force = 8000f;
    int numberOfCollisions = 0;

    private void Awake()
    {
        randomDirections = new Vector3[] {
            transform.forward,
            transform.forward * -1f,
            transform.right,
            transform.right * -1f,
        };

        fallSoundPrefab = Totems.fallSoundPrefab;
    }

    public void Fall()
    {
        if (hasFallen) return;

        hasFallen = true;
        GetComponent<Rigidbody>().mass = 5000f;
        GetComponent<Rigidbody>().isKinematic = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name == "Island" ||
            collision.transform.name == "LeftTiki" ||
            collision.transform.name == "RightTiki")
        {
            if (!particlesHaveBeenPlayed)
            {
                particlesHaveBeenPlayed = collision.transform.name == "Island";
                GetComponent<Rigidbody>().AddForce(randomDirections[Random.Range(0, randomDirections.Length)] * force, ForceMode.Impulse);
                //Instantiate(
                //    dustPrefab, 
                //    dustPrefab.transform.position + transform.position, 
                //    Quaternion.Euler(dustPrefab.transform.eulerAngles + transform.eulerAngles)
                //);
            }

            GameObject sound = Instantiate(fallSoundPrefab, collision.contacts[0].point, Quaternion.identity);
            AudioSource audioSource = sound.GetComponent<AudioSource>();

            numberOfCollisions++;
            if (numberOfCollisions > 1)
            {
                audioSource.volume = GetComponent<Rigidbody>().velocity.magnitude / numberOfCollisions;
            }
            audioSource.pitch = Random.Range(0.8f, 1.1f);
            audioSource.Play();

            Destroy(sound, 2f);
        }
    }
}
