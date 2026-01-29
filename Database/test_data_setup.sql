-- ========================================
-- GroupeV - Test Data Setup Script
-- ========================================
-- Run this script in phpMyAdmin after importing vente_groupe.sql
-- This creates test accounts and sample data

USE vente_groupe;

-- ========================================
-- 1. CREATE TEST ADMIN SELLER
-- ========================================

-- Create admin user
INSERT INTO utilisateur (nom, prenom, email, motdepasse, phone, adresse, created_at, updated_at) 
VALUES (
    'Admin',
    'Test',
    'admin@groupev.com',
    'admin123',
    '0600000000',
    '123 Test Street',
    NOW(),
    NOW()
);

-- Get the user ID
SET @admin_id = LAST_INSERT_ID();

-- Create seller profile (certified)
INSERT INTO vendeur (id_user, nom_entreprise, email_pro, is_certified, siret, adresse_entreprise, created_at, updated_at) 
VALUES (
    @admin_id,
    'Admin Test Company',
    'admin@groupev.com',
    1,  -- Certified (can login)
    '12345678901234',
    '123 Business Ave',
    NOW(),
    NOW()
);

-- ========================================
-- 2. CREATE TEST REGULAR SELLER
-- ========================================

-- Create regular user
INSERT INTO utilisateur (nom, prenom, email, motdepasse, phone, adresse, created_at, updated_at) 
VALUES (
    'Dupont',
    'Marie',
    'marie@example.com',
    'marie123',
    '0611111111',
    '456 Seller Road',
    NOW(),
    NOW()
);

SET @marie_id = LAST_INSERT_ID();

-- Create seller profile (certified)
INSERT INTO vendeur (id_user, nom_entreprise, email_pro, is_certified, created_at, updated_at) 
VALUES (
    @marie_id,
    'Marie Electronics',
    'contact@marie-electronics.com',
    1,
    NOW(),
    NOW()
);

-- ========================================
-- 3. CREATE PENDING SELLER (NOT CERTIFIED)
-- ========================================

INSERT INTO utilisateur (nom, prenom, email, motdepasse, phone, created_at, updated_at) 
VALUES (
    'Martin',
    'Jean',
    'jean@example.com',
    'jean123',
    '0622222222',
    NOW(),
    NOW()
);

SET @jean_id = LAST_INSERT_ID();

INSERT INTO vendeur (id_user, nom_entreprise, email_pro, is_certified, created_at, updated_at) 
VALUES (
    @jean_id,
    'Jean Fashion Store',
    'jean@fashionstore.com',
    0,  -- NOT certified (cannot login until approved)
    NOW(),
    NOW()
);

-- ========================================
-- 4. CREATE CATEGORIES
-- ========================================

INSERT INTO categorie (libelle, created_at, updated_at) VALUES
('Electronics', NOW(), NOW()),
('Clothing', NOW(), NOW()),
('Books', NOW(), NOW()),
('Home & Garden', NOW(), NOW()),
('Sports', NOW(), NOW()),
('Toys', NOW(), NOW()),
('Beauty', NOW(), NOW()),
('Food', NOW(), NOW());

-- ========================================
-- 5. CREATE SAMPLE PRODUCTS (FOR ADMIN)
-- ========================================

-- Get category IDs
SET @cat_electronics = (SELECT id_categorie FROM categorie WHERE libelle = 'Electronics' LIMIT 1);
SET @cat_clothing = (SELECT id_categorie FROM categorie WHERE libelle = 'Clothing' LIMIT 1);
SET @cat_books = (SELECT id_categorie FROM categorie WHERE libelle = 'Books' LIMIT 1);

