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

namespace metagen
{
    public class UserBinaryDataRecorder : BinaryDataRecorder
    {
        public Dictionary<RefID, BitBinaryWriterX> output_writers = new Dictionary<RefID, BitBinaryWriterX>();
        public Dictionary<RefID, BitBinaryReaderX> output_readers = new Dictionary<RefID, BitBinaryReaderX>();
        public UserBinaryDataRecorder(MetaGen component) : base(component)
        {
            metagen_comp = component;
        }

        public void RegisterUserStream(RefID user_id, string name)
        {
            Tuple<BitBinaryWriterX,BitBinaryReaderX> streams = RegisterStream(user_id.ToString() + "_" + name);
            BitBinaryWriterX bitBinaryWriter = streams.Item1;
            BitBinaryReaderX bitBinaryReader = streams.Item2;
            if (bitBinaryWriter != null)
            {
                output_writers[user_id] = bitBinaryWriter;
            }
            if (bitBinaryReader != null)
            {
                output_readers[user_id] = bitBinaryReader;
            }
        }

        public void RegisterUserStreams(string name)
        {
            foreach (var userItem in metagen_comp.userMetaData)
            {
                User user = userItem.Key;
                UserMetadata metadata = userItem.Value;
                if (!metadata.isRecording || (metagen_comp.LocalUser == user && !metagen_comp.record_local_user)) continue;
                RefID user_id = user.ReferenceID;
                RegisterUserStream(user_id, name);
            }
        }

        public void UnregisterUserStreams()
        {
            UnregisterAllStreams();
            output_writers = new Dictionary<RefID, BitBinaryWriterX>();
            output_readers = new Dictionary<RefID, BitBinaryReaderX>();
        }
    }
}