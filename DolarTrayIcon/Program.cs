using System;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.Timers;
using Timer = System.Timers.Timer;
using Microsoft.Win32;

namespace DolarTrayIcon
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DolarAppContext());
        }
    }

    public class DolarAppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private readonly Timer _timer = new Timer();
        private const string AppName = "DolarTrayIcon";

        public DolarAppContext()
        {
            // Evitar problemas de autenticação de certificado SSL
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            trayIcon = new NotifyIcon();
            SetContextMenu();
            _timer.Elapsed += OnElapsedTime;
            _timer.Interval = 5 * 60 * 1000;
            _timer.Enabled = true;

            renderTextOnIcon("USD");
            fetchUSDRate();
        }

        private void SetContextMenu()
        {
            var isStartingAutomatically = IsStartingAutomatically();
            trayIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Sair", Exit),
                new MenuItem(isStartingAutomatically ? "Parar início automático" : "Iniciar automaticamente", SetAutomaticStartUp)
            });

        }

        private bool IsStartingAutomatically()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                var isSet = rk.GetValue(AppName);
                return isSet != null;
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;

        }

        private void SetAutomaticStartUp(object sender, EventArgs e)
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (IsStartingAutomatically())
                {
                    var response = MessageBox.Show("Deseja interromper o início automático deste programa?", "Interromper início automático", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (response == DialogResult.Yes)
                    {
                        rk.DeleteValue(AppName, false);
                        MessageBox.Show("Início automático desativado com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    var response = MessageBox.Show("Deseja ativar o início automático deste programa?", "Ativar início automático", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (response == DialogResult.Yes)
                    {
                        rk.SetValue(AppName, Application.ExecutablePath);
                        MessageBox.Show("Início automático ativado com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar mudança no início automático. " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SetContextMenu();
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            fetchUSDRate();
        }

        void Exit(object sender, EventArgs e)
        {
            // Esconder o ícone antes de fechar o aplicativo.
            // Se não fizer isso, o ícone só desaparece quando o usuário passa o mouse por cima do ícone.
            trayIcon.Visible = false;
            Application.Exit();
        }

        private void fetchUSDRate()
        {
            // URL da API de cotações de moedas
            var urlAPI = @"https://economia.awesomeapi.com.br/json/last/USD-BRL";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    var json = webClient.DownloadString(urlAPI);
                    var list = JsonConvert.DeserializeObject<ExchangeRateList>(json);

                    string message = "USD\n" + list.USDBRL.bid.ToString("N2");
                    renderTextOnIcon(message);
                    trayIcon.Text = message;
                }
            }
            catch (WebException ex)
            {
                renderTextOnIcon("ERR");
            }
        }

        private void renderTextOnIcon(string text)
        {
            int dimensions = 256;
            int border = 5;
            float shadow = 2;
            float fontSize = 74;
            Bitmap bmp = new Bitmap(dimensions, dimensions);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF ImageSize = new RectangleF((border * -1), (border * -1), dimensions + border, dimensions + border);
                RectangleF ShadowSize = new RectangleF((border * -1) + shadow, (border * -1) + shadow, dimensions + border, dimensions + border);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                StringFormat format = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString(text, new Font("Tahoma", fontSize, FontStyle.Bold), Brushes.Black, ShadowSize, format);
                g.DrawString(text, new Font("Tahoma", fontSize, FontStyle.Bold), Brushes.White, ImageSize, format);

                g.Flush();
            }

            IntPtr Hicon = bmp.GetHicon();
            Icon myIcon = Icon.FromHandle(Hicon);
            trayIcon.Icon = myIcon;
            trayIcon.Visible = true;
        }

    }
    public class ExchangeRate
    {
        public string code { get; set; }
        public decimal bid { get; set; }
        public decimal pctChange { get; set; }
    }

    public class ExchangeRateList
    {
        public ExchangeRate USDBRL { get; set; }
    }
}
