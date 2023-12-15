using Microsoft.Maui.Hosting;

namespace MauiFeedMobile.Droid;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseSharedMauiApp();

		return builder.Build();
	}
}
