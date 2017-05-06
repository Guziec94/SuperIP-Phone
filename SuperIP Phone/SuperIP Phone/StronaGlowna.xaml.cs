using Ozeki.Media;
using Ozeki.VoIP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for StronaGlowna.xaml
    /// </summary>
    public partial class StronaGlowna : Page
    {
        Kontakt zalogowanyUzytkownik = (Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"];
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
            //ZablokowacUI(true);
            microphone = (Microphone)Application.Current.Properties["WejscieAudio"];
            speaker = (Speaker)Application.Current.Properties["WyjscieAudio"];
            listaKontaktow = baza_danych.pobierz_liste_kontaktow();
            if (listaKontaktow != null)
            {
                foreach (Kontakt k in listaKontaktow)
                {
                    if (k.AdresIP != "")
                    {
                        ListaKontaktowlistBox.Items.Add(new ListBoxItem { Content = k, IsEnabled = true });
                    }
                    else
                    {
                        ListaKontaktowlistBox.Items.Add(new ListBoxItem { Content = k, IsEnabled = false });
                    }
                }
            }
            watekDoRozmow = new Thread(Nasluchuj);
            watekDoRozmow.Start();
        }

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
            Application.Current.Dispatcher.Invoke(() => { Application.Current.MainWindow.Title = "SuperIP Phone - " + zalogowanyUzytkownik.login + "@" + zalogowanyUzytkownik.AdresIP + ":" + phoneLine.SIPAccount.DomainServerPort; });//ustawienie nazwy okna
            softphone.IncomingCall += softphone_IncomingCall;
            softphone.RegisterPhoneLine(phoneLine);
        }

        private void line_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.NotRegistered || e.State == RegState.Error)
            {
                Statuslabel.Dispatcher.Invoke(() => { Statuslabel.Content += "\nRegistration failed!"; });
            }
            if (e.State == RegState.RegistrationSucceeded)
            {
                Statuslabel.Dispatcher.Invoke(() => { Statuslabel.Content += "\nRegistration succeeded - Online!"; });
            }
        }

        private void Zadzwonbutton_Click(object sender, RoutedEventArgs e)
        {
            IPAddress temp = IPAddress.None;
            string ipToDial = DoZadzwonieniatextBox.Text;
            if (IPAddress.TryParse(ipToDial, out temp))
            {
                StartCall(ipToDial);
                ZakonczRozmowebutton.IsEnabled = true;
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
            Application.Current.Dispatcher.Invoke(()=> { 
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
            Statuslabel.Dispatcher.Invoke(() => { Statuslabel.Content += "\nCall state: " + e.State + " reason: " + e.Reason; });

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
            phoneLine.Dispose();
            watekDoRozmow.Abort();
            watekDoRozmow = null;
        }

        private void Wylogujbutton_Click(object sender, RoutedEventArgs e)
        {
            baza_danych.usun_adres_IP();
            ZakonczNasluch();
            Application.Current.MainWindow.Title = "SuperIP Phone";
            var StronaLogowania = new Logowanie();
            NavigationService nav = NavigationService.GetNavigationService(this);
            nav.Navigate(StronaLogowania);
        }

        private void Ustawieniabutton_Click(object sender, RoutedEventArgs e)
        {
            Ustawienia OknoUstawien = new Ustawienia();
            Application.Current.MainWindow.Visibility = Visibility.Hidden;
            OknoUstawien.ShowDialog();
            Application.Current.MainWindow.Visibility = Visibility.Visible;
            microphone = (Microphone)Application.Current.Properties["WejscieAudio"];
            speaker = (Speaker)Application.Current.Properties["WyjscieAudio"];
        }

        private void ListaKontaktowlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem wybranyItem= (ListBoxItem)ListaKontaktowlistBox.SelectedItem;
            Kontakt wybranyKontakt = (Kontakt)wybranyItem.Content;
            Statuslabel.Content = wybranyKontakt.ToString();
            DoZadzwonieniatextBox.Text = wybranyKontakt.AdresIP;
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

        }
    }
}
