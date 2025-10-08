using Dicom.Imaging;
using OpenTK.Graphics.OpenGL;
using PerfusionAnalyzer.Core.Utils;
using PerfusionAnalyzer.Models;

namespace PerfusionAnalyzer.Core.Services;

public class DicomImageRenderer
{
    private int _textureId;

    private int _width;
    private int _height;

    public void LoadFrameTexture(DicomImage image)
    {
        _width = image.Width;
        _height = image.Height;

        var pixelData = DicomPixelData.Create(image.Dataset);
        var frameData = pixelData.GetFrame(0);
        var pixels = frameData.Data;

        GL.GenTextures(1, out _textureId);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        if (pixelData.BytesAllocated == 2)
        {
            ushort[] ushortPixels = Normalize16BitToUshort(pixels);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance16,
                _width, _height, 0, PixelFormat.Luminance, PixelType.UnsignedShort, ushortPixels);
        }
        else
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance8,
                _width, _height, 0, PixelFormat.Luminance, PixelType.UnsignedByte, pixels);
        }
    }

    public void LoadMapTextureColored(float[,] map)
    {
        _height = map.GetLength(0);
        _width = map.GetLength(1);

        byte[] pixels = NormalizeMapToRgbBytes(map);

        GL.GenTextures(1, out _textureId);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb,
            _width, _height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
    }

    public void Render(int controlWidth, int controlHeight)
    {
        GL.Viewport(0, 0, controlWidth, controlHeight);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        float imageAspect = (float)_width / _height;
        float controlAspect = (float)controlWidth / controlHeight;

        float quadWidth = 1f;
        float quadHeight = 1f;

        if (controlAspect > imageAspect)
        {
            quadWidth = imageAspect / controlAspect;
        }
        else
        {
            quadHeight = controlAspect / imageAspect;
        }

        GL.Begin(PrimitiveType.Quads);
        GL.TexCoord2(0f, 0f); GL.Vertex2(-quadWidth, -quadHeight);
        GL.TexCoord2(1f, 0f); GL.Vertex2(quadWidth, -quadHeight);
        GL.TexCoord2(1f, 1f); GL.Vertex2(quadWidth, quadHeight);
        GL.TexCoord2(0f, 1f); GL.Vertex2(-quadWidth, quadHeight);
        GL.End();

        GL.Disable(EnableCap.Texture2D);
    }

    private ushort[] Normalize16BitToUshort(byte[] pixels)
    {
        int length = _width * _height;
        ushort[] singleChannel = new ushort[length];

        for (int i = 0; i < length; i++)
        {
            singleChannel[i] = (ushort)(pixels[i * 2] + (pixels[i * 2 + 1] << 8));
        }

        ushort min = ushort.MaxValue;
        ushort max = ushort.MinValue;

        foreach (var val in singleChannel)
        {
            if (val < min) min = val;
            if (val > max) max = val;
        }

        int range = max - min;
        if (range == 0) range = 1;

        for (int i = 0; i < length; i++)
        {
            singleChannel[i] = (ushort)((singleChannel[i] - min) * 65535 / range);
        }

        return singleChannel;
    }

    private byte[] NormalizeMapToRgbBytes(float[,] map)
    {
        int height = map.GetLength(0);
        int width = map.GetLength(1);

        byte[] output = new byte[width * height * 3];

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

                Color color = ColorUtils.GetViridisColor(val, min, max);

                int idx = (y * width + x) * 3;
                output[idx] = color.R;
                output[idx + 1] = color.G;
                output[idx + 2] = color.B;
            }
        }

        return output;
    }
}