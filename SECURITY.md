# Sécurité de l'application — GroupeV

Ce document décrit les mécanismes de sécurité mis en place dans l'application cliente GroupeV, ainsi que les recommandations pour les renforcer en environnement de production.

---

## 1. Authentification

### 1.1 Protection anti-force brute

La fenêtre de connexion (`LoginWindow`) implémente un verrouillage temporaire côté client :

```csharp
private const int MaxAttempts = 5;
private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(2);
```

- Après **5 tentatives échouées**, l'interface est verrouillée pendant **2 minutes**.
- Un message indique le temps restant avant de pouvoir réessayer.
- Le compteur de tentatives est réinitialisé après une connexion réussie.

> ⚠️ Ce verrouillage est **côté client uniquement**. Pour une protection robuste, il doit être complété par un verrouillage côté serveur (base de données ou backend PHP).

### 1.2 Validation des entrées

- L'e-mail est assaini avant utilisation (`Trim()`).
- Les requêtes de vérification passent par Entity Framework Core avec des **paramètres liés** (pas de concaténation de chaînes), ce qui protège contre les injections SQL.

---

## 2. Gestion des mots de passe

### 2.1 Algorithmes supportés

Le `PasswordVerifier` détecte automatiquement le type de hachage stocké en base et applique la vérification correspondante :

| Algorithme | Préfixe | Usage |
|------------|---------|-------|
| **Argon2id** | `$argon2id$` | ✅ Recommandé — utilisé par le backend PHP (`PASSWORD_ARGON2ID`) |
| **Argon2i** | `$argon2i$` | Héritage — supporté en lecture |
| **BCrypt** | `$2a$`, `$2b$`, `$2x$`, `$2y$` | Héritage — supporté en lecture |
| **Hex (MD5/SHA-256/SHA-512)** | *(longueur 32/64/128)* | ⛔ Obsolète — supporté uniquement pour migration |
| **Texte clair** | *(aucun préfixe)* | ⛔ Développement uniquement — jamais en production |

### 2.2 Argon2id — paramètres de sécurité

Les paramètres utilisés sont ceux par défaut de PHP `password_hash` avec `PASSWORD_ARGON2ID` :

```
m = 65 536 (mémoire : 64 Mo)
t = 4      (itérations)
p = 1      (parallélisme)
```

Ces paramètres offrent une bonne résistance aux attaques par dictionnaire et aux attaques matérielles (GPU/ASIC).

### 2.3 Recommandations

- ✅ Utiliser exclusivement **Argon2id** pour les nouveaux comptes.
- ✅ Déclencher un re-hachage transparent à la prochaine connexion pour les comptes avec un ancien algorithme (BCrypt, MD5…).
- ❌ Ne jamais stocker de mots de passe en clair, même temporairement.

---

## 3. Connexion à la base de données

### 3.1 Chaîne de connexion

La chaîne de connexion est lue depuis une **variable d'environnement** :

```csharp
var connectionString = Environment.GetEnvironmentVariable("GROUPEV_CONNECTION_STRING")
    ?? "Server=127.0.0.1;Port=32779;Uid=db;Pwd=db;Database=db;...";
```

- En **production**, positionner obligatoirement `GROUPEV_CONNECTION_STRING` avec des identifiants dédiés.
- La valeur de repli (identifiants DDEV `db/db`) ne doit exister qu'en **environnement de développement local**.

> ⚠️ Ne jamais commiter de chaîne de connexion contenant des identifiants réels dans le dépôt Git.

### 3.2 Compte MariaDB dédié

En production, le compte MariaDB utilisé par l'application doit disposer uniquement des droits nécessaires :

```sql
-- Créer un compte dédié avec droits minimaux
CREATE USER 'groupev_app'@'localhost' IDENTIFIED BY 'mot_de_passe_fort';
GRANT SELECT, INSERT, UPDATE, DELETE ON db.* TO 'groupev_app'@'localhost';
FLUSH PRIVILEGES;
```

### 3.3 Résilience des connexions

Entity Framework Core est configuré avec une stratégie de réessai automatique :

```csharp
options.EnableRetryOnFailure(
    maxRetryCount: 3,
    maxRetryDelay: TimeSpan.FromSeconds(5),
    errorNumbersToAdd: null);
options.CommandTimeout(30);
```

Cela protège contre les erreurs transitoires réseau sans exposer l'application à des blocages indéfinis.

---

## 4. Gestion de session

### 4.1 État d'authentification

L'accès au tableau de bord est conditionné par le service d'authentification :

```csharp
if (!AuthenticationService.IsAuthenticated)
{
    // Redirection vers la fenêtre de connexion
}
```

- À chaque ouverture de `MainWindow`, l'état d'authentification est vérifié.
- En cas d'absence d'authentification, l'utilisateur est redirigé vers `LoginWindow` et `MainWindow` est fermée.

### 4.2 Recommandations complémentaires

- ✅ Implémenter une **expiration de session** automatique après une période d'inactivité.
- ✅ Effacer les données sensibles en mémoire (ex. mot de passe saisi) après vérification.
- ✅ Utiliser `SecureString` pour manipuler le mot de passe en mémoire avant vérification.

---

## 5. Données locales (« Se souvenir de moi »)

La fonctionnalité « Se souvenir de moi » enregistre uniquement l'**adresse e-mail** de l'utilisateur :

```
%LOCALAPPDATA%\GroupeV\remember.json
```

- ✅ Seul l'e-mail est persisté — jamais le mot de passe ni le token de session.
- ✅ Le fichier est stocké dans un répertoire utilisateur isolé (`LocalApplicationData`).
- ❌ Le fichier JSON n'est pas chiffré : un autre utilisateur Windows ayant accès au profil peut le lire.

**Recommandation :** chiffrer le fichier avec `System.Security.Cryptography.ProtectedData` (DPAPI) pour lier le fichier au compte Windows de l'utilisateur :

```csharp
// Chiffrement lié au compte Windows courant
var encrypted = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(email),
    null,
    DataProtectionScope.CurrentUser);
```

---

## 6. Journalisation et données sensibles

- En mode **Debug**, le logging EF Core avec données sensibles est activé (`EnableSensitiveDataLogging`).
- En mode **Release**, ce logging est désactivé automatiquement via la directive `#if DEBUG`.

> ⚠️ Vérifier que les builds de production utilisent bien la configuration `Release` et non `Debug`.

---

## 7. Points d'amélioration recommandés

| Priorité | Recommandation |
|----------|----------------|
| 🔴 Haute | Verrouillage anti-force brute côté serveur (base de données ou backend PHP) |
| 🔴 Haute | Compte MySQL dédié avec droits minimaux en production |
| 🔴 Haute | Variable d'environnement obligatoire pour la chaîne de connexion en production |
| 🟠 Moyenne | Chiffrement DPAPI pour le fichier « Se souvenir de moi » |
| 🟠 Moyenne | Expiration automatique de session après inactivité |
| 🟠 Moyenne | Migration progressive des hachages MD5/SHA vers Argon2id |
| 🟡 Faible | Audit log des connexions (réussies et échouées) |
| 🟡 Faible | Utilisation de `SecureString` pour le mot de passe en mémoire |

---

## 8. Signalement d'une vulnérabilité

Si vous découvrez une faille de sécurité dans ce projet, veuillez la signaler de manière responsable en ouvrant une **issue privée** ou en contactant directement l'équipe GroupeV via le dépôt GitHub.

Ne publiez pas de détails exploitables publiquement avant qu'un correctif soit disponible.
