using CementGB.Mod.Utilities;
using Il2CppFemur;
using JetBrains.Annotations;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class MapImages
{
    public static Sprite[] images;

    public static Sprite BaseMapNameToSprite(string mapName)
    {
        foreach (Sprite sprite in images)
        {
            if (sprite.name == mapName)
            {
                return sprite;
            }
        }
        return BMSResources.defaultImage;
    }
}

public static class BMSResources
{
    public static GameObject actorGraphic;
    public static Sprite defaultImage;
}

[RegisterTypeInIl2Cpp]
public class ActorGraphic : MonoBehaviour
{
    private Actor _actor;
    private Image _stickerHead;
    private float maxX;
    private float maxY;
    private float padding;

    private void Awake()
    {
        _stickerHead = transform.Find("Head").GetComponent<Image>();
        RectTransform rect = GetComponent<RectTransform>();
        maxX = rect.sizeDelta.x / 2;
        maxY = rect.sizeDelta.y / 2;
        padding = _stickerHead.GetComponent<RectTransform>().sizeDelta.x / 2 + 5;
    }

    public void SetStickerColour(Color colour)
    {
        _stickerHead.color = colour;
    }

    public void UpdateSticker()
    {
        _stickerHead.transform.localPosition = new Vector2(
            Random.Range(padding - maxX, maxX - padding),
            Random.Range(padding - maxY, maxY - padding)
        );
        LoggingUtilities.VerboseLog($"PADDING: {padding}, MAXX+Y: {maxX}, {maxY}");
        _stickerHead.transform.eulerAngles = new Vector3(0, -90, Random.Range(0f, 360f));
    }
}