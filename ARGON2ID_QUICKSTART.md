# ? ARGON2ID LOGIN - QUICK START

## Your Setup

**PHP Website** ? Uses `password_hash($password, PASSWORD_ARGON2ID)`  
**C# Desktop App** ? Now verifies the same Argon2id hashes ?

## Check Your Database

```sql
SELECT 
    email,
    LEFT(motdepasse, 20) as hash_preview,
    CASE 
        WHEN motdepasse LIKE '$argon2id$%' THEN '? Argon2id'
        ELSE '?? Other'
    END as type
FROM utilisateur u
JOIN vendeur v ON u.id_user = v.id_user
WHERE v.is_certified = 1;
```

## Expected Result

```
email               | hash_preview         | type
--------------------+----------------------+-----------
admin@example.com   | $argon2id$v=19$m=... | ? Argon2id
```

## Try Login

1. **Run app in DEBUG** (F5)
2. **Enter email and password**
3. **Check Output window** for:
   ```
   [PASSWORD] Argon2 verification: SUCCESS
   [PASSWORD] Algorithm: argon2id, Memory: 65536KB
   ```

## If Login Fails

**Check Output window** for exact reason:
- `User not found` ? Email wrong or user doesn't exist
- `Not a seller` ? User exists but not in vendeur table
- `Not certified` ? Set `is_certified = 1`
- `Password mismatch` ? Hash type detected will be shown

## Create Test User (PHP)

```php
<?php
$pdo = new PDO('mysql:host=localhost;dbname=vente_groupe', 'root', '');
$hash = password_hash('test123', PASSWORD_ARGON2ID);
$pdo->exec("INSERT INTO utilisateur (email, motdepasse, nom, prenom, created_at, updated_at) VALUES ('test@test.com', '$hash', 'Test', 'User', NOW(), NOW())");
$id = $pdo->lastInsertId();
$pdo->exec("INSERT INTO vendeur (id_user, is_certified, created_at, updated_at) VALUES ($id, 1, NOW(), NOW())");
echo "Login: test@test.com / test123";
?>
```

## That's It!

The app now automatically:
- ? Detects Argon2id hashes
- ? Verifies them correctly
- ? Falls back to plain text for testing
- ? Shows debug info in DEBUG mode

**No configuration needed!** ??
