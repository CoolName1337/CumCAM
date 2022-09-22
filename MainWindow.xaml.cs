using System;
using System.Collections.Generic;
using System.Linq;
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

namespace CumCAM
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void handleCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(aimCanvas.Visibility == Visibility.Hidden) aimCanvas.Visibility = Visibility.Visible;
            Point p = e.GetPosition(handleCanvas);
            posLabel.Content = p;
            SetAim(p);
        }
        private void handleCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            aimCanvas.Visibility = Visibility.Hidden;
        }

        private double Clamp(double num, double max) => num < max ? num : max;

        private void SetAim(Point p)
        {
            //sorry...
            _horizLine.X1 = _vertLine.Y1 = 0;

            _vertLine.X2 = _vertLine.X1 = Clamp(p.X, handleCanvas.ActualWidth);
            _vertLine.Y2 = handleCanvas.ActualHeight;

            _horizLine.Y2 = _horizLine.Y1 = Clamp(p.Y, handleCanvas.ActualHeight);
            _horizLine.X2 = handleCanvas.ActualWidth;
        }

        private void figuresPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Rectangle rect)
            {
                resTextBox.Text += "clicked\n";
            }
        }
    }
}
