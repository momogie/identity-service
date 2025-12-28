namespace Identity.Api.User.Command;

public class UserCommand : ICommand
{
    [Required]
    public string Name { get; set; }

    [Required]
    [Display(Name = "Role")]
    public string RoleId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    [Required]
    [MinLength(6)]
    public string UserName { get; set; }
}
