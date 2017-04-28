using Ozeki.Media;
using Ozeki.VoIP;
using System;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for StronaGlowna.xaml
    /// </summary>
    public partial class StronaGlowna : Page
    {
        IPAddress wybranyAdresIP;
        static ISoftPhone softphone;   // softphone object
        static IPhoneLine phoneLine;   // phoneline object
        static IPhoneCall call;
        static string caller;
        static Microphone microphone;
        static Speaker speaker;
        static PhoneCallAudioSender mediaSender;
        static PhoneCallAudioReceiver mediaReceiver;
        static MediaConnector connector;

        public StronaGlowna()
        {
            InitializeComponent();
            ZablokowacUI(true);
            wybranyAdresIP = IPAddress.Parse(App.Current.Properties["AdresIP"].ToString());
            Application.Current.MainWindow.Title = "SuperIP Phone - " + App.Current.Properties["Login"].ToString();
        }

        private void ZablokowacUI(bool CzyZablokowac)
        {
            Nasluchujbutton.IsEnabled = CzyZablokowac;
            Zadzwonbutton.IsEnabled = !CzyZablokowac;
            DoZadzwonieniatextBox.IsEnabled = !CzyZablokowac;
        }

        private void Nasluchujbutton_Click(object sender, RoutedEventArgs e)
        {
            Statuslabel.Content = "Rozpoczęto nasłuchiwanie na adresie: " + wybranyAdresIP;
            softphone = SoftPhoneFactory.CreateSoftPhone(wybranyAdresIP, 4900, 5100);
            mediaSender = new PhoneCallAudioSender();
            mediaReceiver = new PhoneCallAudioReceiver();
            connector = new MediaConnector();
            var config = new DirectIPPhoneLineConfig(wybranyAdresIP.ToString(), 5060);
            phoneLine = softphone.CreateDirectIPPhoneLine(config);
            phoneLine.Config.SRTPMode = Ozeki.Common.SRTPMode.Force;
            phoneLine.RegistrationStateChanged += line_RegStateChanged;
            softphone.IncomingCall += softphone_IncomingCall;
            softphone.RegisterPhoneLine(phoneLine);
            ZablokowacUI(false);
        }

        private static void line_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.NotRegistered || e.State == RegState.Error)
                //((MainWindow)Application.Current.MainWindow).label.Content += "\nRegistration failed!";

                if (e.State == RegState.RegistrationSucceeded)
                {
                    //((MainWindow)Application.Current.MainWindow).label.Content += "\nRegistration succeeded - Online!";
                }
        }

        private static void StartCall(string numberToDial)
        {
            if (call == null)
            {
                call = softphone.CreateDirectIPCallObject(phoneLine, new DirectIPDialParameters("5060"), numberToDial);
                call.CallStateChanged += call_CallStateChanged;

                call.Start();
            }
        }

        static void softphone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            call = e.Item;
            caller = call.DialInfo.CallerID;
            call.CallStateChanged += call_CallStateChanged;
            call.Answer();
        }

        static void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            //((MainWindow)Application.Current.MainWindow).label.Content += "\nCall state: " + e.State;

            if (e.State == CallState.Answered)
                SetupDevices();

            if (e.State.IsCallEnded())
                CloseDevices();
        }

        static void SetupDevices()
        {
            microphone.Start();
            connector.Connect(microphone, mediaSender);

            speaker.Start();
            connector.Connect(mediaReceiver, speaker);

            mediaSender.AttachToCall(call);
            mediaReceiver.AttachToCall(call);
        }

        static void CloseDevices()
        {
            microphone.Dispose();
            speaker.Dispose();

            mediaReceiver.Detach();
            mediaSender.Detach();
            connector.Dispose();
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

        private void ZakonczNasluchbutton_Click(object sender, RoutedEventArgs e)
        {
            ZablokowacUI(true);
        }

        private void Wylogujbutton_Click(object sender, RoutedEventArgs e)
        {
            baza_danych.usun_adres_IP();
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
    }
}
