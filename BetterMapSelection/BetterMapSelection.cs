using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppGB.Gamemodes;
using System.Reflection;
using Il2CppGB.UI;
using CementGB.Mod.Utilities;
using Il2CppGB.Platform.Lobby;
using Il2CppGB.UI.Beasts;
using Il2CppDG.Tweening;
using UnityEngine.Events;
using Il2CppGB.Config;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using BetterMapSelection;
using System.Collections.Generic;
using Il2CppSystem;
using System.Runtime.CompilerServices;
using Il2Cpp;
using Il2CppGB.Core;
using Il2CppGB.Game;
using static MelonLoader.MelonLogger;
using Il2CppGB.Networking.Delegates;
using Il2CppCoreNet.Contexts;
using UnityEngine.Rendering;
using Il2CppTMPro;
using JetBrains.Annotations;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Il2CppFemur;
using System.Reflection.Metadata.Ecma335;
using Il2CppCostumes;
using Il2CppGB.Platform.Lobby.Utils;
using CementGB.Mod.Modules.BeastInput;
using Il2CppSystem.Runtime.Remoting.Lifetime;

[assembly: MelonInfo(typeof(BetterMapSelectionMod), "BetterMapSelection", "0.0.1", "dotpy")]
namespace BetterMapSelection
{

    public class GameData
    {
        public GameModeEnum gameMode;
        public List<string> mapNames = new();
        public int mapCount;
        public int winCount;

        public bool votedForWinCount;
        public bool votedForMapCount;
        public bool votedForGameMode;
        public bool votedForTeams;

        public void Reset()
        {
            mapNames.Clear();
            mapCount = 0;
            winCount = 0;
            votedForGameMode = false;
            votedForMapCount = false;
            votedForWinCount = false;
            votedForTeams = false;
        }
    }

    public class BetterMapSelectionMod : MelonMod
    {

        private const float ANIMATION_SPEED = 3.5f;
        private static BetterMapSelectionMod _singleton;
        public static BetterMapSelectionMod Singleton
        {
            get
            {
                return _singleton;
            }
        }

        private GameObject _mapBitPrefab;

        private GameObject _mapGridPrefab;
        private GameObject _mapGrid;
        private bool _addedBaseMaps = false;
        private Dictionary<string, MapUIBit> _mapBits = new();
        private GameObject _mapSelectionUIPrefab;
        private GameObject _activeMapUI;
        public VotingSystem CurrentVotingSystem
        {
            get;
            private set;
        }
        private GameData _currentGameData = new();
        private Transform _activeSelectedValues;
        private bool _menuLoadedBefore = false;
        private LocalBeastSetupTracker _tracker;
        private MenuHandlerGamemodes _menuHandler;
        private GameObject _localBeastMenu;

        private bool _menuHandlerActive = false;
        private bool _busySettingMenuHandler = false;

        private bool _startedVoting = false;
        private GameModeSetupConfiguration _gameModeSetupTracker;

        public override void OnLateInitializeMelon()
        {
            _singleton = this;
            SceneManager.sceneLoaded += (UnityAction<Scene,LoadSceneMode>)OnSceneChanged;
        }

        private void SetupValuesFromAssetBundles()
        {
            AssetBundle assetBundle = EmbeddedUtilities.LoadEmbeddedAssetBundle(Assembly.GetExecutingAssembly(), "BetterMapSelection.bettermapselection"); ;
            _mapSelectionUIPrefab = assetBundle.LoadPersistentAsset<GameObject>("BetterMapSelectionUI");
            _mapBitPrefab = assetBundle.LoadPersistentAsset<GameObject>("MapBit");
            GameObject.DontDestroyOnLoad(_mapSelectionUIPrefab);
            GameObject.DontDestroyOnLoad(_mapBitPrefab);

            var request = assetBundle.LoadAllAssetsAsync<Sprite>();
            request.add_completed((Action<AsyncOperation>)delegate (AsyncOperation _)
            {
                List<Sprite> sprites = new List<Sprite>();
                foreach (var asset in request.allAssets)
                {
                    asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    sprites.Add(asset.Cast<Sprite>());
                }
                MapImages.images = sprites.ToArray();

                BMSResources.defaultImage = MapImages.BaseMapNameToSprite("Default");
            });


            BMSResources.actorGraphic = assetBundle.LoadPersistentAsset<GameObject>("ActorGraphic");
            GameObject.DontDestroyOnLoad(BMSResources.actorGraphic);

            _mapGrid = _mapSelectionUIPrefab.transform.Find("MapGrid").gameObject;
            assetBundle.Unload(false);
        }

