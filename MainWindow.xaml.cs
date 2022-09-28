using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
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
    class Sample
    {
        private Rectangle rect = new Rectangle() { Stroke = new SolidColorBrush(Colors.Gray), StrokeThickness = 4 };
        private Vector pos = new Vector(0, 0);
        public Vector Position
        {
            get => pos;
            set
            {
                pos = value;
                Canvas.SetTop(rect, pos.Y);
                Canvas.SetLeft(rect, pos.X);
            }
        }
        public double Width
        {
            get => rect.Width;
            set => rect.Width = value > 0 ? value : 0;
        }
        public double Height
        {
            get => rect.Height;
            set => rect.Height = value > 0 ? value : 0;
        }
        public void Initialize(Panel canv)
        {
            canv.Children.Add(rect);
        }

    }

    static class VisualGrid
    {
        static public Panel Root;
        static Canvas handle = new();

        static List<Line> vertLines = new();
        static List<Line> horizLines = new();
        static public Vector StartPosition;
        static public int Step = 20;

        private static void InitLines()
        {
            for (int i = 0; i < 100; i++)
                vertLines.Add(new Line() { Stroke = new SolidColorBrush(Color.FromArgb(40,0,0,0)), StrokeThickness=1 });
            for (int i = 0; i < 100; i++)
                horizLines.Add(new Line() { Stroke = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), StrokeThickness=1 });
        }

        public static void SetPos(double kSize)
        {
            int i = 0;
            foreach (var line in vertLines)
            {
                line.X2 = line.X1 = -StartPosition.X + Step * i;
                line.Y1 = -StartPosition.Y*2;
                line.Y2 = StartPosition.Y*2;
                i++;
            }
            i = 0;
            foreach (var line in horizLines)
            {
                line.Y2 = line.Y1 = -StartPosition.Y + Step * i;
                line.X1 = -StartPosition.X*2;
                line.X2 = StartPosition.X*2;
                i++;
            }
        }

        private static void InitHandle()
        {
            foreach (var item in vertLines)
                handle.Children.Add(item);
            foreach (var item in horizLines)
                handle.Children.Add(item);
        }


        public static void Initialize(Panel canv)
        {
            InitLines();
            InitHandle();
            Root = canv;
            Root.Children.Add(handle);
            SetPos(1);
        }
    }

    public partial class MainWindow : Window
    {
        List<Shape> shapes = new List<Shape>();
        public Shape takedShape;

        Vector ZeroPos = new Vector(200, 350);
        Sample CurrentSample;

        public Point? p1, p2;
        public int radius = 100;
        private double kSize = 1;
        int step = 1;
        delegate void DrawDelegate(MouseButtonEventArgs e);
        DrawDelegate Draw;

        public MainWindow()
        {
            InitializeComponent();

            InitializeAll();
            TakedShapes.InitializeCanvas(scaleCanvas);
            CreateSample(1000, 1000);
        }

        private void InitializeAll()
        {
            Canvas.SetTop(scaleCanvas, 0);
            Canvas.SetLeft(scaleCanvas, 0);
        }

        private Point ChangeValues(Point p)                 // Для получения кратных координат, возвращает координаты кратные int step (по умолчанию step = 1)
        {
            var changedValueX = ZeroPos.X % step + step * Math.Round(p.X / step);
            var changedValueY = ZeroPos.Y % step + step * Math.Round(p.Y / step);
            return new Point(changedValueX, changedValueY);
        }

        private void CancelDraw()
        {
            p1 = null;
            TakedShapes.ResetAllPos();
            HideAllBesides(takedShape);
        }

        private void handleCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (aimCanvas.Visibility == Visibility.Hidden) aimCanvas.Visibility = Visibility.Visible;
            Point p = ChangeValues(e.GetPosition(scaleCanvas));               // округление координат точки сразу после получения этих координат
            posLabel.Content = $"{-ZeroPos.X + p.X}; {ZeroPos.Y - p.Y}";


            var pForAim = ChangeValues(e.GetPosition(scaleCanvas));
            SetAim(pForAim);
            //new Point(p.X + Canvas.GetLeft(scaleCanvas) * kSize, p.Y + Canvas.GetTop(scaleCanvas) * kSize)
            if (isMoved)
            {
                Vector delta = e.GetPosition(scaleCanvas) - LastMouseWheelPos;
                var deltaPosX = Canvas.GetTop(scaleCanvas) + delta.Y * kSize;
                var deltaPosY = Canvas.GetLeft(scaleCanvas) + delta.X * kSize;
                Canvas.SetTop(scaleCanvas, deltaPosX);
                Canvas.SetLeft(scaleCanvas, deltaPosY);
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
                    ellipse.Width = ellipse.Height = radius * 2;
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

        public static double Clamp(double num, double max) => num < max ? num : max;
        public static double Clamp(double num, double min, double max) => num > max ? max : num < min ? min : num;

        private void SetAim(Point p)
        {
            double resWidth = handleCanvas.ActualWidth / kSize;
            double resHeight = handleCanvas.ActualHeight / kSize;

            _vertLine.X2 = _vertLine.X1 = p.X;
            _vertLine.Y1 = -resHeight + p.Y;
            _vertLine.Y2 = resHeight + p.Y;

            _horizLine.Y2 = _horizLine.Y1 = p.Y;
            _horizLine.X1 = -resWidth + p.X;
            _horizLine.X2 = resWidth + p.X;
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
                CancelDraw();
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
                shapes.Add(line);
                CanvasAdd(line);
                p1 = null;
                p2 = null;
            }
        }

        public void DrawEllipse(MouseButtonEventArgs e)
        {

            Ellipse ellipse = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black), Width = radius * 2, Height = radius * 2 };
            shapes.Add(ellipse);
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
                Rectangle rectangle = new() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black) };
                shapes.Add(rectangle);
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
            ScaleTransform sctr = new(kSize, kSize);
            scaleCanvas.RenderTransform = sctr;
        }

        public void CreateSample(int width, int height)
        {
            CurrentSample = new Sample() { Height = height, Width = width };
            CurrentSample.Position = ZeroPos - new Vector(0, height);
            CurrentSample.Initialize(scaleCanvas);
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
            if (e.RightButton == MouseButtonState.Pressed)
            {
                CancelDraw();
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

        private void CommandCtrlZ(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveLast();
        }
        public void RemoveLast()
        {
            scaleCanvas.Children.Remove(shapes.Last());
            shapes.Remove(shapes.Last());

        }

        private void aimButton_Click(object sender, RoutedEventArgs e)
        {
            aimCanvas.Visibility = aimButton.IsChecked.Value ? Visibility.Visible : Visibility.Hidden;
        }

        private void gridButton_Click(object sender, RoutedEventArgs e)
        {
            if (gridButton.IsChecked.Value)
            {
                VisualGrid.StartPosition = ZeroPos;
                VisualGrid.Initialize(scaleCanvas);
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