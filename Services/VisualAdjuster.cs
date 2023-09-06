using KinoshitaProductions.Common.Helpers;

namespace KinoshitaProductions.Emvvm.Services;

public static class VisualAdjuster
{
    private static int _preferredColumns;
    private static double _preferredRatio;
    private static double _customUiScaleFactor;
    private static UiScaleStrategy _uiScaleStrategy = UiScaleStrategy.ScaleToScreenOrWindow;
    private static ImageQualityMode _imageQualityPreference = ImageQualityMode.Balanced;
#if __ANDROID__
    private static (double Width, double Height) _maximumDecodeDimensions = (4096, 4096);
#endif
    public static void Initialize(
#if __ANDROID__
        (double Width, double Height) maximumDecodeDimensions
#endif
        )
    {
#if __ANDROID__
        _maximumDecodeDimensions = maximumDecodeDimensions;
#endif
    }
    public static void ConfigureOrientation(int preferredColumns, double preferredRatio = 0.7)
    {
        _preferredColumns = preferredColumns;
        _preferredRatio = preferredRatio;
    }
    public static void ConfigureGlobal(double customUiScaleFactor, UiScaleStrategy uiScaleStrategy = UiScaleStrategy.ScaleToScreenOrWindow, ImageQualityMode imageQualityPreference = ImageQualityMode.Balanced)
    {
        _customUiScaleFactor = customUiScaleFactor;
        _uiScaleStrategy = uiScaleStrategy;
        _imageQualityPreference = imageQualityPreference;
    }
    // ReSharper disable once MemberCanBePrivate.Global
    public static int GetAdjustedDisplayColumns((double Width, double Height) dimensions, VisualContentLayout to)
    {
        switch (to)
        {
#if WINDOWS_UWP
            case VisualContentLayout.ThumbnailColumn:
#endif
            case VisualContentLayout.Thumbnail:
                if (_preferredColumns != 0)
                    return _preferredColumns;

                //one picture per inch
                if (State.Screen.DiagonalScreenSizeInInches < 3) // watch
                {
                    var inchesPerPicture = State.Screen.Orientation == ScreenOrientation.Portrait ? 0.80 : 0.85;

#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + inchesPerPicture) / inchesPerPicture), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / inchesPerPicture), 2);
#endif
                }
                else if (State.Screen.DiagonalScreenSizeInInches < 5.5) // phone
                {
                    var inchesPerPicture = State.Screen.Orientation == ScreenOrientation.Portrait ? 1.10 : 1.20;
#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + inchesPerPicture) / inchesPerPicture), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / inchesPerPicture), 2);
#endif
                }
                else if (State.Screen.DiagonalScreenSizeInInches < 7) // LARGE phone
                {
                    var inchesPerPicture = State.Screen.Orientation == ScreenOrientation.Portrait ? 1.10 : 1.35;
#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + inchesPerPicture) / inchesPerPicture), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / inchesPerPicture), 2);
#endif
                }
                else if (State.Screen.DiagonalScreenSizeInInches < 13) // tablet
                {
                    var inchesPerPicture = State.Screen.Orientation == ScreenOrientation.Portrait ? 1.6 : 1.8;
#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + inchesPerPicture) / inchesPerPicture), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / inchesPerPicture), 2);
#endif
                }
                else if (State.Screen.DiagonalScreenSizeInInches < 18) // laptop
                {
#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + 2.5) / 2.5), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / 2.5), 2);
#endif
                }
                else if (State.Screen.DiagonalScreenSizeInInches < 32) // PC
                {
#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + 4.0) / 4.0), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / 4.0), 2);
#endif
                }
                else
                {
#if WINDOWS_UWP
                    return Math.Max((int)((State.Screen.HorizontalAppSizeInInches + 7.0) / 7.0), 2);
#else
                    return Math.Max((int)(State.Screen.HorizontalAppSizeInInches / 7.0), 2);
#endif
                }
            default:
                return 1;
        }
    }
    // ReSharper disable once MemberCanBePrivate.Global
    public static void GetAdjustedDisplayDimensions((double Width, double Height) dimensions, VisualContentLayout to, AdditionalVisualAdjustmentOperations additionalOperations, out double adjustedWidth, out double adjustedHeight)
    {
        switch (to)
        {
            case VisualContentLayout.FitToScreen:
                {
                    var screenWidth = State.Screen.VirtualWidth;
                    var screenHeight = State.Screen.VirtualHeight;

                    if (dimensions.Width != 0.0 && dimensions.Height != 0.0)
                    {
                        double xRatio = screenWidth / dimensions.Width;
                        double yRatio = screenHeight / dimensions.Height;

                        double scaleToRatio = Math.Min(xRatio, yRatio);

                        dimensions.Width *= scaleToRatio;
                        dimensions.Height *= scaleToRatio;
                    }

                    adjustedWidth = dimensions.Width;
                    adjustedHeight = dimensions.Height;
                    break;
                }
            case VisualContentLayout.FillScreen:
                {
                    var screenWidth = State.Screen.Width;
                    var screenHeight = State.Screen.Height;

                    if (dimensions.Width != 0.0 && dimensions.Height != 0.0)
                    {
                        double xRatio = screenWidth / dimensions.Width;
                        double yRatio = screenHeight / dimensions.Height;

                        double scaleToRatio = Math.Max(xRatio, yRatio);

                        dimensions.Width *= scaleToRatio;
                        dimensions.Height *= scaleToRatio;
                    }

                    adjustedWidth = dimensions.Width;
                    adjustedHeight = dimensions.Height;

                    break;
                }
            case VisualContentLayout.Viewbox:
                // CALCULATE SCREEN SCALING
                double fillPercentage = 1.0;

                if (State.Screen.DiagonalScreenSizeInInches > 30)
                {
                    fillPercentage = 0.6;
                }
                else if (State.Screen.DiagonalScreenSizeInInches > 17)
                {
                    fillPercentage = 0.40;
                }
                else if (State.Screen.DiagonalScreenSizeInInches > 12.5)
                {
                    fillPercentage = 0.60;
                }
                else if (State.Screen.DiagonalScreenSizeInInches > 7.5)
                {
                    fillPercentage = 0.8;
                }

                double screenUseWidth = State.Screen.VirtualMaxWidth * fillPercentage;
                
                // Apply scale factor
                screenUseWidth *= _customUiScaleFactor;

                if (screenUseWidth > State.Screen.VirtualMaxWidth)
                    screenUseWidth = State.Screen.VirtualMaxWidth;

                // CALCULATE WINDOW SCALING
                double windowUseWidth = State.Screen.Width;

                // CALCULATE FINAL SCALING
                double finalUseWidth = windowUseWidth;
                // Might be affected by preview
                var uiScaleStrategy = _uiScaleStrategy;
                switch (uiScaleStrategy)
                {
                    case UiScaleStrategy.NoScaling:
                        break;

                    case UiScaleStrategy.ScaleToScreenOrWindow:
                        finalUseWidth = windowUseWidth < screenUseWidth ? windowUseWidth : screenUseWidth;
                        break;

                    case UiScaleStrategy.ScaleToScreen:
                        finalUseWidth = screenUseWidth;
                        break;

                    case UiScaleStrategy.ScaleToWindow:
                        finalUseWidth = windowUseWidth;
                        break;
                }

                adjustedWidth = finalUseWidth;
                adjustedHeight = dimensions.Height;
                break;
#if WINDOWS_UWP
            case VisualContentLayout.ThumbnailColumn:
                {
                    var screenWidth = State.Screen.Width;

                    var ratio = _preferredRatio;

                    var maxWidth = screenWidth / GetAdjustedDisplayColumns(dimensions, to);
                    var maxHeight = maxWidth * ratio;

                    adjustedWidth = Math.Floor(maxWidth);
                    adjustedHeight = Math.Floor(maxHeight);

                    if ((additionalOperations & AdditionalVisualAdjustmentOperations.AddOnePixelToWidth) != 0)
                        adjustedWidth += 1;
                    break;
                }
#endif
            case VisualContentLayout.Thumbnail:
                {
                    var screenWidth = State.Screen.Width;

                    var ratio = _preferredRatio;
                    
                    var maxWidth = screenWidth / GetAdjustedDisplayColumns(dimensions, to);
                    var maxHeight = maxWidth * ratio;

                    if ((additionalOperations & AdditionalVisualAdjustmentOperations.ScaleProportionally) != 0)
                    {
                        if (dimensions.Width != 0.0 && dimensions.Height != 0.0)
                        {
                            double xRatio = maxWidth / dimensions.Width;
                            double yRatio = maxHeight / dimensions.Height;

                            double scaleToRatio = Math.Max(xRatio, yRatio);

                            dimensions.Width *= scaleToRatio;
                            dimensions.Height *= scaleToRatio;
                        }
                    }
                    else
                    {
                        dimensions.Width = maxWidth;
                        dimensions.Height = maxHeight;
                    }

                    adjustedWidth = Math.Floor(dimensions.Width);
                    adjustedHeight = Math.Floor(dimensions.Height);

#if WINDOWS_UWP
                    if ((additionalOperations & AdditionalVisualAdjustmentOperations.AddOnePixelToWidth) != 0)
                        adjustedWidth += 1;
#endif
                    break;
                }
            default:
                adjustedWidth = dimensions.Width;
                adjustedHeight = dimensions.Height;
                break;
        }
    }

