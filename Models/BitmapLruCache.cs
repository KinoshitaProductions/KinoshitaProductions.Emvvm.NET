#if __ANDROID__ || WINDOWS_UWP
using KinoshitaProductions.Common.Helpers;
using KinoshitaProductions.Common.Services;

namespace KinoshitaProductions.Emvvm.Models;

public class BitmapLruCache : LruCache<BitmapCacheType>
{
    protected override long GetCacheLimitInBytesFor(BitmapCacheType type)
    {
        var limit = type switch
        {
            BitmapCacheType.Thumbnail => Math.Max(8, 16 * MetricsHelper.AvailableRamInMb / 100) * 1024 * 1024, //16MB
            BitmapCacheType.MediumImage => Math.Max(8, 24 * MetricsHelper.AvailableRamInMb / 100) * 1024 * 1024, //24MB
            BitmapCacheType.LargeImage => Math.Max(4, 24 * MetricsHelper.AvailableRamInMb / 100) * 1024 * 1024, //24MB
            BitmapCacheType.ZoomImage => Math.Max(4, 16 * MetricsHelper.AvailableRamInMb / 100) * 1024 * 1024, //16MB
            BitmapCacheType.FullImage => Math.Max(4, 4 * MetricsHelper.AvailableRamInMb / 100) * 1024 * 1024, //4MB, anyways, THERE IS NO REASON TO HAVE THIS ONE BEING USED!!
            _ => 0
        };
        return limit >= 0 ? limit : long.MaxValue;
    }
}
#endif
