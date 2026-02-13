-- Migration: Ajout de la colonne type_vente
-- Date: 2026-01-12
-- Description: Ajout du type de vente aux produits (0=Standard, 1=VenteGroupe, 2=Enchere)

USE vente_groupe;

-- Ajouter la colonne type_vente à la table produit
ALTER TABLE produit 
ADD COLUMN type_vente TINYINT NOT NULL DEFAULT 0 
COMMENT '0=Standard, 1=VenteGroupe, 2=Enchere';

-- Index pour améliorer les requêtes par type de vente
CREATE INDEX idx_type_vente ON produit(type_vente);

-- Vérifier la structure
DESCRIBE produit;

SELECT 'Migration completed successfully' AS status;
