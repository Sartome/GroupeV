# ?? Quick Start Guide - GroupeV Database Connection

## ? 3-Step Setup

### Step 1: Import Database (5 minutes)

1. Open **Laragon** and start MySQL
2. Open **phpMyAdmin**: `http://localhost/phpmyadmin`
3. Create database: `vente_groupe`
4. Import file: `Database/vente_groupe.sql`

### Step 2: Run Application

1. Press `F5` or click **Start** in Visual Studio
2. Wait for main window to load

### Step 3: Test Connection

Click **?? TEST CONNECTION** button in the main window

---

## ?? Main Window Buttons

| Button | Function | Description |
|--------|----------|-------------|
| **?? TEST CONNECTION** | Tests database connection | Shows detailed connection status with statistics |
| **?? SHOW DIAGNOSTICS** | Shows detailed diagnostics | Connection string, provider, migrations, table counts |
| **? HEALTH CHECK** | Opens health check window | Runs comprehensive database tests |
| **? REFRESH DATA** | Reloads all data | Refreshes products, sellers, categories from DB |
| **? EXPORT DATA** | Exports to CSV | Saves current products to CSV file |

---

## ?? Quick Stats Panel

Located in the left sidebar, shows:
- **Database name**: vente_groupe
- **Connection status**: ? CONNECTED / ? OFFLINE
- **Server**: localhost:3306
- **Live counts**: Products, Sellers, Categories

---

## ? Connection Status Indicators

### ? **CONNECTED** (Green)
- Database is accessible
- All tables are present
- Data is loading correctly
- Dashboard shows live data

### ? **OFFLINE** (Red)
- Cannot connect to MySQL server
- Database doesn't exist
- SQL file not imported
- Click **TEST CONNECTION** for details

---

## ?? Troubleshooting

### Problem: "Cannot connect to database"

**Quick Fix:**
1. Check Laragon is running (green icon)
2. MySQL service should be active
3. Try clicking **TEST CONNECTION** button
4. If fails, click **SHOW DIAGNOSTICS**

### Problem: "Database 'vente_groupe' does not exist"

**Quick Fix:**
1. Open phpMyAdmin
2. Import `Database/vente_groupe.sql`
3. Click **REFRESH DATA** in application

### Problem: "No data showing"

**Quick Fix:**
1. Database might be empty
2. Check if SQL file was imported correctly
3. Click **TEST CONNECTION** to see table counts
4. Import test data if needed

---

## ?? Using Database Connection in Code

### Test Connection Programmatically

```csharp
using GroupeV.Utilities;

// Async (recommended)
bool success = await DatabaseTester.TesterConnexionAsync();

// Synchronous
bool success = DatabaseTester.TesterConnexion();
```

### Get Connection Status

```csharp
var (connected, message, stats) = await DatabaseTester.GetConnectionStatusAsync();

if (connected)
{
    Console.WriteLine($"Products: {stats.ProductCount}");
    Console.WriteLine($"Sellers: {stats.SellerCount}");
}
```

### Show Diagnostics

```csharp
await DatabaseTester.AfficherDiagnosticAsync();
```

---

## ?? Database Tables Reference

Your `vente_groupe` database contains:

### Core Tables:
- **utilisateur** - Base user information (all users)
- **vendeur** - Seller-specific data (extends utilisateur)
- **produit** - Products for sale
- **categorie** - Product categories
- **prevente** - Pre-sales/reservations

### Additional Tables:
- **client** - Clients/buyers
- **facture** - Invoices
- **bloquer** - Blocked users
- **debloquer** - Unblock history
- **participation** - User participation
- **signaler** - User reports
- **site_settings** - Application settings

---

## ?? Success Indicators

You'll know everything is working when you see:

? Main window loads without errors
? Quick stats show "? CONNECTED"
? Product count is displayed
? DataGrid shows products
? Seller name appears in header
? Status bar shows "? System ready"

---

## ?? Pro Tips

1. **Always test connection first** after importing database
2. **Use Health Check** for detailed validation
3. **Export data regularly** as backup
4. **Check Quick Stats** for instant connection status
5. **Refresh data** after making changes in phpMyAdmin

---

## ?? Quick Help

**Connection String:**
```
Server=localhost;Uid=root;Pwd=;Database=vente_groupe;
```

**Location:** `DatabaseContext.cs`

**Default Credentials:**
- User: `root`
- Password: *(empty)*
- Port: `3306`

---

**Updated:** 2025
**Version:** 2.0 - Production Ready
**Framework:** .NET 8 + EF Core + MySQL
