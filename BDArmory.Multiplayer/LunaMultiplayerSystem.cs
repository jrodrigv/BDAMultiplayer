using System;
using BDArmory.Core;
using BDArmory.Events;
using BDArmory.Multiplayer.Handler;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Message;
using BDArmory.Multiplayer.Utils;
using LunaClient.Systems;
using LunaClient.Systems.ModApi;
using UnityEngine;

namespace BDArmory.Multiplayer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LunaMultiplayerSystem : MonoBehaviour, IMultiplayerSystem
    {
        private const string ModName = "BDArmory";
        public const bool Relay = true;

        private void Awake()
        {
          RegisterSystem();
        }

        public void RegisterSystem()
        {
            try
            {
                Dependencies.Register<IBdaMessageHandler<DamageEventArgs>, DamageMessageHandler>();
                Dependencies.Register<IBdaMessageHandler<ExplosionEventArgs>, ExplosionMessageHandler>();
                Dependencies.Register<IBdaMessageHandler<ArmorEventArgs>, ArmorMessageHandler>();


                SystemsContainer.Get<ModApiSystem>().RegisterFixedUpdateModHandler(ModName, HandlerFunction);
                SuscribeToCoreEvents();

                Debug.Log("[BDArmory]: LMP Multiplayer found");     
            }
            catch (Exception ex)
            {
                Debug.Log("[BDArmory]: LMP Multiplayer is not installed");
                Debug.LogError(ex);
            }
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
            Dependencies.Get<DamageEventService>().OnActionExecuted += OnActionExecuted;
            Dependencies.Get<ExplosionEventService>().OnActionExecuted += OnActionExecuted;
            Dependencies.Get<ArmorEventService>().OnActionExecuted += OnActionExecuted;
        }

        private void OnActionExecuted(object sender, EventArgs eventArgs)
        {
            SendMessage(eventArgs);
        }

       
        public void SendMessage(EventArgs message)
        {
            BdaMessage messageToSend = new BdaMessage() {Type = message.GetType(), Content = message};

            SystemsContainer.Get<ModApiSystem>().SendModMessage(ModName, BinaryUtils.Serialize(messageToSend), true);
        }

        void OnDestroy()
        {
            
        }
    }
}