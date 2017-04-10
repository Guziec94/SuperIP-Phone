using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                    var strona_glowna = new StronaGlowna();
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
    }
}
