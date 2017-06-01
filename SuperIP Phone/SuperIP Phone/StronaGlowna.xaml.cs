using Ozeki.Media;
using Ozeki.VoIP;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System;
using System.Windows.Input;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for StronaGlowna.xaml
    /// </summary>
    public partial class StronaGlowna : Page
    {
        Kontakt zalogowanyUzytkownik = (Kontakt)System.Windows.Application.Current.Properties["ZalogowanyUzytkownik"];
        ISoftPhone softphone;   // softphone object
        IPhoneLine phoneLine;   // phoneline object
        IPhoneCall call;
        string caller;
        Microphone microphone;
        Speaker speaker;
        PhoneCallAudioSender mediaSender;
        PhoneCallAudioReceiver mediaReceiver;
        MediaConnector connector;
        List<Kontakt> listaKontaktow;
        Thread watekDoRozmow;

        public StronaGlowna()
        {
            InitializeComponent();
            microphone = (Microphone)System.Windows.Application.Current.Properties["WejscieAudio"];
            speaker = (Speaker)System.Windows.Application.Current.Properties["WyjscieAudio"];
            OdswiezListeKontaktow();
            watekDoRozmow = new Thread(Nasluchuj);
            watekDoRozmow.Start();
            baza_danych.broker();
        }

        public void OdswiezListeKontaktow()
        {
            Application.Current.Dispatcher.Invoke(() => {
                /*ListaKontaktow_ListBox.Items.Clear();
                listaKontaktow = baza_danych.pobierz_liste_kontaktow();
                if (listaKontaktow != null)
                {
                    foreach (Kontakt k in listaKontaktow)
                    {
                        if (k.AdresIP != "")
                        {
                            ListaKontaktow_ListBox.Items.Add(new ListBoxItem { Content = k, IsEnabled = true });
                        }
                        else
                        {
                            ListaKontaktow_ListBox.Items.Add(new ListBoxItem { Content = k, IsEnabled = false });
                        }
                    }
                }*/
                listaKontaktow = baza_danych.pobierz_liste_kontaktow();
                ListaKontaktow_ItemsControl.Visibility = Visibility.Visible;
                ListaKontaktow_ItemsControl.Items.Clear();

                if (listaKontaktow != null)
                {
                    foreach (var kontakt in listaKontaktow)
                    {
                        StackPanel zawartosc_listbox = new StackPanel()
                        {
                            Background = kontakt.AdresIP != "" ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Red),//new BrushConverter().ConvertFromString("#FFFB6A33") as SolidColorBrush
                            MaxHeight = 400,
                            Margin = new Thickness(0, 0, 0, 5),
                            Width = ListaKontaktow_ItemsControl.Width - 18,
                            Orientation = Orientation.Horizontal,
                            Name = kontakt.login 
                        };

                        Button Usun_button = new Button()
                        {
                            Margin = new Thickness(20, 0, 0, 0),
                            Content = "Usuń",
                            Width = 70,
                            Height = 70,
                            Name = kontakt.login
                        };
                        Usun_button.Click += Usun_button_Clicked;//podłączenie funkcji usuwającej kontakt

                        TextBlock dane_kontaktu = new TextBlock()
                        {
                            Padding = new Thickness(10, 5, 0, 5),
                            Text = kontakt.ToString(),
                            Width = zawartosc_listbox.Width * 0.75,
                            TextWrapping = TextWrapping.WrapWithOverflow,
                            Name = kontakt.login
                        };
                        dane_kontaktu.PreviewMouseDown += ListaKontaktow_ItemsControl_SelectionChanged;

                        zawartosc_listbox.Children.Add(dane_kontaktu);
                        zawartosc_listbox.Children.Add(Usun_button);

                        ListaKontaktow_ItemsControl.Items.Add(zawartosc_listbox);
                    }
                }
            });
        }

        private void ListaKontaktow_ItemsControl_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            Kontakt wybranyKontakt = listaKontaktow.Find(X => X.login == (sender as TextBlock).Name);
            Status_Label.Content = wybranyKontakt.ToString();
            DoZadzwonienia_TextBox.Text = wybranyKontakt.AdresIP;
        }

        private void Usun_button_Clicked(object sender, RoutedEventArgs e)
        {
            Kontakt wybranyKontakt = listaKontaktow.Find(X => X.login == (sender as Button).Name);
            baza_danych.usun_uzytkownika_z_kontaktow(zalogowanyUzytkownik.login, wybranyKontakt.login);
            OdswiezListeKontaktow();
        }

        #region funkcje dotyczące rozmowy
        private void Nasluchujbutton_Click(object sender, RoutedEventArgs e)
        {
            watekDoRozmow = new Thread(Nasluchuj);
            watekDoRozmow.Start();
        }

        private void Nasluchuj()
        {
            softphone = SoftPhoneFactory.CreateSoftPhone(zalogowanyUzytkownik.AdresIP, 4900, 5100);
            mediaSender = new PhoneCallAudioSender();
            mediaReceiver = new PhoneCallAudioReceiver();
            connector = new MediaConnector();
            var config = new DirectIPPhoneLineConfig(zalogowanyUzytkownik.AdresIP.ToString(), 5060);
            phoneLine = softphone.CreateDirectIPPhoneLine(config);
            phoneLine.Config.SRTPMode = Ozeki.Common.SRTPMode.Force;
            phoneLine.RegistrationStateChanged += line_RegStateChanged;
            phoneLine.SIPAccount.UserName = zalogowanyUzytkownik.login;
            phoneLine.SIPAccount.DisplayName = zalogowanyUzytkownik.imie + " " + zalogowanyUzytkownik.nazwisko;
            System.Windows.Application.Current.Dispatcher.Invoke(() => { System.Windows.Application.Current.MainWindow.Title = "SuperIP Phone - " + zalogowanyUzytkownik.login + "@" + zalogowanyUzytkownik.AdresIP + ":" + phoneLine.SIPAccount.DomainServerPort; });//ustawienie nazwy okna
            softphone.IncomingCall += softphone_IncomingCall;
            softphone.RegisterPhoneLine(phoneLine);
        }

        private void line_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.NotRegistered || e.State == RegState.Error)
            {
                Status_Label.Dispatcher.Invoke(() => { Status_Label.Content += "\nRegistration failed!"; });
            }
            if (e.State == RegState.RegistrationSucceeded)
            {
                Status_Label.Dispatcher.Invoke(() => { Status_Label.Content += "\nRegistration succeeded - Online!"; });
            }
        }

        private void Zadzwonbutton_Click(object sender, RoutedEventArgs e)
        {
            IPAddress temp = IPAddress.None;
            string ipToDial = DoZadzwonienia_TextBox.Text;
            if (IPAddress.TryParse(ipToDial, out temp))
            {
                StartCall(ipToDial);
                ZakonczRozmowe_Button.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Nieprawidłowy adres IP, rozmowa nie może być rozpoczęta.");
            }
        }

        private void StartCall(string numberToDial)
        {
            if (call == null)
            {
                call = softphone.CreateDirectIPCallObject(phoneLine, new DirectIPDialParameters("5060"), numberToDial);
                call.CallStateChanged += call_CallStateChanged;

                call.Start();
            }
        }

        private void softphone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(()=> {
                call = e.Item;
                caller = call.DialInfo.CallerDisplay;
                call.CallStateChanged += call_CallStateChanged;
                CzyOdebrac czyookno = new CzyOdebrac(caller);
                if (czyookno.ShowDialog().Value)
                {
                    call.Answer();
                }
                else
                {
                    call.Reject();
                }
            });
        }

        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            Status_Label.Dispatcher.Invoke(() => { Status_Label.Content += "\nCall state: " + e.State + " reason: " + e.Reason; });

            if (e.State == CallState.Answered)
                SetupDevices();

            if (e.State.IsCallEnded())
                CloseDevices();
        }

        private void SetupDevices()
        {
            microphone.Start();
            connector.Connect(microphone, mediaSender);

            speaker.Start();
            connector.Connect(mediaReceiver, speaker);

            mediaSender.AttachToCall(call);
            mediaReceiver.AttachToCall(call);
        }

        private void CloseDevices()
        {
            microphone.Stop();
            connector.Disconnect(microphone, mediaSender);

            speaker.Stop();
            connector.Disconnect(mediaReceiver, speaker);

            mediaSender.Detach();
            mediaReceiver.Detach();
        }

        private void ZakonczNasluchbutton_Click(object sender, RoutedEventArgs e)
        {
            ZakonczNasluch();
        }

        private void ZakonczNasluch()
        {
            softphone.Close();
            softphone.UnregisterPhoneLine(phoneLine);
            phoneLine.Dispose();
            watekDoRozmow.Abort();
            watekDoRozmow = null;
        }
    #endregion

        private void Wylogujbutton_Click(object sender, RoutedEventArgs e)
        {
            baza_danych.ustaw_status(false);
            ZakonczNasluch();
            System.Windows.Application.Current.MainWindow.Title = "SuperIP Phone";
            baza_danych.broker_stop();
            var StronaLogowania = new Logowanie();
            NavigationService nav = NavigationService.GetNavigationService(this);
            nav.Navigate(StronaLogowania);
        }

        private void Ustawieniabutton_Click(object sender, RoutedEventArgs e)
        {
            Ustawienia OknoUstawien = new Ustawienia();
            System.Windows.Application.Current.MainWindow.Visibility = Visibility.Hidden;
            OknoUstawien.ShowDialog();
            System.Windows.Application.Current.MainWindow.Visibility = Visibility.Visible;
            microphone = (Microphone)System.Windows.Application.Current.Properties["WejscieAudio"];
            speaker = (Speaker)System.Windows.Application.Current.Properties["WyjscieAudio"];
        }

        private void ZakonczRozmowebutton_Click(object sender, RoutedEventArgs e)
        {
            if(call!=null)
            {
                call.HangUp();
                CloseDevices();
                call = null;
            }
        }

        private void DodajZnajomegobutton_Click(object sender, RoutedEventArgs e)
        {
            WyszukajKontakty OknoWyszukiwaniaKontaktow = new WyszukajKontakty();
            System.Windows.Application.Current.MainWindow.Visibility = Visibility.Hidden;
            if (OknoWyszukiwaniaKontaktow.ShowDialog().Value == true)
            {
                OdswiezListeKontaktow();
            }
            System.Windows.Application.Current.MainWindow.Visibility = Visibility.Visible;
        }

        private void UsunKonto_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Czy na pewno chcesz usunąć swoje konto? Ta operacja jest nieodwracalna", "Czy jesteś pewien?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                baza_danych.usun_konto();
                System.Windows.Application.Current.MainWindow.Title = "SuperIP Phone";
                baza_danych.broker_stop();
                var StronaLogowania = new Logowanie();
                NavigationService nav = NavigationService.GetNavigationService(this);
                nav.Navigate(StronaLogowania);
            }
        }
    }
}
