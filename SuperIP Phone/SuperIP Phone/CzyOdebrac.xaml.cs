using System.Media;
using System.Threading.Tasks;
using System.Windows;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for CzyOdebrac.xaml
    /// </summary>
    public partial class CzyOdebrac : Window
    {
        SoundPlayer player;
        bool czy_wydawac_dzwiek;
        bool wyjscie = false;//domyślna wartość gdy ktoś kliknie krzyżyk - odrzuci rozmowę
        Task dzwonek;//Task odpowiadający za wydawnie dźwięku

        public CzyOdebrac(string dzwoniacy)
        {
            InitializeComponent();
            Title = dzwoniacy + " dzwoni, chcesz odebrać?";
            pytanieTextBlock.Text = dzwoniacy + " dzwoni, odebrać?";
            player = new SoundPlayer(@"C:\Windows\Media\Windows Ringin.wav");
            player.Load();
            czy_wydawac_dzwiek = true;
            dzwonek = new Task(() => dzwon_dzwonkiem());
            dzwonek.Start();
        }

        private async void dzwon_dzwonkiem()//funkcja asynchroniczna odpowiadająca za wydawanie dźwięku przychodzącej rozmowy
        {
            while (czy_wydawac_dzwiek)
            {
                player.PlaySync();
                await Task.Delay(750);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            czy_wydawac_dzwiek = false;
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
