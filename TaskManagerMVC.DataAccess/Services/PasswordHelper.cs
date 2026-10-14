using System.Security.Cryptography;

namespace TaskManagerMVC.DataAccess.Services
{
    /// <summary>
    /// Password Helper Interface
    /// Handles password hashing, verification, and strength validation
    /// </summary>
    public interface IPasswordHelper
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);

        // NEW: Password Strength Validation
        PasswordValidationResult ValidatePasswordStrength(string password, string? username = null);
    }

    /// <summary>
    /// Password Validation Result
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public PasswordStrength Strength { get; set; }
    }

    /// <summary>
    /// Password Strength Enum
    /// </summary>
    public enum PasswordStrength
    {
        VeryWeak = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3,
        VeryStrong = 4
    }

    /// <summary>
    /// Password Helper Implementation
    /// Uses PBKDF2 for secure password hashing
    /// Includes custom password strength validation
    /// </summary>
    public class PasswordHelper : IPasswordHelper
    {
        // ==================== PASSWORD SETTINGS (OVERRIDE DEFAULTS) ====================

        /// <summary>
        /// Minimum password length (Default Identity is 6, we override to 8)
        /// </summary>
        public const int MinimumLength = 8;

        /// <summary>
        /// Maximum password length
        /// </summary>
        public const int MaximumLength = 100;

        /// <summary>
        /// Require at least one digit (0-9)
        /// </summary>
        public const bool RequireDigit = true;

        /// <summary>
        /// Require at least one uppercase letter (A-Z)
        /// </summary>
        public const bool RequireUppercase = true;

        /// <summary>
        /// Require at least one lowercase letter (a-z)
        /// </summary>
        public const bool RequireLowercase = true;

        /// <summary>
        /// Require at least one special character (!@#$%^&* etc.)
        /// </summary>
        public const bool RequireSpecialCharacter = true;

        /// <summary>
        /// Minimum unique characters required
        /// </summary>
        public const int RequiredUniqueChars = 4;

        // Special characters allowed
        private const string SpecialCharacters = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Common passwords to reject
        private static readonly string[] CommonPasswords =
        {
            "password", "123456", "12345678", "qwerty", "admin", "letmein",
            "welcome", "monkey", "dragon", "master", "login", "abc123",
            "password123", "admin123", "root", "toor", "pass", "test"
        };

        // ==================== HASHING CONFIGURATION ====================

        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        // ==================== HASH PASSWORD ====================

        /// <summary>
        /// Creates a secure hash of the password using PBKDF2
        /// </summary>
        public string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return $"{Convert.ToBase64String(salt)};{Convert.ToBase64String(hash)}";
        }

        // ==================== VERIFY PASSWORD ====================

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                var parts = hashedPassword.Split(';');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storedHash = Convert.FromBase64String(parts[1]);

                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSize
                );

                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        // ==================== CUSTOM PASSWORD STRENGTH VALIDATOR ====================

        /// <summary>
        /// Validates password strength against custom rules
        /// This overrides ASP.NET Core Identity default password settings
        /// </summary>
        public PasswordValidationResult ValidatePasswordStrength(string password, string? username = null)
        {
            var result = new PasswordValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is required.");
                result.Strength = PasswordStrength.VeryWeak;
                return result;
            }

            // ==================== RULE 1: Minimum Length ====================
            if (password.Length < MinimumLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must be at least {MinimumLength} characters long.");
            }

            // ==================== RULE 2: Maximum Length ====================
            if (password.Length > MaximumLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password cannot exceed {MaximumLength} characters.");
            }

            // ==================== RULE 3: Require Uppercase ====================
            if (RequireUppercase && !password.Any(char.IsUpper))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter (A-Z).");
            }

            // ==================== RULE 4: Require Lowercase ====================
            if (RequireLowercase && !password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter (a-z).");
            }

            // ==================== RULE 5: Require Digit ====================
            if (RequireDigit && !password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one digit (0-9).");
            }

            // ==================== RULE 6: Require Special Character ====================
            if (RequireSpecialCharacter && !password.Any(c => SpecialCharacters.Contains(c)))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character (!@#$%^&* etc.).");
            }

            // ==================== RULE 7: Required Unique Characters ====================
            if (password.Distinct().Count() < RequiredUniqueChars)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must contain at least {RequiredUniqueChars} unique characters.");
            }

            // ==================== RULE 8: No Sequential Characters (abc, 123) ====================
            if (HasSequentialChars(password, 3))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot contain sequential characters (e.g., abc, 123, cba, 321).");
            }

            // ==================== RULE 9: No Repeating Characters (aaa, 111) ====================
            if (HasRepeatingChars(password, 3))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot contain 3 or more repeating characters (e.g., aaa, 111).");
            }

            // ==================== RULE 10: Cannot Contain Username ====================
            if (!string.IsNullOrEmpty(username) && password.ToLower().Contains(username.ToLower()))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot contain your username.");
            }

            // ==================== RULE 11: Common Password Check ====================
            if (CommonPasswords.Any(cp => password.ToLower().Contains(cp)))
            {
                result.IsValid = false;
                result.Errors.Add("Password contains a commonly used word. Please choose a stronger password.");
            }

            // Calculate strength
            result.Strength = CalculateStrength(password);

            return result;
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Check for sequential characters (abc, 123, cba, 321)
        /// </summary>
        private bool HasSequentialChars(string password, int length)
        {
            if (password.Length < length) return false;

            for (int i = 0; i <= password.Length - length; i++)
            {
                // Check ascending sequence (abc, 123)
                bool isAscending = true;
                for (int j = 1; j < length; j++)
                {
                    if (password[i + j] != password[i + j - 1] + 1)
                    {
                        isAscending = false;
                        break;
                    }
                }
                if (isAscending) return true;

                // Check descending sequence (cba, 321)
                bool isDescending = true;
                for (int j = 1; j < length; j++)
                {
                    if (password[i + j] != password[i + j - 1] - 1)
                    {
                        isDescending = false;
                        break;
                    }
                }
                if (isDescending) return true;
            }
            return false;
        }

        /// <summary>
        /// Check for repeating characters (aaa, 111)
        /// </summary>
        private bool HasRepeatingChars(string password, int length)
        {
            if (password.Length < length) return false;

            for (int i = 0; i <= password.Length - length; i++)
            {
                bool isRepeating = true;
                for (int j = 1; j < length; j++)
                {
                    if (password[i + j] != password[i])
                    {
                        isRepeating = false;
                        break;
                    }
                }
                if (isRepeating) return true;
            }
            return false;
        }

        /// <summary>
        /// Calculate password strength score
        /// </summary>
        private PasswordStrength CalculateStrength(string password)
        {
            int score = 0;

            // Length scoring
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;

            // Complexity scoring
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(c => SpecialCharacters.Contains(c))) score++;

            // Unique characters bonus
            if (password.Distinct().Count() >= 8) score++;

            return score switch
            {
                <= 2 => PasswordStrength.VeryWeak,
                3 => PasswordStrength.Weak,
                4 or 5 => PasswordStrength.Medium,
                6 or 7 => PasswordStrength.Strong,
                _ => PasswordStrength.VeryStrong
            };
        }
    }
}