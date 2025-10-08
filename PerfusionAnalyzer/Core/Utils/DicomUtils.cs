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

    public static double[] GetTimePoints(List<DicomImage> frames)
    {
        return frames.Select(GetFrameTime).ToArray();
    }

    public static double GetFrameTime(DicomImage frame)
    {
        var ds = frame.Dataset;

        double triggerTimeMs = ds.GetSingleValueOrDefault(DicomTag.TriggerTime, -1.0);
        if (triggerTimeMs >= 0)
            return triggerTimeMs / 1000.0;

        string contentTime = ds.GetSingleValueOrDefault(DicomTag.ContentTime, string.Empty);
        if (!string.IsNullOrEmpty(contentTime))
            return Convert.ToDouble(contentTime);

        return -1;
    }

    public static double GetEchoTime(DicomImage frame)
    {
        return frame.Dataset.GetSingleValueOrDefault(DicomTag.EchoTime, 30.0) / 1000;
    }

    public static ushort[] FrameToUshort(DicomImage frame, int width, int height)
    {
        var pixelData = DicomPixelData.Create(frame.Dataset);
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

        return pixels;
    }

    public static ushort[][] FramesToUshort(List<DicomImage> frames, int width, int height)
    {
        ushort[][] allFrames = new ushort[frames.Count][];
        for (int f = 0; f < frames.Count; f++)
        {
            allFrames[f] = FrameToUshort(frames[f], width, height);
        }
        return allFrames;
    }

    public static bool[,] ExtractMask(byte[] maskRawData)
    {
        int sqrt_l = (int)System.Math.Sqrt(maskRawData.Length);

        List<int[]> maskRows = new List<int[]>();

        int rowCount = sqrt_l / 4;
        int blockWidth = sqrt_l * 2;

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                int startIndex = i * sqrt_l * 4 + j * sqrt_l * 2;
                int length = sqrt_l * 2;

                int[] row = new int[length];
                for (int k = 0; k < length; k++)
                {
                    row[k] = maskRawData[startIndex + k];
                }
                maskRows.Add(row);
            }
        }

        int height = maskRows.Count;
        int width = maskRows[0].Length;

        int[,] maskArray = new int[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                maskArray[y, x] = maskRows[y][x];
            }
        }

        int newWidth = width / 2;
        int[,] filteredMask = new int[height, newWidth];

        for (int y = 0; y < height; y++)
        {
            int colIndex = 0;
            for (int x = 0; x < width; x++)
            {
                if (x % 2 != 0)
                {
                    filteredMask[y, colIndex] = maskArray[y, x];
                    colIndex++;
                }
            }
        }

        int finalWidth = newWidth / 2;
        int[,] finalMask = new int[height, finalWidth];

        for (int y = 0; y < height; y++)
        {
            int colIndex = 0;
            for (int x = 0; x < newWidth; x++)
            {
                if (x % 2 != 0)
                {
                    finalMask[y, colIndex] = filteredMask[y, x];
                    colIndex++;
                }
            }
        }

        bool[,] maskBool = new bool[height, finalWidth];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < finalWidth; x++)
            {
                maskBool[y, x] = finalMask[y, x] != 0;
            }
        }

        return maskBool;
    }
}