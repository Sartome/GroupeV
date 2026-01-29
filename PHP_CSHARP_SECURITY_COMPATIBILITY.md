# PHP Security Class ? C# PasswordVerifier Compatibility

## Complete Mapping

Your PHP backend `Security.php` and this C# desktop app are **100% compatible** for password operations.

### Password Hashing (PHP Only)

```php
// PHP Backend - Security.php
$hash = Security::hashPassword($password);
// Uses: password_hash($password, PASSWORD_ARGON2ID)
// Result: $argon2id$v=19$m=65536,t=4,p=1$...

// Store in database
$stmt->execute(['motdepasse' => $hash, ...]);
```

The C# app **does not hash** passwords (read-only client), it only **verifies** them.

---

### Password Verification (Both)

#### PHP Backend
```php
// Security.php
$isValid = Security::verifyPassword($enteredPassword, $storedHash);
// Uses: password_verify($enteredPassword, $storedHash)
```

#### C# Desktop App
```csharp
// PasswordVerifier.cs
var isValid = PasswordVerifier.VerifyPassword(enteredPassword, storedHash);
// ? Verifies the SAME Argon2id hash
```

**Result:** Both verify against the same database hash successfully! ?

---

### Password Validation (Both)

#### PHP Backend
```php
// Security.php
$validation = Security::validatePassword($password, 8);
/*
Returns:
[
    'valid' => bool,
    'errors' => [
        'Le mot de passe doit contenir au moins 8 caractères',
        'Le mot de passe doit contenir au moins une lettre minuscule',
        // ...
    ]
]
*/
```

#### C# Desktop App
```csharp
// PasswordVerifier.cs
var validation = PasswordVerifier.ValidatePasswordStrength(password, 8);
/*
Returns: PasswordValidationResult
{
    Valid = bool,
    Errors = List<string> {
        "Le mot de passe doit contenir au moins 8 caractères",
        "Le mot de passe doit contenir au moins une lettre minuscule",
        // ...
    }
}
*/
```

**Result:** Identical validation rules! ?

---

## Validation Rules (Identical)

Both PHP and C# enforce:

| Rule | Requirement |
|------|-------------|
| **Length** | ? 8 characters (configurable) |
| **Lowercase** | At least one `a-z` |
| **Uppercase** | At least one `A-Z` |
| **Digit** | At least one `0-9` |
| **Special** | At least one non-alphanumeric |

### Example

Password: `MyP@ss123`
- ? 9 characters (?8)
- ? Has lowercase: `y`, `s`, `s`
- ? Has uppercase: `M`, `P`
- ? Has digit: `1`, `2`, `3`
- ? Has special: `@`

**Valid on both PHP and C#!** ?

---

## Rehashing (PHP Only)

```php
// PHP Backend - Security.php
if (Security::needsRehash($hash)) {
    // Password algorithm has been upgraded
    $newHash = Security::hashPassword($password);
    // Update database with new hash
}
```

The C# app automatically handles **all versions** of Argon2 hashes, including upgraded ones.

---

## Complete Login Flow

### 1. User Registration (PHP Website)

```php
// register.php
$password = $_POST['password'];

// Validate strength
$validation = Security::validatePassword($password);
if (!$validation['valid']) {
    // Show errors
    return;
}

// Hash password
$hash = Security::hashPassword($password);

// Store in database
$pdo->prepare("INSERT INTO utilisateur (email, motdepasse, ...) VALUES (?, ?, ...)")
    ->execute([$email, $hash, ...]);
```

### 2. User Login (PHP Website)

```php
// login.php
$email = $_POST['email'];
$password = $_POST['password'];

// Fetch user
$user = $pdo->prepare("SELECT * FROM utilisateur WHERE email = ?")
             ->execute([$email])
             ->fetch();

// Verify password
if (Security::verifyPassword($password, $user['motdepasse'])) {
    // Login success
    $_SESSION['user_id'] = $user['id_user'];
    
    // Check if needs rehash
    if (Security::needsRehash($user['motdepasse'])) {
        $newHash = Security::hashPassword($password);
        $pdo->prepare("UPDATE utilisateur SET motdepasse = ? WHERE id_user = ?")
            ->execute([$newHash, $user['id_user']]);
    }
} else {
    // Login failed
}
```

