# GroupeV — ChaosManager

> Application de bureau Windows pour la gestion des ventes, des vendeurs et du catalogue produit.

---

## Présentation

**GroupeV** (nom technique : *ChaosManager*) est une application client lourd développée en C# / WPF sur **.NET 8**. Elle permet aux vendeurs de gérer leur catalogue, de suivre leurs ventes et d'interagir avec le support, via une interface au design **neumorphique**.

L'application se connecte à une base de données **MySQL** partagée (`vente_groupe`) et est conçue pour fonctionner en parallèle d'un backend PHP.

---

## Fonctionnalités principales

### 🔐 Authentification
- Connexion par e-mail / mot de passe
- Protection anti-force brute : verrouillage temporaire après 5 tentatives échouées (2 minutes)
- Option « Se souvenir de moi » (e-mail sauvegardé localement)

### 📊 Tableau de bord
- Vue synthétique : nombre de produits, catégories, vendeurs, factures
- Graphiques interactifs (LiveCharts) :
  - Répartition des produits par catégorie (camembert)
  - Prix moyens par catégorie (barres)
  - Carte thermique d'activité

### 📦 Gestion des produits
- Ajout, modification et suppression de produits
- Association à une catégorie et à un vendeur
- Gestion des images (dimensions, taille)
- Calcul TVA (prix HT + taux TVA)
- Filtrage et recherche en temps réel

### 🛒 Types de vente
| Type | Description |
|------|-------------|
| 🛒 **Standard** | Achat direct à prix fixe |
| 👥 **Vente Groupe** | Prix dégressif selon le nombre d'acheteurs, avec date d'expiration |
| 🔨 **Enchère** | Prix variable, offres concurrentes |

### 👤 Gestion des vendeurs
- Fiche vendeur : nom d'entreprise, SIRET, adresse, e-mail professionnel
- Indicateur de certification vendeur
- Rattachement à un compte utilisateur

### 🎫 Système de tickets de support
- Création de tickets avec titre et message
- Suivi des statuts : `ouvert`, `en_attente`, `fermé`
- Historique des messages par ticket

### 🏥 Diagnostic de la base de données
- Fenêtre dédiée au contrôle de santé de la connexion MySQL
- Vérification de l'existence des tables requises
- Reconnexion automatique avec stratégie de réessai (3 tentatives, délai exponentiel)

---

## Architecture technique

```
GroupeV/
├── Models/              # Entités métier (Produit, Vendeur, Ticket, …)
├── ViewModels/          # Logique de présentation (pattern MVVM)
│   ├── DashboardViewModel.cs
│   ├── ViewModelBase.cs
│   └── RelayCommand.cs
├── Controls/            # Composants WPF personnalisés (NeuDialog)
├── Helpers/             # Utilitaires métier (TypeVenteHelper)
├── Utilities/           # Outils techniques (DatabaseTester)
├── DatabaseContext.cs   # Contexte Entity Framework Core
├── DatabaseHelper.cs    # Vérifications de connexion MySQL
├── PasswordVerifier.cs  # Vérification des mots de passe (Argon2id / BCrypt)
├── LoginWindow.xaml     # Fenêtre d'authentification
├── MainWindow.xaml      # Tableau de bord principal
├── TicketWindow.xaml    # Gestion des tickets
├── EditProductWindow.xaml
└── EditProfileWindow.xaml
```

### Pattern MVVM
- Les **ViewModels** exposent des propriétés observables et des commandes (`ICommand` via `RelayCommand`).
- Les **vues XAML** se lient aux ViewModels via le data binding WPF.
- Le code-behind est limité à l'initialisation, aux animations et aux dialogues.

---

## Stack technique

| Composant | Technologie |
|-----------|-------------|
| Framework | .NET 8 — WPF (net8.0-windows, win-x64) |
| Langage | C# 12 |
| Base de données | MySQL 8 |
| ORM | Entity Framework Core + Pomelo MySql |
| Graphiques | LiveChartsCore + SkiaSharp |
| Hachage des mots de passe | Konscious.Security.Cryptography (Argon2id) |
| Design UI | Neumorphisme (WPF custom styles) |

---

## Prérequis

- Windows 10 / 11 (x64)
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL 8.0+ (ex. via XAMPP) avec la base `vente_groupe`

---

## Configuration

La chaîne de connexion à la base de données est lue depuis la variable d'environnement :

```
GROUPEV_CONNECTION_STRING=Server=localhost;Uid=root;Pwd=...;Database=vente_groupe;
```

En l'absence de cette variable, une valeur par défaut (localhost, sans mot de passe) est utilisée — **à ne jamais utiliser en production**.

---

## Lancement

```powershell
# Depuis le répertoire du projet
dotnet run
```

Ou ouvrir `GroupeV.sln` dans Visual Studio 2022+ et lancer avec **F5**.

---

## Compatibilité avec le backend PHP

Le `PasswordVerifier` est conçu pour être 100 % compatible avec la classe `Security` du backend PHP :

- PHP génère des hachages via `password_hash($pwd, PASSWORD_ARGON2ID)`
- L'application C# les vérifie via `Konscious.Security.Cryptography`
- Les anciens hachages BCrypt, MD5 et SHA sont également supportés (rétrocompatibilité)

---

## Licence

Copyright © 2024 GroupeV Team. Tous droits réservés.
