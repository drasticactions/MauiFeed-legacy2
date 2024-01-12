// <copyright file="AppSettings.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace MauiFeed.Models
{
    /// <summary>
    /// App Settings.
    /// </summary>
    public partial class AppSettings : IRealmObject
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [PrimaryKey]
        [MapTo("_id")]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        /// <summary>
        /// Gets or sets the last updated time of the app.
        /// </summary>
        public DateTimeOffset? LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the underlying app theme int.
        /// </summary>
        public int AppThemeInt { get; set; }

        /// <summary>
        /// Gets or sets the app theme.
        /// </summary>
        public AppTheme AppTheme
        {
            get => (AppTheme)this.AppThemeInt;

            set => this.AppThemeInt = (int)value;
        }

        /// <summary>
        /// Gets or sets the underlying language setting int.
        /// </summary>
        public int LanguageSettingInt { get; set; }

        /// <summary>
        /// Gets or sets the language setting.
        /// </summary>
        public LanguageSetting LanguageSetting
        {
            get => (LanguageSetting)this.LanguageSettingInt;

            set => this.LanguageSettingInt = (int)value;
        }
    }
}