using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using yandereMod;
using DunGen;
using GameNetcodeStuff;
using Unity.Collections;

namespace yandereMod;

public class NetworkingClass : NetworkBehaviour
{
    public static NetworkingClass Instance;

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            if (Instance != null)
                Instance.gameObject.GetComponent<NetworkObject>().Despawn();
        }
        Instance = this;

        base.OnNetworkSpawn();
    }

    [ClientRpc]
    public void SpawnChairClientRpc()
    {
        Plugin.WriteToConsole("In ClientRPC");
        // Should probably be an RPC
        var tiles = FindObjectsOfType<Tile>();
        foreach (var tile in tiles)
        {
            if (tile.gameObject.name.Contains("SmallRoom2"))
            {
                foreach (Transform child in tile.gameObject.transform)
                {
                    if (child.gameObject.name.Contains("AINode"))
                    {
                        Plugin.WriteToConsole("Found node");
                        Plugin.yandereRoomToTarget = child;
                        if (!Plugin.chairSpawned)
                        {
                            Plugin.WriteToConsole("spawning chair");
                            var chair = Instantiate(Plugin.yandereChair, tile.gameObject.transform.position + new Vector3(0, 2, 0), tile.gameObject.transform.rotation, tile.gameObject.transform);
                            // Move chair "forwards" (from where you would look when sitting) 4 units
                            chair.transform.position += chair.transform.forward * -4;
                            Plugin.chairSpawned = true;
                            Plugin.chairLocation = chair.transform;
                            Plugin.WriteToConsole("spawned chair");
                        }
                        return;
                    }
                }
            }
        }
        Plugin.WriteToConsole("No suitable target room found for yandere.");
        // Consider doing this for each round of enemies?
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetUpYandereRoomServerRpc(ulong clientId, NetworkBehaviourReference deadBodyRef, NetworkBehaviourReference yandereRef)
    {
        SetUpYandereRoomClientRpc(clientId, deadBodyRef, yandereRef);
    }

    [ClientRpc]
    public void SetUpYandereRoomClientRpc(ulong clientId, NetworkBehaviourReference deadBodyRef, NetworkBehaviourReference yandereRef)
    {
        PlayerControllerB component;
        deadBodyRef.TryGet(out component);
        yandereAI component2;
        yandereRef.TryGet(out component2);
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            foreach (Transform t in Plugin.chairLocation.transform.parent)
            {
                if (t.name.Contains("Spot Light") && !t.name.Contains("(1)"))
                {
                    t.GetComponent<Light>().color = new Color(0.5f, 0, 0);
                }
                else if (t.name.Contains("Spot Light (1)"))
                {
                    t.GetComponent<Light>().color = new Color(0.5f, 0.5f, 0.5f);
                }
            }

            foreach (Transform t in Plugin.chairLocation)
            {
                if (t.name.Contains("Scavenger"))
                    t.GetComponent<TiedPlayerManager>().enabled = false;

                if (t.name.Contains("Rope") || t.name.Contains("Scavenger") || t.name.Contains("DoorCollider"))
                {
                    t.gameObject.SetActive(true);
                }
            }
        }

        component.deadBody.gameObject.SetActive(false);
        component2.GetComponent<yandereAI>().enabled = false;
        component2.transform.position = new Vector3(0, -1000, 0);
        component2.agent.enabled = false;
        component2.runningSFX.mute = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void disableYandereRoomServerRpc()
    {
        disableYandereRoomClientRpc();
    }

    [ClientRpc]
    public void disableYandereRoomClientRpc()
    {
        foreach (Transform t in Plugin.chairLocation.transform.parent)
        {
            if (t.name.Contains("Spot Light") && !t.name.Contains("(1)"))
            {
                t.GetComponent<Light>().color = new Color(1f, 1f, 1f);
            }
            else if (t.name.Contains("Spot Light (1)"))
            {
                t.GetComponent<Light>().color = new Color(1f, 1f, 1f);
            }
        }
        foreach (Transform t in Plugin.chairLocation)
        {
            if (t.name.Contains("Rope") || t.name.Contains("Scavenger") || t.name.Contains("TiedCamera") || t.name.Contains("DoorCollider"))
            {
                t.gameObject.SetActive(false);

            }
        }

        FindFirstObjectByType<YandereEyeTarget>().gameObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetYandereBoolsServerRpc(bool hasPathToChair, bool setPathToChair)
    {
        SetYandereBoolsClientRpc(hasPathToChair, setPathToChair);
    }

    [ClientRpc]
    public void SetYandereBoolsClientRpc(bool hasPathToChair, bool setPathToChair)
    {
        var ai = FindFirstObjectByType<yandereAI>();
        if (ai)
        {
            ai.hasPathToChair = hasPathToChair;
            ai.setPathToChair = setPathToChair;
        }
    }

    [ClientRpc]
    public void InitAzureClientRpc()
    {
        AzureSTT.Init(Plugin.Azure_api_key.Value, Plugin.Azure_region.Value, Plugin.Azure_language.Value);
        Plugin.chairSpawned = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MovePlayerAudioSourceServerRpc(FixedString128Bytes playerID)
    {
        MovePlayerAudioSourceClientRpc(playerID);
    }

    [ClientRpc]
    public void MovePlayerAudioSourceClientRpc(FixedString128Bytes playerID)
    {
        var comms = FindFirstObjectByType<PlayerVoiceIngameSettings>().transform.parent;
        AudioSource commsAudioSource = null;
        foreach (Transform t in comms.transform)
        {
            if (t.name.Contains(playerID.ToString()))
            {
                commsAudioSource = t.GetComponent<AudioSource>();
                break;
            }
        }
        if (commsAudioSource != null)
            commsAudioSource.transform.parent = FindFirstObjectByType<yandereAI>().transform;
        else
            yandereAI.WriteToConsole("No comms audio source");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ParentPlayerToNoAIServerRpc(NetworkBehaviourReference netRef)
    {
        ParentPlayerToNoAIClientRpc(netRef);
    }

    [ClientRpc]
    public void ParentPlayerToNoAIClientRpc(NetworkBehaviourReference netRef)
    {
        PlayerControllerB component;
        netRef.TryGet(out component);
        if (component == null)
        {
            yandereAI.WriteToConsole("component is null!");
            return;
        }

        component.transform.parent = Plugin.chairLocation;
        component.transform.position = Plugin.chairLocation.position;
    }
}
