//namespace Modules.Identity.Api.User.Command;

//public class UserCommand : ICommand
//{
//    [Required]
//    public string Name { get; set; }

//    [Required]
//    [Display(Name = "Role")]
//    public long? RoleId { get; set; }

//    //[Display(Name = "Group")]
//    //[RequiredIfEqual("Type", "PRINCIPLE")]
//    //public long? GroupId { get; set; }

//    [Required]
//    [MaxLength(255)]
//    public string Email { get; set; }

//    [Required]
//    [MinLength(6)]
//    public string UserName { get; set; }

//    public long? SalesId { get; set; }

//    //[Required]
//    //[MinLength(6)]
//    //public string Password { get; set; }

//    //[Display(Name = "Outlet")]
//    //public List<long> MerchantIds { get; set; }
//}
