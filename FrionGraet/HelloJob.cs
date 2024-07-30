using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{
    [Service(ServiceName = "Test",Interceptor = typeof(DefaultIntercept) )]
    public class HelloJob : ICustomJob
    {
        public HelloJob()
        {
            Console.WriteLine("实列话构造方法!!!");
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void HelloWord()
        {

            Console.WriteLine("我是方法体开始执行我了");
        }
    }
}
