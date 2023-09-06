#if WINDOWS_UWP || __ANDROID__
namespace KinoshitaProductions.Emvvm.Helpers
{
    public static class ScreenHelper
    {
#if WINDOWS_UWP
#if !NET7_0_OR_GREATER
        public static ScreenOrientation ConvertToUniversalOrientation(Windows.Graphics.Display.DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped:
                case Windows.Graphics.Display.DisplayOrientations.Landscape:
                    return ScreenOrientation.Landscape;

                case Windows.Graphics.Display.DisplayOrientations.PortraitFlipped:
                case Windows.Graphics.Display.DisplayOrientations.Portrait:
                    return ScreenOrientation.Portrait;

                default:
                    return ScreenOrientation.Unknown;
            }
        }
#endif
#elif __ANDROID__            
        public static ScreenOrientation ConvertToUniversalOrientation(global::Android.Content.Res.Orientation orientation)
        {
            switch (orientation)
            {
                case Android.Content.Res.Orientation.Landscape:
                    return ScreenOrientation.Landscape;

                case Android.Content.Res.Orientation.Portrait:
                    return ScreenOrientation.Portrait;

                case Android.Content.Res.Orientation.Square:
                case Android.Content.Res.Orientation.Undefined:
                default:
                    return ScreenOrientation.Unknown;
            }
        }
#endif
    }
}
#endif