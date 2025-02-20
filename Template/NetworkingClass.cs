using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using yandereMod;
using DunGen;

namespace yandereMod;

public class NetworkingClass : NetworkBehaviour
{
    public static NetworkingClass Instance;

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
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
}
