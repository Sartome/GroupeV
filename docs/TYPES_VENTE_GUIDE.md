# 📖 Guide d'Utilisation - Types de Vente

## Vue d'ensemble

Le système de types de vente permet de différencier les produits selon leur mode de commercialisation.

```
┌─────────────────────────────────────────────────────────────┐
│                    TYPES DE VENTE                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  🛒 STANDARD (0)         👥 GROUPE (1)        🔨 ENCHÈRE (2)│
│  ─────────────          ──────────────       ───────────── │
│  • Prix fixe            • Remises palier     • Prix variable│
│  • Achat direct         • Quantité minimum   • Offres       │
│  • Badge vert           • Badge orange       • Badge rouge  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛒 Vente Standard

### Caractéristiques
- **Prix fixe** défini par le vendeur
- **Achat immédiat** sans conditions
- **Aucune restriction** de quantité

### Cas d'usage
- Produits courants
- Articles à prix unique
- Vente au détail classique

### Exemple
```
Produit: Photo dédicassée de Barde
Prix: 50,00 €
Type: 🛒 Standard
Action: "Acheter maintenant"
```

---

## 👥 Vente Groupe

### Caractéristiques
- **Réductions dégressives** selon quantité
- **Quantité minimum** requise
- **Prix par palier** configurables

### Cas d'usage
- Achats en gros
- Promotions de groupe
- Ventes B2B

### Exemple
```
Produit: Pack de cartes collector
Prix de base: 100,00 €
Type: 👥 Vente Groupe

Paliers:
• 1-4 unités   : 100,00 € / unité
• 5-9 unités   : 90,00 € / unité (-10%)
• 10+ unités   : 80,00 € / unité (-20%)
```

### Structure de données (future)
```csharp
public class VenteGroupePalier
{
    public int IdPalier { get; set; }
    public int IdProduit { get; set; }
    public int QuantiteMin { get; set; }
    public int QuantiteMax { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal PourcentageRemise { get; set; }
}
```

---

## 🔨 Enchère (Bid)

### Caractéristiques
- **Prix de départ** défini
- **Enchères successives** par paliers
- **Date limite** d'enchère
- **Offre la plus haute** remporte

### Cas d'usage
- Articles rares ou uniques
- Produits collectors
- Ventes événementielles

### Exemple
```
Produit: Épée légendaire signée
Prix de départ: 500,00 €
Type: 🔨 Enchère
Date limite: 20/02/2026 18:00

Enchères actuelles:
• User1: 500,00 € (15/02 10:30)
• User2: 550,00 € (16/02 14:15) ⭐ Meilleure offre
• User3: 525,00 € (15/02 22:45) ❌ Surenchéri
```

### Structure de données (future)
```csharp
public class Enchere
{
    public int IdEnchere { get; set; }
    public int IdProduit { get; set; }
    public decimal PrixDepart { get; set; }
    public decimal PrixActuel { get; set; }
    public decimal PasEnchere { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public int? IdGagnant { get; set; }
}

public class OffreEnchere
{
    public int IdOffre { get; set; }
    public int IdEnchere { get; set; }
    public int IdUser { get; set; }
    public decimal Montant { get; set; }
    public DateTime DateOffre { get; set; }
    public bool EstActive { get; set; }
}
```

---

## 🎯 Implémentation dans l'Interface

### EditProductWindow
```xml
<ComboBox x:Name="TypeVenteComboBox">
    <ComboBoxItem Content="🛒 Vente Standard" Tag="0"/>    <!-- Par défaut -->
    <ComboBoxItem Content="👥 Vente Groupe" Tag="1"/>      <!-- Avec paliers -->
    <ComboBoxItem Content="🔨 Enchère (Bid)" Tag="2"/>     <!-- Avec enchères -->
</ComboBox>
```

### DataGrid Column
```xml
<DataGridTemplateColumn Header="Type" Width="110">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border Background="{Binding TypeVenteColor}">
                <TextBlock Text="{Binding TypeVenteBadge}"/>
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

## 🔄 Workflow Utilisateur

### Création d'un produit
```
1. Cliquer sur "Ajouter Produit"
   ↓
2. Remplir Description, Prix, Catégorie
   ↓
3. Sélectionner le Type de vente
   ↓
   • Standard → Aucune config supplémentaire
   • Groupe → Définir paliers (future)
   • Enchère → Définir dates et pas (future)
   ↓
4. Upload image
   ↓
5. Sauvegarder
```

### Affichage dans le tableau
```
ID | Image | Produit          | Prix     | Type      | Catégorie
─────────────────────────────────────────────────────────────────
3  | [img] | Photo dédicassée | 50,00 €  | [Standard] | Acquisition
12 | [img] | Pack cartes      | 100,00 € | [Groupe]   | Collections
7  | [img] | Épée légendaire  | 500,00 € | [Enchère]  | Rare
```

---

## 📊 Statistiques & Analytics

### Filtres par type
```csharp
// Nombre de produits par type
var standardCount = produits.Count(p => p.TypeVente == 0);
var groupeCount = produits.Count(p => p.TypeVente == 1);
var enchereCount = produits.Count(p => p.TypeVente == 2);

// Prix moyen par type
var avgStandard = produits.Where(p => p.TypeVente == 0).Average(p => p.Prix);
var avgGroupe = produits.Where(p => p.TypeVente == 1).Average(p => p.Prix);
var avgEnchere = produits.Where(p => p.TypeVente == 2).Average(p => p.Prix);
```

### Graphiques suggérés
- **Répartition des types** : PieChart
- **Évolution des prix** : LineChart par type
- **Performance des ventes** : BarChart par type

---

## ⚙️ Configuration Base de Données

### Table actuelle
```sql
CREATE TABLE produit (
    id_produit INT PRIMARY KEY AUTO_INCREMENT,
    description VARCHAR(255),
    prix DECIMAL(10,2),
    id_categorie INT,
    type_vente TINYINT DEFAULT 0,  -- ✨ NOUVEAU
    image VARCHAR(255),
    image_alt VARCHAR(255),
    id_vendeur INT,
    created_at DATETIME,
    updated_at DATETIME
);
```

### Index de performance
```sql
CREATE INDEX idx_type_vente ON produit(type_vente);
CREATE INDEX idx_type_prix ON produit(type_vente, prix);
```

---

## 🚀 Extensions Futures

### Phase 2 : Vente Groupe
- [ ] Table `vente_groupe_palier`
- [ ] UI pour configuration paliers
- [ ] Calcul automatique prix selon quantité
- [ ] Visualisation des remises

### Phase 3 : Enchères
- [ ] Tables `enchere` et `offre_enchere`
- [ ] UI enchères en temps réel (SignalR)
- [ ] Notifications surenchère
- [ ] Timer compte à rebours
- [ ] Historique des offres

### Phase 4 : Paiement
- [ ] Intégration gateway paiement
- [ ] Gestion paniers multi-types
- [ ] Facturation automatique
- [ ] Suivi commandes

---

## 📞 Support

Pour toute question ou suggestion :
- 📧 Email: support@groupev.com
- 💬 Issues: GitHub Repository
- 📖 Wiki: Documentation complète

---

**Version du guide :** 1.0  
**Dernière mise à jour :** 12 Janvier 2026
