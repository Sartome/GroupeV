using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GroupeV.ViewModels
{
    /// <summary>
    /// Classe de base pour tous les ViewModels.
    /// Implémente INotifyPropertyChanged pour notifier la Vue des changements de propriétés.
    /// 
    /// CONCEPT CLÉ MVVM POUR LES ÉTUDIANTS :
    /// - INotifyPropertyChanged est ESSENTIEL pour le binding bidirectionnel
    /// - Quand une propriété change dans le ViewModel, la Vue est automatiquement mise à jour
    /// - SetProperty simplifie le code et évite les répétitions
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        // ========== IMPLÉMENTATION DE INotifyPropertyChanged ==========

        /// <summary>
        /// Événement déclenché lorsqu'une propriété change.
        /// WPF s'abonne automatiquement à cet événement via le Binding.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Méthode pour notifier la Vue qu'une propriété a changé.
        /// 
        /// PARAMÈTRES :
        /// - propertyName : Nom de la propriété qui a changé
        /// - [CallerMemberName] : Attribut magique ! Le compilateur remplit automatiquement
        ///   le nom de la propriété appelante. Plus besoin de l'écrire manuellement !
        /// 
        /// EXEMPLE D'UTILISATION :
        /// OnPropertyChanged(); // Dans le setter de MaPropriete
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Invoke l'événement seulement si des abonnés existent
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Méthode utilitaire pour définir une propriété ET notifier le changement.
        /// 
        /// PATTERN RECOMMANDÉ DANS LE COURS :
        /// Cette méthode centralise la logique de changement de propriété :
        /// 1. Vérifie si la valeur a vraiment changé (évite les notifications inutiles)
        /// 2. Met à jour le champ backing
        /// 3. Notifie la Vue via PropertyChanged
        /// 
        /// SIGNATURE :
        /// - field : Référence au champ backing (private)
        /// - value : Nouvelle valeur à assigner
        /// - propertyName : Nom de la propriété (auto-rempli)
        /// 
        /// RETOUR :
        /// - true si la valeur a changé
        /// - false si la valeur était déjà identique
        /// 
        /// EXEMPLE D'UTILISATION COMPLÈTE :
        /// 
        /// private string _titre = string.Empty;
        /// public string Titre
        /// {
        ///     get => _titre;
        ///     set => SetProperty(ref _titre, value);
        /// }
        /// 
        /// AVANTAGES :
        /// - Code concis (1 ligne au lieu de 5-10)
        /// - Pas d'oubli de notification
        /// - Performance optimisée (pas de notification si valeur identique)
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            // Comparaison d'égalité : évite les notifications si la valeur n'a pas changé
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false; // Aucun changement
            }

            // Mise à jour du champ backing
            field = value;

            // Notification de la Vue
            OnPropertyChanged(propertyName);

            return true; // Changement effectué
        }

        /// <summary>
        /// Variante de SetProperty qui permet d'exécuter une action supplémentaire après le changement.
        /// 
        /// UTILISATION AVANCÉE :
        /// - Utile pour déclencher des validations
        /// - Mettre à jour des propriétés calculées
        /// - Exécuter des commandes
        /// 
        /// EXEMPLE :
        /// public string Prenom
        /// {
        ///     get => _prenom;
        ///     set => SetProperty(ref _prenom, value, () => OnPropertyChanged(nameof(NomComplet)));
        /// }
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                onChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}
