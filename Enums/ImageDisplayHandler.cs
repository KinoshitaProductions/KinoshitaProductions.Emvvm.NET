namespace KinoshitaProductions.Emvvm.Enums;

public enum ImageDisplayHandler
{
    None,
    App,
#if __ANDROID__
    Glide,
#endif
}
