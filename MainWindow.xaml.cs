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
        public Shape takedShape;
        public Point? p1, p2;
        public int radius = 100;
        private double kSize = 1;
        int step = 1;
        delegate void DrawDelegate(MouseButtonEventArgs e);
        DrawDelegate Draw;

        public MainWindow()
        {
            InitializeComponent();
            TakedShapes.InitializeCanvas(scaleCanvas);
            InitializeAll();
            //CreateSample(300,300);
        }

        private void InitializeAll()
        {
            Canvas.SetTop(scaleCanvas, 0);
            Canvas.SetLeft(scaleCanvas, 0);
        }

        private Point ChangeValues(Point p)                 // Для получения кратных координат, возвращает координаты кратные int step (по умолчанию step = 5)
        {
            int changedValueX = (int)(step*Math.Round(p.X/step, MidpointRounding.ToEven));
            int changedValueY = (int)(step*Math.Round(p.Y/step, MidpointRounding.ToEven));
            //int changedValueX = Convert.ToInt32(p.X - p.X % step);
            //int changedValueY = Convert.ToInt32(p.Y - p.Y % step);
            return new Point(changedValueX, changedValueY);
        }

        private void handleCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (aimCanvas.Visibility == Visibility.Hidden) aimCanvas.Visibility = Visibility.Visible;
            Point p = ChangeValues(e.GetPosition(scaleCanvas));               // округление координат точки сразу после получения этих координат
            posLabel.Content = p;
            SetAim(p);

            if (isMoved)
            {
                Vector delta = e.GetPosition(scaleCanvas) - LastMouseWheelPos;
                Canvas.SetTop(scaleCanvas, Canvas.GetTop(scaleCanvas)+delta.Y * kSize);
                Canvas.SetLeft(scaleCanvas, Canvas.GetLeft(scaleCanvas)+delta.X * kSize);
            }

            if (p1 == null && (takedShape == TakedShapes.line || takedShape == TakedShapes.rectangle)) return;
            
            switch (takedShape)
            {
                case Line line:
                    line.X1 = p1.Value.X;
                    line.Y1 = p1.Value.Y;
                    line.X2 = p.X;
                    line.Y2 = p.Y;
                    break;
                case Ellipse ellipse:
                    ellipse.Width = ellipse.Height = radius*2;
                    Canvas.SetTop(ellipse, p.Y - radius);
                    Canvas.SetLeft(ellipse, p.X - radius);
                    break;
                case Rectangle rectangle:
                    SetRectangle(rectangle, p1.Value, p);
                    break;
            }
        }
        private void handleCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            aimCanvas.Visibility = Visibility.Hidden;
        }

        private double Clamp(double num, double max) => num < max ? num : max;

        private void SetAim(Point p)
        {
            //sorry...
            //ok... sorry...
            _horizLine.X1 = _vertLine.Y1 = 0;

            _vertLine.X2 = _vertLine.X1 = Clamp(p.X, scaleCanvas.ActualWidth);
            _vertLine.Y2 = scaleCanvas.ActualHeight;

            _horizLine.Y2 = _horizLine.Y1 = Clamp(p.Y, scaleCanvas.ActualHeight);
            _horizLine.X2 = scaleCanvas.ActualWidth;
        }

        private void figuresPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
            if (e.Source is Shape shape)
            {
                ResetRectColor();
                UidDetect(shape.Uid).Fill = new SolidColorBrush(Colors.DarkGray);
                switch (Convert.ToInt32(shape.Uid))
                {
                    case 0:
                        takedShape = TakedShapes.line;
                        Draw = DrawLine;
                        break;
                    case 1:
                        takedShape = TakedShapes.ellipse;
                        Draw = DrawEllipse;
                        break;
                    case 2:
                        takedShape = TakedShapes.rectangle;
                        Draw = DrawRectangle;
                        break;
                }
                p1 = null;
                TakedShapes.ResetAllPos();
                HideAllBesides(takedShape);
            }
        }
        private void HideAllBesides(Shape sh)
        {
            TakedShapes.ellipse.Visibility = Visibility.Hidden;
            TakedShapes.rectangle.Visibility = Visibility.Hidden;
            TakedShapes.line.Visibility = Visibility.Hidden; 
            takedShape.Visibility = Visibility.Visible;
        }
        private void ResetRectColor()
        {
            foreach (Grid grid in figuresPanel.Children)
            {
                (grid.Children[0] as Rectangle).Fill = new SolidColorBrush(Colors.LightGray);
            }
        }

        private Rectangle UidDetect(string Uid)
        {
            foreach (Grid grid in figuresPanel.Children)
            {
                Rectangle rect = grid.Children[0] as Rectangle;
                if (rect.Uid == Uid) return rect;
            }
            return null;
        }

        private void handleCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Draw?.Invoke(e);
        }

        public void DrawLine(MouseButtonEventArgs e)
        {
            if (p1 != null) p2 = ChangeValues(e.GetPosition(scaleCanvas));
            if (p2 == null) p1 = ChangeValues(e.GetPosition(scaleCanvas));
            if (p1 != null && p2 != null)
            {
                Line line = new() { X1 = p1.Value.X, Y1 = p1.Value.Y, X2 = p2.Value.X, Y2 = p2.Value.Y, StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black) };
                CanvasAdd(line);
                p1 = null;
                p2 = null;
            }
        }

        public void DrawEllipse(MouseButtonEventArgs e)
        {

            Ellipse ellipse = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black), Width = radius * 2, Height = radius * 2 };
            Canvas.SetTop(ellipse, ChangeValues(e.GetPosition(scaleCanvas)).Y - radius);
            Canvas.SetLeft(ellipse, ChangeValues(e.GetPosition(scaleCanvas)).X - radius);
            CanvasAdd(ellipse);
        }

        public void DrawRectangle(MouseButtonEventArgs e)
        {
            
            if (p1 != null) p2 = ChangeValues(e.GetPosition(scaleCanvas));
            if (p2 == null) p1 = ChangeValues(e.GetPosition(scaleCanvas));
            if (p1 != null && p2 != null)
            {
                Rectangle rectangle = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black)};
                SetRectangle(rectangle, p1.Value, p2.Value);
                CanvasAdd(rectangle);
                p1 = null;
                p2 = null;
            }
        }

        private void SetRectangle(Rectangle rect, Point _p1, Point _p2)
        {
            double height = _p2.Y - _p1.Y;
            double width = _p2.X - _p1.X;

            rect.Height = Math.Abs(height);
            rect.Width = Math.Abs(width);

            Canvas.SetTop(rect, height > 0 ? _p1.Y : _p1.Y + height);
            Canvas.SetLeft(rect, width > 0 ? _p1.X : _p1.X + width);
        }

        private void handleCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            kSize += e.Delta > 0 ? 0.1 : (e.Delta < 0 ? -0.1 : 0);
            kSize = kSize > 3 ? 3 : (kSize < 0.3 ? 0.3 : kSize);
            sizeLabel.Content = ((int)(kSize * 100)).ToString() + "%";
            ScaleTransform sctr = new(kSize,kSize);
            scaleCanvas.RenderTransform = sctr;
        }

        public void CreateSample(int width, int height)
        {
            Rectangle rect = new() { Width = width, Height = height, Stroke = new SolidColorBrush(Colors.Aquamarine), StrokeThickness = 2};
            Canvas.SetTop(rect, scaleCanvas.ActualHeight / 2 + height / 2 );
            Canvas.SetLeft(rect, scaleCanvas.ActualWidth / 2 + width / 2);
            CanvasAdd(rect);
        }

        private Point LastMouseWheelPos;
        private bool isMoved = false;

        private void handleCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                isMoved = true;
                LastMouseWheelPos = e.GetPosition(scaleCanvas);
            }
        }

        private void handleCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && isMoved) isMoved = false;
        }

        public void stepButton_Click(object sender, RoutedEventArgs e)
        {
            if (stepButton.IsChecked.Value)
            {
                step = int.Parse(stepTextBox.Text);
                stepTextBox.IsEnabled = true;
            }
            else
            {
                step = 1;
                stepTextBox.IsEnabled = false;
            }
        }

        private void stepTextBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
            }
            step = int.Parse(stepTextBox.Text);
        }

        private void stepTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void CanvasAdd(Shape shape) => scaleCanvas.Children.Add(shape);
    }


    public static class TakedShapes
    {
        public static Line line = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Gray) };
        public static Ellipse ellipse = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Gray) };
        public static Rectangle rectangle = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Gray) };

        public static void InitializeCanvas(Panel handle)
        {
            handle.Children.Add(line);
            handle.Children.Add(ellipse);
            handle.Children.Add(rectangle);
        }
        public static void ResetAllPos()
        {
            line.Y2 = line.Y1 = line.X2 = line.X1 = 0;
            ellipse.Height = ellipse.Width = 0;
            rectangle.Height = rectangle.Width = 0;
        }
    }

}