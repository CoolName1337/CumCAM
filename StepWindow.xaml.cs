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
    public partial class StepWindow : Window
    {
        public static int stepBoxRes;

        public StepWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
                stepBoxRes = Convert.ToInt32(stepBox.Text); 
            }
            catch
            {
                MessageBox.Show("Step hasn't been set. Enter correct value please");
            }
            finally
            {
                if (stepBoxRes > 0) this.DialogResult = true;
                else this.DialogResult = false;
            }
                
        }

        public static int GetStep
        {
            get { return stepBoxRes; }
        }
    }
}
