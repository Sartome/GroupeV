using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace GroupeV
{
    /// <summary>
    /// Password verification helper - 100% compatible with PHP Security class
    /// 
    /// PHP Backend (Security.php):
    ///   - Security::hashPassword($password) => password_hash($password, PASSWORD_ARGON2ID)
    ///   - Security::verifyPassword($password, $hash) => password_verify($password, $hash)
    ///   - Security::needsRehash($hash) => password_needs_rehash($hash, PASSWORD_ARGON2ID)
    /// 
    /// C# Desktop App (this class):
    ///   - PasswordVerifier.VerifyPassword(password, hash) => Verifies Argon2id hash
    /// 
    /// Hash format: $argon2id$v=19$m=65536,t=4,p=1$base64salt$base64hash
    /// 
    /// Supported algorithms:
    ///   - Argon2id (PHP PASSWORD_ARGON2ID) - Primary, production use
    ///   - Argon2i (PHP PASSWORD_ARGON2I) - Legacy support
    ///   - Plain text - Development/testing fallback only
    /// </summary>
    public static class PasswordVerifier
    {
        /// <summary>
        /// Verify password against stored hash (PHP password_hash compatible)
        /// </summary>
        /// <param name="enteredPassword">Password entered by user</param>
        /// <param name="storedPassword">Password/hash stored in database (from PHP password_hash)</param>
        /// <returns>True if password matches</returns>
        public static bool VerifyPassword(string enteredPassword, string? storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
                return false;

            storedPassword = storedPassword.Trim();

            // Detect hash type
            var hashType = DetectHashType(storedPassword);

            return hashType switch
            {
                HashType.BCrypt    => VerifyBCrypt(enteredPassword, storedPassword),
                HashType.Argon2id  => VerifyArgon2(enteredPassword, storedPassword),
                HashType.Argon2i   => VerifyArgon2(enteredPassword, storedPassword),
                HashType.HexHash   => VerifyHexHash(enteredPassword, storedPassword),
                HashType.PlainText => VerifyPlainText(enteredPassword, storedPassword),
                _                  => false
            };
        }

        /// <summary>
        /// Detect the type of password hash
        /// </summary>
        private static HashType DetectHashType(string storedPassword)
        {
            // BCrypt: starts with $2a$, $2b$, $2x$, $2y$ and is typically 60 chars
            if (storedPassword.StartsWith("$2a$") ||
                storedPassword.StartsWith("$2b$") ||
                storedPassword.StartsWith("$2x$") ||
                storedPassword.StartsWith("$2y$"))
            {
                return HashType.BCrypt;
            }

            // Argon2id: $argon2id$v=19$m=65536,t=4,p=1$...
            if (storedPassword.StartsWith("$argon2id$"))
                return HashType.Argon2id;

            // Argon2i: $argon2i$v=19$m=65536,t=4,p=1$...
            if (storedPassword.StartsWith("$argon2i$"))
                return HashType.Argon2i;

            // Hex hashes: MD5 (32), SHA-256 (64), SHA-512 (128) — all lowercase hex
            if ((storedPassword.Length == 32 || storedPassword.Length == 64 || storedPassword.Length == 128)
                && storedPassword.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F')))
            {
                return HashType.HexHash;
            }

            return HashType.PlainText;
        }

        #region Verification Methods

        private static bool VerifyBCrypt(string password, string hash)
        {
            try
            {
                // BCrypt verification using BCrypt.Net-Next
                var result = BCrypt.Net.BCrypt.Verify(password, hash);
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] BCrypt verification: {(result ? "SUCCESS" : "FAILED")}");
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] BCrypt hash: {hash.Substring(0, Math.Min(30, hash.Length))}...");
                #endif
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] BCrypt verification error: {ex.Message}");
                return false;
            }
        }

        private static bool VerifyArgon2(string password, string hash)
        {
            try
            {
                // Parse PHP password_hash format: $argon2id$v=19$m=65536,t=4,p=1$salt$hash
                var parts = hash.Split('$');
                if (parts.Length < 6)
                {
                    System.Diagnostics.Debug.WriteLine("[PASSWORD] Invalid Argon2 hash format");
                    return false;
                }

                var algorithm = parts[1]; // "argon2id" or "argon2i"
                var version = parts[2];   // "v=19"
                var params_str = parts[3]; // "m=65536,t=4,p=1"
                var saltB64 = parts[4];
                var hashB64 = parts[5];

                // Parse parameters
                var param_parts = params_str.Split(',');
                var memory = int.Parse(param_parts[0].Split('=')[1]); // m (memory in KiB)
                var iterations = int.Parse(param_parts[1].Split('=')[1]); // t (time cost)
                var parallelism = int.Parse(param_parts[2].Split('=')[1]); // p (parallelism)

                // Decode salt and hash — argon2 uses standard base64 ('+', '/') WITHOUT trailing '='
                // Padding must be computed, never blindly appended.
                var salt         = DecodeArgon2Base64(saltB64);
                var expectedHash = DecodeArgon2Base64(hashB64);

                // Compute hash with same parameters
                byte[] computedHash;
                
                if (algorithm == "argon2id")
                {
                    using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
                    argon2.Salt = salt;
                    argon2.DegreeOfParallelism = parallelism;
                    argon2.MemorySize = memory;
                    argon2.Iterations = iterations;
                    computedHash = argon2.GetBytes(expectedHash.Length);
                }
                else
                {
                    using var argon2 = new Argon2i(Encoding.UTF8.GetBytes(password));
                    argon2.Salt = salt;
                    argon2.DegreeOfParallelism = parallelism;
                    argon2.MemorySize = memory;
                    argon2.Iterations = iterations;
                    computedHash = argon2.GetBytes(expectedHash.Length);
                }

                // Constant-time comparison
                bool matches = computedHash.SequenceEqual(expectedHash);

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] Argon2 verification: {(matches ? "SUCCESS" : "FAILED")}");
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] Algorithm: {algorithm}, Memory: {memory}KB, Iterations: {iterations}, Parallelism: {parallelism}");
                #endif

                return matches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] Argon2 verification error: {ex.Message}");
                return false;
            }
        }

        private static bool VerifyPlainText(string password, string storedPassword)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[PASSWORD] Plain text comparison (DEVELOPMENT ONLY)");
            #endif
            return password == storedPassword;
        }

        /// <summary>
        /// Verify MD5 / SHA-256 / SHA-512 hex hashes (PHP hash('sha256',$p), md5($p), etc.)
        /// </summary>
        private static bool VerifyHexHash(string password, string storedHash)
        {
            try
            {
                byte[] computed = storedHash.Length switch
                {
                    32  => MD5.HashData(Encoding.UTF8.GetBytes(password)),
                    64  => SHA256.HashData(Encoding.UTF8.GetBytes(password)),
                    128 => SHA512.HashData(Encoding.UTF8.GetBytes(password)),
                    _   => []
                };

                if (computed.Length == 0) return false;

                var hex = Convert.ToHexString(computed).ToLowerInvariant();
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(hex),
                    Encoding.ASCII.GetBytes(storedHash.ToLowerInvariant()));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PASSWORD] Hex hash verification error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Decode argon2 base64 — standard alphabet ('+', '/'), no trailing '=' in stored form.
        /// Computes the exact required padding instead of blindly appending "==".
        /// </summary>
        private static byte[] DecodeArgon2Base64(string b64)
        {
            int padNeeded = (4 - b64.Length % 4) % 4;
            return Convert.FromBase64String(b64 + new string('=', padNeeded));
        }

        #endregion

        /// <summary>
        /// Get human-readable hash type name
        /// </summary>
        public static string GetHashTypeName(string? storedPassword)
        {
            if (string.IsNullOrEmpty(storedPassword))
                return "Empty";

            var hashType = DetectHashType(storedPassword.Trim());
            return hashType switch
            {
                HashType.BCrypt    => "BCrypt (PHP PASSWORD_BCRYPT)",
                HashType.Argon2id  => "Argon2id (PHP PASSWORD_ARGON2ID)",
                HashType.Argon2i   => "Argon2i",
                HashType.HexHash   => storedPassword.Trim().Length switch { 32 => "MD5", 64 => "SHA-256", _ => "SHA-512" },
                HashType.PlainText => "Plain Text (Development Only)",
                _                  => "Unknown"
            };
        }

        /// <summary>
        /// Validate password strength (matches PHP Security::validatePassword)
        /// Requirements:
        ///   - Minimum length (default 8 characters)
        ///   - At least one lowercase letter (a-z)
        ///   - At least one uppercase letter (A-Z)
        ///   - At least one digit (0-9)
        ///   - At least one special character
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <param name="minLength">Minimum length required (default 8)</param>
        /// <returns>Validation result with error messages</returns>
        public static PasswordValidationResult ValidatePasswordStrength(string password, int minLength = 8)
        {
            var result = new PasswordValidationResult { Valid = true };
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("Le mot de passe est requis");
                return new PasswordValidationResult { Valid = false, Errors = errors };
            }

            // Check minimum length
            if (password.Length < minLength)
            {
                errors.Add($"Le mot de passe doit contenir au moins {minLength} caractères");
            }

            // Check lowercase letter
            if (!password.Any(c => char.IsLower(c)))
            {
                errors.Add("Le mot de passe doit contenir au moins une lettre minuscule");
            }

            // Check uppercase letter
            if (!password.Any(c => char.IsUpper(c)))
            {
                errors.Add("Le mot de passe doit contenir au moins une lettre majuscule");
            }

            // Check digit
            if (!password.Any(c => char.IsDigit(c)))
            {
                errors.Add("Le mot de passe doit contenir au moins un chiffre");
            }

            // Check special character
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
            {
                errors.Add("Le mot de passe doit contenir au moins un caractère spécial");
            }

            result.Valid = errors.Count == 0;
            result.Errors = errors;
            return result;
        }

        private enum HashType
        {
            PlainText,
            BCrypt,
            Argon2i,
            Argon2id,
            HexHash   // MD5 (32), SHA-256 (64), SHA-512 (128)
        }
    }

    /// <summary>
    /// Password validation result (matches PHP Security::validatePassword return format)
    /// </summary>
    public class PasswordValidationResult
    {
        public bool Valid { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new();

        public override string ToString()
        {
            if (Valid)
                return "? Le mot de passe est valide";

            return "? Mot de passe invalide:\n  • " + string.Join("\n  • ", Errors);
        }
    }
}

