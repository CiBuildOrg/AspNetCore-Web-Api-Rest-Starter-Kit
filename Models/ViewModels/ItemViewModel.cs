using System.ComponentModel.DataAnnotations;

namespace SampleApi.Models.ViewModels
{
    public class ItemViewModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public int Cost { get; set; }
        public string Color { get; set; }
    }
}