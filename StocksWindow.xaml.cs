using System.Collections.Generic;
using System.Windows;

namespace GroupeV
{
    public partial class StocksWindow : Window
    {
        public StocksWindow()
        {
            InitializeComponent();

            var items = new List<object>
            {
                new { Symbol = "MSFT", Price = 330.12 },
                new { Symbol = "AAPL", Price = 172.45 },
                new { Symbol = "GOOG", Price = 128.34 }
            };

            StocksDataGrid.ItemsSource = items;
        }
    }
}