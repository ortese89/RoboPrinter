using FrontEnds.RoboPrinter.Resources;
using System.ComponentModel.DataAnnotations;

namespace FrontEnds.RoboPrinter.Models.Dto;

public class LoginDto
{
    [Required(ErrorMessageResourceName = "UsernameRichiesto", ErrorMessageResourceType = typeof(Common))]
    public string Username { get; set; }

    [Required(ErrorMessageResourceName = "PasswordRichiesta", ErrorMessageResourceType = typeof(Common))]
    public string Password { get; set; }
}