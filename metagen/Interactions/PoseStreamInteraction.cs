using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseX;
using FrooxEngine;
using System.IO;
using Grpc.Core;
using Google.Protobuf;
using RefID = BaseX.RefID;

namespace metagen
{
    public class PoseStreamInteraction : IInteraction
    {
        MetaGen metagen_comp;
        WebsocketClient wsclient;
        public Dictionary<RefID, BitBinaryReaderX> pose_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public Dictionary<RefID, BinaryWriter> pose_writers = new Dictionary<RefID, BinaryWriter>();
        public bool isInteracting = false;
        private PoseInteraction.PoseInteractionClient client;
        private Channel channel;
        public PoseStreamInteraction(MetaGen component)
        {
            metagen_comp = component;
        }
        public void InteractPoseStreams(float deltaT)
        {
            if (channel.State != ChannelState.Ready) return;
            try
            {
                //UniLog.Log("InteractPoseStreams");
                foreach (var item in pose_writers)
                {
                    RefID refID = item.Key;
                    BinaryWriter writer = item.Value;
                    //read heading bytes from GRPC
                    //byte[] bs = null;
                    ByteString bs1 = client.GetFrameBytes(new RefIDMessage { RefId = refID.ToString() }).Data;
                    //writer.Write(bs1.ToByteArray());
                    //writer.BaseStream.Write(bs1.ToByteArray(),(int) writer.BaseStream.Position, bs1.Length);
                    UniLog.Log(writer.BaseStream.Position);
                    writer.Write(bs1.ToByteArray());
                    UniLog.Log(writer.BaseStream.Position);
                    UniLog.Log(bs1.Length);
                    writer.BaseStream.Position -= bs1.Length;
                    //writer.Write(frame_bs);
                }
                metagen_comp.streamPlayer.PlayStreams();
                metagen_comp.metaRecorder.streamRecorder.RecordStreams(deltaT);
                foreach (var item in pose_readers)
                {
                    RefID refID = item.Key;
                    BinaryReaderX reader = item.Value;
                    //send heading bytes to GRPC
                    byte[] byteArray = reader.ReadBytes((int) reader.BaseStream.Length);
                    Google.Protobuf.ByteString byteString = Google.Protobuf.ByteString.CopyFrom(byteArray, 0, byteArray.Length);
                    client.SendFrameBytes(new Frame { RefId = refID.ToString(), Data = byteString });
                }
            }
            catch (Exception e)
            {
                UniLog.Log("OwO: " + e.Message);
            }
        }
        
        private void WriteHeadings()
        {
            foreach (var item in pose_writers)
            {
                RefID refID = item.Key;
                BinaryWriter writer = item.Value;
                //read heading bytes from GRPC
                ByteString bs1 = client.GetHeadingBytes(new RefIDMessage { RefId = refID.ToString() }).Data;
                UniLog.Log(bs1.Length);
                //byte[] bs = new byte[bs1.Length];
                //bs1.CopyTo(bs, 0);
                //writer.BaseStream.Write(bs1.ToByteArray(),(int) writer.BaseStream.Position, bs1.Length);
                writer.Write(bs1.ToByteArray());
                writer.BaseStream.Position -= bs1.Length;
            }
        }
        private void ReadHeadings()
        {
            foreach (var item in pose_readers)
            {
                RefID refID = item.Key;
                BinaryReaderX reader = item.Value;
                //send heading bytes to GRPC
                byte[] byteArray = reader.ReadBytes((int) reader.BaseStream.Length);
                Google.Protobuf.ByteString byteString = Google.Protobuf.ByteString.CopyFrom(byteArray, 0, byteArray.Length);
                client.SendHeadingBytes(new Heading { RefId = refID.ToString(), Data = byteString });
            }
        }
        public void StartInteracting()
        {
            metagen_comp.StartTask(async ()=>this.StartInteractingInternal());
        }
        private void StartInteractingInternal() {
            try
            {
                UniLog.Log("Start pose stream interaction");
                channel = new Channel("127.0.0.1:" + (40052).ToString(), ChannelCredentials.Insecure);
                UniLog.Log("Started grpc channel");
                client = new PoseInteraction.PoseInteractionClient(channel);
                //wsclient = metagen_comp.Slot.AttachComponent<WebsocketClient>();
                //wsclient.URL.Value = new Uri("http://127.0.0.1:" + (40052).ToString());
                //Acting
                metagen_comp.streamPlayer.PrepareStreamsExternal();
                //RefID user_id = RefID.Parse("IDC00");
                foreach (var item in metagen_comp.streamPlayer.output_writers)
                {
                    RefID refID = item.Key;
                    pose_writers[refID] = item.Value;
                }
                WriteHeadings();
                metagen_comp.streamPlayer.play_hearing = false;
                metagen_comp.streamPlayer.play_voice = false;
                metagen_comp.streamPlayer.StartPlayingExternal();
                metagen_comp.metaRecorder.streamRecorder.StartRecordingExternal();
                foreach (var item in metagen_comp.metaRecorder.streamRecorder.output_readers)
                {
                    RefID refID = item.Key;
                    //BinaryWriterX writer = item.Value;
                    //BitReaderStream binaryReader = new BitReaderStream(writer.BaseStream);
                    pose_readers[refID] = item.Value;
                }
                ReadHeadings();
                UniLog.Log("Finished starting PoseStreamInteraction");
                isInteracting = true;
            }
            catch (Exception e)
            {
                UniLog.Log("TwT: " + e);
            }
        }
        public void StopInteracting()
        {
            isInteracting = false;
            metagen_comp.streamPlayer.StopPlaying();
            metagen_comp.metaRecorder.streamRecorder.StopRecording();
        }

        //void WaitForFinish();
    }
}
