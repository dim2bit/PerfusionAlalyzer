using PerfusionAnalyzer.Models;
using System.Drawing.Imaging;

namespace PerfusionAnalyzer.Services;

public static class ImageManager
{
    public static void SaveMapAsPng(float[,] map, string path, DescriptorType descriptorType)
    {
        int height = map.GetLength(0);
        int width = map.GetLength(1);

        using Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float val = map[y, x];
                if (val < min) min = val;
                if (val > max) max = val;
            }

        float range = max - min;
        if (range == 0) range = 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float val = map[y, x];
                float normVal = (val - min) / range;

                Color color;

                switch (descriptorType)
                {
                    case DescriptorType.AUC:
                        color = PerfusionColorMapsManager.GetAUCColor(val, min, max);
                        break;
                    case DescriptorType.MTT:
                        color = PerfusionColorMapsManager.GetMTTColor(val, min, max);
                        break;
                    case DescriptorType.TTP:
                        color = PerfusionColorMapsManager.GetTTPColor(val, min, max);
                        break;
                    default:
                        int gray = (int)(normVal * 255);
                        gray = System.Math.Clamp(gray, 0, 255);
                        color = Color.FromArgb(255, gray, gray, gray);
                        break;
                }

                bmp.SetPixel(x, y, color);
            }
        }

        bmp.Save(path, ImageFormat.Png);
    }
}