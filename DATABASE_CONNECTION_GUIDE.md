# Database Connection Guide - GroupeV Application

## ? Final Working Version - Production Ready

Your application is now fully configured to connect to the `vente_groupe` MySQL database.

---

## ?? Prerequisites

1. **Laragon** is installed and running
2. **MySQL** service is active (port 3306)
3. **phpMyAdmin** is accessible at: `http://localhost/phpmyadmin`

---

## ??? Database Setup

### Step 1: Import the SQL File

1. Open phpMyAdmin: `http://localhost/phpmyadmin`
2. Click on **"New"** in the left sidebar to create a database
3. Database name: `vente_groupe`
4. Collation: `utf8mb4_general_ci`
5. Click **"Create"**
6. Select the `vente_groupe` database
7. Click on **"Import"** tab
8. Choose file: `C:\Users\marwane.elarrass\source\repos\OpenWizz\GroupeV\Database\vente_groupe.sql`
9. Click **"Go"**

### Step 2: Verify Database Structure

The database should contain these tables:
- `utilisateur` - Base user information
- `vendeur` - Seller-specific information
- `produit` - Products
- `categorie` - Product categories
- `prevente` - Pre-sales
- `bloquer` - Blocked users
- `client` - Clients
- `facture` - Invoices
- And more...

---

## ?? Database Configuration

### Connection String (Already Configured)

Located in: `DatabaseContext.cs`

```csharp
Server=localhost;Uid=root;Pwd=;Database=vente_groupe;
```

**Default Laragon Credentials:**
- Host: `localhost`
- Port: `3306`
- User: `root`
- Password: *(empty)*
- Database: `vente_groupe`

---

## ? Testing the Connection

### Option 1: Using DatabaseTester (Programmatically)

```csharp
using GroupeV.Utilities;

// Async method (recommended)
bool success = await DatabaseTester.TesterConnexionAsync();

// Or synchronous
bool success = DatabaseTester.TesterConnexion();

// Show diagnostics
await DatabaseTester.AfficherDiagnosticAsync();
```

### Option 2: Using Database Health Window (UI)

1. Run the application
2. Click **"HEALTH CHECK"** button in the main window
3. The `DatabaseHealthWindow` will open and run diagnostics

---

## ?? What Gets Tested

When you run connection tests, the system checks:

1. **Connection** - Can connect to MySQL server?
2. **Database Exists** - Does `vente_groupe` database exist?
3. **Tables** - Are all required tables present?
4. **Data Access** - Can query and count records?
5. **Relationships** - Do foreign keys work (Include queries)?
6. **Statistics** - Counts of users, sellers, products, categories

---

## ?? Application Features

### Database Connection Buttons (Main Window):

? **?? TEST CONNECTION** - Tests database connection with detailed feedback
? **?? SHOW DIAGNOSTICS** - Displays comprehensive database diagnostics
? **? HEALTH CHECK** - Opens full health check window with live testing
? **? REFRESH DATA** - Reloads all data from database
? **? EXPORT DATA** - Exports products to CSV file

### Quick Stats Panel:

Shows real-time connection status:
- Database name
- Connection status (? CONNECTED / ? OFFLINE)
- Server address
- Live statistics (products, sellers, categories)

### Main Features:

? **DatabaseContext** - Entity Framework Core context for `vente_groupe`
? **DatabaseHelper** - Health checks and statistics
? **DatabaseTester** - Connection testing utility (async/await)
? **DatabaseHealthWindow** - UI for diagnostics
? **MainWindow** - Dashboard with real data from database
? **Real-time connection checks** - Validates database before loading data
? **Async/Await** - Modern async patterns throughout

---

## ?? Troubleshooting

### Error: "Unable to connect to MySQL server"

**Solution:**
1. Ensure Laragon is started
2. Check MySQL service is running (green in Laragon)
3. Verify port 3306 is not blocked

### Error: "Database 'vente_groupe' does not exist"

**Solution:**
1. Open phpMyAdmin
2. Import `Database/vente_groupe.sql`
3. Refresh the application

### Error: "Access Denied"

**Solution:**
1. Verify credentials in `DatabaseContext.cs`
2. Default Laragon: user=`root`, password=*(empty)*

### Error: "Table doesn't exist"

**Solution:**
1. Re-import the SQL file completely
2. Ensure all tables are created
3. Run diagnostics to see which tables are missing

---

## ?? Production Checklist

- [x] Database context configured (`DatabaseContext.cs`)
- [x] Connection string set correctly
- [x] Entity models created (Produit, Vendeur, Utilisateur, etc.)
- [x] Async patterns implemented
- [x] Error handling with detailed messages
- [x] Health check system in place
- [x] UI integrated with database
- [x] Build successful (no errors)

---

## ?? Code Examples

### Query Products with Relationships

```csharp
using var context = new DatabaseContext();

var products = await context.Produits
    .Include(p => p.Vendeur)
    .Include(p => p.Categorie)
    .Where(p => p.Prix > 0)
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();
```

### Get Database Statistics

```csharp
var stats = await DatabaseHelper.GetStatsAsync();
Console.WriteLine($"Products: {stats.ProductCount}");
Console.WriteLine($"Sellers: {stats.SellerCount}");
Console.WriteLine($"Categories: {stats.CategoryCount}");
```

### Check Connection Status

```csharp
var (connected, message, stats) = await DatabaseTester.GetConnectionStatusAsync();
if (connected)
{
    Console.WriteLine($"? Connected! {stats.ProductCount} products available");
}
else
{
    Console.WriteLine($"? Connection failed: {message}");
}
```

---

## ?? Next Steps

1. **Import the SQL file** in phpMyAdmin (if not done)
2. **Run the application**
3. **Click "HEALTH CHECK"** to verify connection
4. Start developing your features!

---

## ?? Support

If you encounter issues:
1. Check Laragon is running (green status)
2. Verify MySQL port 3306 is active
3. Run diagnostics: `DatabaseTester.AfficherDiagnostic()`
4. Check console output for detailed errors

---

**Version:** 2.0 - Production Ready
**Database:** vente_groupe (MySQL via Laragon)
**Framework:** .NET 8 + Entity Framework Core
**Date:** 2025
