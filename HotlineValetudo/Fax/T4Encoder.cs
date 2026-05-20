using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HotlineValetudo.Fax;

public static class T4Encoder
{
    /// <summary>
    ///     Standard fax width: 1728 pixels (203mm at 8 dots/mm, ITA2 format).
    /// </summary>
    private const int FaxWidth = 1728;

    /// <summary>
    ///     Encodes a pre-converted 1-bit fax TIFF into scanline bits (sync + EOL + data per line).
    ///     Image should be converted with: convert img.png -monochrome -density 204x196 -resize 1728x! -compress group4
    ///     out.tif
    /// </summary>
    public static IEnumerable<bool> Encode(string imagePath)
    {
        using var image = Image.Load(imagePath);
        using var raster = image.CloneAs<L8>();

        AddFaxNoise(raster);

        for (var y = 0; y < raster.Height; y++)
        {
            // Build scanline as boolean array
            var line = new bool[FaxWidth];
            for (var x = 0; x < FaxWidth; x++)
                if (x < raster.Width)
                    line[x] = raster[x, y].PackedValue > 127;
                else
                    line[x] = true; // white padding

            // Sync pattern: 100 bits of 101010...
            for (var i = 0; i < 100; i++)
                yield return i % 2 == 0;

            // EOL: 12 zero bits (terminates previous line, before current line's data)
            for (var i = 0; i < 12; i++)
                yield return false;

            // MH-encoded scanline data
            foreach (var bit in T4ModifiedHuffman.EncodeScanline(line))
                yield return bit;
        }
    }

    private static void AddFaxNoise(Image<L8> raster)
    {
        var rng = new Random();
        var width = raster.Width;
        var height = raster.Height;

        for (var y = 0; y < height; y++)
        {
            // Random full-line corruption
            if (rng.Next(20) == 0)
            {
                var val = rng.Next() % 2 == 0 ? (byte)0 : (byte)255;
                for (var x = 0; x < width; x++)
                    raster[x, y] = new L8(val);
            }

            // Random horizontal static patches
            if (rng.Next(5) == 0)
            {
                var startX = rng.Next(width);
                var patchLen = rng.Next(50, 300);
                for (var x = startX; x < Math.Min(startX + patchLen, width); x++)
                    raster[x, y] = new L8(0);
            }

            // Scattered pixel noise
            for (var x = 0; x < width; x++)
                if (rng.Next(100) == 0)
                {
                    var val = rng.Next() % 2 == 0 ? (byte)0 : (byte)255;
                    raster[x, y] = new L8(val);
                }
        }
    }
}