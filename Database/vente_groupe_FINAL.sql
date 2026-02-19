-- ========================================================================
-- DATABASE SCHEMA: vente_groupe (Sell & Buy Platform)
-- FINAL VERSION WITH ALL ENHANCEMENTS
-- Date: 2026-02-13
-- ========================================================================
-- This is a complete marketplace database supporting:
-- - Multiple user roles (Clients, Sellers, Managers)
-- - Multiple sale types (Buy, Auction, Group Sales)
-- - Product management with multiple images
-- - Review system, support tickets, and moderation tools
-- - Tax management (HT/TTC) for French businesses
-- ========================================================================

-- Drop database if exists (USE WITH CAUTION in production!)
-- DROP DATABASE IF EXISTS vente_groupe;

CREATE DATABASE IF NOT EXISTS vente_groupe
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE vente_groupe;

-- ========================================================================
-- CORE USER TABLES
-- ========================================================================

-- Central user table: stores all users (clients, sellers, managers)
CREATE TABLE Utilisateur (
    id_user INT PRIMARY KEY AUTO_INCREMENT,
    nom VARCHAR(255) NOT NULL,                  -- Last name
    prenom VARCHAR(255) NOT NULL,               -- First name
    adresse VARCHAR(255),                       -- Postal address
    phone VARCHAR(20),                          -- Phone number
    avatar VARCHAR(255) NULL,                   -- User avatar/profile picture
    email VARCHAR(255) NOT NULL UNIQUE,         -- Email (UNIQUE constraint added)
    motdepasse VARCHAR(255) NOT NULL,           -- Hashed password (use bcrypt/argon2)
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_email (email),                    -- Index for login queries
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Sellers/Vendors: enterprise users
CREATE TABLE Vendeur (
    id_user INT PRIMARY KEY,
    nom_entreprise VARCHAR(100) NOT NULL,       -- Company name
    siret VARCHAR(14) UNIQUE,                   -- French business registration (SIRET)
    adresse_entreprise VARCHAR(100),            -- Company address
    email_pro VARCHAR(100),                     -- Professional email
    is_certified BOOLEAN DEFAULT FALSE,         -- Certification status
    is_blocked BOOLEAN DEFAULT FALSE,           -- Block status (for quick checks)
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_user) REFERENCES Utilisateur(id_user) ON DELETE CASCADE,
    INDEX idx_certified (is_certified),
    INDEX idx_blocked (is_blocked)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Clients: customer users
CREATE TABLE Client (
    id_user INT PRIMARY KEY,
    FOREIGN KEY (id_user) REFERENCES Utilisateur(id_user) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Managers/Administrators: admin users
CREATE TABLE Gestionnaire (
    id_user INT PRIMARY KEY,
    FOREIGN KEY (id_user) REFERENCES Utilisateur(id_user) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- PRODUCT CATALOG TABLES
-- ========================================================================

-- Product categories (created by managers)
CREATE TABLE Categorie (
    id_categorie INT PRIMARY KEY AUTO_INCREMENT,
    id_gestionnaire INT NOT NULL,               -- Manager who created category
    lib VARCHAR(100) NOT NULL,                  -- Category name
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_gestionnaire) REFERENCES Gestionnaire(id_user),
    INDEX idx_lib (lib)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Products: main product catalog
CREATE TABLE Produit (
    id_produit INT PRIMARY KEY AUTO_INCREMENT,
    description VARCHAR(255) NOT NULL,          -- Product description
    prix DECIMAL(10,2) NOT NULL,                -- Price TTC (with tax)
    prix_ht DECIMAL(10,2) DEFAULT 0.00,         -- Price HT (without tax)
    taux_tva DECIMAL(5,2) DEFAULT 20.00,        -- VAT rate (French TVA: usually 20%)
    quantity INT DEFAULT 1,                     -- Stock quantity
    image VARCHAR(255),                         -- Primary image path
    image_alt VARCHAR(255),                     -- Alt text for accessibility
    image_size INT,                             -- Image size in bytes
    image_width INT,                            -- Image width in pixels
    image_height INT,                           -- Image height in pixels
    id_vendeur INT NOT NULL,                    -- Seller
    id_categorie INT NULL,                      -- Category (nullable)

    -- Sale type fields (migration applied)
    sale_type ENUM('buy', 'auction', 'group') NOT NULL DEFAULT 'buy',
    group_required_buyers INT NULL DEFAULT NULL,    -- Min buyers for group sale
    group_expires_at DATETIME NULL DEFAULT NULL,    -- Group sale expiration

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    FOREIGN KEY (id_vendeur) REFERENCES Vendeur(id_user) ON DELETE CASCADE,
    FOREIGN KEY (id_categorie) REFERENCES Categorie(id_categorie) ON DELETE SET NULL,

    INDEX idx_produit_vendeur (id_vendeur),
    INDEX idx_produit_categorie (id_categorie),
    INDEX idx_sale_type (sale_type),
    INDEX idx_group_expires_at (group_expires_at),
    INDEX idx_created_at (created_at),
    INDEX idx_quantity (quantity)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Product images: multiple images per product
CREATE TABLE ProduitImages (
    id_image INT PRIMARY KEY AUTO_INCREMENT,
    id_produit INT NOT NULL,
    image_path VARCHAR(255) NOT NULL,
    image_alt VARCHAR(255),
    image_size INT,
    image_width INT,
    image_height INT,
    is_primary BOOLEAN DEFAULT FALSE,           -- Mark primary image
    sort_order INT DEFAULT 0,                   -- Display order
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_produit) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    INDEX idx_produit (id_produit),
    INDEX idx_sort_order (sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- GROUP SALES / PRE-SALES TABLES
-- ========================================================================

-- Pre-sales: group purchase offers
CREATE TABLE Prevente (
    id_prevente INT PRIMARY KEY AUTO_INCREMENT,
    date_limite DATE NOT NULL,                  -- Deadline
    nombre_min INT NOT NULL,                    -- Minimum buyers required
    statut VARCHAR(255) NOT NULL DEFAULT 'en cours',  -- Status: 'en cours', 'validée', 'annulée'
    prix_prevente DECIMAL(10,2) NOT NULL,       -- Pre-sale price
    id_produit INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_produit) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    INDEX idx_statut (statut),
    INDEX idx_date_limite (date_limite)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Invoices: generated for purchases
CREATE TABLE Facture (
    id_facture INT PRIMARY KEY AUTO_INCREMENT,
    date_facture DATE NOT NULL,
    pdf_facture VARCHAR(255),                   -- PDF file path/URL
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Participation: client participation in pre-sales
CREATE TABLE Participation (
    id_particiption INT PRIMARY KEY AUTO_INCREMENT,  -- NOTE: typo in column name (kept for backward compatibility)
    id_client INT NOT NULL,
    id_prevente INT NOT NULL,
    id_facture INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_client) REFERENCES Client(id_user) ON DELETE CASCADE,
    FOREIGN KEY (id_prevente) REFERENCES Prevente(id_prevente) ON DELETE CASCADE,
    FOREIGN KEY (id_facture) REFERENCES Facture(id_facture) ON DELETE SET NULL,
    INDEX idx_client (id_client),
    INDEX idx_prevente (id_prevente)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Pre-purchases: group sale reservations (created dynamically by PrePurchase.php)
CREATE TABLE IF NOT EXISTS pre_purchases (
    id INT PRIMARY KEY AUTO_INCREMENT,
    id_produit INT NOT NULL,
    id_client INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    expires_at DATETIME NULL,
    status ENUM('pending', 'confirmed', 'cancelled', 'expired') NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_produit) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    FOREIGN KEY (id_client) REFERENCES Client(id_user) ON DELETE CASCADE,
    INDEX idx_produit (id_produit),
    INDEX idx_client (id_client),
    INDEX idx_expires_at (expires_at),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- SALES TRANSACTION TABLES
-- ========================================================================

-- Sales: direct purchase records (created dynamically by Sale.php)
CREATE TABLE IF NOT EXISTS sales (
    id INT PRIMARY KEY AUTO_INCREMENT,
    product_id INT NOT NULL,
    buyer_id INT NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (product_id) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    FOREIGN KEY (buyer_id) REFERENCES Utilisateur(id_user) ON DELETE CASCADE,
    INDEX idx_product_id (product_id),
    INDEX idx_buyer_id (buyer_id),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- AUCTION TABLES
-- ========================================================================

-- Auctions: auction management (created dynamically by Auction.php)
CREATE TABLE IF NOT EXISTS auctions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    id_produit INT NOT NULL,
    starting_price DECIMAL(10,2) NOT NULL,
    current_price DECIMAL(10,2) NOT NULL,
    ends_at DATETIME NOT NULL,
    status ENUM('active', 'ended') NOT NULL DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_produit) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    INDEX idx_produit (id_produit),
    INDEX idx_status (status),
    INDEX idx_ends_at (ends_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Bids: auction bids (created dynamically by Auction.php)
CREATE TABLE IF NOT EXISTS bids (
    id INT PRIMARY KEY AUTO_INCREMENT,
    auction_id INT NOT NULL,
    user_id INT NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (auction_id) REFERENCES auctions(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES Utilisateur(id_user) ON DELETE CASCADE,
    INDEX idx_auction_id (auction_id),
    INDEX idx_user_id (user_id),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- REVIEW & RATING TABLES
-- ========================================================================

-- Reviews: product reviews (created dynamically by Review.php)
CREATE TABLE IF NOT EXISTS reviews (
    id INT PRIMARY KEY AUTO_INCREMENT,
    product_id INT NOT NULL,
    user_id INT NOT NULL,
    rating TINYINT NOT NULL CHECK (rating BETWEEN 1 AND 5),  -- Rating 1-5
    comment TEXT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (product_id) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES Utilisateur(id_user) ON DELETE CASCADE,
    INDEX idx_product_id (product_id),
    INDEX idx_user_id (user_id),
    UNIQUE KEY unique_user_product_review (product_id, user_id)  -- One review per user per product
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- SUPPORT & MODERATION TABLES
-- ========================================================================

-- Support tickets (created dynamically by Ticket.php)
CREATE TABLE IF NOT EXISTS tickets (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    subject VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    admin_response TEXT NULL,
    status ENUM('open', 'answered', 'closed') NOT NULL DEFAULT 'open',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Utilisateur(id_user) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_status (status),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Product reports: users can report problematic products
CREATE TABLE Signaler (
    id_signal INT PRIMARY KEY AUTO_INCREMENT,
    id_user INT NOT NULL,
    id_produit INT NOT NULL,
    date_signal DATE NOT NULL,
    reason VARCHAR(255),                        -- Report reason (ENHANCEMENT)
    status ENUM('pending', 'reviewed', 'resolved') DEFAULT 'pending',  -- (ENHANCEMENT)
    FOREIGN KEY (id_user) REFERENCES Utilisateur(id_user) ON DELETE CASCADE,
    FOREIGN KEY (id_produit) REFERENCES Produit(id_produit) ON DELETE CASCADE,
    INDEX idx_user (id_user),
    INDEX idx_produit (id_produit),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seller blocking log: track when managers block sellers
CREATE TABLE Bloquer (
    id_bloquer INT PRIMARY KEY AUTO_INCREMENT,
    id_gestionnaire INT NOT NULL,
    id_vendeur INT NOT NULL,
    raison VARCHAR(255),                        -- Blocking reason (ENHANCEMENT)
    date_blocage TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_gestionnaire) REFERENCES Gestionnaire(id_user) ON DELETE CASCADE,
    FOREIGN KEY (id_vendeur) REFERENCES Vendeur(id_user) ON DELETE CASCADE,
    INDEX idx_gestionnaire (id_gestionnaire),
    INDEX idx_vendeur (id_vendeur),
    INDEX idx_date_blocage (date_blocage)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seller unblocking log: track when managers unblock sellers
CREATE TABLE Debloquer (
    id_debloquer INT PRIMARY KEY AUTO_INCREMENT,
    id_gestionnaire INT NOT NULL,
    id_vendeur INT NOT NULL,
    raison VARCHAR(255),                        -- Unblocking reason (ENHANCEMENT)
    date_deblocage TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_gestionnaire) REFERENCES Gestionnaire(id_user) ON DELETE CASCADE,
    FOREIGN KEY (id_vendeur) REFERENCES Vendeur(id_user) ON DELETE CASCADE,
    INDEX idx_gestionnaire (id_gestionnaire),
    INDEX idx_vendeur (id_vendeur),
    INDEX idx_date_deblocage (date_deblocage)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- CONFIGURATION TABLES
-- ========================================================================

-- Site settings: global configuration parameters
CREATE TABLE site_settings (
    id INT PRIMARY KEY AUTO_INCREMENT,
    setting_key VARCHAR(100) UNIQUE NOT NULL,
    setting_value TEXT,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert default settings
INSERT INTO site_settings (setting_key, setting_value, description) VALUES
('tax_rate', '20.00', 'Taux de taxe en pourcentage (ex: 20.00 pour 20%)'),
('tax_enabled', '1', 'Activer/désactiver les taxes (1 = activé, 0 = désactivé)'),
('tax_name', 'TVA', 'Nom de la taxe (ex: TVA, Tax, etc.)');

-- ========================================================================
-- VIEWS FOR REPORTING (ENHANCEMENTS)
-- ========================================================================

-- View: Active sellers with their product counts
CREATE OR REPLACE VIEW v_active_sellers AS
SELECT
    v.id_user,
    u.nom,
    u.prenom,
    u.email,
    v.nom_entreprise,
    v.is_certified,
    v.is_blocked,
    COUNT(DISTINCT p.id_produit) as total_products,
    COALESCE(SUM(s.amount), 0) as total_sales_amount
FROM Vendeur v
JOIN Utilisateur u ON v.id_user = u.id_user
LEFT JOIN Produit p ON v.id_user = p.id_vendeur
LEFT JOIN sales s ON p.id_produit = s.product_id
WHERE v.is_blocked = FALSE
GROUP BY v.id_user, u.nom, u.prenom, u.email, v.nom_entreprise, v.is_certified, v.is_blocked;

-- View: Product stats with ratings
CREATE OR REPLACE VIEW v_product_stats AS
SELECT
    p.id_produit,
    p.description,
    p.prix,
    p.quantity,
    p.sale_type,
    v.nom_entreprise as seller_name,
    c.lib as category_name,
    COUNT(DISTINCT r.id) as review_count,
    COALESCE(AVG(r.rating), 0) as avg_rating,
    COUNT(DISTINCT s.id) as sales_count,
    COALESCE(SUM(s.amount), 0) as total_revenue
FROM Produit p
LEFT JOIN Vendeur v ON p.id_vendeur = v.id_user
LEFT JOIN Categorie c ON p.id_categorie = c.id_categorie
LEFT JOIN reviews r ON p.id_produit = r.product_id
LEFT JOIN sales s ON p.id_produit = s.product_id
GROUP BY p.id_produit, p.description, p.prix, p.quantity, p.sale_type, v.nom_entreprise, c.lib;

-- ========================================================================
-- STORED PROCEDURES FOR BUSINESS LOGIC (ENHANCEMENTS)
-- ========================================================================

DELIMITER //

-- Procedure: Check and update expired group sales
CREATE PROCEDURE sp_check_expired_group_sales()
BEGIN
    -- Mark expired pre-purchases as expired
    UPDATE pre_purchases
    SET status = 'expired'
    WHERE expires_at IS NOT NULL
    AND expires_at < NOW()
    AND status = 'pending';

    -- Cancel group sales that didn't meet minimum requirements
    UPDATE Prevente
    SET statut = 'annulée'
    WHERE date_limite < CURDATE()
    AND statut = 'en cours';
END //

-- Procedure: Get seller dashboard stats
CREATE PROCEDURE sp_get_seller_dashboard(IN p_seller_id INT)
BEGIN
    SELECT
        COUNT(DISTINCT p.id_produit) as total_products,
        COUNT(DISTINCT s.id) as total_sales,
        COALESCE(SUM(s.amount), 0) as total_revenue,
        COALESCE(AVG(r.rating), 0) as avg_rating
    FROM Vendeur v
    LEFT JOIN Produit p ON v.id_user = p.id_vendeur
    LEFT JOIN sales s ON p.id_produit = s.product_id
    LEFT JOIN reviews r ON p.id_produit = r.product_id
    WHERE v.id_user = p_seller_id
    GROUP BY v.id_user;
END //

-- Procedure: Get platform statistics
CREATE PROCEDURE sp_get_platform_stats()
BEGIN
    SELECT
        (SELECT COUNT(*) FROM Utilisateur) as total_users,
        (SELECT COUNT(*) FROM Vendeur WHERE is_blocked = FALSE) as active_sellers,
        (SELECT COUNT(*) FROM Client) as total_clients,
        (SELECT COUNT(*) FROM Produit) as total_products,
        (SELECT COUNT(*) FROM sales WHERE DATE(created_at) = CURDATE()) as sales_today,
        (SELECT COALESCE(SUM(amount), 0) FROM sales WHERE DATE(created_at) = CURDATE()) as revenue_today,
        (SELECT COALESCE(SUM(amount), 0) FROM sales WHERE YEARWEEK(created_at) = YEARWEEK(NOW())) as revenue_this_week,
        (SELECT COALESCE(SUM(amount), 0) FROM sales WHERE MONTH(created_at) = MONTH(NOW()) AND YEAR(created_at) = YEAR(NOW())) as revenue_this_month;
END //

DELIMITER ;

-- ========================================================================
-- TRIGGERS FOR DATA INTEGRITY (ENHANCEMENTS)
-- ========================================================================

DELIMITER //

-- Trigger: Auto-update Vendeur.is_blocked when a block is added
CREATE TRIGGER trg_after_block_insert
AFTER INSERT ON Bloquer
FOR EACH ROW
BEGIN
    UPDATE Vendeur
    SET is_blocked = TRUE
    WHERE id_user = NEW.id_vendeur;
END //

-- Trigger: Auto-update Vendeur.is_blocked when an unblock is added
CREATE TRIGGER trg_after_unblock_insert
AFTER INSERT ON Debloquer
FOR EACH ROW
BEGIN
    UPDATE Vendeur
    SET is_blocked = FALSE
    WHERE id_user = NEW.id_vendeur;
END //

-- Trigger: Calculate prix_ht when prix is updated
CREATE TRIGGER trg_before_produit_update
BEFORE UPDATE ON Produit
FOR EACH ROW
BEGIN
    IF NEW.prix != OLD.prix THEN
        SET NEW.prix_ht = NEW.prix / (1 + (NEW.taux_tva / 100));
    END IF;
END //

DELIMITER ;

-- ========================================================================
-- SAMPLE DATA (OPTIONAL - COMMENT OUT FOR PRODUCTION)
-- ========================================================================

-- Insert a system admin user to own default categories (password: 'admin123' - CHANGE THIS!)
INSERT INTO Utilisateur (nom, prenom, email, motdepasse)
VALUES ('Admin', 'System', 'admin@groupev.local', '$2y$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi');
SET @admin_id = LAST_INSERT_ID();
INSERT INTO Gestionnaire (id_user) VALUES (@admin_id);

-- Insert default product categories
INSERT INTO Categorie (id_gestionnaire, lib) VALUES
(@admin_id, 'Électronique'),
(@admin_id, 'Vêtements'),
(@admin_id, 'Maison & Jardin'),
(@admin_id, 'Sports & Loisirs'),
(@admin_id, 'Alimentation'),
(@admin_id, 'Livres & Médias'),
(@admin_id, 'Beauté & Santé'),
(@admin_id, 'Jouets & Enfants'),
(@admin_id, 'Auto & Moto'),
(@admin_id, 'Autres');

-- ========================================================================
-- SUPPORT TICKET SYSTEM
-- ========================================================================

-- Support tickets created by sellers
CREATE TABLE ticket (
    id_ticket INT PRIMARY KEY AUTO_INCREMENT,
    titre VARCHAR(255) NOT NULL,
    id_vendeur INT NOT NULL,
    statut ENUM('ouvert', 'fermé', 'en_attente') NOT NULL DEFAULT 'ouvert',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_vendeur) REFERENCES Vendeur(id_user) ON DELETE CASCADE,
    INDEX idx_ticket_vendeur (id_vendeur),
    INDEX idx_ticket_statut (statut)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Messages within a ticket conversation
CREATE TABLE ticket_message (
    id_message INT PRIMARY KEY AUTO_INCREMENT,
    id_ticket INT NOT NULL,
    id_vendeur INT NOT NULL,
    message TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_ticket) REFERENCES ticket(id_ticket) ON DELETE CASCADE,
    FOREIGN KEY (id_vendeur) REFERENCES Vendeur(id_user) ON DELETE CASCADE,
    INDEX idx_msg_ticket (id_ticket)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ========================================================================
-- END OF SCHEMA
-- ========================================================================
