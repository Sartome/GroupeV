# Argon2id Password Verification - PHP Compatible

## What This Is

This C# application verifies passwords that were hashed using **PHP's `password_hash()` with Argon2id**.

Your PHP website and this C# desktop app share the same database and can both verify the same password hashes.

---

## PHP Backend (Website)

```php
<?php
// When user registers or changes password
$password = $_POST['password'];
$hash = password_hash($password, PASSWORD_ARGON2ID);

// Store $hash in database (motdepasse column)
$stmt = $pdo->prepare("UPDATE utilisateur SET motdepasse = ? WHERE email = ?");
$stmt->execute([$hash, $email]);

// When user logs in
$stored_hash = /* fetch from database */;
if (password_verify($_POST['password'], $stored_hash)) {
    // Login success
} else {
    // Login failed
}
?>
```

---

## C# Client (This Desktop App)

```csharp
// Automatically verifies the same Argon2id hash
if (PasswordVerifier.VerifyPassword(enteredPassword, user.MotDePasse)) {
    // Login success - same result as PHP!
}
```

**No configuration needed** - it automatically detects and verifies Argon2id hashes.

---

## Argon2id Hash Format

Example hash from PHP `password_hash()`:
```
$argon2id$v=19$m=65536,t=4,p=1$c29tZXNhbHQ$hashvaluehere
```

**Format breakdown:**
```
$argon2id$    - Algorithm (Argon2id)
v=19          - Version 19
m=65536       - Memory: 64 MB
t=4           - Iterations: 4
p=1           - Parallelism: 1 thread
c29tZXNhbHQ   - Salt (base64)
hashvalue     - Hash (base64)
```

---

## Check Your Database

Run in phpMyAdmin:

```sql
-- Show all seller passwords and their hash type
SELECT 
    u.email,
    LEFT(u.motdepasse, 20) as hash_start,
    LENGTH(u.motdepasse) as length,
    CASE 
        WHEN u.motdepasse LIKE '$argon2id$%' THEN '? Argon2id'
        WHEN u.motdepasse LIKE '$argon2i$%' THEN 'Argon2i (older)'
        ELSE '?? Plain Text'
    END as type
FROM utilisateur u
INNER JOIN vendeur v ON u.id_user = v.id_user
WHERE v.is_certified = 1;
```

---

## Debug Mode

When running in DEBUG (F5), you'll see detailed logs:

```
[LOGIN] User found: ID=1, Email=admin@example.com
[LOGIN] Password in DB: '$argon2id$v=19$...' (Length: 96)
[LOGIN] Password entered: 'admin123' (Length: 8)
[PASSWORD] Argon2 verification: SUCCESS
[PASSWORD] Algorithm: argon2id, Memory: 65536KB, Iterations: 4, Parallelism: 1
? Login successful!
```

---

## Why Argon2id?

- ? **Winner of Password Hashing Competition (2015)**
- ? **Resistant to GPU/ASIC attacks** (memory-hard)
- ? **Configurable cost** (can increase as hardware improves)
- ? **Built into PHP 7.2+** (`PASSWORD_ARGON2ID`)
- ? **Industry standard** for new applications

### Comparison:

| Algorithm | Security | Speed | PHP Built-in |
|-----------|----------|-------|--------------|
| Plain Text | ? None | Fast | No |
| MD5 | ? Broken | Fast | Yes |
| SHA-256 | ?? Not designed for passwords | Fast | Yes |
| BCrypt | ? Good | Medium | Yes |
| Argon2id | ? Best | Configurable | Yes (7.2+) |

---

## Testing

### Create Test User in PHP:

```php
<?php
// create_test_seller.php
$pdo = new PDO('mysql:host=localhost;dbname=vente_groupe', 'root', '');

// Create user
$hash = password_hash('admin123', PASSWORD_ARGON2ID);
$pdo->exec("
    INSERT INTO utilisateur (nom, prenom, email, motdepasse, created_at, updated_at) 
    VALUES ('Admin', 'Test', 'admin@test.com', '$hash', NOW(), NOW())
");

$userId = $pdo->lastInsertId();

// Create seller
$pdo->exec("
    INSERT INTO vendeur (id_user, nom_entreprise, is_certified, created_at, updated_at) 
    VALUES ($userId, 'Test Company', 1, NOW(), NOW())
");

echo "Created test seller: admin@test.com / admin123\n";
?>
```

Run: `php create_test_seller.php`

Then login in the C# app:
- Email: `admin@test.com`
- Password: `admin123`
- ? Should work!

---

## Troubleshooting

### "Password verification failed"

1. **Check hash format in database:**
   ```sql
   SELECT email, motdepasse FROM utilisateur WHERE email = 'admin@test.com';
   ```

2. **Verify it starts with `$argon2id$`**
   - If not, password might be plain text or different hash

3. **Check DEBUG output** (Output window in Visual Studio)
   - Look for `[PASSWORD]` messages
   - Shows exact verification steps

### "User not found"

- Run diagnostic query (see above)
- Check email spelling
- Ensure seller is certified (`is_certified = 1`)

### Plain Text Fallback

If hash doesn't start with `$argon2id$`, the system falls back to plain text comparison (for development only).

---

## Migration: Plain Text ? Argon2id

If you have existing plain text passwords, use this PHP script:

```php
<?php
// migrate_passwords.php
$pdo = new PDO('mysql:host=localhost;dbname=vente_groupe', 'root', '');

// Get all users
$users = $pdo->query("SELECT id_user, email, motdepasse FROM utilisateur")->fetchAll();

foreach ($users as $user) {
    // Skip if already hashed
    if (str_starts_with($user['motdepasse'], '$argon2id$')) {
        echo "Skipping {$user['email']} - already hashed\n";
        continue;
    }
    
    // Hash the plain text password
    $hash = password_hash($user['motdepasse'], PASSWORD_ARGON2ID);
    
    // Update database
    $stmt = $pdo->prepare("UPDATE utilisateur SET motdepasse = ? WHERE id_user = ?");
    $stmt->execute([$hash, $user['id_user']]);
    
    echo "Hashed password for: {$user['email']}\n";
}

echo "Done! All passwords now use Argon2id.\n";
?>
```

**?? Important:** Test with one user first, then run for all.

---

## References

- [PHP password_hash() Documentation](https://www.php.net/manual/en/function.password-hash.php)
- [Argon2 Specification](https://github.com/P-H-C/phc-winner-argon2)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