        private GameObject _AddMap(string mapName, Sprite mapImage)
        {
            if (mapImage == null)
            {
                mapImage = BMSResources.defaultImage;
            }
            MapUIBit mapBit = GameObject.Instantiate(_mapBitPrefab).AddComponent<MapUIBit>();
            mapBit.gameObject.name = mapName;
            mapBit.transform.parent = _mapGrid.transform;
            mapBit.UpdateMap(mapName, mapImage);

            _mapBits[mapName] = mapBit;
            return mapBit.gameObject;
        }

        public static void AddMap(string mapName, Sprite mapImage)
        {
            _singleton._AddMap(mapName, mapImage);
        }

        private void SetMenuHandler(bool value)
        {
            if (_menuHandler == null)
            {
                if (_busySettingMenuHandler)
                {
                    _menuHandlerActive = value;
                }
                else
                {
                    _busySettingMenuHandler = true;
                }
            }
            else
            {
                _menuHandler.enabled = value;
            }
        }

        private void AddBaseMaps()
        {
            _addedBaseMaps = true;

            GameModeSetupConfiguration gmsc = _gameModeSetupTracker;
            foreach (ModeMapStatus map in gmsc.Maps.AvailableMaps)
            {
                _AddMap(map.MapName, MapImages.BaseMapNameToSprite(map.MapName));
            }

            _AddMap("Random", MapImages.BaseMapNameToSprite("Random"));

            SpawnCanvas(GetCanvas().transform);
        }

        private GameObject GetCanvas()
        {
            return GameObject.Find("Beast Menu").transform.Find("Canvas").gameObject;
        }

        private void SpawnCanvas(Transform parent)
        {
            _activeMapUI = GameObject.Instantiate(_mapSelectionUIPrefab, parent);
            _activeMapUI.transform.eulerAngles = new Vector3(0, -90, 0);
            _activeMapUI.transform.localPosition = new Vector3(0, 220, 0);
            _activeMapUI.transform.localScale = Vector3.one * 2.4f;

            _activeSelectedValues = _activeMapUI.transform.Find("SelectedStuff");
        }

        string[] _childrenToDisable = new string[]
        {
            "Wins", "Maps", "StartGame", "Ganemodes"
        };
        private bool _inGameUIDisabled = false;
        private bool _uiShouldBeDisabled = false;
        private void DisableUI()
        {
            _uiShouldBeDisabled = true;
            
            if (_localBeastMenu == null && !TrySettingLocalBeastMenu()) return;

            
            Transform gameModeSelection = _localBeastMenu.transform.Find("UI/GameModeSelection");
            foreach (string childName in _childrenToDisable)
            {
                gameModeSelection.Find(childName).gameObject.SetActive(false);
            }

            _inGameUIDisabled = true;
        }

        private void EnableUI()
        {
            if (_localBeastMenu == null && !TrySettingLocalBeastMenu()) return;
        
            Transform parent = _localBeastMenu.transform.Find("UI/GameModeSelection");
            foreach (string child in _childrenToDisable)
            {
                parent.Find(child).gameObject.SetActive(true);
            }

            SetMenuHandler(true);
        }

        /* TODO
        private void OnDisable()
        {
            EnableUI();
        }

        private void OnEnable()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
                OnMenuLoad();
        }*/

        private void OnMenuLoad()
        {
            if (!_menuLoadedBefore)
            {
                SetupValuesFromAssetBundles();
                _tracker = GameObject.FindObjectOfType<LocalBeastSetupTracker>();
                _menuLoadedBefore = true;
            }

            _startedVoting = false;
            _currentGameData.Reset();

            GameObject canvas = GetCanvas();
            DisableUI();

            if (_addedBaseMaps) 
            {
                SpawnCanvas(canvas.transform);
            }

            SetMenuHandler(false);
        }

