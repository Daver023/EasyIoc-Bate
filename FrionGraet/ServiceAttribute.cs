using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ServiceAttribute: Attribute
    {
        /// <summary>
        /// 定义获取服务的名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 拦截器
        /// </summary>
        public Type Interceptor { get; set; }
    }
}
