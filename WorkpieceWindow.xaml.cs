using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CumCAM
{
    public partial class WorkpieceWindow : Window
    {
        public static int xWP, yWP, zWP;

        public WorkpieceWindow()
        {
            InitializeComponent();
        }


        public static (int, int) GetWP
        {
            get { return (xWP, yWP); }
        }

        private void size_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                xWP = Convert.ToInt32(XBox.Text);
                yWP = Convert.ToInt32(YBox.Text);
                zWP = Convert.ToInt32(ZBox.Text);
                MainWindow mainWindow = new MainWindow();
                this.Close();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("STFU");
            }
        }
    }
}
