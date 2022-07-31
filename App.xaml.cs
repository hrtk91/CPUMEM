using System;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using NLog;

namespace CPUMEM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private DispatcherTimer Timer { get; } = new DispatcherTimer();

        private NotifyIcon Icon { get; } = new NotifyIcon();

        private ILogger logger { get; } = NLog.LogManager.GetCurrentClassLogger();

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
            try
            {
                Timer.Stop();
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{nameof(ClickExit)} : 例外が発生");
            }
        }

        private void Tick(object? sender, EventArgs e)
        {
            try
            {
                var cpuload = GetCpuLoad();
                var memload = GetMemoryLoad();

                UpdateTooltip(cpuload, memload);

                using (var bmp = CreateIcon(cpuload, memload))
                {
                    UpdateIcon(bmp);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{nameof(Tick)} : 例外が発生");
            }
        }

        private float GetCpuLoad()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_Processor"))
                using (var moc = mc.GetInstances())
                {
                    var cpuload = 0.0f;
                    if (moc is not null)
                    {
                        foreach (var mo2 in moc)
                        {
                            var load = (ushort)(mo2["LoadPercentage"] ?? (ushort)0);
                            cpuload = (load is (ushort)0) ? 0.0f : (float)load / 100f;
                            mo2.Dispose();
                        }
                    }
                    return cpuload;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("CPU処理負荷の取得に失敗しました。", ex);
            }
        }

        private float GetMemoryLoad()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_OperatingSystem"))
                using (var moc = mc.GetInstances())
                {
                    var memload = 0.0f;
                    if (moc is not null)
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            var total = (ulong)(mo["TotalVisibleMemorySize"] ?? 0ul);
                            var current = (ulong)(mo["FreePhysicalMemory"] ?? 0ul);

                            if (total is not 0ul)
                            {
                                memload = (float)(total - current) / (float)total;
                            }

                            mo.Dispose();
                        }
                    }
                    return memload;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Memory処理負荷の取得に失敗しました。", ex);
            }
        }

        private void UpdateTooltip(float cpuload, float memload)
        {
            try
            {
                Icon.Text = $"CPU: {(cpuload * 100).ToString("00")}%\r\nMem: {(memload * 100).ToString("00")}%";
            }
            catch (Exception ex)
            {
                throw new Exception("ToolTipの更新処理に失敗しました。", ex);
            }
        }

        private Bitmap CreateIcon(float cpuload, float memload)
        {
            try
            {
                var bitmap = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 枠の描画
                    g.DrawRectangle(new Pen(Color.Gray), new Rectangle(0, 0, bitmap.Width - 1, bitmap.Height - 1));

                    // CPU使用率の描画
                    var cpuHeight = (int)((bitmap.Height - 2) * cpuload);
                    g.FillRectangle(Brushes.Aqua, new Rectangle(4, 30 - cpuHeight, 10, cpuHeight));

                    // メモリ使用率の描画
                    var memHeight = (int)((bitmap.Height - 2) * memload);
                    g.FillRectangle(Brushes.Orange, new Rectangle(18, 30 - memHeight, 10, memHeight));
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception("アイコンの作成処理に失敗しました。", ex);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private void UpdateIcon(Bitmap bmp)
        {
            try
            {
                if (Icon.Icon is not null)
                {
                    DestroyIcon(Icon.Icon.Handle);
                }

                Icon.Icon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
            }
            catch (Exception ex)
            {
                throw new Exception("アイコンの更新処理に失敗しました。", ex);
            }
        }
    }
}