        private void OnSceneChanged(Scene scene, LoadSceneMode _)
        {
            if (scene.name == "Menu")
                OnMenuLoad();
        }

        private void ShowAllowedMapsForGameMode(GameModeEnum gameMode)
        {
            Transform mapGrid = _activeMapUI.transform.Find("MapGrid");
            var maps = _gameModeSetupTracker.Maps.GetMapsFor(gameMode, true);
            for (int i = 0; i < mapGrid.GetChildCount(); ++i)
                mapGrid.GetChild(i).gameObject.SetActive(false);

            foreach (var allowedMap in maps)
            {
                //LoggingUtilities.VerboseLog("ALLOWED MAP: " + allowedMap.MapName);

                var bit = mapGrid.Find(allowedMap.MapName);
                if (bit == null)
                    continue;

                bit.gameObject.SetActive(true);
            }
        }

        private void CreateSelectedBit(int result)
        {
            UIBit mapBit = GameObject.Instantiate(_mapBitPrefab).AddComponent<UIBit>();
            mapBit.transform.SetParent(_activeSelectedValues.transform, false);
            GameObject.Destroy(mapBit.GetComponent<Button>());
            
            string mapName = CurrentVotingSystem.mapBits[result].GetName();
            Sprite mapImage = CurrentVotingSystem.mapBits[result].GetSprite();


            mapBit.UpdateMap(mapName, mapImage);
            mapBit.GetImage().color = CurrentVotingSystem.mapBits[result].GetImage().color;

            mapBit.transform.localScale = Vector3.one;
            //mapBit.transform.localScale = Vector3.zero;
            //mapBit.transform.DOScale(Vector3.one, 1f / ANIMATION_SPEED);
        }

        private Tweener AnimateCurrentGridOff()
        {
            Transform currentGrid = CurrentVotingSystem.mapBits[0].transform.parent;
            currentGrid.localScale = Vector3.one;
            currentGrid.gameObject.SetActive(false);
            return null;  //currentGrid.DOScale(Vector3.zero, 1f / ANIMATION_SPEED);
        }

        private void AnimateGridOn(GameObject grid)
        {
            grid.SetActive(true);
            grid.transform.localScale = Vector3.one; // Vector3.zero
            CreateVotingSystem(grid);

            /*
            grid.transform.DOScale(Vector3.one, 1f / ANIMATION_SPEED).OnComplete(delegate ()
            {
                CreateVotingSystem(grid);
            });*/
        }

        private void OnVotingEnded(int result)
        {
            //AnimateCurrentGridOff().onComplete += (TweenCallback) delegate () { HandleNextVotingSystem(result); };
            CreateSelectedBit(result);
            AnimateCurrentGridOff();
            HandleNextVotingSystem(result);
        }

        private void UpdateBeastTeam(Actor beast, int teamIndex)
        {
            var playerID = BeastInput.FallbackGetPlayerID(beast);
            LobbyManager.Instance.LocalBeasts.PlayerInfo[playerID].TeamIndex = teamIndex;
        }

        private void UpdateActorGraphicColour(Actor beast, ActorGraphic graphic)
        {
            var playerID = BeastInput.FallbackGetPlayerID(beast);
            graphic.SetStickerColour(CostumePool.I.PlayerColorDatabase.GetColorOjectWithID((ushort)(LobbyManager.Instance.LocalBeasts.PlayerInfo[playerID].TeamIndex + 1)).Colors[0]);
        }

        private void UpdateAllBeastsTeams()
        {
            if (CurrentVotingSystem == null) return;
            foreach (Actor actor in CurrentVotingSystem.GetActorVotes().Keys)
            {
                var id = BeastInput.FallbackGetPlayerID(actor);
                UpdateBeastTeam(actor, CurrentVotingSystem.GetActorVotes()[id]);
                UpdateActorGraphicColour(actor, CurrentVotingSystem.GetActorGraphics()[id]);
            }
        }

