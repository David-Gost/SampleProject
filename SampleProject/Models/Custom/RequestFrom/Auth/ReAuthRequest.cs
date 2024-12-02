using System.ComponentModel.DataAnnotations;

namespace SampleProject.Models.Custom.RequestFrom.Auth;

public class ReAuthRequest
{
    [Required]
    public string refreshToken { get; set; }
}