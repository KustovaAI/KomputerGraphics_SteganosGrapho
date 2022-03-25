using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;


namespace SteganosGrapho
{
    abstract class ProcessImg
    {
        protected ProcessBytes process_bytes;
        public virtual Bitmap get_image_r() { return null; }
        public virtual Bitmap get_image_g() { return null; }
        public virtual Bitmap get_image_b() { return null; }
        public virtual Bitmap get_image_raw() { return null; }
        public virtual Bitmap get_image_noised() { return null; }
        public virtual Bitmap get_image_LSB() { return null; }
        public virtual Bitmap get_image_diff() { return null; }
        protected virtual Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            return sourceImage.GetPixel(x, y);
        }
        protected virtual Color calculateNewPixelColor(Bitmap currImage,
        Bitmap originImage, int x, int y)
        {
            Color currColor = currImage.GetPixel(x, y);
            return Color.FromArgb(currColor.R, currColor.G, currColor.B);
        }
        public virtual Bitmap processImage(Bitmap currImage, Bitmap originImage,
        BackgroundWorker worker)
        {
            return currImage;
        }
        public virtual Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            return sourceImage;
        }
        public virtual void processImage(Bitmap sourceImage,
        ref BackgroundWorker worker, bool no_return_value)
        {
            return;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    class FormChannels : ProcessImg
    {
        protected Bitmap image_r;
        protected Bitmap image_g;
        protected Bitmap image_b;
        public override Bitmap get_image_r() { return image_r; }
        public override Bitmap get_image_g() { return image_g; }
        public override Bitmap get_image_b() { return image_b; }
        public override void processImage(Bitmap sourceImage,
        ref BackgroundWorker worker, bool no_return_value)
        {
            ProcessImg prc_im_r = new FormChannelR();
            image_r = prc_im_r.processImage(sourceImage, ref worker);
            ProcessImg prc_im_g = new FormChannelG();
            image_g = prc_im_g.processImage(sourceImage, ref worker);
            ProcessImg prc_im_b = new FormChannelB();
            image_b = prc_im_b.processImage(sourceImage, ref worker);
        }
    }

    class FormChannelX : ProcessImg
    {
        public Bitmap processImage(Bitmap sourceImage, ref BackgroundWorker worker,
        int mult, int summand)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress(Clamp((int)((float)i / resultImage.Width
                * mult + summand), 0, 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage,
                    i, j));
                }
            }
            return resultImage;
        }
    }

    class FormChannelR : FormChannelX
    {
        public override Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            return processImage(sourceImage, ref worker, 33, 0);
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(sourceColor.R, sourceColor.R, sourceColor.R);
        }
    }

    class FormChannelG : FormChannelX
    {
        public override Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            return processImage(sourceImage, ref worker, 33, 33);
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(sourceColor.G, sourceColor.G, sourceColor.G);
        }
    }

    class FormChannelB : FormChannelX
    {
        public override Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            return processImage(sourceImage, ref worker, 33, 66);
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(sourceColor.B, sourceColor.B, sourceColor.B);
        }
    }

    class FormChannelsInverted : FormChannels
    {
        public override void processImage(Bitmap sourceImage,
        ref BackgroundWorker worker, bool no_return_value)
        {
            ProcessImg prc_im_r = new FormChannelR_Inverted();
            image_r = prc_im_r.processImage(sourceImage, ref worker);
            ProcessImg prc_im_g = new FormChannelG_Inverted();
            image_g = prc_im_g.processImage(sourceImage, ref worker);
            ProcessImg prc_im_b = new FormChannelB_Inverted();
            image_b = prc_im_b.processImage(sourceImage, ref worker);
        }
    }

    class FormChannelR_Inverted : FormChannelR
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int color = (sourceColor.R > 0) ? 0 : 255;
            return Color.FromArgb(color, color, color);
        }
    }
    class FormChannelG_Inverted : FormChannelG
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int color = (sourceColor.G > 0) ? 0 : 255;
            return Color.FromArgb(color, color, color);
        }

    }
    class FormChannelB_Inverted : FormChannelB
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage,
        int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int color = (sourceColor.B > 0) ? 0 : 255;
            return Color.FromArgb(color, color, color);
        }
    }

    class LSB4Img : ProcessImg
    {
        Bitmap image_LSB;
        public override Bitmap get_image_LSB() { return image_LSB; }
        public override Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            process_bytes = new ProcessBytes();
            Bitmap resultImage = process_bytes.MakeLSB(sourceImage, ref worker);
            image_LSB = resultImage;
            return resultImage;
        }
    }

    class DiffWithOrigin : ProcessImg
    {
        Bitmap diff_img;
        public override Bitmap get_image_diff() { return diff_img; }
        protected override Color calculateNewPixelColor(Bitmap currImage,
        Bitmap originImage, int x, int y)
        {
            Color currColor = currImage.GetPixel(x, y);
            Color originColor = originImage.GetPixel(x, y);
            return Color.FromArgb(Math.Abs(currColor.R - originColor.R),
            Math.Abs(currColor.G - originColor.G),
            Math.Abs(currColor.B - originColor.B));
        }
        public override Bitmap processImage(Bitmap currImage, Bitmap originImage,
        BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(currImage.Width, currImage.Height);
            for (int i = 0; i < currImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < currImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(currImage,
                    originImage, i, j));
                }
            }
            diff_img = resultImage;
            return resultImage;
        }
    }

    class EncodeRawImg : ProcessImg
    {
        Bitmap image_raw;
        public override Bitmap get_image_raw() { return image_raw; }
        public override Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            process_bytes = new ProcessBytes();
            image_raw = process_bytes.EncodeRawImage(sourceImage, ref worker);
            return image_raw;
        }
    }

    class DecodeImg : ProcessImg
    {
        public override void processImage(Bitmap sourceImage,
        ref BackgroundWorker worker, bool no_return_value)
        {
            process_bytes = new ProcessBytes();
            process_bytes.EjectTextFromImage(sourceImage, ref worker);
        }
    }

    class EncodeNoisedImg : ProcessImg
    {
        Bitmap image_noised;
        public override Bitmap get_image_noised() { return image_noised; }
        public override Bitmap processImage(Bitmap sourceImage,
        ref BackgroundWorker worker)
        {
            process_bytes = new ProcessBytes();
            image_noised = process_bytes.EncodeNoisedImage(sourceImage, ref worker);
            return image_noised;
        }
    }

}