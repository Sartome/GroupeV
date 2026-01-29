# Test Login Credentials

After running `Database/test_data_setup.sql`, use these credentials to login:

> **Note:** The login system now supports both **plain text** and **hashed passwords** (MD5, SHA-256, SHA-512, BCrypt).
> It automatically detects the hash type and verifies accordingly. See `PASSWORD_HASH_GUIDE.md` for details.

## Working Test Accounts (Certified Sellers)

### Admin Account
- **Email:** `admin@groupev.com`
- **Password:** `admin123`
- **Company:** Admin Test Company
- **Status:** ? Certified

### Regular Seller Account
- **Email:** `marie@example.com`
- **Password:** `marie123`
- **Company:** Marie Electronics
- **Status:** ? Certified

## Non-Working Account (For Testing)

### Pending Seller (Will Show "Account Pending Approval")
- **Email:** `jean@example.com`
- **Password:** `jean123`
- **Company:** Jean Fashion Store
- **Status:** ? Not Certified

---

## Common Login Issues & Solutions

### Issue: "User not found"
**Solutions:**
1. Make sure you ran the `test_data_setup.sql` script
2. Check email spelling (emails are case-insensitive now)
3. Verify the database name is `vente_groupe`

### Issue: "Password mismatch"
**Solutions:**
1. Make sure there are no extra spaces in the password
2. Verify the test data was imported correctly
3. Check the `motdepasse` column in phpMyAdmin

### Issue: "Database connection failed"
**Solutions:**
1. Start XAMPP/MySQL service
2. Verify MySQL is running on localhost:3306
3. Check connection string credentials (currently: root with no password)

### Issue: "User exists but is not a seller"
**Solution:**
- The user account exists in `utilisateur` table but not in `vendeur` table
- Run the complete test_data_setup.sql script

### Issue: "Account pending approval"
**Solution:**
- The seller account is not certified (`is_certified = 0`)
- Use a certified account or update the database:
  ```sql
  UPDATE vendeur SET is_certified = 1 WHERE id_user = YOUR_USER_ID;
  ```

---

## How to Debug Login Issues

### In DEBUG mode:
When you run the application in DEBUG mode, you'll see detailed error messages that tell you exactly why login failed:
- How many users are in the database
- If the email was found
- If the user is a seller
- Password comparison details
- Certification status

### Check Visual Studio Output Window:
Look for lines starting with `[LOGIN]` which show:
- Email lookup results
- User ID and stored email
- Password lengths
- Seller status

### Use Database Health Check:
The application should have a database health check feature that shows:
- Connection status
- Number of sellers, products, etc.

---

## Quick Test Query

Run this in phpMyAdmin to see all sellers and their login credentials:

```sql
SELECT 
    u.id_user,
    u.email,
    u.motdepasse as password,
    v.nom_entreprise as company,
    v.is_certified
FROM utilisateur u
INNER JOIN vendeur v ON u.id_user = v.id_user;
```

This will show you all seller accounts with their passwords (remember: this is development only - never store passwords in plain text in production!)
