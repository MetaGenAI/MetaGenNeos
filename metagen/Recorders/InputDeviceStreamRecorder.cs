using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using BaseX;
using System.IO;
using FrooxEngine.CommonAvatar;
using Stream = FrooxEngine.Stream;
using RefID = BaseX.RefID;
using Newtonsoft.Json;

namespace metagen
{
    public class InputDeviceStreamRecorder : UserBinaryDataRecorder, IRecorder
    {
        public Dictionary<RefID, InputDeviceStreamDriver> input_streams = new Dictionary<RefID, InputDeviceStreamDriver>();
        public List<RefID> current_tracked_users = new List<RefID>();
        public bool isRecording = false;
        //public Dictionary<RefID, List<Tuple<string, int>>> property_indices = new Dictionary<RefID, List<Tuple<string, int>>>();
        public Dictionary<RefID, List<InputDeviceMetadata>> inputDeviceMetadatas = new Dictionary<RefID, List<InputDeviceMetadata>>();
        public Dictionary<RefID, List<ValueStream<bool>>> digitalStreams = new Dictionary<RefID, List<ValueStream<bool>>>();
        public Dictionary<RefID, List<ValueStream<float>>> analogStreams = new Dictionary<RefID, List<ValueStream<float>>>();
        public Dictionary<RefID, List<ValueStream<float2>>> analog2DStreams = new Dictionary<RefID, List<ValueStream<float2>>>();
        public Dictionary<RefID, List<ValueStream<float3>>> analog3DStreams = new Dictionary<RefID, List<ValueStream<float3>>>();
        public InputDeviceStreamRecorder(MetaGen component) : base(component)
        {
            metagen_comp = component;
        }

