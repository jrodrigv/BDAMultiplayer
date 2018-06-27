using System;
using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Multiplayer.Handler;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Message;
using BDArmory.Multiplayer.Utils;
using LunaClient.Systems.ModApi;
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

        public void RegisterSystem()
        {
            Dependencies.Register<IBdaMessageHandler<DamageEventArgs>, DamageMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<ExplosionEventArgs>, ExplosionMessageHandler>();
            Dependencies.Register<IBdaMessageHandler<ArmorEventArgs>, ArmorMessageHandler>();
           
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