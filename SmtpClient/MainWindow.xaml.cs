using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using TakeAshUtility;

namespace SmtpClient {

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {

        const int DefaultPort = 25;

        private static Properties.Settings _config = Properties.Settings.Default;
        private static readonly string _fileFilter = new[] {
            new FileFilterItem("All files", ".*"),
            new FileFilterItem("ZIP files", ".zip"),
            new FileFilterItem("JPEG files", ".jpg; .jpeg; .jpe"),
            new FileFilterItem("PNG files", ".png"),
            new FileFilterItem("PDF files", ".pdf"),
            new FileFilterItem("Excel files", ".xlsx; .xls"),
            new FileFilterItem("Word files", ".docx; .doc"),
            new FileFilterItem("Text files", ".txt; .md"),
        }.ToFileFilter();

        private static readonly Regex _regQuoted = new Regex(@"(?<quot>[""'])([^""']+)\k<quot>");
        private static readonly string[] _mailAddressSeparators = new[] { ",", "\n", "\r", "\t", };
        private static readonly Regex _regEscape = new Regex(@"(" + String.Join("|", _mailAddressSeparators) + @")");
        private static readonly Regex _regUnescape = new Regex(@"\\x([0-9a-fA-F]{2})");
        private static readonly InlineComparer<MailAddress> _mailAddressComparer = new InlineComparer<MailAddress>(
            (ma1, ma2) => ma1.Address == ma2.Address,
            (ma) => ma.Address.GetHashCode()
        );

        private System.Net.Mail.SmtpClient _client;
        private bool _configVisibility = true;
        private List<System.IO.FileInfo> _attachments = new List<System.IO.FileInfo>();

        public MainWindow() {
            InitializeComponent();
            var mailAddressesDragOverEventHandler = DragEventHelper.CreateDragOverEventHandler(
                new[] { DataFormats.UnicodeText, DataFormats.Text, }
            );
            textBox_From.PreviewDragOver += mailAddressesDragOverEventHandler;
            textBox_To.PreviewDragOver += mailAddressesDragOverEventHandler;
            textBox_Cc.PreviewDragOver += mailAddressesDragOverEventHandler;
            textBox_Bcc.PreviewDragOver += mailAddressesDragOverEventHandler;
            listBox_Attachments.DragOver += DragEventHelper.CreateDragOverEventHandler(
                new[] { DataFormats.FileDrop, DataFormats.UnicodeText, DataFormats.Text, }
            );
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
            _config.Password = Crypt.Encrypt(textBox_SmtpPassword.SecurePassword);
            _config.Save();
        }

        private void SelectAttachments() {
            var dlg = new OpenFileDialog() {
                Title = "Select attachments",
                FileName = "",
                DefaultExt = ".*",
                Filter = _fileFilter,
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
            };
            if (dlg.ShowDialog() != true) {
                return;
            }
            AddAttachments(dlg.FileNames);
        }

        private void AddAttachments(IEnumerable<string> files) {
            _attachments = _attachments.Union(
                files.Select(file => new System.IO.FileInfo(file)),
                new InlineComparer<System.IO.FileInfo>(
                    (fi1, fi2) => fi1.FullName == fi2.FullName,
                    (fi) => fi.FullName.GetHashCode()
                )).ToList();
            listBox_Attachments.ItemsSource = null;
            listBox_Attachments.ItemsSource = _attachments;
        }

        private void RemoveAttachments() {
            var selected = listBox_Attachments.SelectedItems.OfType<System.IO.FileInfo>().ToList();
            _attachments.RemoveAll(file => selected.Contains(file));
            listBox_Attachments.ItemsSource = null;
            listBox_Attachments.ItemsSource = _attachments;
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
                if (_attachments.Count > 0) {
                    _attachments.ForEach(file =>
                        message.Attachments.Add(new Attachment(file.FullName, GetMimeType(file.Name))));
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
                        textBox_Status.Text = args.Error.GetAllMessages();
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
                textBox_Status.Text = ex.GetAllMessages();
                button_Send.IsEnabled = true;
                button_Cancel.IsEnabled = false;
            }
        }

        /// <summary>
        /// Get MIME type from registry
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <returns>MIME type string</returns>
        /// <remarks>
        /// [c# - Get MIME type from filename extension - Stack Overflow](http://stackoverflow.com/questions/1029740)
        /// </remarks>
        private string GetMimeType(string fileName) {
            var regKey = Registry.ClassesRoot
                .OpenSubKey(System.IO.Path.GetExtension(fileName).ToLower());
            string mimeType;
            return regKey == null || String.IsNullOrEmpty(mimeType = regKey.GetValue("Content Type") as string) ?
                MediaTypeNames.Application.Octet :
                mimeType;
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

        private void button_AddAttachments_Click(object sender, RoutedEventArgs e) {
            SelectAttachments();
        }

        private void listBox_Attachments_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            SelectAttachments();
        }

        private void button_RemoveAttachments_Click(object sender, RoutedEventArgs e) {
            RemoveAttachments();
        }

        private void listBox_Attachments_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            button_RemoveAttachments.IsEnabled = listBox_Attachments.SelectedItems.Count > 0;
        }

