using Il2CppCoreNet.Model;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Il2Cpp;
using Il2CppCoreNet.Objects;
using Il2CppCostumes;
using Il2CppFemur;
using Il2CppGB.Core;
using Il2CppGB.Game.Data;
using Il2CppGB.Networking.Objects;
using MelonLoader;
using Resources = UnityEngine.Resources;
using Il2CppGB.Core.Loading;
using Il2CppCoreNet.Utils;
using Il2CppGB.Game;
using Il2CppGB.Networking.Utils;
using UnityEngine.Networking;
using UnityEngine.Rendering;

[assembly: MelonInfo(typeof(BetterSpawnEnemies), "BetterSpawnEnemies", "0.0.1", "dotpy")]
public class BetterSpawnEnemies : MelonMod
{
    int enemyCount = 0;
    WavesData waveInformation;

    bool inGame = false;

    int selectedEnemyType = -1;

    const string DESELECT_TEXT = "Deselect";

    string[] DEFAULT_BUTTON_TEXT =
    {
        "Spawn medium",
        "Spawn large",
        "Spawn small",
    };

    // DEFAULT_BUTTON_TEXT is copied over on initiliase
    string[] currentButtonTexts;

    private List<NetBeast> aiNetPlayers = new();
    public void SpawnEnemy(int type, Vector3 position)
    {
        Debug.Log("SPAWNING ENEMY");
        if (waveInformation == null)
        {
            waveInformation = Global.Instance.SceneLoader.WavesData;

            if (waveInformation == null)
            {
                MelonLogger.Error("Waves info is NULL!");
                return;
            }
        }

        Wave spawnList = waveInformation.levelWaves[0];

        string name = waveInformation.GetRandomCostume();
        int num = waveInformation.GetRandomColour();
        int gang = spawnList.beasts[0].gangID;

        bool newNet = false;
        NetBeast netBeastRef = null;
        CostumeSaveEntry costumeSaveEntry = MonoSingleton<Global>.Instance.Costumes.CostumePresetDatabase.GetCostumeByPresetName(name);
        if (costumeSaveEntry == null)
        {
            costumeSaveEntry = CostumePool.I.GetCostumeEntry(0);
        }
        NetCostume netCostume = new NetCostume(costumeSaveEntry);
        netCostume.Voice = Actor.GetRandomVoice(false, true);
        ColorObject colorOjectWithID = CostumePool.I.PlayerColorDatabase.GetColorOjectWithID((ushort)num);
        Color primaryColor = Color.gray;
        Color costumeColor = Color.gray;
        if (colorOjectWithID != null && colorOjectWithID.Colors.Length != 0)
        {
            primaryColor = colorOjectWithID.Colors[0];
        }
        if (colorOjectWithID != null && colorOjectWithID.Colors.Length > 1)
        {
            costumeColor = colorOjectWithID.Colors[1];
        }

        for (int j = 0; j < this.aiNetPlayers.Count; j++)
        {
            if (!this.aiNetPlayers[j].Alive)
            {
                netBeastRef = this.aiNetPlayers[j];
                netBeastRef.Alive = true;
                netBeastRef.Costume = netCostume;
                netBeastRef.PrimaryColor = primaryColor;
                netBeastRef.CostumeColor = costumeColor;
            }
        }
        if (netBeastRef == null)
        {
            netBeastRef = new NetBeast(GameMode_Waves.AI_CONTROLLER_STARTIDEX + this.aiNetPlayers.Count, netCostume, primaryColor, costumeColor, gang, NetPlayer.PlayerType.AI, false);
            netBeastRef.Alive = true;
            this.aiNetPlayers.Add(netBeastRef);
            newNet = true;
        }

        GameObject gameObject = null;
        gameObject = UnityEngine.Object.Instantiate<GameObject>(waveInformation.GetSpawnObject(type), position, Quaternion.identity);

        if (gameObject != null)
        {
            netBeastRef.Instance = gameObject;
            Actor component = gameObject.GetComponent<Actor>();
            if (component != null)
            {
                NetModel model = GameObject.FindObjectOfType<NetModel>();

                component.ControlledBy = Actor.ControlledTypes.AI;
                component.playerID = -1;
                component.IsAI = true;
                component.controllerID = netBeastRef.ControllerId;
                component.gangID = gang;
                
                if (!newNet)
                {
                    model.UpdateCollectionItem<NetBeast>("NET_PLAYERS", netBeastRef);
                }
                else
                {
                    model.Add<NetBeast>("NET_PLAYERS", netBeastRef);
                }
                component.DressBeast();
                NetworkServer.Spawn(gameObject);
                GBNetUtils.SetBeastsGang(netBeastRef);
            }
        }
    }

    public override void OnLateInitializeMelon()
    {
        currentButtonTexts = (string[])DEFAULT_BUTTON_TEXT.Clone();

        string path = "Waves/Grind";

        SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)SceneLoaded;
    }

    public void SceneLoaded(Scene scene, LoadSceneMode _)
    {
        inGame = scene.name != Global.MENU_SCENE_NAME;
    }

    public Vector3 GetMousePosition(Vector3 offset)
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 10f;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
        return worldPosition + offset;
    }

    public void CheckIfAIDead()
    {
        if (aiNetPlayers.Count == 0) return;

        foreach (Actor actor in Actor.CachedActors)
        {
            if (actor.ControlledBy == Actor.ControlledTypes.Human && actor.actorState != Actor.ActorState.Dead) return;
        }

        NetModel model = GameObject.FindObjectOfType<NetModel>();
        foreach (var ai in aiNetPlayers)
        {
            model.Remove<NetBeast>("NET_PLAYERS", ai);
            ai.Alive = false;
            ai.Instance.GetComponent<Actor>().actorState = Actor.ActorState.Dead;
        }

        aiNetPlayers.Clear();
    }

    private bool recentlyPressedButton;
    private float recentlyPressedTimer;
    public override void OnLateUpdate()
    {
        if (recentlyPressedButton)
        {
            if (recentlyPressedTimer >= 0.1f)
            {
                recentlyPressedTimer = 0;
                recentlyPressedButton = false;
            }
            else recentlyPressedTimer += Time.deltaTime;
        }

        CheckIfAIDead();

        if (!inGame || selectedEnemyType == -1 || recentlyPressedButton) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnEnemy(selectedEnemyType, GetMousePosition(Vector3.up));
        }

        if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            SpawnEnemy(selectedEnemyType, GetMousePosition(Vector3.up));
        }
    }

    // this code here is horrendous but oh well
    public void HandleButtonPress(int typeID)
    {
        recentlyPressedButton = true;

        if (typeID != selectedEnemyType)
        {
            if (selectedEnemyType != -1)
                currentButtonTexts[selectedEnemyType] = DEFAULT_BUTTON_TEXT[selectedEnemyType];

            currentButtonTexts[typeID] = DESELECT_TEXT;
            selectedEnemyType = typeID;
        }
        else
        {
            currentButtonTexts[selectedEnemyType] = DEFAULT_BUTTON_TEXT[selectedEnemyType];
            selectedEnemyType = -1;
        }
    }

    // so that it goes small, medium, large
    int[] LOOP_ORDER = new int[3] { 2, 0, 1 };
    public override void OnGUI()
    {
        foreach (var index in LOOP_ORDER)
            if (GUILayout.Button(currentButtonTexts[index])) HandleButtonPress(index);
    }
}
