#if __ANDROID__
using Android.Graphics;
using Android.OS;

namespace KinoshitaProductions.Emvvm.Helpers;

public static class BitmapHelper
{
    public static async Task<Bitmap?> GetResizedBitmapFromStream(Stream stream, (double Width, double Height, string? MimeType, VisualContentLayout VisualContentLayout) original, (double Width, double Height) target, bool scaleProportionally = true, ImageQualityMode imageQualityMode = ImageQualityMode.Balanced)
    {
        bool preferHighQualityDecode = (original.MimeType ?? string.Empty).EndsWith("png");
        bool preferHighQualityResize = false;
        switch (original.VisualContentLayout)
        {
            case VisualContentLayout.FitToScreen:
            case VisualContentLayout.FillScreen:
            case VisualContentLayout.Viewbox:
            case VisualContentLayout.Thumbnail:
            case VisualContentLayout.MediumImage:
            case VisualContentLayout.LargeImage:
                break;
            default:
                preferHighQualityDecode = preferHighQualityResize = true;
                break; // continue to resize further 
        }

        if (scaleProportionally)
        {
            // it's smaller than target
            if ((original.Width < target.Width && original.Height < target.Height))
            {
                target.Width = original.Width;
                target.Height = original.Height;
            }
            else if (original.Width < target.Width && original.Height < target.Height && original.Width != 0.0 && original.Height != 0.0)
            {
                var xRatio = target.Width / original.Width;
                var yRatio = target.Height / original.Height;

                var scaleToRatio = Math.Min(xRatio, yRatio);

                original.Width *= scaleToRatio;
                original.Height *= scaleToRatio;
            }
        }

        var inSampleSize = (int)(
            Math.Sqrt(original.Width * original.Height + 1) / (target.Width * target.Height + 1)
            + (preferHighQualityResize ? 0 : 0.98));
        if (inSampleSize <= 0)
            inSampleSize = 1;

        // Ensure quality downscaling if not performance mode
        inSampleSize /= (original.Width / inSampleSize < target.Width ? 2 : 1);
        
        if (Math.Abs(original.Width - target.Width) > 10 && Math.Abs(original.Height - target.Height) > 10)
        {
            Bitmap? tempBitmap;
            if (imageQualityMode == ImageQualityMode.Performance)
            {
                // get scale factor to resize
                var options = new BitmapFactory.Options
                {
                    InSampleSize = Math.Max(1, inSampleSize),
                    InScaled = false,
                    OutWidth = (int)target.Width,
                    OutHeight = (int)target.Height,
                    OutMimeType = original.MimeType,
                };

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    options.OutConfig = preferHighQualityDecode ? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565;
                }

                // resize to inSampleSize
                tempBitmap = await DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options);
                if (tempBitmap == null) return null;
                original.Width = Math.Max(1, original.Width / options.InSampleSize); // adjust to match after sampling
                original.Height = Math.Max(1, original.Height / options.InSampleSize); // adjust to match after samplingMath.Max(1, original.Width / options.InSampleSize); // adjust to match after sampling

                // estimate max pixels that can be fit in video memory without freezing
                var maxEstimatedPixels = Math.Max(State.Screen.MaxHeight * State.Screen.MaxWidth * 1.0, MaximumTextureSizeForDevice.Width * MaximumTextureSizeForDevice.Height * 0.025);

                // check if after resizing to inSampleSize must be further resized
                if (original.Width * original.Height / options.InSampleSize > maxEstimatedPixels)
                {
                    double originalPixels = original.Width * original.Height;
                    double downscaleRatio = maxEstimatedPixels / originalPixels;
                    if (downscaleRatio > 0)
                    {
                        target.Width = original.Width * Math.Sqrt(downscaleRatio);
                        target.Height = original.Height * Math.Sqrt(downscaleRatio);
                    } // force scaling

                    var resizedBitmap = GetResizedBitmapFromBitmapV2(tempBitmap, ((int)target.Width, (int)target.Height));
                    tempBitmap.Recycle();
                    tempBitmap = resizedBitmap;
                }
            }
            else if (imageQualityMode == ImageQualityMode.Balanced)
            {
                var options = new BitmapFactory.Options
                {
                    InSampleSize = Math.Max(1, inSampleSize),
                    InScaled = false,
                    OutWidth = (int)target.Width,
                    OutHeight = (int)target.Height,
                    OutMimeType = original.MimeType
                };

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    options.OutConfig = preferHighQualityDecode ? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565;
                }

