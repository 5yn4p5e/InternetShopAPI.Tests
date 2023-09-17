using FluentAssertions;
using InternetShop.Controllers;
using InternetShop.Interfaces;
using InternetShop.InternetShopModels;
using InternetShop.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace InternetShop.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _mockAccountRepository = new Mock<IAccountRepository>();
            _controller = new AccountController(_mockAccountRepository.Object);
        }

        [Fact]
        public async Task Login_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var testEmail = "testuser@example.com";
            var testPassword = "testpassword";
            var loginViewModel = new LoginViewModel
            {
                Email = testEmail,
                Password = testPassword,
                RememberMe = false
            };
            var testUser = new User { Email = testEmail, UserName = testEmail };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, testEmail) }));
            var userRole = "user";
            var signInResult = SignInResult.Success;

            _mockAccountRepository.Setup(x => x.FindByEmailAsync(testEmail)).ReturnsAsync(testUser);
            _mockAccountRepository.Setup(x => x.PasswordSignInAsync(testEmail, testPassword, false)).ReturnsAsync(signInResult);
            _mockAccountRepository.Setup(x => x.GetRolesAsync(testUser)).ReturnsAsync(new List<string> { userRole });
            _mockAccountRepository.Setup(x => x.GetUserAsync(claimsPrincipal)).ReturnsAsync(testUser);

            // Act
            var result = await _controller.Login(loginViewModel);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new
            {
                message = "Выполнен вход",
                userName = testUser.Email,
                userRole
            });
        }

        [Fact]
        public async Task Login_WithInvalidEmail_ReturnsCreatedResult()
        {
            // Arrange
            var testEmail = "testuser@example.com";
            var testPassword = "testpassword";
            var loginViewModel = new LoginViewModel
            {
                Email = testEmail,
                Password = testPassword
            };

            _mockAccountRepository.Setup(x => x.PasswordSignInAsync(testEmail, testPassword, false)).ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _controller.Login(loginViewModel);

            // Assert
            result.Should().BeOfType<CreatedResult>().Subject.Value.Should().BeEquivalentTo(new
            {
                message = "Вход не выполнен",
                error = new[] { "Неправильный логин и (или) пароль" }
            });
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsCreatedResult()
        {
            // Arrange
            var testEmail = "testuser@example.com";
            var testPassword = "testpassword";
            var loginViewModel = new LoginViewModel
            {
                Email = testEmail,
                Password = testPassword
            };
            var testUser = new User { Email = testEmail };
            var signInResult = SignInResult.Failed;

            _mockAccountRepository.Setup(x => x.FindByEmailAsync(testEmail)).ReturnsAsync(testUser);
            _mockAccountRepository.Setup(x => x.PasswordSignInAsync(testEmail, testPassword, false)).ReturnsAsync(signInResult);

            // Act
            var result = await _controller.Login(loginViewModel);

            // Assert
            result.Should().BeOfType<CreatedResult>().Which.Value.Should().BeEquivalentTo(new
            {
                message = "Вход не выполнен"
            });
        }
    }
}