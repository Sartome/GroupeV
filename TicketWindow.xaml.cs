using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GroupeV.Controls;
using GroupeV.Models;
using Microsoft.EntityFrameworkCore;

namespace GroupeV
{
    /// <summary>
    /// Support ticket system — list + conversation view for the current seller.
    /// </summary>
    public partial class TicketWindow : Window
    {
        private Ticket? _selectedTicket;

        public TicketWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) => await RefreshTicketListAsync();
        }

        // ── Ticket list ──────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task RefreshTicketListAsync()
        {
            try
            {
                var sellerId = AuthenticationService.CurrentSeller?.IdUser ?? 0;
                using var ctx = new DatabaseContext();
                var tickets = await ctx.Tickets
                    .Where(t => t.IdVendeur == sellerId)
                    .OrderByDescending(t => t.UpdatedAt)
                    .ToListAsync();

                TicketListBox.ItemsSource = tickets;
            }
            catch (Exception ex)
            {
                NeuDialog.ShowError(this, "Erreur", $"Impossible de charger les tickets : {ex.Message}");
            }
        }

        private async void TicketListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TicketListBox.SelectedItem is Ticket ticket)
                await LoadConversationAsync(ticket);
        }

        // ── Conversation ─────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadConversationAsync(Ticket ticket)
        {
            _selectedTicket = ticket;
            TicketTitleText.Text = ticket.Titre;
            TicketStatusText.Text = ticket.StatutEmoji;
            EmptyStateText.Visibility = Visibility.Collapsed;
            MessagesScrollViewer.Visibility = Visibility.Visible;
            ReplyArea.Visibility = ticket.Statut != "fermé" ? Visibility.Visible : Visibility.Collapsed;
            CloseTicketBtn.Visibility = ticket.Statut != "fermé" ? Visibility.Visible : Visibility.Collapsed;

            try
            {
                using var ctx = new DatabaseContext();
                var messages = await ctx.TicketMessages
                    .Include(m => m.Vendeur).ThenInclude(v => v!.Utilisateur)
                    .Where(m => m.IdTicket == ticket.IdTicket)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                RenderMessages(messages);
            }
            catch (Exception ex)
            {
                NeuDialog.ShowError(this, "Erreur", $"Impossible de charger les messages : {ex.Message}");
            }
        }

        private void RenderMessages(List<TicketMessage> messages)
        {
            MessagesPanel.Children.Clear();

            foreach (var msg in messages)
            {
                var isMe = msg.IsFromCurrentUser;
                var bubble = BuildMessageBubble(msg, isMe);
                MessagesPanel.Children.Add(bubble);
            }

            // Scroll to bottom
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                () => MessagesScrollViewer.ScrollToBottom());
        }

        private FrameworkElement BuildMessageBubble(TicketMessage msg, bool isMe)
        {
            var accentColor = (SolidColorBrush)FindResource("NeuAccentBrush");
            var surfaceColor = (SolidColorBrush)FindResource("NeuInputBgBrush");
            var textPrimary = (SolidColorBrush)FindResource("NeuTextPrimaryBrush");
            var textSecondary = (SolidColorBrush)FindResource("NeuTextSecondaryBrush");

            var bubble = new Border
            {
                CornerRadius = isMe
                    ? new CornerRadius(14, 14, 2, 14)
                    : new CornerRadius(14, 14, 14, 2),
                Background = isMe ? accentColor : surfaceColor,
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 480,
                Margin = new Thickness(isMe ? 60 : 0, 0, isMe ? 0 : 60, 0)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = msg.Message,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = isMe ? Brushes.White : textPrimary,
                FontFamily = (FontFamily)FindResource("NeuFontFamily")
            });
            stack.Children.Add(new TextBlock
            {
                Text = $"{msg.ExpéditeurNom}  {msg.HeureFormate}",
                FontSize = 10,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = isMe ? new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) : textSecondary,
                FontFamily = (FontFamily)FindResource("NeuMonoFont")
            });

            bubble.Child = stack;

            return new Grid
            {
                HorizontalAlignment = isMe ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 4),
                Children = { bubble }
            };
        }

        // ── Actions ──────────────────────────────────────────────────────────

        private async void NewTicket_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewTicketDialog { Owner = this };
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.Titre)) return;

            try
            {
                var sellerId = AuthenticationService.CurrentSeller?.IdUser ?? 0;
                using var ctx = new DatabaseContext();

                var ticket = new Ticket
                {
                    Titre = dialog.Titre,
                    IdVendeur = sellerId,
                    Statut = "ouvert"
                };
                ctx.Tickets.Add(ticket);
                await ctx.SaveChangesAsync();

                // Add the first message (the description)
                if (!string.IsNullOrWhiteSpace(dialog.Description))
                {
                    ctx.TicketMessages.Add(new TicketMessage
                    {
                        IdTicket = ticket.IdTicket,
                        IdVendeur = sellerId,
                        Message = dialog.Description
                    });
                    await ctx.SaveChangesAsync();
                }

                await RefreshTicketListAsync();
                TicketListBox.SelectedItem = TicketListBox.Items
                    .OfType<Ticket>()
                    .FirstOrDefault(t => t.IdTicket == ticket.IdTicket);
            }
            catch (Exception ex)
            {
                NeuDialog.ShowError(this, "Erreur", $"Impossible de créer le ticket : {ex.Message}");
            }
        }

        private async void SendReply_Click(object sender, RoutedEventArgs e) => await SendReplyAsync();

        private async void ReplyBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                await SendReplyAsync();
            }
        }

        private async System.Threading.Tasks.Task SendReplyAsync()
        {
            var text = ReplyBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || _selectedTicket == null) return;

            var sellerId = AuthenticationService.CurrentSeller?.IdUser ?? 0;

            try
            {
                using var ctx = new DatabaseContext();
                ctx.TicketMessages.Add(new TicketMessage
                {
                    IdTicket = _selectedTicket.IdTicket,
                    IdVendeur = sellerId,
                    Message = text
                });
                await ctx.SaveChangesAsync();
                ReplyBox.Clear();
                await LoadConversationAsync(_selectedTicket);
            }
            catch (Exception ex)
            {
                NeuDialog.ShowError(this, "Erreur", $"Impossible d'envoyer le message : {ex.Message}");
            }
        }

        private async void CloseTicket_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTicket == null) return;
            if (!NeuDialog.Confirm(this, "Fermer le ticket",
                    "Marquer ce ticket comme fermé ? Vous ne pourrez plus y répondre.")) return;

            try
            {
                using var ctx = new DatabaseContext();
                var t = await ctx.Tickets.FindAsync(_selectedTicket.IdTicket);
                if (t != null)
                {
                    t.Statut = "fermé";
                    await ctx.SaveChangesAsync();
                }

                await RefreshTicketListAsync();
                _selectedTicket = null;
                TicketTitleText.Text = "Sélectionnez un ticket";
                TicketStatusText.Text = string.Empty;
                MessagesPanel.Children.Clear();
                ReplyArea.Visibility = Visibility.Collapsed;
                CloseTicketBtn.Visibility = Visibility.Collapsed;
                EmptyStateText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                NeuDialog.ShowError(this, "Erreur", $"Impossible de fermer le ticket : {ex.Message}");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
