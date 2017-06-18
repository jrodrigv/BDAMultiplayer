using System;
using BDArmory.Core;
using BDArmory.Core.Events;
using BDArmory.Core.Services;
using BDArmory.Multiplayer.Interface;
using BDArmory.Multiplayer.Message;
using BDArmory.Multiplayer.Utils;
using LunaClient.Systems;
using LunaClient.Systems.ModApi;
using UnityEngine;

namespace BDArmory.Multiplayer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LunaMultiplayerSytem : MonoBehaviour, IMultiplayerSystem
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
            if (messageReceived.Content is DamageEventArgs)
            {
               Dependencies.Get<IBdaMessageHandler<DamageEventArgs>>().ProcessMessage((DamageEventArgs) messageReceived.Content);
            }
        }

        private void SuscribeToCoreEvents()
        {
           Dependencies.Get<DamageService>().OnActionExecuted += OnActionExecuted;
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

    internal interface IBdaMessageHandler<in T> where T : class, new()
    {
        void ProcessMessage(T message);
    }
}