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

namespace metagen
{
    class FaceStreamRecorder : IRecorder
    {
        public Dictionary<RefID, BitBinaryWriterX> eye_output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, BitBinaryWriterX> mouth_output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, FileStream> eye_output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, FileStream> mouth_output_fss = new Dictionary<RefID, FileStream>();
        public Dictionary<RefID, EyeTrackingStreamManager> eye_streams = new Dictionary<RefID, EyeTrackingStreamManager>();
        public Dictionary<RefID, MouthTrackingStreamManager> mouth_streams = new Dictionary<RefID, MouthTrackingStreamManager>();
        public List<RefID> current_eye_tracking_users = new List<RefID>();
        public List<RefID> current_mouth_tracking_users = new List<RefID>();
        public bool isRecording = false;
        private MetaGen metagen_comp;
        public string saving_folder {
            get {
                return metagen_comp.dataManager.saving_folder;
                }
        }
        public FaceStreamRecorder(MetaGen component)
        {
            metagen_comp = component;
        }

        void WriteEyeStreams(EyeTrackingStreamManager.EyeStreams eyeStreams, BinaryWriterX writer)
        {
            writer.Write(eyeStreams.IsTracking.Target.Value); //bool
            writer.Write(eyeStreams.Position.Target.Value.x); //float
            writer.Write(eyeStreams.Position.Target.Value.y); //float
            writer.Write(eyeStreams.Position.Target.Value.z); //float
            writer.Write(eyeStreams.Direction.Target.Value.x); //float
            writer.Write(eyeStreams.Direction.Target.Value.y); //float
            writer.Write(eyeStreams.Direction.Target.Value.z); //float
            writer.Write(eyeStreams.Openness.Target.Value); //float
            writer.Write(eyeStreams.Widen.Target.Value); //float
            writer.Write(eyeStreams.Squeeze.Target.Value); //float
            writer.Write(eyeStreams.Frown.Target.Value); //float
            writer.Write(eyeStreams.PupilDiameter.Target.Value); //float
        }
        void WriteMouthStreams(MouthTrackingStreamManager mouthStreams, BinaryWriterX writer)
        {
            writer.Write(mouthStreams.IsTracking.Target.Value); //bool
            writer.Write(mouthStreams.Jaw.Target.Value.x); //float
            writer.Write(mouthStreams.Jaw.Target.Value.y); //float
            writer.Write(mouthStreams.Jaw.Target.Value.z); //float
            writer.Write(mouthStreams.JawOpen.Target.Value); //float
            writer.Write(mouthStreams.Tongue.Target.Value.x); //float
            writer.Write(mouthStreams.Tongue.Target.Value.y); //float
            writer.Write(mouthStreams.Tongue.Target.Value.z); //float
            writer.Write(mouthStreams.TongueRoll.Target.Value); //float
            writer.Write(mouthStreams.LipUpperLeftRaise.Target.Value); //float
            writer.Write(mouthStreams.LipUpperRightRaise.Target.Value); //float
            writer.Write(mouthStreams.LipLowerLeftaise.Target.Value); //float
            writer.Write(mouthStreams.LipLowerRightRaise.Target.Value); //float
            writer.Write(mouthStreams.LipUpperHorizontal.Target.Value); //float
            writer.Write(mouthStreams.LipLowerHorizontal.Target.Value); //float
            writer.Write(mouthStreams.MouthLeftSmileFrown.Target.Value); //float
            writer.Write(mouthStreams.MouthRightSmileFrown.Target.Value); //float
            writer.Write(mouthStreams.MouthPout.Target.Value); //float
            writer.Write(mouthStreams.LipTopOverturn.Target.Value); //float
            writer.Write(mouthStreams.LipBottomOverturn.Target.Value); //float
            writer.Write(mouthStreams.LipTopOverUnder.Target.Value); //float
            writer.Write(mouthStreams.LipBottomOverUnder.Target.Value); //float
            writer.Write(mouthStreams.CheekLeftPuffSuck.Target.Value); //float
            writer.Write(mouthStreams.CheekRightPuffSuck.Target.Value); //float
        }
        public void RecordStreams(float deltaT)
        {
            foreach (RefID user_id in current_eye_tracking_users)
            {
                //Encode the streams
                BinaryWriterX writer = eye_output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float

                EyeTrackingStreamManager eyeStreams = eye_streams[user_id];
                //WRITE left eye streams
                WriteEyeStreams(eyeStreams.LeftEyeStreams, writer);
                //WRITE right eye streams
                WriteEyeStreams(eyeStreams.RightEyeStreams, writer);

            }

            foreach (RefID user_id in current_mouth_tracking_users)
            {
                //Encode the streams
                BinaryWriterX writer = mouth_output_writers[user_id];

                //WRITE deltaT
                writer.Write(deltaT); //float

                MouthTrackingStreamManager mouthStreams = mouth_streams[user_id];
                //WRITE mouth streams
                WriteMouthStreams(mouthStreams, writer);
            }

        }
        public void StartRecording()
        {
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                if (!(metadata.isRecording || metagen_comp.record_everyone) || !metagen_comp.record_local_user && user == metagen_comp.World.LocalUser) continue;
                RefID user_id = user.ReferenceID;
                bool has_eye_tracking = user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Any<SyncVar>((Func<SyncVar, bool>)(i => i["Type"].GetValue<string>(true) == "Eye Tracking"));
                bool has_mouth_tracking = user.Devices.Where<SyncVar>((Func<SyncVar, bool>)(i => i.IsDictionary)).Any<SyncVar>((Func<SyncVar, bool>)(i => i["Type"].GetValue<string>(true) == "Lip Tracking"));

                if (has_eye_tracking) {
                    eye_output_fss[user_id] = new FileStream(saving_folder + "/" + user_id.ToString() + "_eye_streams.dat", FileMode.Create, FileAccess.ReadWrite);

                    BitWriterStream bitstream = new BitWriterStream(eye_output_fss[user_id]);
                    eye_output_writers[user_id] = new BitBinaryWriterX(bitstream);
                    eye_streams[user_id] = user.Root.Slot.GetComponent<EyeTrackingStreamManager>();
                    //WRITE the absolute time
                    eye_output_writers[user_id].Write((float)DateTimeOffset.Now.ToUnixTimeMilliseconds()); //absolute time
                    current_eye_tracking_users.Add(user_id);
                }

                if (has_mouth_tracking) {
                    mouth_output_fss[user_id] = new FileStream(saving_folder + "/" + user_id.ToString() + "_mouth_streams.dat", FileMode.Create, FileAccess.ReadWrite);

                    BitWriterStream bitstream = new BitWriterStream(mouth_output_fss[user_id]);
                    mouth_output_writers[user_id] = new BitBinaryWriterX(bitstream);
                    mouth_streams[user_id] = user.Root.Slot.GetComponent<MouthTrackingStreamManager>();
                    //WRITE the absolute time
                    mouth_output_writers[user_id].Write((float)DateTimeOffset.Now.ToUnixTimeMilliseconds()); //absolute time
                    current_mouth_tracking_users.Add(user_id);
                }
            }
            isRecording = true;
        }
        public void StopRecording()
        {
            foreach (var item in eye_output_writers)
            {
                item.Value.Flush();
            }
            foreach (var item in mouth_output_writers)
            {
                item.Value.Flush();
            }
            foreach (var item in eye_output_fss)
            {
                item.Value.Close();
            }
            foreach (var item in mouth_output_fss)
            {
                item.Value.Close();
            }
            eye_output_writers = new Dictionary<RefID, BitBinaryWriterX>();
            mouth_output_writers = new Dictionary<RefID, BitBinaryWriterX>();
            eye_output_fss = new Dictionary<RefID, FileStream>();
            mouth_output_fss = new Dictionary<RefID, FileStream>();
            current_eye_tracking_users = new List<RefID>();
            current_mouth_tracking_users = new List<RefID>();
            isRecording = false;
        }
        public void WaitForFinish()
        {

        }
    }
}
