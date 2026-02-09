using MockFundraisingApp.Models;
using System.ComponentModel.DataAnnotations;

namespace MockFundraisingApp.ViewModels
{
    public class RequestDetailsVm
    {
        public FundraisingRequest Request { get; set; } = new();
        public List<Donation> Donations { get; set; } = new();

        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(80)]
        public string DonorName { get; set; } = "";

        [Range(1, 1_000_000)]
        public double Amount { get; set; }
    }
}
