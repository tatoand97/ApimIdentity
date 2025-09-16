using System.ComponentModel.DataAnnotations;

namespace ApimIdenty.Options;

public class ApimOptions
{
    [Required]
    public string? BaseUrl { get; set; }

    [Required]
    public string? Scope { get; set; }

    public string? SampleEndpoint { get; set; }
}