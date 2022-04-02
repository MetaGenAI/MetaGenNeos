using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;

namespace metagen.Interactions
{
    public class FieldInteraction : IInteraction
    {
        public bool isInteracting = false;
        public bool isRecording = false;
        private MetaGen metagen_comp;
        private WebsocketClient client;
        private Dictionary<string,IField> input_fields = new Dictionary<string,IField>();
        private Dictionary<string,IField> output_fields = new Dictionary<string,IField>();
        public TextInteraction(MetaGen component)
        {
            metagen_comp = component;
        }

        private void OnTextMessageReceived(WebsocketClient client, string msg)
        {
            //UniLog.Log("hi");
            UniLog.Log(msg);
            // UniLog.Log(output_field.Value);
            string name = msg.split("/")[0];
            string value_str = msg.Substring(name.Length);
            metagen_comp.World.RunSynchronously(() =>
            {
                if (output_field.ContainsKey(name) && output_field[name] != null)
                    output_field[name].Value = ParseString(value_str);
            });
        }
        private void OnBinaryMessageReceived(WebsocketClient client, byte[] msg)
        {
            //UniLog.Log("ho");
            //UniLog.Log(msg);
        }
        private void OnInputFieldChanged(string name, SyncField<IField> field)
        {
            metagen_comp.StartTask(async () => await client.Send(name+"/"+field.Value.ToString()));
        }
        public void StartInteracting()
        {
            UniLog.Log("Start Field interaction");
            UniLog.Log("Attaching component");
            client = metagen_comp.interaction_slot.GetComponent<WebsocketClient>();
            if (client == null)
            {
                client = metagen_comp.interaction_slot.AttachComponent<WebsocketClient>();
            }

            UniLog.Log("Attached component");
            client.TextMessageReceived += OnTextMessageReceived;
            client.BinaryMessageReceived += OnBinaryMessageReceived;

            client.HandlingUser.Target = FrooxEngine.Engine.Current.WorldManager.FocusedWorld.LocalUser;
            client.URL.Value = new Uri("ws://localhost:8766");

            //Input fields
            Slot input_field_holders = metagen_comp.input_interaction_fields_slot;
            List<Slot> inputFieldsHolders = input_field_holders?.GetAllChildren();
            if (inputFieldsHolders != null)
            {
                foreach(Slot s in inputFieldsHolders)
                {
                    List<ReferenceField<IField>> referenceSources = s.GetComponentsInChildren<ReferenceField<IField>>();

                foreach(ReferenceField<IField> referenceSource in referenceSources)
                    {
                        IField field = referenceSource.Reference.Target;
                        string name = referenceSource.Slot.Name;
                        if (field != null) field.OnValueChange += new SyncFieldEvent<string>((SyncField<IField> field)=> OnInputFieldChanged(name, field));
                        input_fields[name] = field;
                    }
                }
            }

            //Output fields
            Slot output_field_holders = metagen_comp.output_interaction_fields_slot;
            List<Slot> outputFieldsHolders = output_field_holders?.GetAllChildren();
            if (outputFieldsHolders != null)
            {
                foreach(Slot s in outputFieldsHolders)
                {
                    List<ReferenceField<IField>> referenceSources = s.GetComponentsInChildren<ReferenceField<IField>>();

                foreach(ReferenceField<IField> referenceSource in referenceSources)
                    {
                        IField field = referenceSource.Reference.Target;
                        string name = referenceSource.Slot.Name;
                        output_fields[name] = field;
                    }
                }
            }
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
