using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace chat
{
    public partial class MainWindow : Window
    {
        IPAddress[] MyIPList = Dns.GetHostByName(Dns.GetHostName()).AddressList;

        public MainWindow()
        {
            InitializeComponent();

            
            foreach (IPAddress Adr in MyIPList)
            {
                comboBox.Items.Add(Adr);
            }
            comboBox.SelectedIndex = comboBox.Items.Count - 1;
        }

        private void textName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenChatWindow();
            }
        }

        private void OpenChatWindow()
        {
            ChatWindow chatWindow = new ChatWindow(textName.Text, MyIPList[comboBox.Items.Count - 1]);
            chatWindow.Show();

            Close();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            OpenChatWindow();
        }
    }
}
