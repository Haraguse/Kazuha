using System.Text.Json;

namespace Luminalium.Core.Configuration;

public sealed class ConfigurationService
{
    public const string BaseSettingsFileName = "settings.json";
    public const string ActiveProfileFileName = "_active";

    private readonly string _settingsDirectory;

    public ConfigurationService(string settingsDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsDirectoryPath);
        _settingsDirectory = Path.GetFullPath(settingsDirectoryPath);
    }

    public string SettingsDirectoryPath => _settingsDirectory;

    public string GetActiveSettingsPath()
    {
        var basePath = Path.Combine(_settingsDirectory, BaseSettingsFileName);
        var markerPath = Path.Combine(_settingsDirectory, ActiveProfileFileName);

        if (!File.Exists(markerPath))
        {
            return basePath;
        }

        try
        {
            var profileName = File.ReadAllText(markerPath).Trim();
            if (profileName.Length == 0 || profileName == "default")
            {
                return basePath;
            }

            if (Path.GetFileName(profileName) != profileName ||
                profileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return basePath;
            }

            var profilePath = Path.Combine(_settingsDirectory, profileName + ".json");
            return File.Exists(profilePath) ? profilePath : basePath;
        }
        catch (IOException)
        {
            return basePath;
        }
        catch (UnauthorizedAccessException)
        {
            return basePath;
        }
    }

    public ConfigurationLoadResult Load()
    {
        var path = GetActiveSettingsPath();

        try
        {
            Directory.CreateDirectory(_settingsDirectory);
        }
        catch (IOException exception)
        {
            return RecoveredDefault(
                path,
                new ConfigurationLoadWarning(
                    ConfigurationLoadWarningCode.ReadFailure,
                    path,
                    $"Configuration directory could not be created: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            return RecoveredDefault(
                path,
                new ConfigurationLoadWarning(
                    ConfigurationLoadWarningCode.ReadFailure,
                    path,
                    $"Configuration directory could not be created: {exception.Message}"));
        }

        if (!File.Exists(path))
        {
            var config = LuminaliumConfig.CreateDefault();
            var saveResult = SaveToPath(config, path);
            if (saveResult.IsSuccess)
            {
                return ConfigurationLoadResult.CreatedDefault(config, path);
            }

            return RecoveredDefault(
                path,
                new ConfigurationLoadWarning(
                    ConfigurationLoadWarningCode.PersistenceFailure,
                    path,
                    $"Default configuration could not be persisted: {saveResult.Error!.Message}"));
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (IOException exception)
        {
            return RecoveredDefault(
                path,
                new ConfigurationLoadWarning(
                    ConfigurationLoadWarningCode.ReadFailure,
                    path,
                    $"Configuration could not be read: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            return RecoveredDefault(
                path,
                new ConfigurationLoadWarning(
                    ConfigurationLoadWarningCode.ReadFailure,
                    path,
                    $"Configuration could not be read: {exception.Message}"));
        }

        try
        {
            var jsonBytes = HasUtf8Bom(bytes) ? bytes[3..] : bytes;
            using var document = JsonDocument.Parse(
                jsonBytes,
                new JsonDocumentOptions
                {
                    AllowDuplicateProperties = false,
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                });
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                return RecoverCorrupt(
                    path,
                    bytes,
                    ConfigurationLoadWarningCode.InvalidRoot,
                    "Configuration JSON must contain an object at the root.");
            }

            var schemaVersionIssue = ValidateSchemaVersion(document.RootElement);
            if (schemaVersionIssue is not null)
            {
                return RecoverCorrupt(
                    path,
                    bytes,
                    schemaVersionIssue.Code,
                    schemaVersionIssue.Message,
                    schemaVersionIssue.Field);
            }

            var config = document.RootElement.Deserialize<LuminaliumConfig>(ConfigurationJson.Options);
            if (config is null)
            {
                return RecoverCorrupt(
                    path,
                    bytes,
                    ConfigurationLoadWarningCode.InvalidRoot,
                    "Configuration JSON must contain an object at the root.");
            }

            var issue = Validate(config);
            if (issue is not null)
            {
                return RecoverCorrupt(path, bytes, issue.Code, issue.Message, issue.Field);
            }

            return ConfigurationLoadResult.Loaded(config, path);
        }
        catch (JsonException exception)
        {
            return RecoverCorrupt(
                path,
                bytes,
                ConfigurationLoadWarningCode.MalformedJson,
                $"Configuration JSON is malformed at line {exception.LineNumber}, byte {exception.BytePositionInLine}: {exception.Message}");
        }
        catch (NotSupportedException exception)
        {
            return RecoverCorrupt(
                path,
                bytes,
                ConfigurationLoadWarningCode.InvalidValue,
                $"Configuration contains an unsupported value: {exception.Message}");
        }
    }

    public ConfigurationSaveResult Save(LuminaliumConfig config)
    {
        var path = GetActiveSettingsPath();
        return SaveToPath(config, path);
    }

    private ConfigurationSaveResult SaveToPath(LuminaliumConfig config, string path)
    {
        if (config is null)
        {
            return ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.InvalidConfiguration,
                    path,
                    "Configuration must not be null."));
        }

        var issue = Validate(config);
        if (issue is not null)
        {
            return ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.InvalidConfiguration,
                    path,
                    issue.Message,
                    issue.Field));
        }

        byte[] bytes;
        try
        {
            bytes = JsonSerializer.SerializeToUtf8Bytes(config, ConfigurationJson.Options);
        }
        catch (JsonException exception)
        {
            return ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.SerializationFailure,
                    path,
                    $"Configuration could not be serialized: {exception.Message}"));
        }
        catch (NotSupportedException exception)
        {
            return ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.SerializationFailure,
                    path,
                    $"Configuration contains an unsupported value: {exception.Message}"));
        }

        try
        {
            Directory.CreateDirectory(_settingsDirectory);
        }
        catch (IOException exception)
        {
            return ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.WriteFailure,
                    path,
                    $"Configuration directory could not be created: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            return ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.WriteFailure,
                    path,
                    $"Configuration directory could not be created: {exception.Message}"));
        }

        var temporaryPath = Path.Combine(
            _settingsDirectory,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        ConfigurationSaveResult result;

        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
            result = ConfigurationSaveResult.Success(path);
        }
        catch (IOException exception)
        {
            result = ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.ReplacementFailure,
                    path,
                    $"Configuration could not be atomically replaced: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            result = ConfigurationSaveResult.Failure(
                new ConfigurationSaveError(
                    ConfigurationSaveErrorCode.ReplacementFailure,
                    path,
                    $"Configuration could not be atomically replaced: {exception.Message}"));
        }

        if (File.Exists(temporaryPath))
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException exception)
            {
                result = ConfigurationSaveResult.Failure(
                    new ConfigurationSaveError(
                        ConfigurationSaveErrorCode.TemporaryFileCleanupFailure,
                        path,
                        $"Temporary configuration file could not be removed: {exception.Message}"));
            }
            catch (UnauthorizedAccessException exception)
            {
                result = ConfigurationSaveResult.Failure(
                    new ConfigurationSaveError(
                        ConfigurationSaveErrorCode.TemporaryFileCleanupFailure,
                        path,
                        $"Temporary configuration file could not be removed: {exception.Message}"));
            }
        }

        return result;
    }

    private ConfigurationLoadResult RecoverCorrupt(
        string path,
        byte[] bytes,
        ConfigurationLoadWarningCode code,
        string message,
        string? field = null)
    {
        string? backupPath = null;
        string? backupFailure = null;

        try
        {
            backupPath = Path.Combine(
                _settingsDirectory,
                $"{Path.GetFileNameWithoutExtension(path)}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.bak");
            File.WriteAllBytes(backupPath, bytes);
        }
        catch (IOException exception)
        {
            backupFailure = $" The corrupt bytes could not be backed up: {exception.Message}";
            backupPath = null;
        }
        catch (UnauthorizedAccessException exception)
        {
            backupFailure = $" The corrupt bytes could not be backed up: {exception.Message}";
            backupPath = null;
        }

        var warning = new ConfigurationLoadWarning(
            code,
            path,
            message + (backupPath is null ? backupFailure : $" The original bytes were backed up to '{backupPath}'."),
            backupPath,
            field);
        return RecoveredDefault(path, warning);
    }

    private static ConfigurationLoadResult RecoveredDefault(
        string path,
        ConfigurationLoadWarning warning) =>
        ConfigurationLoadResult.RecoveredDefault(LuminaliumConfig.CreateDefault(), path, warning);

    private static ConfigurationValidationIssue? Validate(LuminaliumConfig config)
    {
        if (config.SchemaVersion != LuminaliumConfig.CurrentSchemaVersion)
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidSchemaVersion,
                $"Configuration schema version {config.SchemaVersion} is not supported; expected {LuminaliumConfig.CurrentSchemaVersion}.",
                nameof(LuminaliumConfig.SchemaVersion));
        }

        if (config.Appearance is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Appearance));
        }

        if (config.General is null)
        {
            return MissingSection(nameof(LuminaliumConfig.General));
        }

        if (config.Toolbar is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Toolbar));
        }

        if (config.Linkage is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Linkage));
        }

        if (config.Overlay is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Overlay));
        }

        if (config.Ppt is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Ppt));
        }

        if (config.SelfPen is null)
        {
            return MissingSection(nameof(LuminaliumConfig.SelfPen));
        }

        if (config.Notifications is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Notifications));
        }

        if (config.Security is null)
        {
            return MissingSection(nameof(LuminaliumConfig.Security));
        }

        if (config.Security.PasswordProtectionEnabled &&
            string.IsNullOrWhiteSpace(config.Security.PasswordHash))
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidValue,
                "PasswordHash must contain a salted hash when password protection is enabled.",
                nameof(SecuritySettings.PasswordHash));
        }

        if (!string.IsNullOrEmpty(config.Security.PasswordHash) &&
            !IsSaltedPasswordHash(config.Security.PasswordHash))
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidValue,
                "PasswordHash must use an algorithm-prefixed salted-hash format.",
                nameof(SecuritySettings.PasswordHash));
        }

        if (config.Appearance.ThemeId is null ||
            config.Appearance.OverlayTheme is null ||
            config.Appearance.AccentColor is null)
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidValue,
                "Appearance string values must not be null.",
                nameof(LuminaliumConfig.Appearance));
        }

        if (config.Toolbar.QuickLaunchApps is null ||
            config.Toolbar.ToolbarOrder is null ||
            config.Toolbar.DisabledTools is null)
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidValue,
                "Toolbar arrays must not be null.",
                nameof(LuminaliumConfig.Toolbar));
        }

        if (config.General.SplashStyle is null ||
            config.General.SplashStartTime is null ||
            config.General.SplashEndTime is null ||
            config.Overlay.OverlayScreen is null ||
            config.Ppt is null ||
            config.SelfPen.PenColor is null ||
            config.SelfPen.HighlightColor is null ||
            config.SelfPen.CustomPenColors is null ||
            config.SelfPen.CustomHighlightColors is null ||
            config.Security.PasswordHash is null)
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidValue,
                "Configuration string and array values must not be null.");
        }

        return null;
    }

    private static ConfigurationValidationIssue? ValidateSchemaVersion(JsonElement root)
    {
        if (!root.TryGetProperty(nameof(LuminaliumConfig.SchemaVersion), out var property))
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidSchemaVersion,
                $"Configuration is missing required field '{nameof(LuminaliumConfig.SchemaVersion)}'.",
                nameof(LuminaliumConfig.SchemaVersion));
        }

        if (property.ValueKind is not JsonValueKind.Number || !property.TryGetInt32(out _))
        {
            return new ConfigurationValidationIssue(
                ConfigurationLoadWarningCode.InvalidSchemaVersion,
                $"Configuration field '{nameof(LuminaliumConfig.SchemaVersion)}' must be a 32-bit integer.",
                nameof(LuminaliumConfig.SchemaVersion));
        }

        return null;
    }

    private static bool IsSaltedPasswordHash(string value)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var components = value.Split('$', StringSplitOptions.RemoveEmptyEntries);
        return components.Length >= 3 &&
            components[0] is "scrypt" or "argon2id" or "pbkdf2";
    }

    private static bool HasUtf8Bom(byte[] bytes) =>
        bytes.Length >= 3 &&
        bytes[0] == 0xEF &&
        bytes[1] == 0xBB &&
        bytes[2] == 0xBF;

    private static ConfigurationValidationIssue MissingSection(string section) =>
        new(
            ConfigurationLoadWarningCode.InvalidSection,
            $"Configuration section '{section}' must not be null.",
            section);
}
