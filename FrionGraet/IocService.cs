using FastIOC.Proxy;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace FrionGraet
{
    public class IocService
    {
        public static ConcurrentDictionary<string, object> serviceContainer = new ConcurrentDictionary<string, object>();

        public static void RegistService(Type t)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetInterfaces().Contains(t)).ToList();
            //var types = AssemblyLoadContext.Default.Assemblies.SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(t))).ToArray();
            foreach (var type in types)
            {
                var iocServiceAttr = (ServiceAttribute)type.GetCustomAttribute(typeof(ServiceAttribute), true);
                if (iocServiceAttr is not null)
                {
                    var instance = DynamictProxy.CreateProxyObject(t, type, iocServiceAttr.Interceptor);
                    serviceContainer.GetOrAdd(iocServiceAttr.ServiceName, instance);
                }
            }
        }

        public static T GetSingleton<T>(string serviceName)
        {
            if (serviceContainer.TryGetValue(serviceName, out var _service))
            {

                return (T)_service;
            }
            throw new Exception("没有注册!!!");
        }
    }
}
