using System;
using System.Collections;
using System.Collections.Generic;
using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Multiplayer.Handler;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Message;
using BDArmory.Multiplayer.Utils;
using LmpClient;
using LmpClient.Systems.Lock;
using LmpClient.Systems.ModApi;
using LmpCommon.Enums;
using UnityEngine;

namespace BDArmory.Multiplayer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LunaMultiplayerSystem : MonoBehaviour, IMultiplayerSystem
    {
        private const string ModName = "BDArmory";
        public const bool Relay = true;
        public EventData<string, byte[]> onModMessageReceivedEvent;
        public static Queue<MissileFireEventArgs> missileMessagePending { get; set; } = new Queue<MissileFireEventArgs>();


        void Start()
        {
            RegisterSystem();
            SuscribeToCoreEvents();
        }

        public void Update()
        {
            SetupBDArmoryMultiplayer();
            ProcessDelayedMessages();
        }

        private void SetupBDArmoryMultiplayer()
        {
            ObtainBasicLMPData();
            ProcessDelayedMessages();
        }

        private void ProcessDelayedMessages()
        {
            if (missileMessagePending.Count == 0)
            {
                return;
            }

            if (Dependencies.Get<IBdaMessageHandler<MissileFireEventArgs>>()
                .ProcessMessage(missileMessagePending.Peek()))
            {
                missileMessagePending.Dequeue();
            }
        }

        private static void ObtainBasicLMPData()
        {
            if (MainSystem.NetworkState <= ClientState.Disconnected)
            {
                BDArmorySettings.MULTIPLAYER_ACTIVE = false;
            }
            else
            {
                BDArmorySettings.MULTIPLAYER_ACTIVE = true;

                if (FlightGlobals.ActiveVessel == null) return;

                if (String.IsNullOrEmpty(BDArmorySettings.MULTIPLAYER_OWNER_ID))
                {
                    BDArmorySettings.MULTIPLAYER_OWNER_ID =
                        LockSystem.LockQuery.GetControlLockOwner(FlightGlobals.ActiveVessel.id);
                }
                else
                {
                    foreach (var locklmp in LockSystem.LockQuery.GetAllLocks())
                    {
                        if (LockSystem.LockQuery.GetControlLockOwner(locklmp.VesselId) ==
                            BDArmorySettings.MULTIPLAYER_OWNER_ID &&
                            !BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(locklmp.VesselId))
                        {
                            BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Add(locklmp.VesselId);
                        }
                    }
                }
            }
        }

        public void RegisterSystem()
        {
            Dependencies.Register<IBdaMessageHandler<DamageEventArgs>, DamageMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<ExplosionEventArgs>, ExplosionMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<ArmorEventArgs>, ArmorMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<VesselTeamChangeEventArgs>, VesselTeamChangeMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<ForceEventArgs>, ForceMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<TurretAimEventArgs>, TurretAimMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<FireEventArgs>, FireMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<MissileFireEventArgs>,MissileFireMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<RequestVesselTeamEventArgs>, RequestVesselTeamMessageHandler>();
        }

        public void HandlerFunction(byte[] messageData)
        {
            BdaMessage messageReceived = BinaryUtils.Deserialize<BdaMessage>(messageData);

            ProcessReceivedMessage(messageReceived);
        }

        private void ProcessReceivedMessage(BdaMessage messageReceived)
        {
            switch (messageReceived.Content)
            {
                case DamageEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<DamageEventArgs>>().ProcessMessage(args);
                    break;
                case ExplosionEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<ExplosionEventArgs>>().ProcessMessage(args);
                    break;
                case ForceEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<ForceEventArgs>>().ProcessMessage(args);
                    break;
                case VesselTeamChangeEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<VesselTeamChangeEventArgs>>().ProcessMessage(args);
                    break;
                case ArmorEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<ArmorEventArgs>>().ProcessMessage(args);
                    break;
                case FireEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<FireEventArgs>>().ProcessMessage(args);
                    break;
                case TurretAimEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<TurretAimEventArgs>>().ProcessMessage(args);
                    break;
                case MissileFireEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<MissileFireEventArgs>>().ProcessMessage(args);
                    break;
                case RequestVesselTeamEventArgs args:
                    Dependencies.Get<IBdaMessageHandler<RequestVesselTeamEventArgs>>().ProcessMessage(args);
                    break;

            }
        }

        private void SuscribeToCoreEvents()
        {
            GameEvents.onVesselLoaded.Add(OnVesselLoad);
            onModMessageReceivedEvent = GameEvents.FindEvent<EventData<string, byte[]>>("onModMessageReceived");
            if (onModMessageReceivedEvent != null)
            {
                BDArmorySettings.MULTIPLAYER_ACTIVE = true;
                Debug.Log("[BDArmory]: LMP Multiplayer enabled");
                onModMessageReceivedEvent.Add(OnModMessageReceived);

                Dependencies.Get<DamageEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<ExplosionEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<ArmorEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<VesselTeamChangeService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<ForceEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<TurretAimEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<FireEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<MissileFiredEventService>().OnActionExecuted += OnActionExecuted;
                Dependencies.Get<RequestTeamEventService>().OnActionExecuted += OnActionExecuted;
            }
            else
            {
                Debug.Log("[BDArmory]: LMP Multiplayer disabled");
                BDArmorySettings.MULTIPLAYER_ACTIVE = false;
            }
        }

        private void OnVesselLoad(Vessel vessel)
        {
            if (!BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(vessel.id))
            {
                Dependencies.Get<RequestTeamEventService>().PublishRequest(vessel.id);
            }
        }

        private void OnModMessageReceived(string id, byte[] data)
        {
            if (id == ModName && data.Length > 0)
            {
                HandlerFunction(data);
            }
        }

        private void OnActionExecuted(object sender, EventArgs eventArgs)
        {
            SendMessage(eventArgs);
        }

       
        public void SendMessage(EventArgs message)
        {
            BdaMessage messageToSend = new BdaMessage() {Type = message.GetType(), Content = message};

            byte[] messageToBeSend = BinaryUtils.Serialize(messageToSend);

            ModApiSystem.Singleton.SendModMessage(ModName, messageToBeSend, messageToBeSend.Length, true);
        }

        void OnDestroy()
        {
            if (onModMessageReceivedEvent != null)
            {
                onModMessageReceivedEvent.Remove(OnModMessageReceived);
            }
        }
    }
}