        private bool _updateBeastTeams = false;
        private void HandleNextVotingSystem(int result)
        {
            if (!_currentGameData.votedForGameMode)
            {
                _currentGameData.votedForGameMode = true;
                _currentGameData.gameMode = (GameModeEnum)((GameModeUIBit)CurrentVotingSystem.mapBits[result]).gameMode;
                _menuHandler.SetCurrentGameMode(_currentGameData.gameMode);
                
                string nextGrid = _currentGameData.gameMode == GameModeEnum.Football ? "FootballTeamGrid" :
                    _currentGameData.gameMode == GameModeEnum.GangMelee ? "GangTeamGrid" :
                    "WinCountGrid";

                if (nextGrid == "WinCountGrid")
                    _currentGameData.votedForTeams = true;
                else
                    _updateBeastTeams = true;

                GameObject activeMapCountGrid = _activeMapUI.transform.Find(nextGrid).gameObject;
                AnimateGridOn(activeMapCountGrid);

                ShowAllowedMapsForGameMode(_currentGameData.gameMode);
            }
            else if (!_currentGameData.votedForTeams)
            {
                _currentGameData.votedForTeams = true;
                _updateBeastTeams = false;
                UpdateAllBeastsTeams();

                _currentGameData.mapCount = _currentGameData.winCount = 1;
                _currentGameData.votedForWinCount = _currentGameData.votedForMapCount = true;

                GameObject activeMapCountGrid = _activeMapUI.transform.Find("MapGrid").gameObject;
                AnimateGridOn(activeMapCountGrid);
            }
            else if (!_currentGameData.votedForWinCount)
            {
                _currentGameData.votedForWinCount = true;
                _currentGameData.winCount = (CurrentVotingSystem.mapBits[result] as MapCountUIBit).mapCount;

                GameObject activeMapGrid = _activeMapUI.transform.Find("MapCountGrid").gameObject;
                AnimateGridOn(activeMapGrid);
            }
            else if (!_currentGameData.votedForMapCount)
            {
                _currentGameData.votedForMapCount = true;
                _currentGameData.mapCount = (CurrentVotingSystem.mapBits[result] as MapCountUIBit).mapCount;

                GameObject activeMapGrid = _activeMapUI.transform.Find("MapGrid").gameObject;
                AnimateGridOn(activeMapGrid);
            }
            else if (_currentGameData.mapNames.Count < _currentGameData.mapCount - 1)
            {
                string mapName = CurrentVotingSystem.mapBits[result].GetName();
                if (mapName == "Random")
                    mapName = CurrentVotingSystem.mapBits[Random.Range(0, CurrentVotingSystem.mapBits.Length - 2)].GetName();

                _currentGameData.mapNames.Add(mapName);

                GameObject activeMapGrid = _activeMapUI.transform.Find("MapGrid").gameObject;
                AnimateGridOn(activeMapGrid);
            }
            else
            {
                _currentGameData.mapNames.Add(CurrentVotingSystem.mapBits[result].GetName());
                StartGame();
            }
        }

        private void StartGame()
        {
            _menuHandler.selectedConfig = GBConfigLoader.CreateRotationConfig(
                new Il2CppStringArray(_currentGameData.mapNames.ToArray()),
                _currentGameData.gameMode,
                _currentGameData.winCount,
                false,
                5 * 3600
            );
            LobbyManager instance = LobbyManager.Instance;
            GameManagerNew.OnGameManagerCreated += (Handler)_menuHandler.SetConfigOnGameManager;
            instance.LobbyStates.CurrentState = (LobbyState.State.Ready | LobbyState.State.InGame);
            instance.LobbyStates.UpdateLobbyState();
            LobbyManager.Instance.LocalBeasts.SetupNetMemberContext(false);
            _menuHandler.SetupLoadScreen();
            MonoSingleton<Global>.Instance.LevelLoadSystem.ShowLoadingScreen(3f, (Action)delegate
            {
                UnityEngine.Debug.Log("Launching Host...");
                NetMemberContext.LocalHostedGame = true;
                MonoSingleton<Global>.Instance.UNetManager.LaunchHost();
            }, -1f, false);
        }

