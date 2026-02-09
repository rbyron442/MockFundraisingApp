using FluentAssertions;
using MockFundraisingApp.ViewModels;
using System.ComponentModel.DataAnnotations;


namespace MockFundraisingApp.Tests
{
    public class ValidationTests
    {
        [Fact]
        public void Amount_WhenZero_FailsValidation()
        {
            var vm = new RequestDetailsVm
            {
                DonorName = "Rich",
                Amount = 0
            };

            var ctx = new ValidationContext(vm);
            var results = new List<ValidationResult>();

            var ok = Validator.TryValidateObject(vm, ctx, results, validateAllProperties: true);

            ok.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(RequestDetailsVm.Amount)));
        }

        [Fact]
        public void DonorName_WhenEmpty_ShowsFriendlyMessage()
        {
            var vm = new RequestDetailsVm
            {
                DonorName = "",
                Amount = 10
            };

            var ctx = new ValidationContext(vm);
            var results = new List<ValidationResult>();

            var ok = Validator.TryValidateObject(vm, ctx, results, validateAllProperties: true);

            ok.Should().BeFalse();
            results.Select(r => r.ErrorMessage).Should().Contain("Please enter your name.");
        }
    }
}