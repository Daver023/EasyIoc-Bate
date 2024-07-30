using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{
    public class CustomComparer : IComparable<string>
    {

        public static CustomComparer Instance = new CustomComparer();

        public int CompareTo(string? other)
        {
            throw new NotImplementedException();
        }
    }
}
