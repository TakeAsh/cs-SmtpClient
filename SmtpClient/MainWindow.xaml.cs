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

namespace SmtpClient {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {

        const int DefaultPort = 25;

        private System.Net.Mail.SmtpClient _client;
        private bool _configVisibility = true;

        public MainWindow() {
            InitializeComponent();
            textBox_SmtpPort.Text = DefaultPort.ToString();
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
            try {
                var message = new MailMessage(
                    textBox_From.Text,
                    textBox_To.Text,
                    textBox_Subject.Text,
                    textBox_Body.Text
                );
                var port = DefaultPort;
                _client = new System.Net.Mail.SmtpClient(
                    textBox_SmtpServer.Text,
                    int.TryParse(textBox_SmtpPort.Text, out port) ?
                        port :
                        DefaultPort
                ) {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    EnableSsl = checkBox_SmtpSsl.IsChecked == true,
                    Credentials = (!String.IsNullOrEmpty(textBox_SmtpAccount.Text) ?
                        new NetworkCredential(textBox_SmtpAccount.Text, textBox_SmtpPassword.Password) :
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
            panel_Config.Visibility = _configVisibility ?
                Visibility.Visible :
                Visibility.Collapsed;
        }

        private void button_CopyMessage_Click(object sender, RoutedEventArgs e) {
            Clipboard.SetText(textBox_Status.Text);
        }
    }
}