-- Products for admin seller
INSERT INTO produit (description, prix, id_vendeur, id_categorie, image, image_alt, created_at, updated_at) VALUES
('Laptop Dell XPS 15 - High Performance', 1299.99, @admin_id, @cat_electronics, 'https://via.placeholder.com/300x200/00ff00/000000?text=Laptop', 'Dell XPS 15', NOW(), NOW()),
('Smartphone Samsung Galaxy S23', 899.99, @admin_id, @cat_electronics, 'https://via.placeholder.com/300x200/00ff00/000000?text=Phone', 'Samsung Galaxy', NOW(), NOW()),
('Wireless Headphones Sony WH-1000XM5', 349.99, @admin_id, @cat_electronics, 'https://via.placeholder.com/300x200/00ff00/000000?text=Headphones', 'Sony Headphones', NOW(), NOW()),
('4K Monitor LG UltraWide', 599.99, @admin_id, @cat_electronics, 'https://via.placeholder.com/300x200/00ff00/000000?text=Monitor', 'LG Monitor', NOW(), NOW()),
('Gaming Mouse Logitech G Pro', 79.99, @admin_id, @cat_electronics, 'https://via.placeholder.com/300x200/00ff00/000000?text=Mouse', 'Gaming Mouse', NOW(), NOW());

-- ========================================
-- 6. CREATE SAMPLE PRODUCTS (FOR MARIE)
-- ========================================

INSERT INTO produit (description, prix, id_vendeur, id_categorie, image, created_at, updated_at) VALUES
('Men T-Shirt Cotton Black Size L', 29.99, @marie_id, @cat_clothing, 'https://via.placeholder.com/300x200/00ff00/000000?text=TShirt', NOW(), NOW()),
('Women Jeans Blue Denim Size M', 49.99, @marie_id, @cat_clothing, 'https://via.placeholder.com/300x200/00ff00/000000?text=Jeans', NOW(), NOW()),
('Programming Book: Clean Code by Robert Martin', 39.99, @marie_id, @cat_books, 'https://via.placeholder.com/300x200/00ff00/000000?text=Book', NOW(), NOW()),
('Sneakers Nike Air Max 90', 129.99, @marie_id, @cat_clothing, 'https://via.placeholder.com/300x200/00ff00/000000?text=Sneakers', NOW(), NOW());

-- ========================================
-- 7. VERIFY DATA
-- ========================================

-- Check users
SELECT 
    u.id_user,
    u.nom,
    u.prenom,
    u.email,
    v.nom_entreprise,
    v.is_certified,
    CASE WHEN v.is_certified = 1 THEN 'YES ?' ELSE 'NO ?' END as can_login
FROM utilisateur u
LEFT JOIN vendeur v ON u.id_user = v.id_user
ORDER BY u.created_at DESC;

-- Check products
SELECT 
    p.id_produit,
    p.description,
    p.prix,
    c.libelle as category,
    CONCAT(u.prenom, ' ', u.nom) as seller
FROM produit p
LEFT JOIN categorie c ON p.id_categorie = c.id_categorie
LEFT JOIN vendeur v ON p.id_vendeur = v.id_user
LEFT JOIN utilisateur u ON v.id_user = u.id_user
ORDER BY p.created_at DESC;

-- ========================================
-- 8. TEST CREDENTIALS SUMMARY
-- ========================================

/*
===========================================
TEST ACCOUNTS CREATED:
===========================================

1. ADMIN (CERTIFIED - CAN LOGIN)
   Email: admin@groupev.com
   Password: admin123
   Company: Admin Test Company
   Products: 5 electronics items

2. MARIE (CERTIFIED - CAN LOGIN)
   Email: marie@example.com
   Password: marie123
   Company: Marie Electronics
   Products: 4 items (clothing + book)

3. JEAN (NOT CERTIFIED - CANNOT LOGIN)
   Email: jean@example.com
   Password: jean123
   Company: Jean Fashion Store
   Status: Pending approval
   
   To approve Jean:
   UPDATE vendeur SET is_certified = 1 WHERE id_user = {jean_id};

===========================================
CATEGORIES CREATED:
===========================================
- Electronics
- Clothing
- Books
- Home & Garden
- Sports
- Toys
- Beauty
- Food

===========================================
USAGE:
===========================================
1. Run this script in phpMyAdmin
2. Open GroupeV application
3. Login with admin@groupev.com / admin123
4. Try adding, editing, deleting products
5. Logout and login with marie@example.com / marie123
6. Test Jean account (should fail - not certified)

===========================================
*/
