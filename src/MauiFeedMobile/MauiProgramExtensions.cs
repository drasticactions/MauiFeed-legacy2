using Drastic.Services;
using MauiFeed.MauiUI.Services;
using MauiFeed.Services;
using Microsoft.Extensions.Logging;

namespace MauiFeedMobile;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .UseVirtualListView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var db = "database2.db";
        #if ANDROID
        db = Path.Combine(FileSystem.AppDataDirectory, db);
        #endif

        var database = new DatabaseContext(db);
        builder.Services.AddSingleton<IAppDispatcher, MauiAppDispatcher>();
        builder.Services.AddSingleton<IErrorHandlerService, MauiErrorHandler>();
        builder.Services.AddSingleton<ITemplateService, HandlebarsTemplateService>();
        builder.Services.AddSingleton<DatabaseContext>(database);
        builder.Services.AddSingleton<FeedService>();
        builder.Services.AddSingleton<RssFeedCacheService>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder;
    }
}