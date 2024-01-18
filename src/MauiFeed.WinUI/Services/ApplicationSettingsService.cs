using MauiFeed.Models;
using Microsoft.UI.Xaml;
using Realms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MauiFeed.Services;

/// <summary>
/// Application Settings Service.
/// </summary>
public class ApplicationSettingsService
{
    private ThemeSelectorService themeSelectorService;
    private Realm databaseContext;
    private AppSettings appSettings;
    private CultureInfo defaultCulture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettingsService"/> class.
    /// </summary>
    /// <param name="context">Database Context.</param>
    /// <param name="themeSelectorService">Theme selector service.</param>
    public ApplicationSettingsService(RealmConfigurationBase context, ThemeSelectorService themeSelectorService)
    {
        this.databaseContext = Realm.GetInstance(context);
        this.defaultCulture = Thread.CurrentThread.CurrentUICulture;
        var appSettings = this.databaseContext.All<AppSettings>().FirstOrDefault();
        if (appSettings is null)
        {
            appSettings = new AppSettings();
            this.databaseContext.Write(() =>
            {
                this.databaseContext.Add(appSettings);
            });
        }

        this.appSettings = appSettings;
        this.themeSelectorService = themeSelectorService;
    }

    /// <summary>
    /// Gets or sets the Last Updated Time.
    /// </summary>
    public DateTimeOffset? LastUpdated
    {
        get
        {
            return this.appSettings.LastUpdated;
        }

        set
        {
            this.databaseContext.Write(() => { this.appSettings.LastUpdated = value; });
            this.UpdateAppSettings();
        }
    }

    /// <summary>
    /// Gets or sets the application theme.
    /// </summary>
    public LanguageSetting ApplicationLanguageSetting
    {
        get
        {
            return this.appSettings.LanguageSetting;
        }

        set
        {
            this.databaseContext.Write(() => {
                this.appSettings.LanguageSetting = value;
            });
            this.UpdateAppSettings();
        }
    }

    /// <summary>
    /// Gets or sets the application theme.
    /// </summary>
    public AppTheme ApplicationElementTheme
    {
        get
        {
            return this.appSettings.AppTheme;
        }

        set
        {
            this.databaseContext.Write(() => {
                this.appSettings.AppTheme = value;
            });
            this.UpdateAppSettings();
        }
    }

    /// <summary>
    /// Refresh the app with the given app settings.
    /// </summary>
    public void RefreshApp()
    {
        this.UpdateCulture();
        this.UpdateTheme();
    }

    /// <summary>
    /// Update Theme.
    /// </summary>
    public void UpdateTheme()
    {
        ElementTheme theme;

        switch (this.ApplicationElementTheme)
        {
            case AppTheme.Default:
                theme = ElementTheme.Default;
                break;
            case AppTheme.Light:
                theme = ElementTheme.Light;
                break;
            case AppTheme.Dark:
                theme = ElementTheme.Dark;
                break;
            default:
                theme = ElementTheme.Default;
                break;
        }

        this.themeSelectorService.SetTheme(theme);
    }

    /// <summary>
    /// Update Culture.
    /// </summary>
    public void UpdateCulture()
    {
        var culture = this.defaultCulture;
        switch (this.ApplicationLanguageSetting)
        {
            case LanguageSetting.English:
                culture = new CultureInfo("en-US");
                break;
            case LanguageSetting.Japanese:
                culture = new CultureInfo("ja-JP");
                break;
        }

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private void UpdateAppSettings()
    {
        this.RefreshApp();
    }
}
