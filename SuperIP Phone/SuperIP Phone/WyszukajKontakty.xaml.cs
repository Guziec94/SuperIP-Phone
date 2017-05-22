using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for WyszukajKontakty.xaml
    /// </summary>
    public partial class WyszukajKontakty : Window
    {
        Dictionary<int, string> lista_dzialow;
        
        public WyszukajKontakty()
        {
            InitializeComponent();

            lista_dzialow = baza_danych.pobierz_liste_dzialow();
            ListaDzialow_ComboBox.ItemsSource = lista_dzialow;
            ListaDzialow_ComboBox.DisplayMemberPath = "Value";
            ListaDzialow_ComboBox.SelectedIndex = 0;
        }


        #region funkcje ukrywające/odkrywające pola tekstowe
        private void Login_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Login_TextBox.IsEnabled = Login_CheckBox.IsChecked == true ? true : false;
            Login_TextBox.Visibility = Login_CheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void Imie_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Imie_TextBox.IsEnabled = Imie_CheckBox.IsChecked == true ? true : false;
            Imie_TextBox.Visibility = Imie_CheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void Nazwisko_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Nazwisko_TextBox.IsEnabled = Nazwisko_CheckBox.IsChecked == true ? true : false;
            Nazwisko_TextBox.Visibility = Nazwisko_CheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void Dzial_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ListaDzialow_ComboBox.IsEnabled = Dzial_CheckBox.IsChecked == true ? true : false;
            ListaDzialow_ComboBox.Visibility = Dzial_CheckBox.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }
    #endregion

        private void WyszukajKontakty_button_Click(object sender, RoutedEventArgs e)
        {
            WyszukajKontakty_button.IsEnabled = false;
            string _login = Login_CheckBox.IsChecked == true ? Login_TextBox.Text : "%";
            string _imie = Imie_CheckBox.IsChecked == true ? Imie_TextBox.Text : "%";
            string _nazwisko = Nazwisko_CheckBox.IsChecked == true ? Nazwisko_TextBox.Text : "%";
            string _ID_Dzialu = Dzial_CheckBox.IsChecked == true ? ((KeyValuePair<int, string>)ListaDzialow_ComboBox.SelectedItem).Key.ToString() : "%";
            if (_login != "%" || _imie != "%" || _nazwisko != "%" || _ID_Dzialu != "%")
            {
                List<Kontakt> znalezione_kontakty = baza_danych.wyszukaj_kontakty(_login, _imie, _nazwisko, _ID_Dzialu);
                Height = 700;
                ZnalezioneKontakty_ListBox.Visibility = Visibility.Visible;
                ZnalezioneKontakty_ListBox.Items.Clear();
                Rect workArea = SystemParameters.WorkArea;
                Top = (workArea.Height - Height) / 2 + workArea.Top;

                if (znalezione_kontakty != null)
                {
                    foreach (var kontakt in znalezione_kontakty)
                    {
                        StackPanel zawartosc_listbox = new StackPanel();
                        zawartosc_listbox.MaxHeight = 400;
                        zawartosc_listbox.Margin = new Thickness(17, 5, 0, 5);
                        zawartosc_listbox.Width = ZnalezioneKontakty_ListBox.Width;
                        zawartosc_listbox.Orientation = Orientation.Horizontal;

                        Button dodaj_button = new Button();
                        dodaj_button.Click += Dodaj_Button_Clicked;
                        dodaj_button.Content = "Dodaj";
                        dodaj_button.Width = dodaj_button.Height = 60;

                        TextBlock dane_kontaktu = new TextBlock();
                        dane_kontaktu.Text = kontakt.ToString();
                        dane_kontaktu.Width = zawartosc_listbox.Width * 0.75;
                        dane_kontaktu.TextWrapping = TextWrapping.WrapWithOverflow;
                        
                        dodaj_button.Name = kontakt.login;
                        zawartosc_listbox.Name = kontakt.login;

                        zawartosc_listbox.Children.Add(dane_kontaktu);
                        zawartosc_listbox.Children.Add(dodaj_button);

                        ZnalezioneKontakty_ListBox.Items.Add(zawartosc_listbox);
                    }
                }
                else
                {
                    TextBlock pusto = new TextBlock();
                    pusto.Text = "Nie znaleziono żadnych kontaktów spełniających wybrane kryteria";
                    pusto.TextWrapping = TextWrapping.Wrap;
                    pusto.Padding = new Thickness(30,15,30,0);
                    pusto.TextAlignment = TextAlignment.Center;
                    ZnalezioneKontakty_ListBox.Items.Add(pusto);
                }
            }
            else
            {
                MessageBox.Show("Musisz wybrać przynajmniej jedno kryterium, aby wyszukać użytkowników.");
            }
            WyszukajKontakty_button.IsEnabled = true;
        }

        private void Dodaj_Button_Clicked(object sender, RoutedEventArgs e)
        {
            string login_do_dodania = (sender as Button).Name;
            
        }
    }
}
