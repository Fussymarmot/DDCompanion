using System;

namespace DDCompanion.Services;

public static class ImageCompressionService
{
    private const int MaxSize = 800;
    private const int Quality = 80;

    public static async Task<string> CompressAsync(Stream input)
    {
#if ANDROID
        return await CompressAndroidAsync(input);
#else
        // Для не-Android платформ сохраняем исходный файл, чтобы сервис
        // оставался доступным при сборке проекта под другие платформы.
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        return $"data:image/jpeg;base64,{Convert.ToBase64String(memory.ToArray())}";
#endif
    }

#if ANDROID
    private static async Task<string> CompressAndroidAsync(Stream input)
    {
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);

        var sourceBytes = memory.ToArray();

        var bounds = new Android.Graphics.BitmapFactory.Options
        {
            InJustDecodeBounds = true
        };

        using (var boundsStream = new MemoryStream(sourceBytes))
        {
            Android.Graphics.BitmapFactory.DecodeStream(boundsStream, null, bounds);
        }

        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
            throw new InvalidOperationException("Не удалось определить размер изображения.");

        var sampleSize = CalculateSampleSize(bounds.OutWidth, bounds.OutHeight);

        var options = new Android.Graphics.BitmapFactory.Options
        {
            InSampleSize = sampleSize,
            InPreferredConfig = Android.Graphics.Bitmap.Config.Rgb565
        };

        Android.Graphics.Bitmap? bitmap;

        using (var imageStream = new MemoryStream(sourceBytes))
        {
            bitmap = Android.Graphics.BitmapFactory.DecodeStream(imageStream, null, options);
        }

        if (bitmap is null)
            throw new InvalidOperationException("Не удалось загрузить изображение.");

        Android.Graphics.Bitmap? resizedBitmap = null;

        try
        {
            resizedBitmap = Resize(bitmap);
            var bitmapToSave = resizedBitmap;

            using var output = new MemoryStream();
            bitmapToSave.Compress(
                Android.Graphics.Bitmap.CompressFormat.Jpeg,
                Quality,
                output);

            return $"data:image/jpeg;base64,{Convert.ToBase64String(output.ToArray())}";
        }
        finally
        {
            if (resizedBitmap is not null && !ReferenceEquals(resizedBitmap, bitmap))
                resizedBitmap.Recycle();

            bitmap.Recycle();
        }
    }

    private static int CalculateSampleSize(int width, int height)
    {
        var largestSide = Math.Max(width, height);
        var sampleSize = 1;

        while (largestSide / sampleSize > MaxSize * 2)
            sampleSize *= 2;

        return sampleSize;
    }

    private static Android.Graphics.Bitmap Resize(Android.Graphics.Bitmap bitmap)
    {
        if (bitmap.Width <= MaxSize && bitmap.Height <= MaxSize)
            return bitmap;

        var scale = Math.Min(
            (float)MaxSize / bitmap.Width,
            (float)MaxSize / bitmap.Height);

        var newWidth = Math.Max(1, (int)(bitmap.Width * scale));
        var newHeight = Math.Max(1, (int)(bitmap.Height * scale));

        return Android.Graphics.Bitmap.CreateScaledBitmap(
            bitmap,
            newWidth,
            newHeight,
            true);
    }
#endif
}
