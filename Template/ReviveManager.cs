using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using static Unity.Netcode.FastBufferWriter;
using UnityEngine;

namespace yandereMod
{
    public class ReviveManager : NetworkBehaviour
    {
        public static ReviveManager Instance { get; private set; }

        private void Awake()
        {
            yandereAI.WriteToConsole("REV AWAKE");
            Instance = this;
            
        }

        private int GetPlayerIndex(string username)
        {
            StartOfRound instance = StartOfRound.Instance;
            if (instance == null)
            {
                return -1;
            }
            PlayerControllerB[] allPlayerScripts = instance.allPlayerScripts;
            for (int i = 0; i < allPlayerScripts.Length; i++)
            {
                if (allPlayerScripts[i] != null && allPlayerScripts[i].playerUsername == username)
                {
                    return i;
                }
            }
            return -1;
        }

        public void ReviveSinglePlayer(Vector3 position, PlayerControllerB p)
        {
            RevivePlayer(position, p);
        }

        private void RevivePlayer(Vector3 position, PlayerControllerB p)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0003: Unknown result type (might be due to invalid IL or missing references)
            RevivePlayerClientRpc(position, p);
            SyncLivingPlayersServerRpc();
        }

        [ClientRpc]
        private void RevivePlayerClientRpc(Vector3 spawnPosition, PlayerControllerB p)
        {
            PlayerControllerB component = p;
            if (component == null)
            {
                return;
            }
            int playerIndex = GetPlayerIndex(component.playerUsername);
            component.ResetPlayerBloodObjects(component.isPlayerDead || component.isPlayerControlled);
            component.isClimbingLadder = false;
            component.clampLooking = false;
            component.inVehicleAnimation = false;
            component.disableMoveInput = false;
            component.disableLookInput = false;
            component.disableInteract = false;
            component.ResetZAndXRotation();
            ((Collider)component.thisController).enabled = true;
            component.health = 100;
            component.hasBeenCriticallyInjured = false;
            component.disableSyncInAnimation = false;
            if (component.isPlayerDead)
            {
                component.isPlayerDead = false;
                component.isPlayerControlled = true;
                component.isInElevator = true;
                component.isInHangarShipRoom = true;
                component.isInsideFactory = false;
                component.parentedToElevatorLastFrame = false;
                component.overrideGameOverSpectatePivot = null;
                if (((NetworkBehaviour)component).IsOwner)
                {
                    StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
                }
                component.TeleportPlayer(spawnPosition, false, 0f, false, true);
                component.setPositionOfDeadPlayer = false;
                component.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[playerIndex], true, true);
                ((Behaviour)component.helmetLight).enabled = false;
                component.Crouch(false);
                component.criticallyInjured = false;
                if (component.playerBodyAnimator != null)
                {
                    component.playerBodyAnimator.SetBool("Limp", false);
                }
                component.bleedingHeavily = false;
                component.activatingItem = false;
                component.twoHanded = false;
                component.inShockingMinigame = false;
                component.inSpecialInteractAnimation = false;
                component.freeRotationInInteractAnimation = false;
                component.inAnimationWithEnemy = null;
                component.holdingWalkieTalkie = false;
                component.speakingToWalkieTalkie = false;
                component.isSinking = false;
                component.isUnderwater = false;
                component.sinkingValue = 0f;
                component.statusEffectAudio.Stop();
                component.DisableJetpackControlsLocally();
                component.health = 100;
                component.mapRadarDotAnimator.SetBool("dead", false);
                component.externalForceAutoFade = Vector3.zero;
                if (((NetworkBehaviour)component).IsOwner)
                {
                    HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                    component.hasBegunSpectating = false;
                    HUDManager.Instance.RemoveSpectateUI();
                    HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                    component.hinderedMultiplier = 1f;
                    component.isMovementHindered = 0;
                    component.sourcesCausingSinking = 0;
                    component.reverbPreset = StartOfRound.Instance.shipReverb;
                }
            }
            yandereAI.WriteToConsole("Past first part");
            SoundManager.Instance.earsRingingTimer = 0f;
            component.voiceMuffledByEnemy = false;
            SoundManager.Instance.playerVoicePitchTargets[playerIndex] = 1f;
            SoundManager.Instance.SetPlayerPitch(1f, playerIndex);
            if (component.currentVoiceChatIngameSettings == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (component.currentVoiceChatIngameSettings != null)
            {
                if (component.currentVoiceChatIngameSettings.voiceAudio == null)
                {
                    component.currentVoiceChatIngameSettings.InitializeComponents();
                }
                if (component.currentVoiceChatIngameSettings.voiceAudio != null)
                {
                    ((Component)component.currentVoiceChatIngameSettings.voiceAudio).GetComponent<OccludeAudio>().overridingLowPass = false;
                }
            }
            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (localPlayerController == component)
            {
                localPlayerController.bleedingHeavily = false;
                localPlayerController.criticallyInjured = false;
                if (localPlayerController.playerBodyAnimator != null)
                {
                    localPlayerController.playerBodyAnimator.SetBool("Limp", false);
                }
                localPlayerController.health = 100;
                HUDManager.Instance.UpdateHealthUI(100, false);
                localPlayerController.spectatedPlayerScript = null;
                ((Behaviour)HUDManager.Instance.audioListenerLowPass).enabled = false;
                HUDManager.Instance.RemoveSpectateUI();
                HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, localPlayerController);
            }
            RagdollGrabbableObject[] array = FindObjectsOfType<RagdollGrabbableObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (!((GrabbableObject)array[i]).isHeld)
                {
                    if (((NetworkBehaviour)this).IsServer && ((NetworkBehaviour)array[i]).NetworkObject.IsSpawned)
                    {
                        ((NetworkBehaviour)array[i]).NetworkObject.Despawn(true);
                    }
                    else
                    {
                        Destroy(((Component)array[i]).gameObject);
                    }
                }
                else if (((GrabbableObject)array[i]).isHeld && ((GrabbableObject)array[i]).playerHeldBy != null)
                {
                    ((GrabbableObject)array[i]).playerHeldBy.DropAllHeldItems(true, false);
                }
            }
            DeadBodyInfo[] array2 = FindObjectsOfType<DeadBodyInfo>();
            for (int j = 0; j < array2.Length; j++)
            {
                Destroy(((Component)array2[j]).gameObject);
            }
            if (((NetworkBehaviour)this).IsServer)
            {
                StartOfRound instance = StartOfRound.Instance;
                instance.livingPlayers++;
                StartOfRound.Instance.allPlayersDead = false;
            }
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SyncLivingPlayersServerRpc()
        {
            StartOfRound instance = StartOfRound.Instance;
            int num = 0;
            PlayerControllerB[] allPlayerScripts = instance.allPlayerScripts;
            foreach (PlayerControllerB val3 in allPlayerScripts)
            {
                if (val3 != null && val3.isPlayerControlled && !val3.isPlayerDead)
                {
                    num++;
                }
            }
            instance.livingPlayers = num;
            instance.allPlayersDead = num == 0;
            SyncLivingPlayersClientRpc(num, instance.allPlayersDead);
        }

        [ClientRpc]
        private void SyncLivingPlayersClientRpc(int newLiving, bool allDead)
        {               
            StartOfRound instance = StartOfRound.Instance;
            instance.livingPlayers = newLiving;
            instance.allPlayersDead = allDead;
        }
    }
}
