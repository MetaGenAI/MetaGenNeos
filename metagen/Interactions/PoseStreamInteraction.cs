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
        public Dictionary<RefID, BitBinaryWriterX> pose_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public bool isInteracting = false;
        private PoseInteraction.PoseInteractionClient client;
        private Channel channel;
        public PoseStreamInteraction(MetaGen component)
        {
            metagen_comp = component;
        }
        public void InteractPoseStreams(float deltaT)
        {
            foreach (var item in pose_writers)
            {
                RefID refID = item.Key;
                BinaryWriterX writer = item.Value;
                //read heading bytes from GRPC
                byte[] bs = null;
                client.GetFrameBytes(new RefIDMessage { RefId = refID.ToString() }).Data.CopyTo(bs, 0);
                writer.Write(bs);
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
        private void WriteHeadings()
        {
            foreach (var item in pose_writers)
            {
                RefID refID = item.Key;
                BinaryWriterX writer = item.Value;
                //read heading bytes from GRPC
                byte[] bs = null;
                client.GetHeadingBytes(new RefIDMessage { RefId = refID.ToString() }).Data.CopyTo(bs, 0);
                writer.Write(bs);
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
            UniLog.Log("Start pose stream interaction");
            channel = new Channel("127.0.0.1:" + (40052).ToString(), ChannelCredentials.Insecure);
            UniLog.Log("Started grpc channel");
            client = new PoseInteraction.PoseInteractionClient(channel);
            //wsclient = metagen_comp.Slot.AttachComponent<WebsocketClient>();
            //wsclient.URL.Value = new Uri("http://127.0.0.1:" + (40052).ToString());
            //Acting
            metagen_comp.streamPlayer.PrepareStreamsExternal();
            //RefID user_id = RefID.Parse("IDC00");
            foreach (var item in metagen_comp.streamPlayer.output_readers)
            {
                RefID refID = item.Key;
                BinaryReaderX reader = item.Value;
                BitWriterStream binaryWriter = new BitWriterStream(reader.TargetStream);
                pose_writers[refID] = new BitBinaryWriterX(binaryWriter);
            }
            WriteHeadings();
            metagen_comp.streamPlayer.StartPlayingExternal();
            metagen_comp.metaRecorder.streamRecorder.StartRecordingExternal();
            foreach (var item in metagen_comp.metaRecorder.streamRecorder.output_writers)
            {
                RefID refID = item.Key;
                BinaryWriterX writer = item.Value;
                BitReaderStream binaryReader = new BitReaderStream(writer.TargetStream);
                pose_readers[refID] = new BitBinaryReaderX(binaryReader);
            }
            ReadHeadings();
            isInteracting = true;
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
