using System.ComponentModel.DataAnnotations;

namespace MockFundraisingApp.ViewModels
{
    public class CreateRequestVm
    {
        [Required]
        [StringLength(100)]
        public string Requester { get; set; } = "";

        [Required]
        [Range(1, 1_000_000)]
        public double DonationLimit { get; set; }

        [Required]
        public string RequestType { get; set; } = "";
    }
}
