using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace API.API_Clean_Architecture.Models.ProbelmDetails;

public class ApiProblemDetails : ProblemDetails {
	private readonly Dictionary<string, object?> _extensions;

	public ApiProblemDetails() {
		_extensions = new Dictionary<string, object?>();
	}

	public ApiProblemDetails(string title, int status, string? detail = null, string? type = null,
		string? instance = null)
		: this() {
		Title = title;
		Status = status;
		Detail = detail;
		Type = type;
		Instance = instance;
	}

    /// <summary>
    ///     Extensiones personalizadas que se serializan junto con las propiedades principales
    /// </summary>
    [JsonExtensionData]
	public IDictionary<string, object?> Extensions => _extensions;

    /// <summary>
    ///     Timestamp cuando ocurrió el problema
    /// </summary>
    [JsonPropertyName("timestamp")]
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     ID único para rastrear el problema
    /// </summary>
    [JsonPropertyName("traceId")]
	public string? TraceId { get; set; }

    /// <summary>
    ///     Errores de validación específicos
    /// </summary>
    [JsonPropertyName("errors")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IDictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    ///     Agrega una extensión personalizada de forma segura
    /// </summary>
    public void AddExtension(string key, object? value) {
		// Evitar conflictos con propiedades principales
		var reservedKeys = new[] { "timestamp", "traceId", "errors", "type", "title", "status", "detail", "instance", };

		if (reservedKeys.Contains(key.ToLower())) {
			throw new ArgumentException($"Cannot use reserved key '{key}'. Use the corresponding property instead.");
		}

		_extensions[key] = value;
	}

    /// <summary>
    ///     Agrega un error de validación
    /// </summary>
    public void AddError(string field, params string[] messages) {
		Errors ??= new Dictionary<string, string[]>();
		Errors[field] = messages;
	}
}