        private void CreateVotingSystem(GameObject grid)
        {
            /*
            try
            {
                
                for (int i = 0; i < grid.transform.childCount; ++i)
                {
                    var bit = grid.transform.GetChild(i);
                    if (bit.gameObject.GetComponent<UIBit>() == null)
                    {
                        bit.gameObject.AddComponent<UIBit>();
                    }
                }
            }
            catch
            {
                LoggingUtilities.VerboseLog("FIRST HALF!");
            }*/
            grid.SetActive(true);
            UIBit[] bits = grid.GetComponentsInChildren<UIBit>();
            TMP_Text timerText = _activeMapUI.transform.Find("Timer").GetComponent<TMP_Text>();
            CurrentVotingSystem = new VotingSystem(10f, bits, Mathf.Min(6, bits.Length), _activeMapUI.transform, timerText);
            CurrentVotingSystem.VotingEnded += OnVotingEnded;
        }

        private void StartVoting()
        {
            _startedVoting = true;
            _activeMapUI.SetActive(true);
            GameObject activeGameModeGrid = _activeMapUI.transform.Find("GameModeGrid").gameObject;
            CreateVotingSystem(activeGameModeGrid);
        }

        private bool IsLocalLobby()
        {
            return _localBeastMenu.activeSelf;
        }

        // returns true if succeeded
        // returns false if failed
        private bool TrySettingLocalBeastMenu()
        {
            GameObject canvas = GetCanvas();
            if (canvas == null)
            {
                return false;
            }
            _localBeastMenu = canvas.transform.Find("Local Beast Select Menu").gameObject;
            if (_localBeastMenu == null)
            {
                return false;
            }

            return true;
        }

        private void CancelVoting()
        {
            if (CurrentVotingSystem == null) return;
            
            for (int i = 0; i < _activeSelectedValues.childCount; ++i)
                GameObject.Destroy(_activeSelectedValues.GetChild(i).gameObject);

            for (int i = 0; i < _activeMapUI.transform.childCount; ++i)
            {
                Transform child = _activeMapUI.transform.GetChild(i);
                if (child != _activeSelectedValues && child.name != "Timer")
                    child.gameObject.SetActive(false);
            }

            CurrentVotingSystem.EndVote();
            CurrentVotingSystem = null;
            _activeMapUI.SetActive(false);
            _startedVoting = false;
            _currentGameData.Reset();
        }

        public override void OnUpdate()
        {
            /* TODO
            if (!modFile.GetBool("UseCustomMenu"))
            {
                return;
            }
            */


            if (SceneManager.GetActiveScene().name != "Menu")
            {
                return;
            }

            if (_busySettingMenuHandler)
            {

                MenuHandlerGamemodes[] gamemodes = GameObject.FindObjectsOfType<MenuHandlerGamemodes>(true);
                if (gamemodes != null)
                    foreach (MenuHandlerGamemodes menuHandler in gamemodes)
                    {
                        if (menuHandler == null) continue;
                        if (menuHandler.type == MenuHandlerGamemodes.MenuType.Local)
                            _menuHandler = menuHandler;
                        if (menuHandler.tracker != null)
                            _gameModeSetupTracker = menuHandler.tracker;
                    }

                if (_menuHandler != null)
                {
                    _menuHandler.enabled = _menuHandlerActive;
                    _busySettingMenuHandler = false;
                }
            }

            if (!_addedBaseMaps && _menuHandler != null && _gameModeSetupTracker != null)
            {
                _gameModeSetupTracker.Maps.GetMapsFor(GameModeEnum.Melee); // triggers Cement's patch to add custom scenes
                AddBaseMaps();
            }

            // will return if failed
            if (_localBeastMenu == null && !TrySettingLocalBeastMenu())
                return;

            if (_uiShouldBeDisabled && !_inGameUIDisabled)
                DisableUI();

            if (_tracker == null)
            {
                _tracker = GameObject.FindObjectOfType<LocalBeastSetupTracker>();
                if (_tracker == null)
                {
                    return;
                }
            }

            if (IsLocalLobby())
            {
                if (_tracker.AllActiveBeastsReady())
                {
                    if (!_startedVoting) StartVoting();
                }
                else if (CurrentVotingSystem != null)
                    CancelVoting();

                if (_updateBeastTeams)
                    UpdateAllBeastsTeams();
            }

            if (CurrentVotingSystem != null)
            {
                CurrentVotingSystem.Tick(Time.deltaTime);
            }
        }
    }
}