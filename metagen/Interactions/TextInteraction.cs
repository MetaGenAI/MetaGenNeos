using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;

namespace metagen.Interactions
{
    public class TextInteraction : IInteraction
    {
        public bool isInteracting = false;
        public bool isRecording = false;
        private MetaGen metagen_comp;
        private WebsocketClient client;
        private Sync<string> output_field;
        private Sync<string> input_field;
        public TextInteraction(MetaGen component)
        {
            metagen_comp = component;
        }
        //private void AddEventHandlers(WebsocketClient client)
        //{
        //    UniLog.Log("Websocket connected");
        //    client.TextMessageReceived += OnTextMessageReceived;
        //    client.BinaryMessageReceived += OnBinaryMessageReceived;
        //}

        private void OnTextMessageReceived(WebsocketClient client, string msg)
        {
            //UniLog.Log("hi");
            UniLog.Log(msg);
            UniLog.Log(output_field.Value);
            metagen_comp.World.RunSynchronously(() =>
            {
                output_field.Value = msg;
            });
        }
        private void OnBinaryMessageReceived(WebsocketClient client, byte[] msg)
        {
            //UniLog.Log("ho");
            //UniLog.Log(msg);
        }
        private void OnInputFieldChanged(SyncField<string> msg)
        {
            metagen_comp.StartTask(async () => await client.Send(msg.Value));
        }
        public void StartInteracting()
        {
            UniLog.Log("Start Text interaction");
            UniLog.Log("Attaching component");
            client = metagen_comp.interaction_slot.GetComponent<WebsocketClient>();
            if (client == null)
            {
                client = metagen_comp.interaction_slot.AttachComponent<WebsocketClient>();
            }
            //client = metagen_comp.Slot.AttachComponent<WebsocketClient>();
            UniLog.Log("Attached component");
            //client.Connected += AddEventHandlers;
            client.TextMessageReceived += OnTextMessageReceived;
            client.BinaryMessageReceived += OnBinaryMessageReceived;
            //client.HandlingUser.Target = metagen_comp.LocalUser;
            client.HandlingUser.Target = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.LocalUser;
            client.URL.Value = new Uri("ws://localhost:8765");
            //client.URL.Value = new Uri("ws://localhost:8765");
            //client.TextMessageReceived += OnTextMessageReceived;
            //client.BinaryMessageReceived += OnBinaryMessageReceived;
            bool could_read_output_field = metagen_comp.interaction_space.TryReadValue<Sync<string>>("output field", out output_field);
            bool could_read_input_field = metagen_comp.interaction_space.TryReadValue<Sync<string>>("input field", out input_field);
            if (input_field != null) input_field.OnValueChange += new SyncFieldEvent<string>(OnInputFieldChanged);
            isInteracting = true;
        }
        public void StopInteracting()
        {
            UniLog.Log("Stop Text interaction");
            client.URL.Value = null;
            client.TextMessageReceived -= OnTextMessageReceived;
            client.BinaryMessageReceived -= OnBinaryMessageReceived;
            isInteracting = false;
        }
    }
}
