using System.Windows;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for CzyOdebrac.xaml
    /// </summary>
    public partial class CzyOdebrac : Window
    {
        bool wyjscie = false;//domyślna wartość gdy ktoś kliknie krzyżyk - odrzuci rozmowę
        public CzyOdebrac(string dzwoniacy)
        {
            InitializeComponent();
            Title = dzwoniacy + " dzwoni, chcesz odebrać?";
            pytanieTextBlock.Text = dzwoniacy + " dzwoni, odebrać?";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = wyjscie;
        }

        private void odbierzButton_Click(object sender, RoutedEventArgs e)
        {
            wyjscie = true;
            Close();
        }

        private void odrzucButton_Click(object sender, RoutedEventArgs e)
        {
            wyjscie = false;
            Close();
        }
    }
}
