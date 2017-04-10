using Ozeki.Media;
using Ozeki.VoIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
            WyborAdresuIP();
            WyborMikrofonu();
            WyborGlosnikow();
            ZablokowacUI(true);
            baza_danych.polacz_z_baza();
        }

        private void ZablokowacUI(bool CzyZablokowac)
        {
            AudioINcomboBox.IsEnabled = CzyZablokowac;
            AudioOUTcomboBox.IsEnabled = CzyZablokowac;
            AdresIPcomboBox.IsEnabled = CzyZablokowac;
            Nasluchujbutton.IsEnabled = CzyZablokowac;
            Zadzwonbutton.IsEnabled = !CzyZablokowac;
            DoZadzwonieniatextBox.IsEnabled = !CzyZablokowac;
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

        private void WyborGlosnikow()
        {
            int i = 0;
            speaker = Speaker.GetDefaultDevice();
            foreach (var device in Speaker.GetDevices())
            {
                AudioOUTcomboBox.Items.Add(device);
                if (speaker.DeviceInfo.ToString() == device.ProductName)
                {
                    AudioOUTcomboBox.SelectedIndex = i;
                }
                i++;
            }
        }

        private void WyborMikrofonu()
        {
            int i = 0;
            microphone = Microphone.GetDefaultDevice();
            foreach (var device in Microphone.GetDevices())
            {
                AudioINcomboBox.Items.Add(device);
                if (microphone.DeviceInfo.ToString() == device.ProductName)
                {
                    AudioINcomboBox.SelectedIndex = i;
                }
                i++;
            }
        }

        private void AdresIPcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string wybraneIP = AdresIPcomboBox.SelectedItem as string;
            wybranyAdresIP = IPAddress.Parse(wybraneIP);
        }

        private void AudioINcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDeviceInfo wybrane = AudioINcomboBox.SelectedItem as AudioDeviceInfo;
            microphone = Microphone.GetDevice(wybrane);
        }

        private void AudioOUTcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDeviceInfo wybrane = AudioOUTcomboBox.SelectedItem as AudioDeviceInfo;
            speaker = Speaker.GetDevice(wybrane);
        }

        private void Nasluchujbutton_Click(object sender, RoutedEventArgs e)
        {
            label.Content = "Rozpoczęto nasłuchiwanie na adresie: " + wybranyAdresIP;
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
    }
}
