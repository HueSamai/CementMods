using UnityEngine;
using Il2CppFemur;
using CementGB.Mod.Modules.PoolingModule;
using CementGB.Mod.Utilities;
using System.Collections;
using MelonLoader;
using HarmonyLib;
using System.Reflection;
using Assembly = System.Reflection.Assembly;
using System.Collections.Generic;
using System;

[assembly: MelonInfo(typeof(Gore), "Gore", "0.0.1", "dotpy", "https://github.com/HueSamai/CementMods/releases")]
public class Gore : MelonMod
{
    public static float cachedVelocityThreshold;
    private static GameObject bloodParticlePrefab;
    private static float particleDuration;

    private static Dictionary<GameObject, float> bloodToPool = new();

    public override void OnLateInitializeMelon()
    {
        AssetBundle goreAssetBundle = AssetBundleUtilities.LoadEmbeddedAssetBundle(System.Reflection.Assembly.GetExecutingAssembly(), "Gore.gore");
        bloodParticlePrefab = AssetBundleUtilities.LoadPersistentAsset<GameObject>(goreAssetBundle, "Blood Particles");
        goreAssetBundle.Unload(false);

        particleDuration = bloodParticlePrefab.GetComponent<ParticleSystem>().main.duration;

        Pool.RegisterPrefab(bloodParticlePrefab, ResetBlood);

        CacheVelocityThreshold();
    }

    public static void AddBloodToPool(GameObject gameObject)
    {
        bloodToPool[gameObject] = particleDuration + 0.5f; // give it some leeway 
    }

    private void CacheVelocityThreshold()
    {
        cachedVelocityThreshold = 14;
    }

    public void ResetBlood(GameObject bloodParticles)
    {
        ParticleSystem particles = bloodParticles.GetComponent<ParticleSystem>();
        particles.Clear();
        particles.Play();
    }

    public override void OnUpdate()
    {
        foreach (GameObject blood in bloodToPool.Keys)
        {
            if (bloodToPool[blood] < 0f)
            {
                Pool.PoolObject(blood);
                bloodToPool.Remove(blood);
                continue;
            }
            bloodToPool[blood] -= Time.deltaTime;
        }
    }


    [HarmonyPatch(typeof(CollisionHandeler), nameof(CollisionHandeler.OnCollisionEnter))]
    private static class CollisionHandlerPatch
    {
        public static void Prefix(CollisionHandeler __instance, Collision collision)
        {
            try
            {
                CollisionHandeler other = collision.gameObject.GetComponentInParent<CollisionHandeler>();
                if (other != null && __instance.actor != other.actor)
                {
                    Vector3 relativeVelocity = collision.relativeVelocity;
                    float magnitude = relativeVelocity.magnitude;

                    if (magnitude < cachedVelocityThreshold)
                    {
                        return;
                    }
                    ContactPoint contact = collision.GetContact(0);
                    GameObject bloodParticles = Pool.Instantiate
                    (
                        bloodParticlePrefab,
                        contact.point,
                        Quaternion.FromToRotation(Vector3.up, contact.normal)
                    );
                    AddBloodToPool(bloodParticles);
                }
            }
            catch (Exception e)
            {
                Melon<Gore>.Logger.Error(e);
            }
        }
    }
}
