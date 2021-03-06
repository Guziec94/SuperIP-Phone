﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for Rejestracja.xaml
    /// </summary>
    public partial class Rejestracja : Page
    {
        Dictionary<int, string> lista_dzialow;

        public Rejestracja()
        {
            InitializeComponent();
            lista_dzialow = baza_danych.pobierz_liste_dzialow();
            lista_dzialow_comboBox.ItemsSource = lista_dzialow;
            lista_dzialow_comboBox.DisplayMemberPath = "Value";
            lista_dzialow_comboBox.SelectedIndex = 0;
        }

        private void zarejestruj_button_Click(object sender, RoutedEventArgs e)
        {
            string login = login_textBox.Text;
            string imie = imie_textBox.Text;
            string nazwisko = nazwisko_textBox.Text;
            KeyValuePair<int, string> dzial = ((KeyValuePair<int, string>)lista_dzialow_comboBox.SelectedValue);

            if (login != null && passwordBox.Password != "" && imie != null && dzial.Value != null && nazwisko !="") 
            {
                if (passwordBox.Password.Length < 8)
                {
                    MessageBox.Show("Hasło musi mieć minimum 8 znaków.");
                }
                else
                {
                    if (passwordBox.Password == passwordBox2.Password)
                    {
                        if (baza_danych.zarejestruj(login, passwordBox.Password, imie, nazwisko, dzial.Key))
                        {
                            var okno_logowania = new Logowanie();
                            NavigationService nav = NavigationService.GetNavigationService(this);
                            nav.Navigate(okno_logowania);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Podane hasła się różnią.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Uzupełnij wymagane pola.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void wstecz_button_Click(object sender, RoutedEventArgs e)
        {
            var okno_logowania = new Logowanie();
            NavigationService nav = NavigationService.GetNavigationService(this);
            nav.Navigate(okno_logowania);
        }
    }
}
