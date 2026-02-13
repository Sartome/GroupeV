using System.Windows;
using System.Windows.Media;

namespace GroupeV.Controls;

/// <summary>
/// Popup neumorphique thématique remplaçant MessageBox.
/// </summary>
public partial class NeuDialog : Window
{
    public bool Result { get; private set; }

    private NeuDialog()
    {
        InitializeComponent();
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    /// <summary>
    /// Affiche un popup d'information.
    /// </summary>
    public static void ShowInfo(Window? owner, string title, string message)
    {
        var dlg = CreateDialog(owner, title, message, DialogIcon.Info);
        dlg.ShowDialog();
    }

    /// <summary>
    /// Affiche un popup de succès.
    /// </summary>
    public static void ShowSuccess(Window? owner, string title, string message)
    {
        var dlg = CreateDialog(owner, title, message, DialogIcon.Success);
        dlg.ShowDialog();
    }

    /// <summary>
    /// Affiche un popup d'erreur.
    /// </summary>
    public static void ShowError(Window? owner, string title, string message)
    {
        var dlg = CreateDialog(owner, title, message, DialogIcon.Error);
        dlg.ShowDialog();
    }

    /// <summary>
    /// Affiche un popup d'avertissement.
    /// </summary>
    public static void ShowWarning(Window? owner, string title, string message)
    {
        var dlg = CreateDialog(owner, title, message, DialogIcon.Warning);
        dlg.ShowDialog();
    }

    /// <summary>
    /// Affiche un popup de confirmation Oui/Non.
    /// </summary>
    public static bool Confirm(Window? owner, string title, string message)
    {
        var dlg = CreateDialog(owner, title, message, DialogIcon.Question);
        dlg.PrimaryButton.Content = "Oui";
        dlg.SecondaryButton.Content = "Non";
        dlg.SecondaryButton.Visibility = Visibility.Visible;
        dlg.ShowDialog();
        return dlg.Result;
    }

    private static NeuDialog CreateDialog(Window? owner, string title, string message, DialogIcon icon)
    {
        var dlg = new NeuDialog
        {
            TitleTextBlock = { Text = title },
            MessageTextBlock = { Text = message }
        };

        if (owner != null)
            dlg.Owner = owner;

        ApplyIcon(dlg, icon);
        return dlg;
    }

    private static void ApplyIcon(NeuDialog dlg, DialogIcon icon)
    {
        switch (icon)
        {
            case DialogIcon.Info:
                dlg.IconText.Text = "ℹ";
                dlg.IconBorder.Background = (SolidColorBrush)dlg.FindResource("NeuAccentBrush");
                dlg.IconText.Foreground = Brushes.White;
                break;
            case DialogIcon.Success:
                dlg.IconText.Text = "✓";
                dlg.IconBorder.Background = (SolidColorBrush)dlg.FindResource("NeuSuccessBrush");
                dlg.IconText.Foreground = Brushes.White;
                break;
            case DialogIcon.Warning:
                dlg.IconText.Text = "⚠";
                dlg.IconBorder.Background = (SolidColorBrush)dlg.FindResource("NeuWarningBrush");
                dlg.IconText.Foreground = Brushes.White;
                break;
            case DialogIcon.Error:
                dlg.IconText.Text = "✕";
                dlg.IconBorder.Background = (SolidColorBrush)dlg.FindResource("NeuDangerBrush");
                dlg.IconText.Foreground = Brushes.White;
                break;
            case DialogIcon.Question:
                dlg.IconText.Text = "?";
                dlg.IconBorder.Background = (SolidColorBrush)dlg.FindResource("NeuAccentBrush");
                dlg.IconText.Foreground = Brushes.White;
                break;
        }
    }

    private enum DialogIcon { Info, Success, Warning, Error, Question }
}
