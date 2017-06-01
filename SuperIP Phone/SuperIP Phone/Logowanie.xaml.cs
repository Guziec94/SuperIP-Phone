using Ozeki.Media;
using System;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for Logowanie.xaml
    /// </summary>
    public partial class Logowanie : Page
    {
        public string login;

        public Logowanie()
        {
            InitializeComponent();
            baza_danych.polacz_z_baza();
            WyborAdresuIP();
            WybierzDomyslnySprzet();
        }

        private void WybierzDomyslnySprzet()
        {
            while (Microphone.GetDevicesCount() == 0)
            {
                if (MessageBox.Show("Brak wejściowego urządzenia audio, bez odpowiedniego sprzętu aplikacja nie będzie działać. Podłącz mikrofon i naciśnij Tak, aby kontynuować. Jeśli naciśniesz Nie aplikacja zostanie zamknięta.", "Brak odpowiedniego sprzętu", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                {
                    Environment.Exit(0);
                }
            }
            while (Speaker.GetDevicesCount() == 0)
            {
                if (MessageBox.Show("Brak wyjściowego urządzenia audio, bez odpowiedniego sprzętu aplikacja nie będzie działać. Podłącz głośniki lub słuchawki i naciśnij Tak, aby kontynuować. Jeśli naciśniesz Nie aplikacja zostanie zamknięta.", "Brak odpowiedniego sprzętu", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                {
                    Environment.Exit(0);
                }
            }
            if (System.Windows.Application.Current.Properties["WejscieAudio"] == null)
            {
                System.Windows.Application.Current.Properties["WejscieAudio"] = Microphone.GetDefaultDevice();
            }
            if (System.Windows.Application.Current.Properties["WyjscieAudio"] == null)
            {
                Speaker glosnik = Speaker.GetDefaultDevice();
                glosnik.Volume = 1;
                System.Windows.Application.Current.Properties["WyjscieAudio"] = glosnik;
            }
        }

        private void zaloz_konto_label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var okno_rejestracji = new Rejestracja();
            NavigationService nav = NavigationService.GetNavigationService(this);
            nav.Navigate(okno_rejestracji);
        }

        private void zaloguj_button_Click(object sender, RoutedEventArgs e)
        {
            string login = login_textBox.Text;
            if (login != "" && passwordBox.Password != "") 
            {
                if(baza_danych.zaloguj(login, passwordBox.Password))
                {
                    baza_danych.ustaw_status(true);
                    var strona_glowna = new StronaGlowna();
                    System.Windows.Application.Current.Properties["strona_glowna"] = strona_glowna;//używane przez baza_danych.cs by móc wywoływać metody z StronaGlowna.cs
                    NavigationService nav = NavigationService.GetNavigationService(this);
                    nav.Navigate(strona_glowna);
                }
                else
                {
                    MessageBox.Show("Login lub hasło jest nieprawidłowe.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Login ani hasło nie może być puste.", "Uzupełnij wymagane pola", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void WyborAdresuIP()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.IsDnsEligible)
                    {
                        AdresIPcomboBox.Items.Add(ip.Address.ToString());
                    }
                }
            }
            AdresIPcomboBox.SelectedIndex = 0;
        }

        private void AdresIPcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string wybraneIP = AdresIPcomboBox.SelectedItem as string;
            System.Windows.Application.Current.Properties["AdresIP"] = wybraneIP;
        }
    }
}