    public static (double Width, double Height) GetAdjustedDisplayDimensions((double Width, double Height) dimensions, VisualContentLayout to, AdditionalVisualAdjustmentOperations additionalOperations = AdditionalVisualAdjustmentOperations.None)
    {
        GetAdjustedDisplayDimensions(dimensions, to, additionalOperations, out var adjustedWidth, out var adjustedHeight);
        return (adjustedWidth, adjustedHeight);
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    public static void GetAdjustedDecodeDimensions((double Width, double Height) dimensions, VisualContentLayout to, AdditionalVisualAdjustmentOperations additionalOperations, out double adjustedWidth, out double adjustedHeight)
    {
        // get maximum target size
        var targetDimensions = GetStandardContentSizeFor(to);
        targetDimensions = ScaleHelper.ScaleToFitDimensions(dimensions, targetDimensions);
        switch (additionalOperations)
        {
            case AdditionalVisualAdjustmentOperations.AddOnePixelToWidth:
                targetDimensions.Width += 1;
                break;

            case AdditionalVisualAdjustmentOperations.ScaleProportionally:
            case AdditionalVisualAdjustmentOperations.None:
            default:
                break;
        }

        // scale based on factors
        GetAdjustedDisplayDimensions(dimensions, to, additionalOperations, out adjustedWidth, out adjustedHeight);
#if WINDOWS_UWP
        var qualityFactor = 4.0;
#else
        var qualityFactor = 1.0;
#endif
        adjustedWidth = (int)(adjustedWidth * qualityFactor);
        adjustedHeight = (int)(adjustedHeight * qualityFactor);

        // reduce crude expected dimensions
        switch (_imageQualityPreference)
        {
            case ImageQualityMode.Performance:
                adjustedWidth *= 0.5;
                adjustedHeight *= 0.5;
                break;

            case ImageQualityMode.Balanced:
                adjustedWidth *= 0.7;
                adjustedHeight *= 0.7;
                break;
        }
        // if larger than target area, force downscale
#if WINDOWS_UWP
        if (adjustedWidth > dimensions.Width || adjustedHeight > dimensions.Height)
        {
            adjustedWidth = dimensions.Width;
            adjustedHeight = dimensions.Height;
        }
#else
        if (adjustedWidth > targetDimensions.Width || adjustedHeight > targetDimensions.Height)
        {
            adjustedWidth = targetDimensions.Width;
            adjustedHeight = targetDimensions.Height;
        }
#endif
        // if less than half of target area, force upscale
        if (adjustedWidth < targetDimensions.Width / 2.0 || adjustedHeight < targetDimensions.Height / 2.0)
        {
            adjustedWidth = targetDimensions.Width / 2.0;
            adjustedHeight = targetDimensions.Height / 2.0;
        }
        // if larger than source image, force downscale (DO NOT ADD ELSE IF)
        if (adjustedWidth > dimensions.Width || adjustedHeight > dimensions.Height)
        {
            adjustedWidth = dimensions.Width;
            adjustedHeight = dimensions.Height;
        }

        // reduce final dimensions
        if (to != VisualContentLayout.Thumbnail)
        {
            switch (_imageQualityPreference)
            {
                case ImageQualityMode.Performance:
                    adjustedWidth *= 0.7;
                    adjustedHeight *= 0.7;
                    break;

                case ImageQualityMode.Balanced:
                    adjustedWidth *= 0.83;
                    adjustedHeight *= 0.83;
                    break;
            }
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static (double Width, double Height) GetAdjustedDecodeDimensions((double Width, double Height) dimensions, VisualContentLayout to, AdditionalVisualAdjustmentOperations additionalOperations = AdditionalVisualAdjustmentOperations.None)
    {
        GetAdjustedDecodeDimensions(dimensions, to, additionalOperations, out var adjustedWidth, out var adjustedHeight);
        return (adjustedWidth, adjustedHeight);
    }
    
    private static (double Width, double Height) GetStandardContentSizeFor(VisualContentLayout contentType)
    {
        (double Width, double Height) result;
        switch (contentType)
        {
            case VisualContentLayout.Original:
            case VisualContentLayout.FullImage:
            case VisualContentLayout.ZoomImage:
                result = (State.Screen.MaxWidth, State.Screen.MaxHeight);
                switch (_imageQualityPreference)
                {
                    case ImageQualityMode.Performance:
                        break;

                    case ImageQualityMode.Balanced:
                        result = (result.Width * 1.2, result.Height * 1.2);
                        break;

                    case ImageQualityMode.Quality:
                        result = (result.Width * 1.4, result.Height * 1.4);
                        break;
                }
                break;

            case VisualContentLayout.Thumbnail:
                double thumbnailColumns = Math.Max(1, GetAdjustedDisplayColumns((0, 0), VisualContentLayout.Thumbnail));
                double thumbnailRatio = Math.Max(0.5, _preferredRatio);
                result = (State.Screen.MaxWidth / thumbnailColumns, State.Screen.MaxWidth * thumbnailRatio / thumbnailColumns);
                break;

            case VisualContentLayout.MediumImage:
                result = (State.Screen.MaxWidth * 0.75, State.Screen.MaxHeight);
                break;

            case VisualContentLayout.LargeImage:
                result = (State.Screen.MaxWidth * 0.75, State.Screen.MaxHeight);
                break;

            default:
                result = (State.Screen.MaxWidth, State.Screen.MaxHeight);
                break;
        }
        return result;
    }
    
    public static (double Width, double Height) GetOptimalDecodeDimensions(double width, double height, VisualContentLayout visualContentLayout)
    {
        var decodeDimensions = VisualAdjuster.GetAdjustedDecodeDimensions((width, height), visualContentLayout, AdditionalVisualAdjustmentOperations.ScaleProportionally);

        // check up for hardware limitations here, such as max texture size, video card memory, etc.
#if __ANDROID__
        decodeDimensions = ScaleHelper.DownscaleToFitDimensions(decodeDimensions, _maximumDecodeDimensions);
#endif
        return (Math.Min(width, decodeDimensions.Width), Math.Min(height, decodeDimensions.Height));
    }
}