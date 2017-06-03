using Ozeki.Media;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SuperIP_Phone
{
    /// <summary>
    /// Interaction logic for Ustawienia.xaml
    /// </summary>
    public partial class Ustawienia : Window
    {
        Ozeki.Media.WaveStreamPlayback wavePlayer;
        bool CzyUruchomionyTest;
        Task TaskDoTestow;
        Microphone microphone;
        Speaker speaker;

        public Ustawienia()
        {
            InitializeComponent();
            wavePlayer = new WaveStreamPlayback("C:/Windows/Media/Alarm05.wav");
            CzyUruchomionyTest = false;
            WyborMikrofonu();
            WyborGlosnikow();
        }

        private void WyborGlosnikow()
        {
            int i = 0;
            speaker = (Speaker)System.Windows.Application.Current.Properties["WyjscieAudio"];
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
            microphone = (Microphone)System.Windows.Application.Current.Properties["WejscieAudio"];
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

        private void Glosnikslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            speaker.Volume = (float)Glosnikslider.Value / 100;
        }

        private void Mikrofonslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            microphone.Volume = (float)Mikrofonslider.Value / 100;
        }

        private void TestGlosnikabutton_Click(object sender, RoutedEventArgs e)
        {
            if (!CzyUruchomionyTest)
            {
                CzyUruchomionyTest = true;
                TaskDoTestow = new Task(() =>
                {
                    MediaConnector mediaConnector = new MediaConnector();

                    mediaConnector.Connect(wavePlayer, speaker);
                    wavePlayer.Start();
                    speaker.Start();
                    TestGlosnikabutton.Dispatcher.Invoke(() => {
                        TestGlosnikabutton.Content = "Zatrzymaj test";
                        TestMikrofonubutton.IsEnabled = false;
                    });

                    while (wavePlayer.IsStreaming && CzyUruchomionyTest == true) ;

                    mediaConnector.Disconnect(wavePlayer, speaker);
                    wavePlayer.Stop();
                    speaker.Stop();
                    TestGlosnikabutton.Dispatcher.Invoke(() => {
                        TestGlosnikabutton.Content = "Testuj wejście audio";
                        TestMikrofonubutton.IsEnabled = true;
                    });
                    CzyUruchomionyTest = false;
                });
                TaskDoTestow.Start();
            }
            else
            {
                CzyUruchomionyTest = false;
                TestGlosnikabutton.Dispatcher.Invoke(() => {
                    TestGlosnikabutton.Content = "Testuj wejście audio";
                    TestMikrofonubutton.IsEnabled = true;
                });
            }
        }

        private void TestMikrofonubutton_Click(object sender, RoutedEventArgs e)
        {
            if (!CzyUruchomionyTest)
            {
                TaskDoTestow = new Task(()=>
                {
                    MediaConnector mediaConnector = new MediaConnector();
                    mediaConnector.Connect(microphone, speaker);
                    microphone.Start();
                    speaker.Start();
                    while (CzyUruchomionyTest) ;
                    mediaConnector.Disconnect(microphone, speaker);
                    microphone.Stop();
                    speaker.Stop();
                });
                TaskDoTestow.Start();
                CzyUruchomionyTest = true;
                TestMikrofonubutton.Content = "Zatrzymaj test";
                TestGlosnikabutton.IsEnabled = false;
            }
            else
            {
                CzyUruchomionyTest = false;
                TestMikrofonubutton.Content = "Testuj wejście audio";
                TestGlosnikabutton.IsEnabled = true;
            }
        }

        private void AudioINcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDeviceInfo wybrane = AudioINcomboBox.SelectedItem as AudioDeviceInfo;
            microphone = Microphone.GetDevice(wybrane);
            Mikrofonslider.Value = microphone.Volume * 100;
        }

        private void AudioOUTcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioDeviceInfo wybrane = AudioOUTcomboBox.SelectedItem as AudioDeviceInfo;
            speaker = Speaker.GetDevice(wybrane);
            Glosnikslider.Value = speaker.Volume * 100;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CzyUruchomionyTest = false;
            System.Windows.Application.Current.Properties["WejscieAudio"] = microphone;
            System.Windows.Application.Current.Properties["WyjscieAudio"] = speaker;
        }
    }
}
