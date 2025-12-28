namespace Identity.Api.Application.Command;

public class ApplicationCommand
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Url { get; set; }

    [Required]
    [MaxLength(255)]
    public string RedirectUrl { get; set; }
}
