using Dicom;
using Dicom.Imaging;

namespace PerfusionAnalyzer.Core.Dicom;

public static class DicomUtils
{
    public static List<DicomImage> LoadDicomImages(string[] filePaths)
    {
        var images = new List<DicomImage>();

        foreach (var path in filePaths)
        {
            images.Add(new DicomImage(path));
        }

        return images;
    }

    public static double[] GetTimePoints(List<DicomImage> images)
    {
        return images.Select(GetTriggerTime).ToArray();
    }

    public static double GetTriggerTime(DicomImage image)
    {
        return image.Dataset.GetSingleValueOrDefault(DicomTag.TriggerTime, -1.0) / 1000;
    }

    public static double GetEchoTime(DicomImage image)
    {
        return image.Dataset.GetSingleValueOrDefault(DicomTag.EchoTime, 30.0) / 1000;
    }

    public static ushort[][] FramesToUshort(List<DicomImage> frames, int width, int height)
    {
        ushort[][] allFrames = new ushort[frames.Count][];
        for (int f = 0; f < frames.Count; f++)
        {
            var pixelData = DicomPixelData.Create(frames[f].Dataset);
            byte[] rawBytes = pixelData.GetFrame(0).Data;

            int numPixels = width * height;
            ushort[] pixels = new ushort[numPixels];

            if (pixelData.BitsAllocated == 8)
            {
                for (int i = 0; i < numPixels; i++)
                    pixels[i] = rawBytes[i];
            }
            else if (pixelData.BitsAllocated == 16)
            {
                for (int i = 0; i < numPixels; i++)
                    pixels[i] = BitConverter.ToUInt16(rawBytes, i * 2);
            }

            allFrames[f] = pixels;
        }
        return allFrames;
    }

    public static bool[,] CreateMask(ushort[] baselineFrame, int width, int height, ushort threshold)
    {
        var mask = new bool[height, width];
        for (int i = 0; i < baselineFrame.Length; i++)
            mask[i / width, i % width] = baselineFrame[i] > threshold;
        return mask;
    }
}