// Compresses picked images (avatars, weapon and armor photos) into small base64 JPEG data URIs.
// Real compression is only implemented for Android; other platforms keep the original bytes.

using System;

namespace DDCompanion.Services;

public static class ImageCompressionService
{
    // Avatar profile: shown large on the character page.
    private const int AvatarMaxSize = 800;
    private const int AvatarQuality = 80;

    // Equipment profile: shown as a small card, so a smaller size keeps .ddc files light.
    private const int EquipmentMaxSize = 600;
    private const int EquipmentQuality = 72;

    public static Task<string> CompressAsync(Stream input) =>
        CompressAsync(input, AvatarMaxSize, AvatarQuality);

    public static Task<string> CompressEquipmentAsync(Stream input) =>
        CompressAsync(input, EquipmentMaxSize, EquipmentQuality);

    public static async Task<string> CompressAsync(Stream input, int maxSize, int quality)
    {
#if ANDROID
        return await CompressAndroidAsync(input, maxSize, quality);
#else
        // Non-Android platforms keep the original file so the service
        // still works when the project is built for other targets.
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        return $"data:image/jpeg;base64,{Convert.ToBase64String(memory.ToArray())}";
#endif
    }

#if ANDROID
    private static async Task<string> CompressAndroidAsync(Stream input, int maxSize, int quality)
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

        var sampleSize = CalculateSampleSize(bounds.OutWidth, bounds.OutHeight, maxSize);

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
            resizedBitmap = Resize(bitmap, maxSize);
            var bitmapToSave = resizedBitmap;

            using var output = new MemoryStream();
            bitmapToSave.Compress(
                Android.Graphics.Bitmap.CompressFormat.Jpeg,
                quality,
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

    private static int CalculateSampleSize(int width, int height, int maxSize)
    {
        var largestSide = Math.Max(width, height);
        var sampleSize = 1;

        while (largestSide / sampleSize > maxSize * 2)
            sampleSize *= 2;

        return sampleSize;
    }

    private static Android.Graphics.Bitmap Resize(Android.Graphics.Bitmap bitmap, int maxSize)
    {
        if (bitmap.Width <= maxSize && bitmap.Height <= maxSize)
            return bitmap;

        var scale = Math.Min(
            (float)maxSize / bitmap.Width,
            (float)maxSize / bitmap.Height);

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
