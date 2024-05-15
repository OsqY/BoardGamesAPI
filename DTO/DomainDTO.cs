using System.ComponentModel.DataAnnotations;

namespace MyBGList.DTO
{
    public class DomainDTO
    {
        [Required]
        public int Id { get; set; }

        [RegularExpression("^[A-Za-z]+$", ErrorMessage =
            "Value must contain only letters(no spaces, digits or other chars.)")]
        public string? Name { get; set; }
    }
}
