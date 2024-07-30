using FastIOC.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{

    [Component]
    public interface ICustomJob
    {
        void HelloWord();
    }
}
