using System.ComponentModel.DataAnnotations;

namespace AlertSystem.Api.Models.DTOs.Preferences;

public class CreatePreferenceRequest
{
    [Required]
    [RegularExpression("^(breaking_news|market|natural_disaster|tech)$",
        ErrorMessage = "Category must be one of: breaking_news, market, natural_disaster, tech")]
    public string Category { get; set; } = string.Empty;

    public string? Keyword { get; set; }
}
