using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections.Concurrent;
using BaseX;
// Contains common types for AVI format like FourCC
using SharpAvi;
// Contains types used for writing like AviWriter
using SharpAvi.Output;
// Contains types related to encoding like Mpeg4VideoEncoderVcm
using SharpAvi.Codecs;
using System.Runtime.InteropServices;
using System.Drawing;

namespace metagen
{
    class VisualRecorder
    {
        public AviWriter writer;
        public int width;
        public int height;
        public int frameRate;
        public String fileName;
        private Task writtingTask;
        private ConcurrentQueue<byte[]> framesQueue;
        private bool should_finish = false;
        private bool loop_finished = false;
        public VisualRecorder(String fileName, int width, int height, int frameRate)
        {
            this.width = width;
            this.height = height;
            this.frameRate = frameRate;
            this.fileName = fileName;
            framesQueue = new ConcurrentQueue<byte[]>();
            UniLog.Log("Starting writerloop");
            writtingTask = Task.Run(FileWriterLoop);
        }
        public void FileWriterLoop()
        {
            writer = new AviWriter("test.avi")
            {
                FramesPerSecond = frameRate,
                // Emitting AVI v1 index in addition to OpenDML index (AVI v2)
                // improves compatibility with some software, including 
                // standard Windows programs like Media Player and File Explorer
                EmitIndex1 = true
            };
            var stream = writer.AddVideoStream();
            stream.Height = height;
            stream.Width = width;
            stream.Codec = KnownFourCCs.Codecs.Uncompressed;
            stream.BitsPerPixel = BitsPerPixel.Bpp32;
            byte[] frame;
            while (true)
            {
                //UniLog.Log("writerloop");
                if (framesQueue.TryDequeue(out frame))
                {
                    //Bitmap bmp = ToBitmap(frame);
                    //Bitmap bmpReduced = ReduceBitmap(bmp, width, height);
                    //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    //var bits = bitmap.LockBits(new Rectangle(0, 0, width,height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    //Marshal.Copy(frame, 0, bits.Scan0, frame.Length);
                    //bitmap.UnlockBits(bits);
                    //System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    //using (var gr = Graphics.FromImage(bmp))
                    //    gr.DrawImage(bitmap, new Rectangle(0, 0, width, height));
                    //var buffer = new byte[width * height * 4];//(widght - _ - height - _ - 4);
                    //var bits2 = bmp.LockBits(new Rectangle(0, 0, stream.Width, stream.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    //Marshal.Copy(bits2.Scan0, buffer, 0, buffer.Length);
                    //bitmap.UnlockBits(bits2);
                    stream.WriteFrame(true,frame,0,frame.Length);
                }
                if (should_finish)
                {
                    writer.Close();
                    break;
                }
            }
            loop_finished = true;
        }
        public void WriteFrame(byte[] frame)
        {
            framesQueue.Enqueue(frame);
        }
        public void Close()
        {
            should_finish = true;
            while (!loop_finished) { }
            writtingTask.Dispose();
        }
        public Bitmap ToBitmap(byte[] byteArrayIn)
        {
            var ms = new System.IO.MemoryStream(byteArrayIn);
            var returnImage = System.Drawing.Image.FromStream(ms);
            var bitmap = new System.Drawing.Bitmap(returnImage);

            return bitmap;
        }

        public Bitmap ReduceBitmap(Bitmap original, int reducedWidth, int reducedHeight)
        {
            var reduced = new Bitmap(reducedWidth, reducedHeight);
            using (var dc = Graphics.FromImage(reduced))
            {
                // you might want to change properties like
                dc.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                dc.DrawImage(original, new Rectangle(0, 0, reducedWidth, reducedHeight), new Rectangle(0, 0, original.Width, original.Height), GraphicsUnit.Pixel);
            }

            return reduced;
        }
    }
}
