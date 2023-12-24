using Foundation;
using MauiFeed.Models;
using UIKit;

namespace MauiFeed.Apple.Models;

public class SidebarImage : UIImage, ISidebarImage
{
    public static ISidebarImage GenerateImage(byte[] cache)
        => (SidebarImage)SidebarImage.LoadFromData(NSData.FromArray(cache))!;
}