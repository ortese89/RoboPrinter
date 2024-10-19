using FrontEnds.RoboPrinter.Resources;
using FrontEnds.RoboPrinter.Validations;
using System.ComponentModel.DataAnnotations;

namespace FrontEnds.RoboPrinter.Models.Dto;

public class NewProductDto
{
    [Required(ErrorMessageResourceName = "NomeProdottoRichiesto", ErrorMessageResourceType = typeof(Common))]
    public string Name { get; set; }

    [RequiredInteger(ErrorMessageResourceName = "IdProdottoRichiesto", ErrorMessageResourceType = typeof(Common))]
    public int ProductIdToClone { get; set; }
}