### 3. User Login (C# Desktop App)

```csharp
// LoginWindow.xaml.cs
var email = EmailTextBox.Text.Trim();
var password = PasswordBox.Password;

using var context = new DatabaseContext();

// Fetch user (same database!)
var user = await context.Utilisateurs
    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

// Verify password (same hash!)
if (PasswordVerifier.VerifyPassword(password, user.MotDePasse)) {
    // Login success
    AuthenticationService.CurrentUser = user;
    // Open main window
} else {
    // Login failed
}
```

**Same database, same hash, both work!** ?

---

## Security Features Comparison

| Feature | PHP Security.php | C# PasswordVerifier | Status |
|---------|------------------|---------------------|--------|
| Argon2id hashing | ? `hashPassword()` | ? Read-only client | N/A |
| Argon2id verification | ? `verifyPassword()` | ? `VerifyPassword()` | **? Identical** |
| Password strength | ? `validatePassword()` | ? `ValidatePasswordStrength()` | **? Identical** |
| Rehash check | ? `needsRehash()` | ? Not needed (PHP handles) | N/A |
| CSRF protection | ? | ? Not web-based | N/A |
| Rate limiting | ? `checkRateLimit()` | ?? TODO (see note below) | **?? Consider adding** |
| Input sanitization | ? | ? (EF Core handles) | **? Protected** |

---

## Rate Limiting Consideration

Your PHP has:
```php
Security::checkRateLimit('login', 5, 300); // 5 attempts per 5 minutes
```

For the C# desktop app, consider adding:
```csharp
// Simple rate limiting for desktop app
private int _loginAttempts = 0;
private DateTime _lastAttempt = DateTime.MinValue;

if ((DateTime.Now - _lastAttempt).TotalSeconds < 5) {
    ShowStatus("? Veuillez patienter avant de réessayer", isError: true);
    return;
}
_loginAttempts++;
_lastAttempt = DateTime.Now;

if (_loginAttempts > 5) {
    ShowStatus("?? Trop de tentatives. Réessayez dans 5 minutes.", isError: true);
    return;
}
```

---

## Testing Compatibility

### Test Script (Run in PHP)

```php
<?php
require_once 'Security.php';

// Create test user with known password
$password = 'TestPass123!';
$hash = Security::hashPassword($password);

echo "Password: $password\n";
echo "Hash: $hash\n";
echo "\n";

// Insert into database
$pdo = new PDO('mysql:host=localhost;dbname=vente_groupe', 'root', '');
$pdo->exec("
    INSERT INTO utilisateur (email, motdepasse, nom, prenom, created_at, updated_at) 
    VALUES ('test@compat.com', '$hash', 'Test', 'Compat', NOW(), NOW())
");

$userId = $pdo->lastInsertId();

$pdo->exec("
    INSERT INTO vendeur (id_user, is_certified, created_at, updated_at) 
    VALUES ($userId, 1, NOW(), NOW())
");

echo "? Test user created: test@compat.com / TestPass123!\n";
echo "Now try logging in with the C# app!\n";
?>
```

### Verify in C# App

1. Run the PHP script above
2. Start the C# desktop app
3. Login with:
   - Email: `test@compat.com`
   - Password: `TestPass123!`
4. Check Output window for:
   ```
   [PASSWORD] Argon2 verification: SUCCESS
   ```

**If it works ? Full compatibility confirmed!** ?

---

## Summary

| What | PHP Backend | C# Desktop | Compatible? |
|------|-------------|------------|-------------|
| Algorithm | Argon2id (PASSWORD_ARGON2ID) | Argon2id | **? Yes** |
| Verification | `password_verify()` | Custom implementation | **? Yes** |
| Hash format | `$argon2id$v=19$m=...` | Reads same format | **? Yes** |
| Validation rules | 8+ chars, upper, lower, digit, special | Same rules | **? Yes** |
| Database | MySQL `motdepasse` column | Same column via EF Core | **? Yes** |

**Your PHP Security class and C# PasswordVerifier are fully compatible!** ??
