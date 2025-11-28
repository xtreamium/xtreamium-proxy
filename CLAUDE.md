#Configuration
IOptions<AppConfiguration> is only uses for initial settings, in day to day operation all settings should be read from the database through the ISettingsRepository interface. 