using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Avalonia.Platform;

namespace FluentAvalonia.UI;

/// <summary>
/// Helper class for storing localized string for FluentAvalonia/WinUI controls
/// </summary>
/// <remarks>
/// The string resources are taken from the WinUI repo. Not all resources in WinUI
/// may be available here, only those that are known to be used in a control
/// </remarks>
public class FALocalizationHelper
{
	/// <summary>
	/// Dictionary of language entries for a resource name. &lt;language, value&gt; where
	/// language is the abbreviated name, e.g., en-US
	/// </summary>
	public class LocalizationEntry : Dictionary<string, string>
	{
		public LocalizationEntry()
			: base((IEqualityComparer<string>?)StringComparer.InvariantCultureIgnoreCase)
		{
		}
	}

	private class LocalizationMap : Dictionary<string, LocalizationEntry>
	{
		public LocalizationMap()
			: base((IEqualityComparer<string>?)StringComparer.InvariantCultureIgnoreCase)
		{
		}
	}

	[JsonSerializable(typeof(LocalizationMap))]
	[GeneratedCode("System.Text.Json.SourceGeneration", "10.0.14.27113")]
	private class FALocalizationJsonSerializerContext : JsonSerializerContext, IJsonTypeInfoResolver
	{
		private JsonTypeInfo<LocalizationEntry>? _LocalizationEntry;

		private JsonTypeInfo<LocalizationMap>? _LocalizationMap;

		private JsonTypeInfo<string>? _String;

		private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

		private const BindingFlags InstanceMemberBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		/// Defines the source generated JSON serialization contract metadata for a given type.
		/// </summary>
		public JsonTypeInfo<LocalizationEntry> LocalizationEntry => _LocalizationEntry ?? (_LocalizationEntry = (JsonTypeInfo<LocalizationEntry>)base.Options.GetTypeInfo(typeof(LocalizationEntry)));

		/// <summary>
		/// Defines the source generated JSON serialization contract metadata for a given type.
		/// </summary>
		public JsonTypeInfo<LocalizationMap> LocalizationMap => _LocalizationMap ?? (_LocalizationMap = (JsonTypeInfo<LocalizationMap>)base.Options.GetTypeInfo(typeof(LocalizationMap)));

		/// <summary>
		/// Defines the source generated JSON serialization contract metadata for a given type.
		/// </summary>
		public JsonTypeInfo<string> String => _String ?? (_String = (JsonTypeInfo<string>)base.Options.GetTypeInfo(typeof(string)));

		/// <summary>
		/// The default <see cref="T:System.Text.Json.Serialization.JsonSerializerContext" /> associated with a default <see cref="T:System.Text.Json.JsonSerializerOptions" /> instance.
		/// </summary>
		public static FALocalizationJsonSerializerContext Default { get; } = new FALocalizationJsonSerializerContext(new JsonSerializerOptions(s_defaultOptions));

		/// <summary>
		/// The source-generated options associated with this context.
		/// </summary>
		protected override JsonSerializerOptions? GeneratedSerializerOptions { get; } = s_defaultOptions;

