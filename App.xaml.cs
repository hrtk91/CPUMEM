using System;
using System.Drawing;
using System.Management;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace CPUMEM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private DispatcherTimer Timer { get; } = new DispatcherTimer();

        private NotifyIcon Icon { get; } = new NotifyIcon();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //MenuItemの作成
            var menuItem = new ToolStripMenuItem
            {
                Text = "&Exit",
            };
            menuItem.Click += ClickExit;

            //MenuにMenuItemを追加
            //Menuのインスタンス化
            var menu = new ContextMenuStrip();
            menu.Items.Add(menuItem);

            //Menuをタスクトレイのアイコンに追加
            Icon.Visible = true;
            Icon.ContextMenuStrip = menu;

            // タイマー設定
            Timer.Interval = TimeSpan.FromSeconds(.5);
            Timer.Tick += Tick;
            Timer.Start();
        }

        private void ClickExit(object? sender, EventArgs e)
        {
            Timer.Stop();
            System.Windows.Application.Current.Shutdown();
        }

        private void Tick(object? sender, EventArgs e)
        {
            using (var mc = new ManagementClass("Win32_OperatingSystem"))
            using (var moc = mc.GetInstances())
            using (var mc2 = new ManagementClass("Win32_Processor"))
            using (var moc2 = mc2.GetInstances())
            {
                var cpuload = 0.0f;
                foreach (var mo2 in moc2)
                {
                    var load = (UInt16)mo2["LoadPercentage"];
                    cpuload = (float)load / 100f;
                    mo2.Dispose();
                }

                var memload = 0.0f;
                foreach (ManagementObject mo in moc)
                {
                    var total = (UInt64)mo["TotalVisibleMemorySize"];
                    var current = (UInt64)mo["FreePhysicalMemory"];
                    memload = (float)(total - current) / (float)total;
                    Icon.Text =$"CPU: {(cpuload * 100).ToString("00")}%\r\nMem: {(memload * 100).ToString("00")}%";

                    mo.Dispose();
                }

                var bitmap = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 枠の描画
                    g.DrawRectangle(new Pen(Color.Gray), new Rectangle(0, 0, bitmap.Width - 1, bitmap.Height - 1));
                    
                    // CPU使用率の描画
                    g.FillRectangle(Brushes.Aqua,  new Rectangle(4, 32 - (int)(bitmap.Height * cpuload), 10, (int)(bitmap.Height * cpuload)));

                    // メモリ使用率の描画
                    g.FillRectangle(Brushes.Orange,  new Rectangle(18, 32 - (int)(bitmap.Height * memload), 10, (int)(bitmap.Height * memload)));
                }

                // アイコンに反映
                Icon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            }
        }
    }
}
