# ?? Authentication & Product Management Guide

## ? New Features Added

Your GroupeV application now includes:

1. **?? Login System** - Secure seller authentication
2. **?? Account Creation** - New seller registration
3. **? Add Products** - Create new products
4. **?? Edit Products** - Modify existing products
5. **??? Delete Products** - Remove products from catalog
6. **?? Logout** - Secure session management

---

## ?? Application Flow

### 1. **Application Starts ? Login Window**

```
[LOGIN WINDOW]
?
Enter credentials ? Authenticate ? Main Dashboard
?
New user? ? Register Window ? Account created ? Back to Login
```

### 2. **Main Dashboard (After Login)**

```
[MAIN WINDOW]
??? Database Operations
?   ??? ?? Test Connection
?   ??? ?? Show Diagnostics
?   ??? ? Health Check
?
??? Data Operations
?   ??? ? Add Product     ? Opens product creation window
?   ??? ?? Edit Product    ? Opens product editor (select product first)
?   ??? ? Refresh Data
?   ??? ? Export Data
?
??? Account
    ??? ?? Logout          ? Returns to login window
```

---

## ?? Login System

### Features:
- ? Email/password authentication
- ? Validates seller certification status
- ? Stores current user session
- ? User-friendly error messages

### Test Credentials (Development Mode):

```
Email: admin@groupev.com
Password: admin123
```

> ?? **Important**: Create a real seller account in the database or use the registration feature!

### Login Logic:
1. Validates email exists in `utilisateur` table
2. Checks if user is a seller (exists in `vendeur` table)
3. Verifies password (?? currently plain text - use hashing in production!)
4. Checks if seller is certified (`is_certified = true`)
5. Creates session with `AuthenticationService`
6. Opens main dashboard

---

## ?? Creating New Accounts

**Important:** Account registration is disabled. New accounts must be created via:

### Option 1: Direct Database Insert (Development)

```sql
-- 1. Create user
INSERT INTO utilisateur (nom, prenom, email, motdepasse, phone, created_at, updated_at) 
VALUES ('Doe', 'John', 'john@example.com', 'password123', '0600000000', NOW(), NOW());

-- 2. Get user ID
SET @user_id = LAST_INSERT_ID();

-- 3. Create seller profile (certified)
INSERT INTO vendeur (id_user, nom_entreprise, email_pro, is_certified, created_at, updated_at) 
VALUES (@user_id, 'John Company', 'john@company.com', 1, NOW(), NOW());
```

### Option 2: Use Test Data Script

Run the provided `Database/test_data_setup.sql` script which creates test accounts.

### Option 3: Admin Panel (Future Feature)

Build an admin interface to manage user accounts.

---

## ? Adding Products

### How to Add:

1. Login to dashboard
2. Click **"? ADD PRODUCT"** button
3. Fill product form:
   - **Description*** (required)
   - **Price (DH)*** (required, > 0)
   - **Category*** (required, select from dropdown)
   - **Image URL** (optional)
   - **Image Alt Text** (optional)
4. Click **"? SAVE"**

### What Happens:
- New `produit` record created
- `id_vendeur` set to current logged-in seller
- `created_at` and `updated_at` timestamps added
- Dashboard refreshes with new product
- Success message displayed

---

## ?? Editing Products

### How to Edit:

1. Select a product in the data grid (click on row)
2. Click **"?? EDIT PRODUCT"** button
3. Edit form opens with current values
4. Modify fields as needed
5. Click **"? SAVE"**

### Features:
- Pre-filled with current product data
- Shows product ID (read-only)
- All fields editable except ID and seller
- Real-time validation
- **Delete** button available (see below)

### What Gets Updated:
- Description
- Price
- Category
- Image URL
- Image Alt Text
- `updated_at` timestamp

---

## ??? Deleting Products

### How to Delete:

**Method 1: From Edit Window**
1. Open product in edit mode
2. Scroll to bottom
3. Click **"?? DELETE PRODUCT"** button
4. Confirm deletion
5. Product removed from database

**Method 2: From Dashboard** (Future feature)
- Select product ? Right-click ? Delete

### Safety Features:
- ? Confirmation dialog before deletion
- ? Shows product details before delete
- ? "Cannot be undone" warning
- ? Success/error feedback

---

## ?? Logout System

### How to Logout:

1. Click **"?? LOGOUT"** button
2. Confirm logout
3. Session cleared
4. Return to login window

### What Happens:
- `AuthenticationService.CurrentUser` = null
- `AuthenticationService.CurrentSeller` = null
- Main window closes
- Login window opens

---

## ?? Database Tables Used

### Authentication:

```sql
utilisateur table:
- id_user (PK)
- nom, prenom
- email (for login)
- motdepasse (?? plain text for now!)
- phone
- created_at, updated_at

vendeur table:
- id_user (FK ? utilisateur)
- nom_entreprise (company name)
- email_pro (professional email)
- is_certified (approval status)
- created_at, updated_at
```

### Products:

