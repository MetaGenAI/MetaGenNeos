using System;
using System.Collections.Generic;
using System.Text;
using Accord.Video.FFMPEG;
using System.Drawing;

namespace metagen
{
    class VisualRecorder
    {
        public VideoFileWriter writer;
        public int width;
        public int height;
        public int frameRate;
        public VisualRecorder(String fileName, int width, int height, int frameRate)
        {
            writer = new VideoFileWriter();
            this.width = width;
            this.height = height;
            this.frameRate = frameRate;
            writer.Open(fileName, width, height, frameRate, VideoCodec.Raw);
        }
        public void WriteFrame(byte[] frame)
        {
            var bmp = ToBitmap(frame);
            Bitmap bmpReduced = ReduceBitmap(bmp, width, height);

            writer.WriteVideoFrame(bmpReduced);
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
