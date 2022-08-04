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
    public class BinaryDataRecorder
    {
        public Dictionary<string, BitBinaryWriterX> output_writers_base = new Dictionary<string, BitBinaryWriterX>();
        public Dictionary<string, BitBinaryReaderX> output_readers_base = new Dictionary<string, BitBinaryReaderX>();
        public Dictionary<string, FileStream> output_fss_base = new Dictionary<string, FileStream>();
        public Source source_type;
        protected MetaGen metagen_comp;
        public string saving_folder {
            get {
                return metagen_comp.dataManager.saving_folder;
                }
        }
        public BinaryDataRecorder(MetaGen component)
        {
            metagen_comp = component;
        }
        public Tuple<BitBinaryWriterX,BitBinaryReaderX> RegisterStream(string name)
        {
            BitWriterStream bitWriterStream = null;
            BitReaderStream bitReaderStream = null;
            BitBinaryWriterX bitBinaryWriter = null;
            BitBinaryReaderX bitBinaryReader = null;
            if (this.source_type == Source.FILE)
            {
                if (output_fss_base.ContainsKey(name)) throw new Exception("output_fss already contains the key when RegisterStream in BinaryDataRecorder");
                output_fss_base[name] = new FileStream(saving_folder + "/" + name + ".dat", FileMode.Create, FileAccess.ReadWrite);

                bitWriterStream = new BitWriterStream(output_fss_base[name]);
                bitBinaryWriter = new BitBinaryWriterX(bitWriterStream);
                if (output_writers_base.ContainsKey(name)) throw new Exception("output_writers already contains the key when RegisterStream in BinaryDataRecorder");
                output_writers_base[name] = bitBinaryWriter;
            }
            if (this.source_type == Source.STREAM)
            {
                MemoryStream memory_stream = new MemoryStream();

                bitWriterStream = new BitWriterStream(memory_stream);
                bitReaderStream = new BitReaderStream(memory_stream);
                bitBinaryWriter = new BitBinaryWriterX(bitWriterStream);
                bitBinaryReader = new BitBinaryReaderX(bitReaderStream);
                if (output_writers_base.ContainsKey(name)) throw new Exception("output_writers already contains the key when RegisterStream in BinaryDataRecorder");
                output_writers_base[name] = bitBinaryWriter;
                if (output_readers_base.ContainsKey(name)) throw new Exception("output_readers already contains the key when RegisterStream in BinaryDataRecorder");
                output_readers_base[name] = bitBinaryReader;
            }
            return new Tuple<BitBinaryWriterX,BitBinaryReaderX>(bitBinaryWriter, bitBinaryReader);
        }

        public void UnregisterStream(string name)
        {
            if (output_fss_base.ContainsKey(name))
            {
                output_fss_base[name].Flush();
                output_fss_base[name].Close();
                output_fss_base.Remove(name);
            }
            if (output_writers_base.ContainsKey(name))
            {
                output_writers_base[name].Flush();
                output_writers_base[name].Close();
                output_writers_base.Remove(name);
            }
            if (output_readers_base.ContainsKey(name))
            {
                output_readers_base[name].Close();
                output_readers_base.Remove(name);
            }
        }
        public void UnregisterAllStreams()
        {
            //foreach (var item in output_fss_base)
            //{
            //    item.Value.Flush();
            //    item.Value.Close();
            //}
            output_fss_base.Clear();
            foreach (var item in output_writers_base)
            {
                item.Value.Flush();
                item.Value.Close();
            }
            output_writers_base.Clear();
            foreach (var item in output_readers_base)
            {
                item.Value.Close();
            }
            output_readers_base.Clear();
        }
        public enum Source
        {
            FILE = 0,
            STREAM = 1,
        };
    }
}