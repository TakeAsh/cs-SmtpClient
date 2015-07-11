using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
using TakeAsh;  // string.TryParse<T>(), Crypt

namespace SmtpClient {

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {

        const int DefaultPort = 25;

        private System.Net.Mail.SmtpClient _client;
        private bool _configVisibility = true;
        private Properties.Settings _config = Properties.Settings.Default;

        public MainWindow() {
            InitializeComponent();
            Crypt.Entropy = Encoding.Unicode.GetBytes("Salt; Must be secret!");
            LoadConfig();
        }

        private void LoadConfig() {
            textBox_SmtpServer.Text = _config.Server;
            textBox_SmtpPort.Text = _config.Port.ToString();
            checkBox_SmtpSsl.IsChecked = _config.SSL;
            textBox_SmtpAccount.Text = _config.Account;
            textBox_SmtpPassword.Password = Crypt.DecryptToString(_config.Password);
        }

        private void SaveConfig() {
            _config.Server = textBox_SmtpServer.Text;
            _config.Port = textBox_SmtpPort.Text.TryParse(DefaultPort);
            _config.SSL = checkBox_SmtpSsl.IsChecked == true;
            _config.Account = textBox_SmtpAccount.Text;
            _config.Password = Crypt.Encrypt(textBox_SmtpPassword.Password);
            _config.Save();
        }

        private string MakeErrorMessage(Exception ex) {
            var ret = ex.Message;
            var innerEx = ex.InnerException;
            while (innerEx != null) {
                ret += "\n" + innerEx.Message;
                innerEx = innerEx.InnerException;
            }
            return ret;
        }

        private void button_Send_Click(object sender, RoutedEventArgs e) {
            button_Send.IsEnabled = false;
            button_Cancel.IsEnabled = true;
            textBox_Status.Text = "Sending...";
            SaveConfig();
            try {
                var message = new MailMessage(
                    textBox_From.Text,
                    textBox_To.Text,
                    textBox_Subject.Text,
                    textBox_Body.Text
                );
                if (!String.IsNullOrEmpty(textBox_Cc.Text)) {
                    message.CC.Add(textBox_Cc.Text);
                }
                if (!String.IsNullOrEmpty(textBox_Bcc.Text)) {
                    message.Bcc.Add(textBox_Bcc.Text);
                }
                _client = new System.Net.Mail.SmtpClient(
                    _config.Server,
                    _config.Port
                ) {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    EnableSsl = _config.SSL,
                    Credentials = (!String.IsNullOrEmpty(_config.Account) ?
                        new NetworkCredential(_config.Account, Crypt.DecryptToSecureString(_config.Password)) :
                        null),
                };
                _client.SendCompleted += (s, args) => {
                    var msg = args.UserState as MailMessage;
                    if (args.Error != null) {
                        // After disposing client, whenever "e.Error == null" is true.
                        textBox_Status.Text = MakeErrorMessage(args.Error);
                    } else if (args.Cancelled) {
                        textBox_Status.Text = "Cancel";
                    } else {
                        textBox_Status.Text = "Complete";
                    }
                    msg.Dispose();
                    _client.Dispose();
                    button_Send.IsEnabled = true;
                    button_Cancel.IsEnabled = false;
                };
                _client.SendAsync(message, message);
            }
            catch (Exception ex) {
                textBox_Status.Text = MakeErrorMessage(ex);
                button_Send.IsEnabled = true;
                button_Cancel.IsEnabled = false;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e) {
            if (_client != null) {
                _client.SendAsyncCancel();
            }
        }

        private void button_Config_Click(object sender, RoutedEventArgs e) {
            _configVisibility = !_configVisibility;
            if (!_configVisibility) {
                SaveConfig();
            }
            panel_Config.Visibility = _configVisibility ?
                Visibility.Visible :
                Visibility.Collapsed;
        }

        private void button_CopyMessage_Click(object sender, RoutedEventArgs e) {
            Clipboard.SetText(textBox_Status.Text);
        }
    }
}
