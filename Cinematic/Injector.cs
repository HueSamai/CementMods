using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CinematicInjector), "Cinematic", "0.0.1", "dotpy and TheOwlOfLife", "https://api.github.com/repos/HueSamai/CementMods/releases")]
public class CinematicInjector : MelonMod {
    static GameObject hostObject;
    public override void OnLateInitializeMelon() {
        hostObject = new GameObject();
        GameObject.DontDestroyOnLoad(hostObject);
        
        hostObject.AddComponent<CinematicMod>();
        hostObject.AddComponent<InfiniteGrip>();
        hostObject.AddComponent<RemoveKillVolumes>();
    }
}
