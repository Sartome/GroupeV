-- ========================================
-- QUICK DIAGNOSTIC QUERIES
-- ========================================
-- Run these in phpMyAdmin to check your database status

-- 1. Check if database exists and is selected
SELECT DATABASE() as current_database;

-- 2. Show all tables in the database
SHOW TABLES;

-- 3. Count records in each table
SELECT 
    'utilisateur' as table_name, 
    COUNT(*) as record_count 
FROM utilisateur
UNION ALL
SELECT 'vendeur', COUNT(*) FROM vendeur
UNION ALL
SELECT 'produit', COUNT(*) FROM produit
UNION ALL
SELECT 'categorie', COUNT(*) FROM categorie
UNION ALL
SELECT 'prevente', COUNT(*) FROM prevente;

-- 4. Show all sellers with their login credentials and password hash type
-- (DEVELOPMENT ONLY - Never expose passwords in production!)
-- Verifies PHP Security::hashPassword() / Security::verifyPassword() compatibility
SELECT 
    u.id_user as 'User ID',
    u.email as 'Email',
    LEFT(u.motdepasse, 30) as 'Hash Preview',
    LENGTH(u.motdepasse) as 'Length',
    CASE 
        WHEN u.motdepasse LIKE '$argon2id$%' THEN '? Argon2id (Security::hashPassword)'
        WHEN u.motdepasse LIKE '$argon2i$%' THEN 'Argon2i'
        WHEN u.motdepasse LIKE '$2a$%' OR u.motdepasse LIKE '$2b$%' OR u.motdepasse LIKE '$2y$%' THEN 'BCrypt'
        WHEN LENGTH(u.motdepasse) = 128 AND u.motdepasse REGEXP '^[a-fA-F0-9]+$' THEN 'SHA-512'
        WHEN LENGTH(u.motdepasse) = 64 AND u.motdepasse REGEXP '^[a-fA-F0-9]+$' THEN 'SHA-256'
        WHEN LENGTH(u.motdepasse) = 32 AND u.motdepasse REGEXP '^[a-fA-F0-9]+$' THEN 'MD5'
        ELSE '?? Plain Text'
    END as 'Hash Type',
    CONCAT(u.prenom, ' ', u.nom) as 'Name',
    v.nom_entreprise as 'Company',
    CASE 
        WHEN v.is_certified = 1 THEN '? Certified' 
        ELSE '? Not Certified' 
    END as 'Status',
    u.created_at as 'Created At'
FROM utilisateur u
INNER JOIN vendeur v ON u.id_user = v.id_user
ORDER BY v.is_certified DESC, u.created_at DESC;

-- 5. Show users who are NOT sellers
SELECT 
    u.id_user,
    u.email,
    CONCAT(u.prenom, ' ', u.nom) as name,
    'Not a seller' as note
FROM utilisateur u
LEFT JOIN vendeur v ON u.id_user = v.id_user
WHERE v.id_user IS NULL;

-- 6. Check for any password issues (empty or null passwords)
SELECT 
    id_user,
    email,
    CASE 
        WHEN motdepasse IS NULL THEN 'NULL PASSWORD'
        WHEN motdepasse = '' THEN 'EMPTY PASSWORD'
        WHEN LENGTH(motdepasse) < 3 THEN 'TOO SHORT'
        ELSE 'OK'
    END as password_status,
    LENGTH(motdepasse) as password_length
FROM utilisateur
WHERE id_user IN (SELECT id_user FROM vendeur);

-- 7. Test a specific login (change the email and password)
SELECT 
    u.id_user,
    u.email,
    u.motdepasse,
    v.nom_entreprise,
    v.is_certified,
    CASE
        WHEN v.id_user IS NULL THEN '? Not a seller'
        WHEN v.is_certified = 0 THEN '? Not certified'
        WHEN u.motdepasse = 'admin123' THEN '? Password matches'
        ELSE '? Password does not match'
    END as login_result
FROM utilisateur u
LEFT JOIN vendeur v ON u.id_user = v.id_user
WHERE u.email = 'admin@groupev.com';  -- Change this to test different emails

-- 8. If no sellers exist, here's a quick fix - create a test seller:
-- Uncomment and run if you need a quick test account:

/*
-- Create test user and seller in one go
INSERT INTO utilisateur (nom, prenom, email, motdepasse, phone, created_at, updated_at) 
VALUES ('Test', 'User', 'test@test.com', 'test123', '0600000000', NOW(), NOW());

INSERT INTO vendeur (id_user, nom_entreprise, is_certified, created_at, updated_at) 
VALUES (LAST_INSERT_ID(), 'Test Company', 1, NOW(), NOW());

-- Now you can login with: test@test.com / test123
*/