```sql
produit table:
- id_produit (PK)
- description
- prix (decimal price)
- id_vendeur (FK ? vendeur)
- id_categorie (FK ? categorie)
- image, image_alt
- created_at, updated_at
```

---

## ?? Security Notes

### ?? Current Implementation (DEVELOPMENT ONLY):

1. **Plain Text Passwords** - NOT SECURE!
   - Passwords stored as plain text in database
   - **TODO**: Implement BCrypt or Argon2 hashing

2. **Simple Session** - In-memory only
   - `AuthenticationService` stores current user
   - Lost on application restart
   - **TODO**: Implement proper session management

3. **No Token System** - Direct database queries
   - **TODO**: Implement JWT tokens for API calls

### ? Production Requirements:

```csharp
// Install password hashing library
dotnet add package BCrypt.Net-Next

// Example password hashing:
using BCrypt.Net;

// Hash on registration:
user.MotDePasse = BCrypt.HashPassword(password);

// Verify on login:
bool isValid = BCrypt.Verify(password, user.MotDePasse);
```

---

## ?? Code Examples

### Check if User is Authenticated:

```csharp
if (AuthenticationService.IsAuthenticated)
{
    var currentUser = AuthenticationService.CurrentUser;
    var currentSeller = AuthenticationService.CurrentSeller;
    
    Console.WriteLine($"Logged in as: {currentUser.Email}");
    Console.WriteLine($"Company: {currentSeller.NomEntreprise}");
}
```

### Get Current Seller's Products:

```csharp
using var context = new DatabaseContext();

var myProducts = await context.Produits
    .Where(p => p.IdVendeur == AuthenticationService.CurrentSeller.IdUser)
    .Include(p => p.Categorie)
    .ToListAsync();
```

### Programmatically Create Product:

```csharp
var newProduct = new Produit
{
    Description = "My Product",
    Prix = 99.99m,
    IdCategorie = 1,
    IdVendeur = AuthenticationService.CurrentSeller.IdUser,
    CreatedAt = DateTime.Now,
    UpdatedAt = DateTime.Now
};

context.Produits.Add(newProduct);
await context.SaveChangesAsync();
```

---

## ?? Testing Checklist

### Authentication:
- [ ] Login with valid credentials
- [ ] Login with invalid credentials shows error
- [ ] Login with non-seller account shows error
- [ ] Login with uncertified seller shows warning
- [ ] Register new account successfully
- [ ] Registration validation works (email, password match, etc.)
- [ ] Logout clears session and returns to login

### Product Management:
- [ ] Add new product saves to database
- [ ] Edit product updates correctly
- [ ] Delete product removes from database
- [ ] Validation prevents invalid data (empty desc, negative price)
- [ ] Category dropdown loads correctly
- [ ] Products show in dashboard after creation/edit

### UI/UX:
- [ ] Status messages display correctly
- [ ] Buttons enable/disable during operations
- [ ] Error messages are user-friendly
- [ ] Success confirmations appear
- [ ] Dashboard refreshes after changes

---

## ?? Troubleshooting

### "Email already registered"
- **Cause**: Email exists in `utilisateur` table
- **Solution**: Use different email or check database

### "Access denied. Seller account required"
- **Cause**: User exists but no `vendeur` record
- **Solution**: Ensure user has seller profile in database

### "Your account is pending approval"
- **Cause**: `is_certified = false`
- **Solution**: Update database: `UPDATE vendeur SET is_certified = 1 WHERE id_user = ?`

### "Please select a product to edit"
- **Cause**: No product selected in grid
- **Solution**: Click on a product row first, then click Edit

### "Cannot connect to database"
- **Cause**: MySQL not running or database doesn't exist
- **Solution**: Check Laragon, import SQL file, test connection

---

## ?? Next Steps / Future Features

### Short Term:
1. ? Implement password hashing (BCrypt)
2. ? Add "Remember Me" checkbox
3. ? Add password reset functionality
4. ? Admin approval interface for sellers
5. ? Product search/filter in dashboard

### Long Term:
1. ? Role-based access control (Admin, Seller, Client)
2. ? Product images upload (not just URL)
3. ? Order management system
4. ? Sales analytics dashboard
5. ? Multi-language support

---

## ?? Key Classes

### `AuthenticationService`
```csharp
public static class AuthenticationService
{
    public static Utilisateur? CurrentUser { get; set; }
    public static Vendeur? CurrentSeller { get; set; }
    public static bool IsAuthenticated => CurrentUser != null && CurrentSeller != null;
    public static void Logout();
}
```

### `LoginWindow`
- Entry point for authentication
- Validates credentials
- Creates session
- Opens main dashboard

### `RegisterWindow`
- New seller registration
- Creates `utilisateur` + `vendeur`
- Validation for all fields
- Pending approval system

### `EditProductWindow`
- Dual purpose: Create/Edit
- Constructor overload for edit mode
- Loads categories from database
- Validation before save
- Delete functionality in edit mode

---

**Version:** 2.0 with Authentication
**Date:** 2025
**Framework:** .NET 8 + WPF + EF Core + MySQL
**Security Level:** ?? Development (Use hashing in production!)
