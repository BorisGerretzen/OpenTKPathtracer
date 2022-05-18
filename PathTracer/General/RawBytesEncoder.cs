using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace PathTracer.Helpers;

public class RawBytesEncoder : IImageEncoder {
    public void Encode<TPixel>(Image<TPixel> image, Stream stream) where TPixel : unmanaged, IPixel<TPixel> {
        stream.SetLength(image.Height * image.Width * 3);
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++) {
            var target = new Rgba32();
            image[x, y].ToRgba32(ref target);
            stream.WriteByte(target.R);
            stream.WriteByte(target.G);
            stream.WriteByte(target.B);
        }
    }

    public Task EncodeAsync<TPixel>(Image<TPixel> image, Stream stream, CancellationToken cancellationToken) where TPixel : unmanaged, IPixel<TPixel> {
        throw new NotImplementedException();
    }
}