        private void listBox_Attachments_Drop(object sender, DragEventArgs e) {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null) {
                AddAttachments(files);
                e.Handled = true;
                return;
            }
            var text = (e.Data.GetData(DataFormats.UnicodeText) as string) ??
                (e.Data.GetData(DataFormats.Text) as string);
            if (text != null &&
                (files = text.SplitTrim(new[] { "\n", "\r" })
                    .Where(file => System.IO.File.Exists(file))
                    .ToArray()).Length > 0) {
                AddAttachments(files);
                e.Handled = true;
                return;
            }
        }

        private void textBox_MailAddress_Drop(object sender, DragEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox == null) {
                return;
            }
            e.Handled = true;
            var text = (e.Data.GetData(DataFormats.UnicodeText) as string) ??
                (e.Data.GetData(DataFormats.Text) as string);
            var mailAddresses = ToMailAddresses(text).FirstOrDefault();
            if (mailAddresses != null) {
                textBox.Text = mailAddresses.ToString();
                return;
            }
        }

        private void textBox_MailAddresses_Drop(object sender, DragEventArgs e) {
            var textBox = sender as TextBox;
            if (textBox == null) {
                return;
            }
            var text = (e.Data.GetData(DataFormats.UnicodeText) as string) ??
                (e.Data.GetData(DataFormats.Text) as string);
            var mailAddresses = ToMailAddresses(text);
            if (mailAddresses != null && mailAddresses.Count > 0) {
                textBox.Text = String.Join(
                    ", ",
                    String.IsNullOrEmpty(textBox.Text) ?
                        mailAddresses :
                        ToMailAddresses(textBox.Text).Union(mailAddresses, _mailAddressComparer)
                );
                e.Handled = true;
                return;
            }
        }

        private List<MailAddress> ToMailAddresses(string text) {
            if (String.IsNullOrEmpty(text)) {
                return null;
            }
            return _regQuoted.Replace(
                text,
                (m1) => "\"" + _regEscape.Replace(
                    m1.Groups[1].Value,
                    (m2) => "\\x" + ((int)m2.Groups[1].Value[0]).ToString("X2")
                ) + "\""
            ).SplitTrim(_mailAddressSeparators)
            .Select(address => {
                try {
                    return new MailAddress(_regUnescape.Replace(
                        address,
                        (m) => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()
                    ));
                }
                catch (Exception ex) {
                    textBox_Status.Text = ex.GetAllMessages();
                    return null;
                }
            }).Where(address => address != null)
            .Distinct(_mailAddressComparer)
            .ToList();
        }
    }
}
