using TaskManagerMVC.DataAccess.Services;

namespace TaskManagerMVC.Tests
{
    public class PasswordHelperTests
    {
        private readonly PasswordHelper _helper = new();

        // ==================== HASHING ====================

        [Fact]
        public void HashPassword_ThenVerify_WithSamePassword_ReturnsTrue()
        {
            var hash = _helper.HashPassword("Zx9!kQ7#mW2p");

            Assert.True(_helper.VerifyPassword("Zx9!kQ7#mW2p", hash));
        }

        [Fact]
        public void VerifyPassword_WithWrongPassword_ReturnsFalse()
        {
            var hash = _helper.HashPassword("Zx9!kQ7#mW2p");

            Assert.False(_helper.VerifyPassword("Zx9!kQ7#mW2q", hash));
        }

        [Fact]
        public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
        {
            var first = _helper.HashPassword("Zx9!kQ7#mW2p");
            var second = _helper.HashPassword("Zx9!kQ7#mW2p");

            Assert.NotEqual(first, second);
            Assert.True(_helper.VerifyPassword("Zx9!kQ7#mW2p", first));
            Assert.True(_helper.VerifyPassword("Zx9!kQ7#mW2p", second));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-hash")]
        [InlineData("bad;base64!")]
        public void VerifyPassword_WithMalformedHash_ReturnsFalse(string storedHash)
        {
            Assert.False(_helper.VerifyPassword("Zx9!kQ7#mW2p", storedHash));
        }

        // ==================== STRENGTH VALIDATION ====================

        [Fact]
        public void ValidatePasswordStrength_StrongPassword_IsValid()
        {
            var result = _helper.ValidatePasswordStrength("Zx9!kQ7#mW2p");

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Equal(PasswordStrength.Strong, result.Strength);
        }

        [Fact]
        public void ValidatePasswordStrength_Empty_IsRequiredAndVeryWeak()
        {
            var result = _helper.ValidatePasswordStrength("");

            Assert.False(result.IsValid);
            Assert.Contains("Password is required.", result.Errors);
            Assert.Equal(PasswordStrength.VeryWeak, result.Strength);
        }

        [Theory]
        [InlineData("Zx9!kQ7", "at least 8 characters")]
        [InlineData("zx9!kq7#mw2p", "uppercase letter")]
        [InlineData("ZX9!KQ7#MW2P", "lowercase letter")]
        [InlineData("Zx!kQ#mWpRtY", "digit")]
        [InlineData("Zx9kQ7mW2pRt", "special character")]
        [InlineData("Zx9!kQ7#abc2", "sequential characters")]
        [InlineData("Zx9!kQ7#cba2", "sequential characters")]
        [InlineData("Zx9!kQ7#mmm2", "repeating characters")]
        [InlineData("Password1!Zx", "commonly used word")]
        public void ValidatePasswordStrength_BreaksOneRule_ReportsThatRule(string password, string expectedError)
        {
            var result = _helper.ValidatePasswordStrength(password);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains(expectedError));
        }

        [Fact]
        public void ValidatePasswordStrength_ContainsUsername_IsRejected()
        {
            var result = _helper.ValidatePasswordStrength("Zx9!LokeshQ7", username: "lokesh");

            Assert.False(result.IsValid);
            Assert.Contains("Password cannot contain your username.", result.Errors);
        }

        [Fact]
        public void ValidatePasswordStrength_TooLong_IsRejected()
        {
            var result = _helper.ValidatePasswordStrength("Zx9!kQ7#mW2p" + new string('y', 95));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("cannot exceed 100 characters"));
        }

        [Theory]
        [InlineData("aaaaaaaa", PasswordStrength.VeryWeak)]
        [InlineData("Zx9!kQ7#", PasswordStrength.Strong)]
        [InlineData("Zx9!kQ7#mW2pRt5$", PasswordStrength.VeryStrong)]
        public void ValidatePasswordStrength_ScoresStrength(string password, PasswordStrength expected)
        {
            Assert.Equal(expected, _helper.ValidatePasswordStrength(password).Strength);
        }
    }
}
