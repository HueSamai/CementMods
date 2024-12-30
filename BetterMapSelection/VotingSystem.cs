using System;
using UnityEngine;
using CementGB.Mod.Modules.BeastInput;
using System.Collections.Generic;
using Il2CppFemur;
using Random = UnityEngine.Random;
using CementGB.Mod.Utilities;
using UnityEngine.InputSystem;
using Il2CppTMPro;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.Controls;
using Il2CppGB.UI.Beasts;

public class VotingSystem
{
    private Dictionary<Actor, int> _actorVoteIndices = new();
    private Dictionary<Actor, ActorGraphic> _actorGraphics = new();
    private bool _busy = true;
    private int numberOfRows;
    private int numberOfCollumns;
    private int mapBitsLength;
    public UIBit[] mapBits;
    private Transform canvasParent;
    private float timer;
    private TMP_Text timerText;
    private float timeWaited;
    private bool movedThisFrame;

    private Color originalTimerTextColour;

    public event Action<int> VotingEnded;

    public VotingSystem(float time, UIBit[] mapBits, int numberOfCollumns, Transform canvasParent, TMP_Text timerText)
    {
        LoggingUtilities.VerboseLog($"Creating voting system! {mapBits.Length}");
        mapBitsLength = mapBits.Length;
        this.mapBits = mapBits;
        for (int i = 0; i < mapBits.Length; ++i)
        {
            this.mapBits[i].SetGridIndex(i);
        }
        this.numberOfCollumns = numberOfCollumns;
        numberOfRows = Mathf.CeilToInt(mapBitsLength / numberOfCollumns);
        this.canvasParent = canvasParent;
        timer = time;
        this.timerText = timerText;
        timerText.text = "Waiting...";
        originalTimerTextColour = timerText.color;
        LoggingUtilities.VerboseLog($"Created voting system!");
    }
    
    public Actor GetActorFromPlayerID(int playerId)
    {
        var bneastMenuState = GameObject.FindObjectOfType<BeastMenuState>();
        if (bneastMenuState == null) return null;

        foreach (var pointState in bneastMenuState._pointStates)
        {
            if (pointState._linkedLocal.PlayerID == playerId)
                return pointState._beast;
        }

        return null;
    }

