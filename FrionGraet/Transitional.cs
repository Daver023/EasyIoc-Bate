using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastIOC.Annotation
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Transitional : System.Attribute
    {
        public bool AutoRollBack { set; get; }
        public IsolationLevel TransitonLevel { set; get; }

        public Transitional()
        {
            AutoRollBack = true;
            TransitonLevel = IsolationLevel.ReadCommitted;
        }
    }
}
