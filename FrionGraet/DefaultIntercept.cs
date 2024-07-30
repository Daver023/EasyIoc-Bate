using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{
    public class DefaultIntercept : IIntercept
    {
        public void Before(object @object, string MethodName, object[] Parameters)
        {
            Console.WriteLine($"{DateTime.Now}我是在方法之前执行的");
        }

        public void After(object @object, string MethodName, object Result, DateTime Start, DateTime End)
        {
            Console.WriteLine($"{DateTime.Now}我是在方法之后执行的");
        }
    }
}
