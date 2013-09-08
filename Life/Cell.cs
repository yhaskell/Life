using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Life
{
    public class Cell
    {
        public double X { get; set; }
        public double Y { get; set; }

        public IEnumerable<Cell> Neighbourhood { get; set; }

        public double Density { get; set; }
    }
}