                // resize to inSampleSize
                tempBitmap = await DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options);
                if (tempBitmap == null) return null;
                original.Width = Math.Max(1, original.Width / options.InSampleSize); // adjust to match after sampling
                original.Height = Math.Max(1, original.Height / options.InSampleSize); // adjust to match after samplingMath.Max(1, original.Width / options.InSampleSize); // adjust to match after sampling

                // check if requires a quality resize
                if (preferHighQualityResize)
                {
                    // estimate max pixels that can be fit in video memory without freezing
                    var maxEstimatedPixels = Math.Max(State.Screen.MaxHeight * State.Screen.MaxWidth * 1.0, MaximumTextureSizeForDevice.Width * MaximumTextureSizeForDevice.Height * 0.025);

                    // check if after resizing to inSampleSize must be further resized
                    if (original.Width * original.Height / options.InSampleSize > maxEstimatedPixels)
                    {
                        var originalPixels = original.Width * original.Height;
                        var downscaleRatio = maxEstimatedPixels / originalPixels;
                        if (downscaleRatio > 0)
                        {
                            target.Width = original.Width * Math.Sqrt(downscaleRatio);
                            target.Height = original.Height * Math.Sqrt(downscaleRatio);
                        } // force scaling

                        var resizedBitmap = GetResizedBitmapFromBitmapV2(tempBitmap, ((int)target.Width, (int)target.Height));
                        tempBitmap.Recycle();
                        tempBitmap = resizedBitmap;
                    }
                }
            }
            else
            {
                var options = new BitmapFactory.Options
                {
                    InSampleSize = Math.Max(1, inSampleSize),
                    InScaled = false,
                    OutWidth = (int)target.Width,
                    OutHeight = (int)target.Height,
                    OutMimeType = original.MimeType
                };

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    options.OutConfig = preferHighQualityDecode ? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565;
                }

                // resize to inSampleSize
                tempBitmap = await DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options);
                if (tempBitmap == null) return null;
                original.Width = Math.Max(1, original.Width / options.InSampleSize); // adjust to match after sampling
                original.Height = Math.Max(1, original.Height / options.InSampleSize); // adjust to match after samplingMath.Max(1, original.Width / options.InSampleSize); // adjust to match after sampling

                // check if requires a quality resize
                if (preferHighQualityResize)
                {
                    // estimate max pixels that can be fit in video memory without freezing
                    double maxEstimatedPixels = Math.Max(State.Screen.MaxHeight * State.Screen.MaxWidth * 2.0, MaximumTextureSizeForDevice.Width * MaximumTextureSizeForDevice.Height * 0.05);

                    // check if after resizing to inSampleSize must be further resized
                    if (original.Width * original.Height / options.InSampleSize > maxEstimatedPixels)
                    {
                        double originalPixels = original.Width * original.Height;
                        double downscaleRatio = maxEstimatedPixels / originalPixels;
                        if (downscaleRatio > 0)
                        {
                            target.Width = original.Width * Math.Sqrt(downscaleRatio);
                            target.Height = original.Height * Math.Sqrt(downscaleRatio);
                        } // force scaling

                        var resizedBitmap = GetResizedBitmapFromBitmapV2(tempBitmap, ((int)target.Width, (int)target.Height));
                        tempBitmap.Dispose();
                        return resizedBitmap;
                    }
                }
            }
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // tempBitmap.SetConfig(Bitmap.Config.Hardware); // untested
            }
            tempBitmap?.PrepareToDraw();
            return tempBitmap;
        }
        else
        {
            var options = new BitmapFactory.Options
            {
                InSampleSize = 1,
                OutWidth = (int)target.Width,
                OutHeight = (int)target.Height,
                OutMimeType = original.MimeType,
            };
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                options.OutConfig = preferHighQualityDecode ? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565;
            }
            var tempBitmap = await DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options);
            tempBitmap?.PrepareToDraw();
            return tempBitmap;
        }
    }

    private static async Task<Bitmap?> DecodeStreamAsync(Stream stream, Rect rect, BitmapFactory.Options options)
    {
        try
        {
            return await BitmapFactory.DecodeStreamAsync(stream, rect, options);
        }
        catch (Exception)
        {
            // probably too large? try smaller decode
            options.OutWidth = Math.Max(1, options.OutWidth * 3 / 4);
            options.OutHeight = Math.Max(1, options.OutHeight * 3 / 4);
            try
            {
                return await BitmapFactory.DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options);
            }
            catch (Exception)
            {
                // probably too large? try smaller decode
                options.InSampleSize = 2;
                options.OutWidth = Math.Max(1, options.OutWidth * 4 / 6);
                options.OutHeight = Math.Max(1, options.OutHeight * 4 / 6);

                return await BitmapFactory.DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options);
            }
        }
    }


    public static async Task<Bitmap?> GetBitmapFromBytes(byte[] bytes)
    {
        return await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
    }

    public static async Task<(double Width, double Height)> GetBitmapDimensionsFromBytes(byte[] bytes)
    {
        // First we will decode measurements to *fix* the expected width and height
        BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
        await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length, options).ConfigureAwait(false);
        return (options.OutWidth, options.OutHeight);
    }

    public static async Task<(double Width, double Height, string? MimeType)> GetBitmapDimensionsFromStream(Stream stream)
    {
        // First we will decode measurements to *fix* the expected width and height
        BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
        await BitmapFactory.DecodeStreamAsync(stream, new Rect(0, 0, 0, 0), options).ConfigureAwait(false);
        return (options.OutWidth, options.OutHeight, options.OutMimeType);
    }
    
    private static Bitmap? GetResizedBitmapFromBitmapV2(Bitmap bm, (int Width, int Height) target)
    {
        // "RECREATE" THE NEW BITMAP
        var resizedBitmap = Bitmap.CreateScaledBitmap(bm, target.Width, target.Height, true);
        return resizedBitmap;
    }
    
    private static (double Width, double Height) _maximumTextureSizeForDevice = (-1, -1);
    private static (double Width, double Height) MaximumTextureSizeForDevice
    {
        get
        {
            if (_maximumTextureSizeForDevice.Width < 0.0 || _maximumTextureSizeForDevice.Height < 0.0)
            {
                var maxTextureSize = new int[1];
                Android.Opengl.GLES10.GlGetIntegerv(Javax.Microedition.Khronos.Opengles.IGL10.GlMaxTextureSize, maxTextureSize, 0);

                if (maxTextureSize.Length > 0 && maxTextureSize[0] > 0)
                {
                    _maximumTextureSizeForDevice = (maxTextureSize[0], maxTextureSize[0]);
                }
            }
            return _maximumTextureSizeForDevice.Width < 0 ? (Math.Min(State.Screen.MaxWidth, State.Screen.MaxHeight), Math.Min(State.Screen.MaxWidth, State.Screen.MaxHeight)) : _maximumTextureSizeForDevice;
        }
    }
}
#endif