        public void RecordStreams(float deltaT)
        {
            foreach (RefID user_id in current_tracked_users)
            {
                //Encode the streams
                BinaryWriterX writer = output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float

                //WRITE DIGITAL STREAMS
                foreach( ValueStream<bool> stream in digitalStreams[user_id])
                {
                    writer.Write(stream.Value);
                }

                //WRITE ANALOG STREAMS
                foreach( ValueStream<float> stream in analogStreams[user_id])
                {
                    writer.Write(stream.Value);
                }

                //WRITE ANALOG2D STREAMS
                foreach( ValueStream<float2> stream in analog2DStreams[user_id])
                {
                    writer.Write(stream.Value);
                }

                //WRITE ANALOG3D STREAMS
                foreach( ValueStream<float3> stream in analog3DStreams[user_id])
                {
                    writer.Write(stream.Value);
                }
            }
        }
        private void WriteInputDeviceMetadataFiles()
        {
            foreach (RefID user_id in current_tracked_users)
            {
                string json = JsonConvert.SerializeObject(inputDeviceMetadatas[user_id]);
                System.IO.File.WriteAllText(saving_folder + "/" + user_id.ToString() + "_input_device_metadata.txt", json);
            }
        }
        public void StartRecording()
        {
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                //if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                if (!metadata.isRecording) continue;
                RefID user_id = user.ReferenceID;

                InputDeviceStreamDriver inputDeviceStreamDriver = InputDeviceStreamDriverExtensions.GetInputDeviceStreamDriver(user);
                if (inputDeviceStreamDriver != null)
                {
                    RegisterUserStream(user_id, "input_streams");
                    input_streams[user_id] = inputDeviceStreamDriver;
                    current_tracked_users.Add(user_id);
                    digitalStreams[user_id] = new List<ValueStream<bool>>();
                    analogStreams[user_id] = new List<ValueStream<float>>();
                    analog2DStreams[user_id] = new List<ValueStream<float2>>();
                    analog3DStreams[user_id] = new List<ValueStream<float3>>();
                    //IStandardController controller = metagen_comp.InputInterface.GetDevice<IStandardController>(d => d.Side == Chirality.Left);
                    foreach (InputDeviceStreamDriver.Device device in inputDeviceStreamDriver.Devices)
                    {
                        //InputDevice inputDevice = metagen_comp.InputInterface.GetDevice<InputDevice>(d => d.DeviceIndex == device.DeviceIndex);
                        //IStandardController controller = inputDevice as IStandardController;
                        //Nullable<Chirality> side = null;
                        //if (controller != null)
                        //{
                        //    side = controller.Side;
                        //}
                        List<Tuple<string, int>> propertyIndices = new List<Tuple<string, int>>();
                        foreach (InputDeviceStreamDriver.Driver<bool> digitalDriver in device.Digital_Drivers)
                        {
                            int index = digitalDriver.PropertyIndex.Value;
                            ValueStream<bool> stream = digitalDriver.Stream.Target;
                            //ControllerProperty property = inputDevice.GetProperty<Digital>(index);
                            //string name = property.Name;
                            string name = stream.Name;
                            propertyIndices.Add(new Tuple<string, int>(name,index));
                            digitalStreams[user_id].Add(stream);
                        }
                        foreach (InputDeviceStreamDriver.Driver<float> analogDriver in device.Analog_Drivers)
                        {
                            int index = analogDriver.PropertyIndex.Value;
                            ValueStream<float> stream = analogDriver.Stream.Target;
                            //ControllerProperty property = inputDevice.GetProperty<Digital>(index);
                            //string name = property.Name;
                            string name = stream.Name;
                            propertyIndices.Add(new Tuple<string, int>(name,index));
                            analogStreams[user_id].Add(stream);
                        }
                        foreach (InputDeviceStreamDriver.Driver<float2> analog2DDriver in device.Analog2D_Drivers)
                        {
                            int index = analog2DDriver.PropertyIndex.Value;
                            ValueStream<float2> stream = analog2DDriver.Stream.Target;
                            //ControllerProperty property = inputDevice.GetProperty<Digital>(index);
                            //string name = property.Name;
                            string name = stream.Name;
                            propertyIndices.Add(new Tuple<string, int>(name,index));
                            analog2DStreams[user_id].Add(stream);
                        }
                        foreach (InputDeviceStreamDriver.Driver<float3> analog3DDriver in device.Analog3D_Drivers)
                        {
                            int index = analog3DDriver.PropertyIndex.Value;
                            ValueStream<float3> stream = analog3DDriver.Stream.Target;
                            //ControllerProperty property = inputDevice.GetProperty<Digital>(index);
                            //string name = property.Name;
                            string name = stream.Name;
                            propertyIndices.Add(new Tuple<string, int>(name,index));
                            analog3DStreams[user_id].Add(stream);
                        }
                        inputDeviceMetadatas[user_id].Add(new InputDeviceMetadata()
                        {
                            //Name = inputDevice.Name,
                            DeviceIndex = device.DeviceIndex,
                            //Type = inputDevice.GetType(),
                            //Side = side,
                            PropertyIndices = propertyIndices
                        });
                    }

                    //WRITE number of input devices
                    output_writers[user_id].Write(inputDeviceMetadatas[user_id].Count);

                    foreach (InputDeviceMetadata inputDeviceMetadata in inputDeviceMetadatas[user_id])
                    {
                        //WRITE device index
                        output_writers[user_id].Write(inputDeviceMetadata.DeviceIndex);
                        //WRITE number of properties for device
                        output_writers[user_id].Write(inputDeviceMetadata.PropertyIndices.Count);
                        foreach (Tuple<string, int> tuple in inputDeviceMetadata.PropertyIndices)
                        {
                            //WRITE property index
                            int index = tuple.Item2;
                            output_writers[user_id].Write(index);
                        }
                    }

                    //CommonToolStreamDriver...

                }

            }
            isRecording = true;
        }
        public void StopRecording()
        {
            UnregisterUserStreams();
            current_tracked_users = new List<RefID>();
            input_streams = new Dictionary<RefID, InputDeviceStreamDriver>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
    public class InputDeviceMetadata
    {
        //public string Name;
        public int DeviceIndex;
        //public Type Type;
        //public Nullable<Chirality> Side;
        public List<Tuple<string, int>> PropertyIndices;

        public InputDeviceMetadata() { }
    }
}
