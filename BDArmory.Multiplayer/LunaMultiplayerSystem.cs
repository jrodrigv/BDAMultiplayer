using System;
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

        void Start()
        {
          RegisterSystem();
        }

        public void Update()
        {
            SetupBDArmoryMultiplayer();
        }

        private void SetupBDArmoryMultiplayer()
        {
            if (MainSystem.NetworkState <= ClientState.Disconnected)
            {
                BDArmorySettings.MULTIPLAYER_ACTIVE = false;
            }
            else
            {
                BDArmorySettings.MULTIPLAYER_ACTIVE = true;

                if(FlightGlobals.ActiveVessel == null) return;
                
                if ( String.IsNullOrEmpty(BDArmorySettings.MULTIPLAYER_OWNER_ID))
                {
                    BDArmorySettings.MULTIPLAYER_OWNER_ID =
                        LockSystem.LockQuery.GetControlLockOwner(FlightGlobals.ActiveVessel.id);
                }
                else
                {
                    var locks = LockSystem.LockQuery.GetAllLocks();

                    foreach (var locklmp in locks)
                    {
                        if (LockSystem.LockQuery.GetControlLockOwner(locklmp.VesselId) == BDArmorySettings.MULTIPLAYER_OWNER_ID && !BDArmorySettings.MULTIPLAYER_VESSELS_OWNED.Contains(locklmp.VesselId))
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

            SuscribeToCoreEvents();
            
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
                case DamageEventArgs _:
                    Debug.Log("[BDArmory]: DamageEventArgs");
                    Dependencies.Get<IBdaMessageHandler<DamageEventArgs>>().ProcessMessage((DamageEventArgs) messageReceived.Content);
                    break;
                case ExplosionEventArgs _:
                    Dependencies.Get<IBdaMessageHandler<ExplosionEventArgs>>().ProcessMessage((ExplosionEventArgs)messageReceived.Content);
                    break;
                case VesselTeamChangeEventArgs _:
                    Dependencies.Get<IBdaMessageHandler<VesselTeamChangeEventArgs>>().ProcessMessage((VesselTeamChangeEventArgs)messageReceived.Content);
                    break;
                case ArmorEventArgs _:
                    Dependencies.Get<IBdaMessageHandler<ArmorEventArgs>>().ProcessMessage((ArmorEventArgs)messageReceived.Content);
                    break;
               
            }
        }

        private void SuscribeToCoreEvents()
        {
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
            }
            else
            {
                Debug.Log("[BDArmory]: LMP Multiplayer disabled");
                BDArmorySettings.MULTIPLAYER_ACTIVE = false;
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