		private JsonTypeInfo<LocalizationEntry> Create_LocalizationEntry(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LocalizationEntry> jsonTypeInfo))
			{
				JsonCollectionInfoValues<LocalizationEntry> collectionInfo = new JsonCollectionInfoValues<LocalizationEntry>
				{
					ObjectCreator = () => new LocalizationEntry(),
					SerializeHandler = LocalizationEntrySerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<LocalizationEntry, string, string>(options, collectionInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private void LocalizationEntrySerializeHandler(Utf8JsonWriter writer, LocalizationEntry? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			foreach (KeyValuePair<string, string> item in value)
			{
				writer.WriteString(item.Key, item.Value);
			}
			writer.WriteEndObject();
		}

		private JsonTypeInfo<LocalizationMap> Create_LocalizationMap(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<LocalizationMap> jsonTypeInfo))
			{
				JsonCollectionInfoValues<LocalizationMap> collectionInfo = new JsonCollectionInfoValues<LocalizationMap>
				{
					ObjectCreator = () => new LocalizationMap(),
					SerializeHandler = LocalizationMapSerializeHandler
				};
				jsonTypeInfo = JsonMetadataServices.CreateDictionaryInfo<LocalizationMap, string, LocalizationEntry>(options, collectionInfo);
				jsonTypeInfo.NumberHandling = null;
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		private void LocalizationMapSerializeHandler(Utf8JsonWriter writer, LocalizationMap? value)
		{
			if (value == null)
			{
				writer.WriteNullValue();
				return;
			}
			writer.WriteStartObject();
			foreach (KeyValuePair<string, LocalizationEntry> item in value)
			{
				writer.WritePropertyName(item.Key);
				LocalizationEntrySerializeHandler(writer, item.Value);
			}
			writer.WriteEndObject();
		}

		private JsonTypeInfo<string> Create_String(JsonSerializerOptions options)
		{
			if (!TryGetTypeInfoForRuntimeCustomConverter(options, out JsonTypeInfo<string> jsonTypeInfo))
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
			}
			jsonTypeInfo.OriginatingResolver = this;
			return jsonTypeInfo;
		}

		/// <inheritdoc />
		public FALocalizationJsonSerializerContext()
			: base(null)
		{
		}

		/// <inheritdoc />
		public FALocalizationJsonSerializerContext(JsonSerializerOptions options)
			: base(options)
		{
		}

		private static bool TryGetTypeInfoForRuntimeCustomConverter<TJsonMetadataType>(JsonSerializerOptions options, out JsonTypeInfo<TJsonMetadataType> jsonTypeInfo)
		{
			JsonConverter runtimeConverterForType = GetRuntimeConverterForType(typeof(TJsonMetadataType), options);
			if (runtimeConverterForType != null)
			{
				jsonTypeInfo = JsonMetadataServices.CreateValueInfo<TJsonMetadataType>(options, runtimeConverterForType);
				return true;
			}
			jsonTypeInfo = null;
			return false;
		}

		private static JsonConverter? GetRuntimeConverterForType(Type type, JsonSerializerOptions options)
		{
			for (int i = 0; i < options.Converters.Count; i++)
			{
				JsonConverter jsonConverter = options.Converters[i];
				if (jsonConverter != null && jsonConverter.CanConvert(type))
				{
					return ExpandConverter(type, jsonConverter, options, validateCanConvert: false);
				}
			}
			return null;
		}

		private static JsonConverter ExpandConverter(Type type, JsonConverter converter, JsonSerializerOptions options, bool validateCanConvert = true)
		{
			if (validateCanConvert && !converter.CanConvert(type))
			{
				throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
			}
			if (converter is JsonConverterFactory jsonConverterFactory)
			{
				converter = jsonConverterFactory.CreateConverter(type, options);
				if (converter == null || converter is JsonConverterFactory)
				{
					throw new InvalidOperationException($"The converter '{jsonConverterFactory.GetType()}' cannot return null or a JsonConverterFactory instance.");
				}
			}
			return converter;
		}

		/// <inheritdoc />
		public override JsonTypeInfo? GetTypeInfo(Type type)
		{
			base.Options.TryGetTypeInfo(type, out JsonTypeInfo typeInfo);
			return typeInfo;
		}

		JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			if (type == typeof(LocalizationEntry))
			{
				return Create_LocalizationEntry(options);
			}
			if (type == typeof(LocalizationMap))
			{
				return Create_LocalizationMap(options);
			}
			if (type == typeof(string))
			{
				return Create_String(options);
			}
			return null;
		}
	}

	private readonly LocalizationMap _mappings;

	private static readonly string s_enUS;

	public static FALocalizationHelper Instance { get; }

	private FALocalizationHelper()
	{
		using Stream utf8Json = AssetLoader.Open(new Uri("avares://FluentAvalonia/Assets/ControlStrings.json"), (Uri)null);
		_mappings = JsonSerializer.Deserialize(utf8Json, FALocalizationJsonSerializerContext.Default.LocalizationMap);
	}

	static FALocalizationHelper()
	{
		s_enUS = "en-US";
		Instance = new FALocalizationHelper();
	}

	/// <summary>
	/// Gets a string resource by the specified name using the CurrentUICulture
	/// </summary>
	public string GetLocalizedStringResource(string resName)
	{
		return GetLocalizedStringResource(CultureInfo.CurrentUICulture, resName);
	}

	/// <summary>
	/// Gets a string resource by the specified name and using the specified culture
	/// </summary>
	/// <remarks>
	/// InvariantCulture is not supported here and will default to en-US
	/// </remarks>
	public string GetLocalizedStringResource(CultureInfo ci, string resName)
	{
		string key = ((ci != CultureInfo.InvariantCulture) ? ci.Name : s_enUS);
		if (_mappings.ContainsKey(resName))
		{
			LocalizationEntry localizationEntry = _mappings[resName];
			if (localizationEntry.ContainsKey(key))
			{
				return localizationEntry[key];
			}
			if (localizationEntry.ContainsKey(s_enUS))
			{
				return localizationEntry[s_enUS];
			}
		}
		return string.Empty;
	}
}
