using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibreHardwareMonitor.Hardware;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Forms;

// WPF 负责窗口，Windows Forms 提供托盘图标和右键菜单
namespace ComputerMonitoring
{
    public partial class MainWindow : Window
    {
        private Computer computer;
        private DispatcherTimer timer;
        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            // 创建(实例化) MainWindows.xaml 中定义的界面，并连接 CpuText 等控件名称
            InitializeComponent();
            
            // 设置窗口位置
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = 0;
            this.Top  = SystemParameters.PrimaryScreenHeight - this.Height;

            // 不出现在任务栏中
            this.ShowInTaskbar = false;

            // 创建托盘图标
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new Icon("Assets/DreamStar.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = "Computer Monitor";

            // 双击显示窗口
            // (s, e) => { ... } 是匿名函数
            notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            // 右键Exit退出程序
            // var 相当于 auto
            var menu = new ContextMenuStrip();
            menu.Items.Add("Exit", null, (s, e) =>
            {
                notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });

            notifyIcon.ContextMenuStrip = menu;
            // 绑定关闭事件，普通关闭窗口时取消关闭，改为隐藏
            this.Closing += (s, e) =>
            {
                e.Cancel = true;
                this.Hide();
            };

            // 打开硬件监控
            computer = new Computer()
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };
            computer.Open();

            // 启动每秒触发一次的定时器
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            // 绑定鼠标左键事件，拖动窗口
            this.MouseLeftButtonDown += (s, e) => { 
                if (e.ButtonState == MouseButtonState.Pressed) DragMove(); 
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            double cpuLoad = 0, NvidiaLoad = 0, AmdLoad = 0,ramLoad = 0;
            var elecQuan = System.Windows.Forms.SystemInformation.PowerStatus.BatteryLifePercent; 
            foreach (var hardware in computer.Hardware)
            {
                // 更新每个硬件的状态
                hardware.Update();
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        foreach (var sensor in hardware.Sensors)
                            // 每个cpu传感器，如果是总负载，有值就使用，无值就赋 0 
                            if (sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total")
                                cpuLoad = sensor.Value ?? 0;
                        break;
                    case HardwareType.GpuAmd:
                        foreach (var sensor in hardware.Sensors)
                            if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU"))
                                AmdLoad = sensor.Value ?? 0;
                        break;
                    case HardwareType.GpuNvidia:
                        foreach (var sensor in hardware.Sensors)
                            if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU"))
                                NvidiaLoad = sensor.Value ?? 0;
                        break;
                    case HardwareType.Memory:
                        foreach (var sensor in hardware.Sensors)
                            if (sensor.SensorType == SensorType.Load)
                                ramLoad = sensor.Value ?? 0;
                        break;
                }
            }

            // $"..." 用来把变量插入字符串，F1 表示保留一位小数，例如 CPU: 23.5%
            CpuText.Text = $"CPU: {cpuLoad:F1}%";
            NvidiaText.Text = $"Nvidia: {NvidiaLoad:F1}%";
            AmdText.Text = $"AMD: {AmdLoad:F1}%";
            RamText.Text = $"RAM: {ramLoad:F1}%";
            BatteryText.Text = $"EQ: {elecQuan * 100:F1}%";
            TimeText.Text = $"Time: {DateTime.Now.ToString("HH:mm")}";
        }
    }
}