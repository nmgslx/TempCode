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
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace WpfPhotoSize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            public int cb; // Size of the structure in bytes
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName; // Name of the device
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString; // Description of the device
            public int StateFlags; // Device state flags
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID; // Device ID
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey; // Registry key for the device
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(
            string lpDevice,
            uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        class DisplayInfo
        {
            public string name;
            public float dpiX;
            public float dpiY;
            public int widthCm;
            public int heightCm;
            public int width;
            public int height;
            public string deviceID;
            public float scaling { get => dpiX/96; }
        }

        DisplayInfo [] displayInfo = new DisplayInfo[Screen.AllScreens.Length];
        public MainWindow()
        {
            InitializeComponent();
            LoadDisplayInfo();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            textBlock1.Text = "";
            foreach (var display in displayInfo)
            {
                textBlock1.Text += $"{ display.name } { display.width }x{ display.height }  { display.widthCm }cm x{ display.heightCm }cm { display.widthCm/2.54:F1}x{ display.heightCm / 2.54:F1}\"\n";
            }
            var disp = displayInfo[0];
            photo4x6.Width = disp.width / disp.widthCm * 2.54 * 6 / disp.scaling;
            photo4x6.Height = disp.height / disp.heightCm * 2.54 * 4 / disp.scaling;

            photo5x7.Width = disp.width / disp.widthCm * 2.54 * 7 / disp.scaling;
            photo5x7.Height = disp.height / disp.heightCm * 2.54 * 5 / disp.scaling;

            photo8x12.Width = disp.width / disp.widthCm * 2.54 * 12 / disp.scaling;
            photo8x12.Height = disp.height / disp.heightCm * 2.54 * 8 / disp.scaling;

            photo12x18.Width = disp.width / disp.widthCm * 2.54 * 18 / disp.scaling;
            photo12x18.Height = disp.height / disp.heightCm * 2.54 * 12 / disp.scaling;

            photo8x10.Width = disp.width / disp.widthCm * 2.54 * 10 / disp.scaling;
            photo8x10.Height = disp.height / disp.heightCm * 2.54 * 8 / disp.scaling;

            lable4x6.Margin = new Thickness(10, photo4x6.Height - 30, 0, 0);
            lable5x7.Margin = new Thickness(10, photo5x7.Height - 30, 0, 0);
            lable8x10.Margin = new Thickness(10, photo8x10.Height - 30, 0, 0);
            lable8x12.Margin = new Thickness(photo8x12.Width-100, photo8x12.Height - 30, 0, 0);
            lable12x18.Margin = new Thickness(10, photo12x18.Height - 30, 0, 0);
        }

        private void LoadDisplayInfo()
        {
            string displayKey = @"SYSTEM\CurrentControlSet\Enum\DISPLAY";
            using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(displayKey))
            {
                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    displayInfo[i] = new DisplayInfo();
                    var screen = Screen.AllScreens[i];
                    using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    {
                        displayInfo[i].dpiX = g.DpiX;
                        displayInfo[i].dpiY = g.DpiY;
                    }
                    displayInfo[i].width = screen.Bounds.Width;
                    displayInfo[i].height = screen.Bounds.Height;

                    var device = new DISPLAY_DEVICE();
                    device.cb = Marshal.SizeOf(device);
                    if (EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
                    {
                        displayInfo[i].deviceID = device.DeviceID;
                        if (baseKey == null) continue;

                        foreach (string monitorKeyName in baseKey.GetSubKeyNames())
                        {
                            if (!displayInfo[i].deviceID.Contains(monitorKeyName)) continue;
                            using (RegistryKey monitorKey = baseKey.OpenSubKey(monitorKeyName))
                            {
                                foreach (string subKeyName in monitorKey.GetSubKeyNames())
                                {
                                    using (RegistryKey subKey = monitorKey.OpenSubKey(subKeyName))
                                    {
                                        object classGUID = subKey.GetValue("ClassGUID");
                                        if (classGUID==null || !displayInfo[i].deviceID.Contains((string)classGUID)) continue;
                                    }
                                    using (RegistryKey deviceParams = monitorKey.OpenSubKey(subKeyName + @"\Device Parameters"))
                                    {
                                        if (deviceParams == null) continue;
                                        var edid = (byte[])deviceParams.GetValue("EDID");
                                        if (edid == null) continue;
                                        displayInfo[i].widthCm = edid[21];
                                        displayInfo[i].heightCm = edid[22];
                                        displayInfo[i].name = monitorKeyName;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lableState.Content = $"{this.Width} x {this.Height}";
        }
    }
}
