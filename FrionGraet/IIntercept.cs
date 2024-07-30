using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{
    public interface IIntercept
    {
        void Before(object @object, string MethodName, object[] Parameters);
        void After(object @object, string MethodName, object Result, DateTime Start, DateTime End);
    }
}
