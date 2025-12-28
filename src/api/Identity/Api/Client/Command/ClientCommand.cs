namespace Identity.Api.Client.Command;

public class ClientCommand
{
    [Required]
    [MaxLength(255)]
    public string ApplicationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
}
