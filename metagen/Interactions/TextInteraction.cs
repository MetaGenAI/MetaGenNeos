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
        public TextInteraction(MetaGen component)
        {
            metagen_comp = component;
            client = metagen_comp.Slot.AttachComponent<WebsocketClient>();
            client.HandlingUser.Target = metagen_comp.LocalUser;
            client.URL.Value = new Uri("ws://localhost:8765");
        }

        private void OnTextMessageReceived(WebsocketClient client, string msg)
        {
            UniLog.Log(msg);
        }
        public void StartInteracting()
        {
            client.URL.Value = new Uri("ws://localhost:8765");
            client.TextMessageReceived += OnTextMessageReceived;
        }
        public void StopInteracting()
        {
            client.URL.Value = null;
            client.TextMessageReceived -= OnTextMessageReceived;
        }
    }
}
