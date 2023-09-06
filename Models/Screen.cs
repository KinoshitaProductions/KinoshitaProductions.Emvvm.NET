// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace KinoshitaProductions.Emvvm.Models
{
    public class Screen
    {
        public ScreenOrientation Orientation { get; set; }
        public double DiagonalScreenSizeInInches => Math.Sqrt(Math.Pow(MaxWidth / RawDpiX, 2.0) + Math.Pow(MaxHeight / RawDpiY, 2.0));

        public double HorizontalScreenSizeInInches => MaxWidth / RawDpiX;

        public double VerticalScreenSizeInInches => MaxHeight / RawDpiY;

        public double DiagonalAppSizeInInches => Math.Sqrt(Math.Pow(Width / RawDpiX, 2.0) + Math.Pow(Height / RawDpiY, 2.0));

        public double HorizontalAppSizeInInches => Width / RawDpiX;

        public double VerticalAppSizeInInches => Height / RawDpiY;

        public double ScaleFactor { get; set; }
        public double RawDpiX { get; set; }
        public double RawDpiY { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double VirtualWidth => Width / ScaleFactor; //after OS calculations
        public double VirtualHeight => Height / ScaleFactor; //after OS calculations
        public double MaxHeight { get; set; }
        public double MaxWidth { get; set; }

        public double VirtualMaxWidth => MaxWidth / ScaleFactor; //after OS calculations
        public double VirtualMaxHeight => MaxHeight / ScaleFactor; //after OS calculation
        public double Pixels => Height * Width;
        public double VirtualPixels => Height * Width;
    }
}