using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;

namespace Life
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateField();
            CalculateNeighbourhood();

            Root.Width = 100 + FieldWidth * 10;
            Root.Height = 100 + FieldHeight * 10;

            DispatcherTimer tmr = new DispatcherTimer();

            tmr.Interval = new TimeSpan(100000);
            tmr.IsEnabled = true;

            tmr.Tick += tmr_Tick;

            tmr.Start();

        }

        private Random rg { get; set; }

        int iterator = 0;
        void tmr_Tick(object sender, EventArgs e)
        {
            
            Update();
            var mass = Field.Sum(x => x.Density);
            Dispatcher.Invoke(new Action(() => this.Title = string.Format("Iteration: {0}, Mass: {1}", ++iterator, mass)));
        }

        private void CreateField()
        {
            rg = new Random();
            Field = new List<Cell>();
            FieldView = new Dictionary<Cell, Rectangle>();
            Densities = new Dictionary<Cell, double>();
            for (int i = 0; i < FieldWidth; i++)
                for (int j = 0; j < FieldHeight; j++)
                    Generate(i * 10 + 50, j * 10 + 50, i > 2 * FieldWidth / 3 ? rg.Next(0, 2) : 0);

            
        }

        #region Constants 
        
        const int FieldWidth = 150;
        const int FieldHeight = 80;
        const int FieldCount = FieldWidth * FieldHeight; 

        #endregion

        #region Field update logic

        Brush Inactive = new SolidColorBrush(Color.FromRgb(252, 255, 242));
        Brush Active = new SolidColorBrush(Color.FromRgb(42, 42, 42));

        private List<Cell> Field { get; set; }
        private Dictionary<Cell, Rectangle> FieldView { get; set; }
        private Dictionary<Cell, double> Densities { get; set; }

        public void Generate(double x, double y, double density)
        {
            var cell = new Cell { X = x, Y = y, Density = density };
            var view = new Rectangle { Width = 8, Height = 8, Fill = density == 1 ? Active : Inactive };

            view.SetValue(Canvas.LeftProperty, x - 5);
            view.SetValue(Canvas.TopProperty, y - 5);

            FieldView[cell] = view;
            Field.Add(cell);

            Root.Children.Add(view);
        }

        public double Distance(Cell fst, Cell snd)
        {
            var diffx = Math.Abs(fst.X - snd.X);
            var diffy = Math.Abs(fst.Y - snd.Y);

            if (diffx <= 15 && diffy <= 15) return 1; 
            else return 100;
        }

        public void CalculateNeighbourhood()
        {
            foreach (var cell in Field)            
                cell.Neighbourhood = Field.Where(x => cell != x && Distance(cell, x) <= 2).ToList();            
        }

        public double CalculateDensityG(Cell cell) // General Density
        {            
            var nd = cell.Neighbourhood.Sum(x=>x.Density);
            if (nd == 3) return 1;
            else if (nd == 2) return cell.Density;
            else return 0;            
        }

        public double CalculateDensityS(Cell cell) // General Density + 1
        {
            var nd = cell.Neighbourhood.Sum(x => x.Density);
            if (nd == 3) return 1;
            else if (nd == 2) return cell.Density;
            else return 0;
        }

        public double CalculateDensityPD(Cell cell) // Probability Density - toDie
        {
            var nd = cell.Neighbourhood.Sum(x => x.Density);
            var p = rg.NextDouble();
            if (nd == 3) return p > 0.001 ? 1 : 0;
            if (nd == 2) return p > 0.001 ? cell.Density : 1 - cell.Density;
            else if (nd == 1 || nd == 4) return p > 0.999 ? 1 : 0;
            else if (nd == 5) return p > 0.9999 ? 1 : 0;
            else return 0;
        }

        public double CalculateDensityPSt(Cell cell) // Probability Density - toStabilize
        {
            var nd = cell.Neighbourhood.Sum(x => x.Density);
            var p = rg.NextDouble();
            if (nd == 3) return p > 0.0001 ? 1 : 0;
            if (nd == 2) return p > 0.05 ? cell.Density : 1 - cell.Density;
            else if (nd == 1 || nd == 4) return p > 0.99 ? 1 : 0;
            else if (nd == 5) return p > 0.999 ? 1 : 0;
            else return 0;
        }

        public double CalculateDensityQ(Cell cell)
        {
            if (iterator / 200 % 2 == 0)
            {
                return CalculateDensityPD(cell);
            }
            else return CalculateDensityPSt(cell);
        }

        public double CalculateDensitySummer(Cell cell)
        {
            var nd = cell.Neighbourhood.Sum(x => x.Density);
            if (nd == 3) return 1;
            else if (nd == 2) return cell.Density;
            else return 0;
        }
        public double CalculateDensityWinter(Cell cell)
        {
            var nd = cell.Neighbourhood.Sum(x => x.Density);
            var r = rg.NextDouble();
            if (nd == 3 && r > 0.001) return 1;
            else if (nd == 2 && r >  0.01) return cell.Density; 
            else return 0;
        }
        public double CalculateDensityHeaven(Cell cell)
        {
            var nd = cell.Neighbourhood.Sum(x => x.Density);
            if (nd > 1 && nd < 4) return 1;
            else if (nd == 1 || nd == 4) return cell.Density;
            else return 0;
        }
        public double CalculateDensitySeasons(Cell cell)
        {
            if (cell.X < FieldWidth * 3.33) return CalculateDensityHeaven(cell);
            else if (cell.X < FieldWidth * 6.66) return CalculateDensityWinter(cell);
            else return CalculateDensitySummer(cell);
        }

        public void Update()
        {
            foreach (var cell in Field) Densities[cell] = CalculateDensitySeasons(cell);
            Dispatcher.Invoke(new Action(() =>
            {
                foreach (var cell in Field)
                {
                    cell.Density = Densities[cell];
                    FieldView[cell].Fill = (cell.Density == 1) ? Active : Inactive;
                }
            }));
        }

        #endregion
    }
}
