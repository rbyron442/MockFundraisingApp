using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MockFundraisingApp.ViewModels;

namespace MockFundraisingApp.Tests;

public class CreateRequestVmValidationTests
{
    [Fact]
    public void Requester_WhenEmpty_FailsValidation()
    {
        var vm = new CreateRequestVm
        {
            Requester = "",
            DonationLimit = 100,
            RequestType = "Medical"
        };

        var results = Validate(vm);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(CreateRequestVm.Requester)));
    }

    [Fact]
    public void DonationLimit_WhenZero_FailsValidation()
    {
        var vm = new CreateRequestVm
        {
            Requester = "Rich",
            DonationLimit = 0,
            RequestType = "Medical"
        };

        var results = Validate(vm);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(CreateRequestVm.DonationLimit)));
    }

    [Fact]
    public void RequestType_WhenEmpty_FailsValidation()
    {
        var vm = new CreateRequestVm
        {
            Requester = "Rich",
            DonationLimit = 100,
            RequestType = ""
        };

        var results = Validate(vm);

        results.Should().Contain(r => r.MemberNames.Contains(nameof(CreateRequestVm.RequestType)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }
}
