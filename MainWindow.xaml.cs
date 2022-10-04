using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO.Packaging;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.Pkcs;
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
        static Sample sample;
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

        public static Sample Instance()
        {
            if (sample == null)
                sample = new Sample();
            return sample;
        }

        private Sample()
        {

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
        static public Sample sample;

        static List<Line> vertLines = new();
        static List<Line> horizLines = new();
        static public Vector StartPosition;
        static public int Step = 20;

        static public Visibility Visibility
        {
            get => handle.Visibility;
            set => handle.Visibility = value;
        }

        private static void InitLines()
        {
            for (int i = 0; i < sample.Width; i++)
                vertLines.Add(new Line() { Stroke = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), StrokeThickness = 0.25 });
            for (int i = 0; i < sample.Height; i++)
                horizLines.Add(new Line() { Stroke = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), StrokeThickness = 0.25 });
        }


        public static void SetVisibleLine(int step) //hide some lines except step line
        {
            for (int i = 0; i < vertLines.Count; i++)
            {
                vertLines[i].Visibility = i % step == 0 ? Visibility.Visible : Visibility.Hidden;
            }
            for (int i = 0; i < horizLines.Count; i++)
            {
                horizLines[i].Visibility = i % step == 0 ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public static void SetPos()
        {
            int i = 0;
            foreach (var line in vertLines)
            {
                line.X2 = line.X1 = StartPosition.X + i;
                line.Y1 = StartPosition.Y;
                line.Y2 = StartPosition.Y - sample.Height;
                i++;
            }
            i = 0;
            foreach (var line in horizLines)
            {
                line.Y2 = line.Y1 = StartPosition.Y - i;
                line.X1 = StartPosition.X;
                line.X2 = StartPosition.X + sample.Width;
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
            SetPos();
        }
    }

    public partial class MainWindow : Window
    {
        public static List<GShape> shapes = new List<GShape>();
        public GShape takedShape;

        public static Vector ZeroPos = new Vector(200, 350);
        Sample CurrentSample;

        public Point? p1, p2;
        public static int radius = 10;
        private double kSize = 1;
        static int step = 1;

        public static int Step
        {
            get => step;
            set
            {
                step = value;
                VisualGrid.SetVisibleLine(step);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            GShape.scaleCanvas = scaleCanvas;
            CommandBinding commandBinding = new CommandBinding();
            commandBinding.Command = ApplicationCommands.Undo;
            commandBinding.Executed += CommandCtrlZ;
            this.CommandBindings.Add(commandBinding);
            (int x, int y) = WorkpieceWindow.GetWP;
            CreateSample(x,y);
            InitializeAll();
        }

        private void InitializeAll()
        {
            VisualGrid.StartPosition = ZeroPos;
            VisualGrid.sample = CurrentSample;
            VisualGrid.Initialize(scaleCanvas);
            VisualGrid.Visibility = Visibility.Hidden;
            Canvas.SetTop(scaleCanvas, 0);
            Canvas.SetLeft(scaleCanvas, 0);
        }

        public static Point ChangeValues(Point p)                 // Для получения кратных координат, возвращает координаты кратные int step (по умолчанию step = 1)
        {
            var changedValueX = ZeroPos.X % step + (int)p.X / step * step + step * Math.Round(( p.X % step) / step);
            var changedValueY = ZeroPos.Y % step + (int)p.Y / step * step + step * Math.Round(( p.Y % step) / step);
            return new Point(changedValueX, changedValueY);
        }

        private void CancelDraw()
        {
            GShape.p1 = null;
            TakedShapes.ResetAllPos();
            HideAllBesides(takedShape);
        }

        private void handleCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (aimCanvas.Visibility == Visibility.Hidden && aimButton.IsChecked.Value) aimCanvas.Visibility = Visibility.Visible;
            Point p = ChangeValues(e.GetPosition(scaleCanvas));               // округление координат точки сразу после получения этих координат
            posLabel.Content = $"{-ZeroPos.X + p.X}; {ZeroPos.Y - p.Y}";
            var pForAim = ChangeValues(e.GetPosition(scaleCanvas));
            SetAim(pForAim);
            //new Point(p.X + Canvas.GetLeft(scaleCanvas) * kSize, p.Y + Canvas.GetTop(scaleCanvas) * kSize)
            if (isMoved)
            {
                Vector delta = (e.GetPosition(scaleCanvas) - LastMouseWheelPos) * kSize;
                var realSamplePosX = Canvas.GetLeft(scaleCanvas) + CurrentSample.Position.X * kSize;
                var realSamplePosY = Canvas.GetTop(scaleCanvas) + CurrentSample.Position.Y * kSize;
                if (realSamplePosX < -CurrentSample.Width * kSize) delta.X = delta.X < 0 ? 0 : delta.X;
                if (realSamplePosY < -CurrentSample.Height * kSize) delta.Y = delta.Y < 0 ? 0 : delta.Y;
                if (realSamplePosX > handleCanvas.ActualWidth) delta.X = delta.X > 0 ? 0 : delta.X;
                if (realSamplePosY > handleCanvas.ActualHeight) delta.Y = delta.Y > 0 ? 0 : delta.Y;
                var deltaPosY = Canvas.GetTop(scaleCanvas) + delta.Y;                                                                                    //epta...
                var deltaPosX = Canvas.GetLeft(scaleCanvas) + delta.X;                                                                                   // ... --- ... --- ... --- ... ---
                Canvas.SetTop(scaleCanvas, deltaPosY);
                Canvas.SetLeft(scaleCanvas, deltaPosX);
            }
            takedShape?.SetPos(p);
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
                radiusTB.Visibility = Visibility.Hidden;
                switch (Convert.ToInt32(shape.Uid))
                {
                    case 0:
                        takedShape = TakedShapes.line;
                        
                        break;
                    case 1:
                        takedShape = TakedShapes.ellipse;
                        radiusTB.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        takedShape = TakedShapes.rectangle;
                        break;
                }
                CancelDraw();
            }
        }
        private void HideAllBesides(GShape sh)
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
            takedShape?.Draw(e);
        }

        public static void SetRectangle(Rectangle rect, Point _p1, Point _p2)
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

            CurrentSample = Sample.Instance();
            CurrentSample.Height = height;
            CurrentSample.Width = width;
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
                Step = int.Parse(stepTextBox.Text);
                stepTextBox.IsEnabled = true;
            }
            else
            {
                Step = 1;
                stepTextBox.IsEnabled = false;
            }
        }

        private void stepTextBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text[0]))
                e.Handled = true;
            Step = int.Parse(stepTextBox.Text);
        }

        private void stepTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true; 
            if (stepTextBox.Text == string.Empty || stepTextBox.Text == "0")
                stepTextBox.Text = "1";
        }

        private void CommandCtrlZ(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveLast();
        }
        public void RemoveLast()
        {
            if (shapes.Count > 0)
            {
                scaleCanvas.Children.Remove(shapes.Last().shape);
                shapes.Remove(shapes.Last());
            }
        }

        private void gridButton_Click(object sender, RoutedEventArgs e)
        {
            if (gridButton.IsChecked.Value)
            {
                VisualGrid.Visibility = Visibility.Visible;
                VisualGrid.SetVisibleLine(step);
            }
            else
            {
                VisualGrid.Visibility = Visibility.Hidden;
            }
        }

        private void radiusTB_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            if (radiusTB.Text == string.Empty || radiusTB.Text == "0")
                radiusTB.Text = "1";
        }

        private void radiusTB_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text[0]))
                e.Handled = true;
            radius = int.Parse(radiusTB.Text);
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            resTextBox.Text = CreateGCode();
        }

        public string CreateGCode()
        {
            string res = string.Empty;
            foreach (GShape shape in shapes)
            {
                res += shape.GetGCode();
            }
            return res + "M02\n";
        }

    }

    public class GLine : GShape
    {
        public GLine(Shape _shape) : base(_shape) { }

        public override void Draw(MouseButtonEventArgs e)
        {
            if (p1 != null) p2 = MainWindow.ChangeValues(e.GetPosition(scaleCanvas));
            if (p2 == null) p1 = MainWindow.ChangeValues(e.GetPosition(scaleCanvas));
            if (p1 != null && p2 != null)
            {
                new GLine (new Line () { X1 = p1.Value.X, Y1 = p1.Value.Y, X2 = p2.Value.X, Y2 = p2.Value.Y, StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black) });
                p1 = p2 = null;
            }
        }

        public override string GetGCode()
        {
            Line line = shape as Line;
            return $"G00 X{line.X1 - MainWindow.ZeroPos.X} Y{-line.Y1 + MainWindow.ZeroPos.Y}\nM03 Z-5\nG01 X{line.X2 - MainWindow.ZeroPos.X} Y{-line.Y2 + MainWindow.ZeroPos.Y}\nM05 Z0\n";
        }
        public override void SetPos(Point p)
        {
            if (p1 == null) return;
            Line _line = shape as Line;
            _line.X1 = p1.Value.X;
            _line.Y1 = p1.Value.Y;
            _line.X2 = p.X;
            _line.Y2 = p.Y;
        }
    }

    public class GEllipse : GShape
    {
        public GEllipse (Shape _shape) : base (_shape) { }

        public override void Draw(MouseButtonEventArgs e)
        {
            GEllipse ge = new GEllipse(new Ellipse () { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black), Width = MainWindow.radius * 2, Height = MainWindow.radius * 2 });
            
            Canvas.SetTop(ge.shape, MainWindow.ChangeValues(e.GetPosition(scaleCanvas)).Y - MainWindow.radius);
            Canvas.SetLeft(ge.shape, MainWindow.ChangeValues(e.GetPosition(scaleCanvas)).X - MainWindow.radius);
            
        }

        public override string GetGCode()
        {
            double PosX = Canvas.GetLeft(shape) - MainWindow.ZeroPos.X + shape.Width / 2;
            double PosY = -Canvas.GetTop(shape) + MainWindow.ZeroPos.Y - shape.Height / 2;
            return $"G03 X{PosX} Y{PosY} R{shape.Width / 2}\n";
        }
        public override void SetPos(Point p)
        {
            shape.Width = shape.Height = MainWindow.radius * 2;
            Canvas.SetTop(shape, p.Y - MainWindow.radius);
            Canvas.SetLeft(shape, p.X - MainWindow.radius);
        }
    }

    public class GRectangle : GShape
    {
        
        public GRectangle(Shape _shape) : base(_shape) { }

        public override void Draw(MouseButtonEventArgs e)
        {
            if (p1 != null) p2 = MainWindow.ChangeValues(e.GetPosition(scaleCanvas));
            if (p2 == null) p1 = MainWindow.ChangeValues(e.GetPosition(scaleCanvas));
            if (p1 != null && p2 != null)
            {
                GRectangle gr = new GRectangle(new Rectangle () { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Black) });
                MainWindow.SetRectangle(gr.shape as Rectangle, p1.Value, p2.Value);
                p1 = null;
                p2 = null;
            }
        }

        public override string GetGCode()
        {
            double PosX = Canvas.GetLeft(shape) - MainWindow.ZeroPos.X;
            double PosY = -Canvas.GetTop(shape) + MainWindow.ZeroPos.Y;
            return $"G00 X{PosX} Y{PosY}\nM03 Z-5\nG01 X{PosX + shape.Width}\nG01 Y{PosY - shape.Height}\nG01 X{PosX}\nG01 Y{PosY}\nM05 Z0\n";
        }

        public override void SetPos(Point p)
        {   
            if (p1 == null) return;
            MainWindow.SetRectangle(shape as Rectangle, p1.Value, p);
        }
    }

    public abstract class GShape
    {
        public GShape(Shape _shape)
        {
            shape = _shape;
            shape.DataContext = this;
            MainWindow.shapes.Add(this);
            scaleCanvas.Children.Add(shape);
        }
        public Visibility Visibility { get => shape.Visibility; set => shape.Visibility = value; }
        public static Point? p1, p2;
        public static Canvas scaleCanvas;
        public Shape shape;
        public abstract void Draw(MouseButtonEventArgs e);
        public abstract string GetGCode();
        public abstract void SetPos(Point p);
    }

    public static class TakedShapes
    {
        public static GLine line = new(new Line() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Gray) });
        public static GEllipse ellipse = new(new Ellipse() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Gray) });
        public static GRectangle rectangle = new(new Rectangle() { StrokeThickness = 2, Stroke = new SolidColorBrush(Colors.Gray) });
        public static void ResetAllPos()
        {
            Line _line = line.shape as Line; 
            _line.Y2 = _line.Y1 = _line.X2 = _line.X1 = 0;
            ellipse.shape.Height = ellipse.shape.Width = 0;
            rectangle.shape.Height = rectangle.shape.Width = 0;
        }
    }
}