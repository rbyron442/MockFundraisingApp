using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using MockFundraisingApp.Controllers;
using System.Reflection;


namespace MockFundraisingApp.Tests
{
    public class AuthorizeAttributeTests
    {
        [Fact]
        public void RequestsController_CreateGet_IsProtected()
        {
            var method = typeof(RequestsController).GetMethods()
                .Single(m => m.Name == "Create" && m.GetParameters().Length == 0);

            method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Should().NotBeEmpty();
        }

        [Fact]
        public void RequestsController_CreatePost_IsProtected()
        {
            var method = typeof(RequestsController).GetMethods()
                .Single(m => m.Name == "Create" && m.GetParameters().Length == 1);

            method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Should().NotBeEmpty();
        }
    }
}
