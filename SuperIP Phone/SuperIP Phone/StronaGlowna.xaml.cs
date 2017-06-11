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
        Kontakt wybranyUzytkownik;
        Kontakt rozmawiajacyUzytkownik;//jest nullem gdy rozmowa nie jest prowadzona lub jest prowadzona z użytkownikiem, którego nie ma na liście kontaktów
        internal List<Kontakt> listaKontaktow;
        ISoftPhone softphone;   // softphone object
        IPhoneLine phoneLine;   // phoneline object
        public IPhoneCall call;
        string caller;
        Microphone microphone;
        Speaker speaker;
        PhoneCallAudioSender mediaSender;
        PhoneCallAudioReceiver mediaReceiver;
        MediaConnector connector;
        Thread watekDoRozmow;
        System.Windows.Threading.DispatcherTimer stoper;
        int uplynelo_sekund;

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
                if (listaKontaktow != null)
                {
                    var stara_lista = listaKontaktow;
                    listaKontaktow = baza_danych.pobierz_liste_kontaktow();
                    foreach (var k in stara_lista)
                    {
                        Kontakt nowy_k = listaKontaktow.Find(x => x.login == k.login);
                        if (nowy_k != null)
                        {
                            nowy_k.wiadomosci = k.wiadomosci;
                        }
                    }
                }
                else
                {
                    listaKontaktow = baza_danych.pobierz_liste_kontaktow();
                }

                ListaKontaktow_ItemsControl.Visibility = Visibility.Visible;
                ListaKontaktow_ItemsControl.Items.Clear();

                if (listaKontaktow != null)
                {
                    foreach (var kontakt in listaKontaktow)
                    {
                        StackPanel zawartosc_listbox = new StackPanel()
                        {
                            Background = kontakt.AdresIP != "" ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Red),//new BrushConverter().ConvertFromString("#FFFB6A33") as SolidColorBrush
                            Margin = new Thickness(0, 0, 0, 5),
                            Width = ListaKontaktow_ItemsControl.Width - 18,
                            Orientation = Orientation.Horizontal,
                            MinHeight = 70
                        };

                        Button Usun_button = new Button()
                        {
                            Margin = new Thickness(20, 0, 0, 0),
                            Content = "🗑",//🗑
                            FontSize = 35,
                            Width = 55,
                            Height = 55,
                            Name = kontakt.login
                        };
                        Usun_button.Click += Usun_button_Clicked;//podłączenie funkcji usuwającej kontakt

                        TextBlock dane_kontaktu = new TextBlock()
                        {
                            Padding = new Thickness(10, 5, 0, 5),
                            Text = kontakt.ToString(),
                            Width = zawartosc_listbox.Width * 0.75,
                            TextWrapping = TextWrapping.WrapWithOverflow,
                            Name = kontakt.login,
                            Foreground = new SolidColorBrush(Colors.Black),
                            FontSize = 15
                        };
                        dane_kontaktu.PreviewMouseDown += ListaKontaktow_ItemsControl_SelectionChanged;

                        zawartosc_listbox.Children.Add(dane_kontaktu);
                        zawartosc_listbox.Children.Add(Usun_button);

                        ListaKontaktow_ItemsControl.Items.Add(zawartosc_listbox);
                    }
                }
                else
                {
                    listaKontaktow = new List<Kontakt>();
                    TextBlock dane_kontaktu = new TextBlock()
                    {
                        Padding = new Thickness(10, 5, 0, 5),
                        Text = "Twoja lista kontaktów jest pusta, dodaj użytkowników używając przycisku \"Dodaj Kontakt\" znajdującego się powyżej.",
                        Width = ListaKontaktow_ItemsControl.Width * 0.9,
                        TextWrapping = TextWrapping.WrapWithOverflow,
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 25
                    };
                    ListaKontaktow_ItemsControl.Items.Add(dane_kontaktu);
                }
            });
        }

        private void ListaKontaktow_ItemsControl_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            wybranyUzytkownik = listaKontaktow.Find(X => X.login == (sender as TextBlock).Name);
            WybranyUzytkownik_Label.Content = "Wybrany użytkownik: " + wybranyUzytkownik.login;
            Wiadomosci_ItemsControl.Items.Clear();
            foreach (var w in wybranyUzytkownik.wiadomosci)
            {
                Wiadomosci_ItemsControl.Items.Add(w);
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                WyslijWiadomosc_Button.Visibility = TrescWiadomosci_TextBox.Visibility = Visibility.Visible;
                if (wybranyUzytkownik.AdresIP != "")
                {
                    WyslijWiadomosc_Button.IsEnabled = TrescWiadomosci_TextBox.IsEnabled = true;
                }
                else
                {
                    WyslijWiadomosc_Button.IsEnabled = TrescWiadomosci_TextBox.IsEnabled = false;
                }
            });
        }

        private void Usun_button_Clicked(object sender, RoutedEventArgs e)
        {
            Kontakt wybranyKontakt = listaKontaktow.Find(X => X.login == (sender as Button).Name);
            baza_danych.usun_uzytkownika_z_kontaktow(zalogowanyUzytkownik.login, wybranyKontakt.login);
            OdswiezListeKontaktow();
        }

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


    #region funkcje dotyczące rozmowy
        private void Nasluchuj()
        {
            softphone = SoftPhoneFactory.CreateSoftPhone(zalogowanyUzytkownik.AdresIP, 4900, 5100);
            mediaSender = new PhoneCallAudioSender();
            mediaReceiver = new PhoneCallAudioReceiver();
            connector = new MediaConnector();
            var config = new DirectIPPhoneLineConfig(zalogowanyUzytkownik.AdresIP.ToString(), 5060);
            phoneLine = softphone.CreateDirectIPPhoneLine(config);
            phoneLine.Config.SRTPMode = Ozeki.Common.SRTPMode.Prefer;
            phoneLine.RegistrationStateChanged += line_RegStateChanged;
            phoneLine.SIPAccount.UserName = zalogowanyUzytkownik.login;
            phoneLine.SIPAccount.DisplayName = zalogowanyUzytkownik.imie + " " + zalogowanyUzytkownik.nazwisko;
            System.Windows.Application.Current.Dispatcher.Invoke(() => { System.Windows.Application.Current.MainWindow.Title = "SuperIP Phone - " + zalogowanyUzytkownik.login + "@" + zalogowanyUzytkownik.AdresIP + ":" + phoneLine.SIPAccount.DomainServerPort; });//ustawienie nazwy okna
            softphone.IncomingCall += softphone_IncomingCall;
            phoneLine.InstantMessaging.MessageReceived += PhoneLine_InstantMessageReceived;
            softphone.RegisterPhoneLine(phoneLine);
            foreach (var kodek in softphone.Codecs)
            {
                softphone.EnableCodec(kodek.PayloadType);
            }
        }

        private void line_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.NotRegistered || e.State == RegState.Error)
            {
                Status_TextBlock.Dispatcher.Invoke(() => { Status_TextBlock.Text = "Status: Offline - wystąpił nieoczekiwany błąd, nie można prowadzić rozmów"; });
            }
            if (e.State == RegState.RegistrationSucceeded)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Status_TextBlock.Text = "Stauts: Online, teraz można rozpocząć rozmowę";
                    Zadzwon_Button.IsEnabled = true;
                    WyslijWiadomosc_Button.IsEnabled = true;
                });
            }
        }

        private void Zadzwonbutton_Click(object sender, RoutedEventArgs e)
        {
            if (wybranyUzytkownik != null)
            {
                rozmawiajacyUzytkownik = wybranyUzytkownik;
                IPAddress temp = IPAddress.None;
                string ipToDial = rozmawiajacyUzytkownik.AdresIP;
                if (IPAddress.TryParse(ipToDial, out temp))
                {
                    StartCall(ipToDial);
                    ZakonczRozmowe_Button.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show("Użytkownik jest offline, rozmowa nie może być rozpoczęta.");
                }
            }
            else
            {
                MessageBox.Show("Aby rozpocząć rozmowę, najpierw wybierz użytkownika z listy kontaktów, a następnie naciśnij przycisk zadzwoń.");
            }
        }

        private void ZakonczRozmowebutton_Click(object sender, RoutedEventArgs e)
        {
            if (call != null)
            {
                CloseDevices();
                call.HangUp();
                call = null;
            }
        }

        private void StartCall(string numberToDial)
        {
            if (call == null || call.CallState == CallState.Completed)
            {
                call = softphone.CreateDirectIPCallObject(phoneLine, new DirectIPDialParameters("5060"), numberToDial);
                call.CallStateChanged += call_CallStateChanged;

                call.Start();
            }
        }

        private void softphone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            if (call == null)
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    call = e.Item;
                    caller = call.DialInfo.CallerDisplay;
                    call.CallStateChanged += call_CallStateChanged;
                    CzyOdebrac czyookno = new CzyOdebrac(caller);
                    if (czyookno.ShowDialog().Value)
                    {
                        if (call != null)
                        {
                            call.Answer();
                        }
                    }
                    else
                    {
                        if (call != null)
                        {
                            call.Reject();
                        }
                    }
                });
        }

        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            if (e.State == CallState.Answered)//rozmowa odebrana
            {
                SetupDevices();
            }
            else if (e.Reason == "INVITE timed out")//użytkownik nie odebrał
            {
                stoper.Stop();//stoper odpowiadający za "dzwonię...."
                Status_TextBlock.Dispatcher.Invoke(() =>
                {
                    ZakonczRozmowe_Button.IsEnabled = false;
                    Status_TextBlock.Text = "Status: Użytkownik nie odpowiada.";
                });
            }
            else if (e.State.IsCallEnded())//rozmowa zakończona
            {
                CloseDevices();
                call = null;
                Status_TextBlock.Dispatcher.Invoke(() => { Status_TextBlock.Text = "Status: Online, teraz można rozpocząć rozmowę"; });
            }
            else if (e.State.IsInCall())
            {
                Status_TextBlock.Dispatcher.Invoke(() => { Status_TextBlock.Text = "Status: w trakcie rozmowy z użytkownikiem " + call.OtherParty.UserName; });
            }
            else if (e.Reason == "Dialing")//trwa nawiązywanie połączenia
            {
                uplynelo_sekund = 0;
                stoper = new System.Windows.Threading.DispatcherTimer();
                stoper.Interval = TimeSpan.FromMilliseconds(500);
                stoper.Tick += ((object sender2, EventArgs e2) => {
                    uplynelo_sekund++;
                    Status_TextBlock.Dispatcher.Invoke(() => {
                        Status_TextBlock.Text = "Status: Dzownię";
                        for (int i = 0; i < uplynelo_sekund % 6; i++)
                        {
                            Status_TextBlock.Text += ".";
                        }
                    });
                });
                stoper.Start();
            }
            else
            {
                Status_TextBlock.Dispatcher.Invoke(() => { Status_TextBlock.Text = "Status: " + e.State + ", " + e.Reason; });
            }
        }

        private void Stoper_Tick(object sender, EventArgs e)
        {
            uplynelo_sekund++;
            CzasRozmowy_Label.Content = "Czas rozmowy: " + TimeSpan.FromSeconds(uplynelo_sekund).ToString(@"hh\:mm\:ss");
        }

        private void SetupDevices()
        {
            microphone.Start();
            connector.Connect(microphone, mediaSender);

            speaker.Start();
            connector.Connect(mediaReceiver, speaker);

            mediaSender.AttachToCall(call);
            mediaReceiver.AttachToCall(call);

            Application.Current.Dispatcher.Invoke(() =>
            {
                uplynelo_sekund = 0;
                CzasRozmowy_Label.Content = "Czas rozmowy: " + TimeSpan.FromSeconds(uplynelo_sekund).ToString(@"hh\:mm\:ss");
                ZakonczRozmowe_Button.IsEnabled = true;
                if (rozmawiajacyUzytkownik != null)//my dzwonimy
                {
                    //rozmawiajacyUzytkownik.wiadomosci.Add(DateTime.Now.ToShortTimeString() + " rozpoczęto rozmowę");
                    call.OtherParty.UserName = rozmawiajacyUzytkownik.login;
                }
                else//ktos dzwoni
                {
                    var kontakt = listaKontaktow.Find(X => X.login == call.OtherParty.UserName);
                    rozmawiajacyUzytkownik = kontakt;
                }
                CzasRozmowy_Label.Visibility = Visibility.Visible;
                if (stoper != null)
                {
                    stoper.Stop();
                }
                stoper = new System.Windows.Threading.DispatcherTimer();
                stoper.Interval = TimeSpan.FromSeconds(1);
                stoper.Tick += Stoper_Tick;
                stoper.Start();
            });
        }

        private void CloseDevices()
        {
            microphone.Stop();
            connector.Disconnect(microphone, mediaSender);

            speaker.Stop();
            connector.Disconnect(mediaReceiver, speaker);

            mediaSender.Detach();
            mediaReceiver.Detach();

            CzasRozmowy_Label.Dispatcher.Invoke(() =>
            {
                if (call != null)
                {
                    if (rozmawiajacyUzytkownik != null)
                    {
                        //rozmawiajacyUzytkownik.wiadomosci.Add(DateTime.Now.ToShortTimeString() + " zakończono rozmowę");
                    }
                }
                CzasRozmowy_Label.Visibility = Visibility.Hidden;
                ZakonczRozmowe_Button.IsEnabled = false;
            });
            if (stoper != null)
            {
                stoper.Stop();
            }
            rozmawiajacyUzytkownik = null;
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

    #region funkcje dotyczące wiadomości
        private void WyslijWiadomosc_Button_Click(object sender, RoutedEventArgs e)//wysyłanie wiadomości
        {
            if (TrescWiadomosci_TextBox.Text == "")
            {
                MessageBox.Show("Treść wiadomości, nie może być pusta.");
            }
            else if (wybranyUzytkownik != null)
            {
                if (wybranyUzytkownik.AdresIP == "")
                {
                    MessageBox.Show("Wiadomości można wysyłać tylko do osbób, które są aktualnie online.");
                }
                else
                {
                    InstantMessage IM = new InstantMessage("", zalogowanyUzytkownik.login, wybranyUzytkownik.login, TrescWiadomosci_TextBox.Text, "");
                    phoneLine.InstantMessaging.SendDirectIPMessage(IM, wybranyUzytkownik.AdresIP);
                    wybranyUzytkownik.wiadomosci.Add("Ty napisałeś: " + TrescWiadomosci_TextBox.Text);
                    Application.Current.Dispatcher.Invoke(() => {
                        Wiadomosci_ItemsControl.Items.Insert(0, "Ty napisałeś: " + TrescWiadomosci_TextBox.Text);
                        TrescWiadomosci_TextBox.Text = "";
                    });
                }
            }
            else
            {
                MessageBox.Show("Najpierw wybierz użytkownika, a następnie wysyłaj wiadomości");
            }
        }

        private void PhoneLine_InstantMessageReceived(object sender, InstantMessage e)//odbieranie wiadomości
        {
            string login_nadawcy = e.Sender.Substring(0, e.Sender.IndexOf("@"));
            Kontakt nadawca = listaKontaktow.Find(x => x.login == login_nadawcy);
            if (nadawca != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    nadawca.wiadomosci.Add(login_nadawcy + " napisał: " + e.Content);
                    Wiadomosci_ItemsControl.Items.Add(login_nadawcy + " napisał: " + e.Content);
                });
            }
        }
    #endregion
    }
}