    private void HandleKeybinds()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame) dPressed(BeastInput.KeyboardMouseBeast);
        if (Keyboard.current.aKey.wasPressedThisFrame) aPressed(BeastInput.KeyboardMouseBeast);
        if (Keyboard.current.wKey.wasPressedThisFrame) wPressed(BeastInput.KeyboardMouseBeast);
        if (Keyboard.current.sKey.wasPressedThisFrame) sPressed(BeastInput.KeyboardMouseBeast);

        foreach (var actor in Actor._ActorCache)
        {
            if (actor == BeastInput.KeyboardMouseBeast) continue;

            InputDevice device = BeastInput.GetDevicesFor(actor)[0];
            if (device == null)
            {
                LoggingUtilities.VerboseLog($"device for actor {actor.name} is null!!!!!!!!!!!!!!!!!!");
                continue;
            }

            LoggingUtilities.VerboseLog($"got device for actor {actor.name}");

            if (device.GetChildControl<ButtonControl>(InputCode.leftstickRight).wasPressedThisFrame) dPressed(actor);
            if (device.GetChildControl<ButtonControl>(InputCode.leftstickLeft).wasPressedThisFrame) aPressed(actor);
            if (device.GetChildControl<ButtonControl>(InputCode.leftstickUp).wasPressedThisFrame) wPressed(actor);
            if (device.GetChildControl<ButtonControl>(InputCode.leftstickDown).wasPressedThisFrame) sPressed(actor);
        }
    }

    // returns -1 for an invalid index,
    // otherwise the return value is the new valid index for the actor graphic
    private int GetNewIndex(int index, Vector2Int direction)
    {
        int y = (int)Mathf.Floor(index / (float)numberOfCollumns);
        int x = index - y * numberOfCollumns;
        LoggingUtilities.VerboseLog($"Current X: {x}, Y: {y}");
        
        x += direction.x;
        y += direction.y;

        LoggingUtilities.VerboseLog($"New X: {x}, Y: {y}");

        if (x < 0 || y < 0)
            return -1;

        if (x >= numberOfCollumns || y > numberOfRows)
            return -1;

        int newIndex = x + y * numberOfCollumns;
        if (newIndex  >= mapBitsLength)
            return -1;


        return newIndex;
    }

    private void dPressed(Actor a)
    {
        if (!_busy)
            return;

        LoggingUtilities.VerboseLog("D was pressed!");

        MoveActor(a, Vector2Int.right);
    }

    private void aPressed(Actor a)
    {
        if (!_busy)
            return;

        MoveActor(a, Vector2Int.left);
    }

    private void wPressed(Actor a)
    {
        if (!_busy)
            return;

        // inverted here because of how the indices are laid out
        MoveActor(a, Vector2Int.down);
    }

    private void sPressed(Actor a)
    {
        if (!_busy)
            return;

        // inverted here because of how the indices are laid out
        MoveActor(a, Vector2Int.up);
    }

    private void AddActor(Actor a)
    {
        _actorVoteIndices[a] = 0;
        GameObject actorGraphic = GameObject.Instantiate(BMSResources.actorGraphic);
        actorGraphic.transform.SetParent(canvasParent);
        _actorGraphics[a] = actorGraphic.AddComponent<ActorGraphic>();
        _actorGraphics[a].SetStickerColour(a.primaryColor);
        actorGraphic.transform.eulerAngles = new Vector3(0, -90, 0);
        actorGraphic.transform.localScale = Vector3.one;
    }

    private void MoveActor(Actor a, Vector2Int direction)
    {
        LoggingUtilities.VerboseLog($"Moving actor!");
        LoggingUtilities.VerboseLog($"Actor {a}!");
        if (!_actorVoteIndices.ContainsKey(a))
        {
            AddActor(a);
            UpdateGraphic(a, _actorVoteIndices[a]);
            return;
        }

        int newIndex = GetNewIndex(_actorVoteIndices[a], direction);
        LoggingUtilities.VerboseLog($"new index {newIndex}");
        if (newIndex != -1)
        {
            movedThisFrame = true;
            _actorVoteIndices[a] = newIndex;
            UpdateGraphic(a, newIndex);
        }
    }

    public void MoveActor(Actor a, int index)
    {
        LoggingUtilities.VerboseLog($"Moving actor!");
        LoggingUtilities.VerboseLog($"Actor {a}!");
        if (!_actorVoteIndices.ContainsKey(a))
        {
            AddActor(a);
        }

        if (index < 0 || index >= mapBits.Length) return;

        movedThisFrame = true;
        _actorVoteIndices[a] = index;
        UpdateGraphic(a, index);
    }

    private void UpdateGraphic(Actor a, int index)
    {
        int graphicIndex = index;
        _actorGraphics[a].transform.position = mapBits[graphicIndex].transform.position;
        _actorGraphics[a].UpdateSticker();
    }

    private void SetTimerText()
    {
        if (timeWaited > 0.5f)
        {
            timerText.color = Color.red;
            timerText.text = (2f - timeWaited).ToString("0.0");
        }
        else
        {
            timerText.color = originalTimerTextColour;
            timerText.text = Mathf.Max(timer, 0.0f).ToString(timer <= 3 ? "0.0" : "0");
        }
    }

    public void Tick(float deltaTime)
    {
        if (!_busy)
            return;

        HandleKeybinds();

        if (_actorVoteIndices.Count == 0)
            return;

        SetTimerText();
       
        if (timer <= 0 || timeWaited > 2f)
        {
            EndVote();
            if (VotingEnded != null)
            {
                VotingEnded.Invoke(GetResult());
            }
        }

        timer -= deltaTime;
        if (movedThisFrame)
        {
            timeWaited = 0;
        }
        else
        {
            timeWaited += deltaTime;
        }

        movedThisFrame = false;
    }

    public void EndVote()
    {
        _busy = false;
        timerText.color = originalTimerTextColour;
        foreach (ActorGraphic graphic in _actorGraphics.Values)
        {
            GameObject.Destroy(graphic.gameObject);
        }
    }

    public Dictionary<Actor, int> GetActorVotes()
    {
        return _actorVoteIndices;
    }

    public Dictionary<Actor, ActorGraphic> GetActorGraphics()
    {
        return _actorGraphics;
    }

    private int GetResult()
    {
        if (_actorVoteIndices.Count == 0)
        {
            return Random.Range(0, mapBitsLength - 1);
        }

        Dictionary<int, int> _indexOccurrences = new();
        foreach (int index in _actorVoteIndices.Values)
        {
            if (!_indexOccurrences.ContainsKey(index))
                _indexOccurrences[index] = 0;

            _indexOccurrences[index]++;
        }

        int mostAbundant = -1;
        List<int> mostAbundantIndices = new ();

        foreach (int index in _indexOccurrences.Keys)
        {
            if (_indexOccurrences[index] > mostAbundant)
            {
                mostAbundantIndices.Clear();
                mostAbundantIndices.Add(index);
                mostAbundant = _indexOccurrences[index];
            }
            else if (_indexOccurrences[index] == mostAbundant)
            {
                mostAbundantIndices.Add(index);
            }
        }

        return mostAbundantIndices[Random.Range(0, mostAbundantIndices.Count - 1)];
    }
}