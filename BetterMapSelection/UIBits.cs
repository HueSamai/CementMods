using UnityEngine;
using Il2CppTMPro;
using UnityEngine.UI;
using MelonLoader;
using Il2CppGB.Gamemodes;
using UnityEngine.Events;
using BetterMapSelection;
using CementGB.Mod.Modules.BeastInput;
using System.ComponentModel;
using System;
using Il2CppInterop.Runtime.InteropTypes.Fields;

[RegisterTypeInIl2Cpp]
public class UIBit : MonoBehaviour
{
    private Image _image;
    private TMP_Text _nameDisplay;
    private int _gridIndex;
    private Button _button;

    private void Awake()
    {
        // setup values
        _image = GetComponent<Image>();
        _nameDisplay = GetComponentInChildren<TMP_Text>();
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener((UnityAction)HandleClick);
        }
    }

    private void HandleClick()
    {
        BetterMapSelectionMod.Singleton.CurrentVotingSystem.MoveActor(BeastInput.KeyboardMouseBeast, _gridIndex);
    }

    public void SetGridIndex(int i)
    {
        _gridIndex = i;
    }

    public int GetGridIndex()
    {
        return _gridIndex;
    }

    public void UpdateMap(string newName, Sprite newImage)
    {
        _image.sprite = newImage;
        _nameDisplay.text = newName;
    }

    public Sprite GetSprite()
    {
        return _image.sprite;
    }

    public Image GetImage()
    {
        return _image;
    }

    public string GetName()
    {
        return _nameDisplay.text;
    }
}

[RegisterTypeInIl2Cpp]
public class MapUIBit : UIBit
{
    public string mapName;
}

[RegisterTypeInIl2Cpp]
public class GameModeUIBit : UIBit
{
    public int gameMode => _gameMode.Get();
    public Il2CppValueField<int> _gameMode;
}

[RegisterTypeInIl2Cpp]
public class MapCountUIBit : UIBit
{
    public int mapCount => _mapCount.Get();

    public Il2CppValueField<int> _mapCount;
}