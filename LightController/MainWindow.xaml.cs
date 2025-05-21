using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ABI.Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LightController
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            update_PortsList();
            PortComboBox.DataContext = this;
            PortComboBox.SelectedValue = Ports[0];
            BaudRateComboBox.DataContext = this;
            BaudRateComboBox.SelectedValue = BaudRates[0];
            Status.DataContext = serialHelper;
            BrightnessSlider.DataContext = this;
            ColorPicker.DataContext = this;
        }

        readonly SerialHelper serialHelper = new();
        private static CancellationTokenSource _tokenSource = new();
        private static DateTime _last_call_time;
        private static readonly TimeSpan _min_call_interval = TimeSpan.FromSeconds(2);
        private string[] _ports;
        public string[] Ports { get { return _ports; } }
        public int[] BaudRates { get; } = [ 9600, 19200, 38400, 57600, 115200 ];
        private Windows.UI.Color _currentColor = Windows.UI.Color.FromArgb(255,255,255,180);
        public Windows.UI.Color CurrentColor {
            get => _currentColor;
            set
            {
                _currentColor = value;
                
                if (serialHelper.IsPortOpen)
                {
                    var str = "color " + value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
                    var bytes = Encoding.UTF8.GetBytes(str);
                    var func = async () => await serialHelper.SendAsync(bytes, bytes.Length);
                    if (DateTime.Now - _last_call_time < _min_call_interval)
                    {
                        _tokenSource.Cancel();
                        var token = _tokenSource.Token;
                        //发送最后一次调用的命令,若后续有调用则终止
                        var task = Task.Run(()=> serialHelper.Send(bytes, bytes.Length), token);
                        return;
                    }
                    else
                    {
                        func();
                        _last_call_time = DateTime.Now;
                    }
                }
            }
        }
        private int _currentBrightness = 200;
        public int CurrentBrightness {
            get => _currentBrightness;
            set 
            {
                _currentBrightness = value;
                if (serialHelper.IsPortOpen)
                {
                    var str = "light " + value.ToString();
                    var bytes = Encoding.UTF8.GetBytes(str);
                    var func = async () => await serialHelper.SendAsync(bytes, bytes.Length);
                    if (DateTime.Now - _last_call_time < _min_call_interval)
                    {
                        _tokenSource.Cancel();
                        var token = _tokenSource.Token;
                        //发送最后一次调用的命令,若后续有调用则终止
                        var task = Task.Run(() => serialHelper.Send(bytes, bytes.Length), token);
                        return;
                    }
                    else
                    {
                        func();
                        _last_call_time = DateTime.Now;
                    }
                }
            }
        }
        private void OpenPort(string? port, Int32? baudRate)
        {
            if ((port != null && port != "") && baudRate.HasValue)
            {
                if (serialHelper.IsPortOpen)
                {
                    serialHelper.ClosePort();
                }
                serialHelper.InitPort(port, baudRate.Value);
                serialHelper.ErrorLog += printLog;
                serialHelper.WarnLog += printLog2;
                serialHelper.InfoLog += printLog2;
                serialHelper.OpenPort();
                Status.Text = serialHelper.IsPortOpen ? "OK" : "Error";
            }
        }
        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as TextBlock;
            CurrentLightMode.Text = item.Text;
            if (serialHelper.IsPortOpen)
            {
                var str = item.Text;
                var bytes = Encoding.UTF8.GetBytes(str);
                var func = async () => await serialHelper.SendAsync(bytes, bytes.Length);
                func();
            }
            Task.Delay(10).ContinueWith(_ => LightModeButton.Flyout.Hide(), TaskScheduler.FromCurrentSynchronizationContext());
        }
        private void update_PortsList()
        {
            _ports = SerialHelper.GetAllPortNames();
        }
        private void PortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var port = comboBox?.SelectedItem as string;
            int? baudRate = int.Parse(BaudRateComboBox.Text!= ""? BaudRateComboBox.Text : "9600");
            OpenPort(port, baudRate);
        }

        private void BaudRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? port = PortComboBox.Text;
            int? baudRate = int.Parse(BaudRateComboBox.Text != "" ? BaudRateComboBox.Text : "9600");
            OpenPort(port, baudRate);
        }
        private static void printLog(string message, Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine(message);
        }
        private static void printLog2(string message)
        {
            Debug.WriteLine(message);
        }

    }
    //public class LightControllerCore
    //{
    //    public LightControllerCore()
    //    {
    //        _ports = SerialHelper.GetAllPortNames();
    //    }
    //    readonly SerialHelper serialHelper = new();
    //    private string[] _ports;
    //    public string[] Ports { get { return _ports; } }
    //    public int[] BaudRates { get; } = [9600, 19200, 38400, 57600, 115200];
    //    private int _currentBrightness = 200;
    //    public int CurrentBrightness
    //    {
    //        get => _currentBrightness;
    //        set
    //        {
    //            _currentBrightness = value;
    //            if (serialHelper.IsPortOpen)
    //            {
    //                var str = "light " + value.ToString();
    //                var bytes = Encoding.UTF8.GetBytes(str);
    //                var func = async () => await serialHelper.SendAsync(bytes, bytes.Length);
    //                func();
    //            }
    //        }
    //    }
    //    private void update_PortsList()
    //    {
    //        _ports = SerialHelper.GetAllPortNames();
    //    }

    //}
    public class Bool2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool va = (bool)value;
            return va ? "OK" : "Error";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
