using FastIOC.Annotation;
using FastIOC.Builder;
using FrionGraet;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace FastIOC.Proxy
{
    public class DynamictProxy
    {

        public static Object CreateProxyObject(Type InterfaceType, Type ImpType)
        {
            return CreateProxyObject(InterfaceType, ImpType, typeof(DefaultIntercept));
        }

        public static Object CreateProxyObject(Type InterfaceType, Type ImpType, Type InterceptType, bool inheritMode = false, bool isInterceptAllMethod = true, bool IsSaveDll = false)
        {
            string nameOfAssembly = ImpType.Name + "ProxyAssembly";
            string nameOfModule = ImpType.Name + "ProxyModule";
            string nameOfType = ImpType.Name + "Proxy";

            var assemblyName = new AssemblyName(nameOfAssembly);
            ModuleBuilder moduleBuilder = null;
            AssemblyBuilder assemblyBuilder = null;
            if (IsSaveDll)
            {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,AssemblyBuilderAccess.RunAndCollect);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(nameOfModule);
            }
            else
            {
                var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                moduleBuilder = assembly.DefineDynamicModule(nameOfModule);
            }



            TypeBuilder typeBuilder;
            if (inheritMode)
            {
                typeBuilder = moduleBuilder.DefineType(
              nameOfType, TypeAttributes.Public, ImpType);
            }
            else
            {
                typeBuilder = moduleBuilder.DefineType(
              nameOfType, TypeAttributes.Public, null, new[] { InterfaceType });
            }

            InjectInterceptor(typeBuilder, InterfaceType, ImpType, InterceptType, inheritMode, isInterceptAllMethod);
            Type t = typeBuilder.CreateType();
            if (IsSaveDll)
            {
               // assemblyBuilder.CreateInstance(nameOfType + ".dll");
            }
            return Activator.CreateInstance(t);
        }

        private static void InjectInterceptor(TypeBuilder typeBuilder, Type InterfaceType, Type ImpType, Type InterceptType, bool inheritMode, bool isInterceptAllMethod)
        {
            ConstructorInfo objCtor = ImpType.GetConstructor(new Type[0]);
            Type[] constructorArgs = { };

            // ---- 变量定义 ----
            var constructorBuilder = typeBuilder.DefineConstructor(
           MethodAttributes.Public, CallingConventions.Standard, constructorArgs);
            var ilOfCtor = constructorBuilder.GetILGenerator();


            //---- 拦截类对象定义 ----
            //声明
            if (InterceptType == null)
            {
                InterceptType = typeof(DefaultIntercept);
            }
            var fieldInterceptor = typeBuilder.DefineField(
               "_interceptor", InterceptType, FieldAttributes.Private);
            //赋值
            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Newobj, InterceptType.GetConstructor(new Type[0]));
            ilOfCtor.Emit(OpCodes.Stfld, fieldInterceptor);

            //---- 实现类对象定义 ----
            //FieldBuilder fieldBeProxy = typeBuilder.DefineField(
            //"_beproxy", ImpType, FieldAttributes.Private);
            ////---- 调用构造函数 ----
            //ilOfCtor.Emit(OpCodes.Ldarg_0);
            //ilOfCtor.Emit(OpCodes.Call, objCtor);

            //ilOfCtor.Emit(OpCodes.Ret);

            //声明
            FieldBuilder fieldBeProxy = null;
            if (inheritMode)
            {
                fieldBeProxy = typeBuilder.DefineField(
               "_beproxy", ImpType, FieldAttributes.Private);
                //---- 调用构造函数 ----
                ilOfCtor.Emit(OpCodes.Ldarg_0);
                ilOfCtor.Emit(OpCodes.Call, objCtor);

                ilOfCtor.Emit(OpCodes.Ret);
            }
            else
            {
                fieldBeProxy = typeBuilder.DefineField(
             "_beproxy", ImpType, FieldAttributes.Private);
                //---- 注入 ----
                ilOfCtor.Emit(OpCodes.Ldarg_0);
                ilOfCtor.Emit(OpCodes.Call, typeof(System.Object).GetConstructor(new Type[0]));
                ilOfCtor.Emit(OpCodes.Ldarg_0);
                ilOfCtor.Emit(OpCodes.Call, typeof(ContainerBuilder).GetMethod("GetInstance"));
                ilOfCtor.Emit(OpCodes.Ldtoken, ImpType);
                ilOfCtor.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                ilOfCtor.Emit(OpCodes.Callvirt, typeof(ContainerBuilder).GetMethod("ResolveAsClassProxyModel"));
                ilOfCtor.Emit(OpCodes.Castclass, ImpType);
                ilOfCtor.Emit(OpCodes.Stfld, fieldBeProxy);
                ilOfCtor.Emit(OpCodes.Ret);
            }



            // ---- 定义类中的方法 ----

            var methodsOfType = ImpType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(a => a.IsVirtual).ToArray();
            string[] ignoreMethodName = new[] { "GetType", "ToString", "GetHashCode", "Equals" };
            for (var i = 0; i < methodsOfType.Length; i++)
            {

                bool IsMethodIntercept = true;
                bool IsTransitinal = false;
                var method = methodsOfType[i];
                //---- 过滤Object基类方法,如果是继承，基类中的属性get，set方法也要过滤 ----
                if (ignoreMethodName.Contains(method.Name) || method.Name.StartsWith("set_") || method.Name.StartsWith("get_"))
                {
                    continue;
                }
#if false

                // ---- 判断方法是否需要拦截 ----
                if (isInterceptAllMethod)
                {
                    if (method.GetCustomAttribute(typeof(IgnoreInterceptAttibute)) == null)
                    {
                        IsMethodIntercept = true;
                    }

                }
                else
                {
                    if (method.GetCustomAttribute(typeof(InterceptAttibute)) != null)
                    {
                        IsMethodIntercept = true;
                    }

                }
#endif

                // ---- 判断方法是否需要纳入事物管理中 ----
                if (method.GetCustomAttribute(typeof(Transitional)) != null)
                {
                    IsTransitinal = true;
                }

                var methodParameterTypes =
                   method.GetParameters().Select(p => p.ParameterType).ToArray();

                // ---- 定义方法名与参数 ----
                MethodAttributes methodAttributes;
                if (inheritMode)
                {
                    methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual;
                }
                else
                {
                    methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
                }
                var methodBuilder = typeBuilder.DefineMethod(method.Name, methodAttributes, CallingConventions.Standard, method.ReturnType, methodParameterTypes);

                //如果是泛型方法
                if (method.IsGenericMethod)
                {
                    //获取所有泛型参数类型定义
                    Type[] Args = method.GetGenericArguments();
                    List<string> GenericArgNames = new List<string>();
                    for (int j = 0; j < Args.Length; j++)
                    {
                        GenericArgNames.Add(Args[j].Name);
                    }
                    //代理类方法生成泛型参数定义
                    GenericTypeParameterBuilder[] DGP = methodBuilder.DefineGenericParameters(GenericArgNames.ToArray());
                    //泛型参数约束设置
                    for (int j = 0; j < DGP.Length; j++)
                    {
                        //泛型参数继承约束
                        DGP[j].SetBaseTypeConstraint(Args[j].BaseType);
                        //泛型参数完成接口约束
                        DGP[j].SetInterfaceConstraints(Args[j].GetInterfaces());
                    }

                }

                var ilOfMethod = methodBuilder.GetILGenerator();
                var methodresult = ilOfMethod.DeclareLocal(typeof(object));  //instance of result
                //赋值方法结果对象初始值为null
                ilOfMethod.Emit(OpCodes.Ldnull);
                ilOfMethod.Emit(OpCodes.Stloc_0);

#if false

                if (IsTransitinal)
                {
                    //开始try模块
                    ilOfMethod.BeginExceptionBlock();
                    //调用TransitionManage.StartTransition
                    //获取事物隔离级别，默认读已提交
                    Transitional transition = (Transitional)method.GetCustomAttribute(typeof(Transitional));
                    ilOfMethod.Emit(OpCodes.Ldc_I4, (Int32)transition.TransitonLevel);
                    ilOfMethod.Emit(OpCodes.Call, typeof(TransitionManage).GetMethod("StartTransition", new Type[] { typeof(IsolationLevel) }));
                    ilOfMethod.Emit(OpCodes.Ldarg_0);
                }
#endif

                // ---- before ----
                if (IsMethodIntercept)
                {

                    var parameters = ilOfMethod.DeclareLocal(typeof(object[]));
                    ilOfMethod.Emit(OpCodes.Ldc_I4, methodParameterTypes.Length);
                    ilOfMethod.Emit(OpCodes.Newarr, typeof(object));
                    ilOfMethod.Emit(OpCodes.Stloc, parameters);

                    for (var j = 0; j < methodParameterTypes.Length; j++)
                    {
                        ilOfMethod.Emit(OpCodes.Ldloc, parameters);
                        ilOfMethod.Emit(OpCodes.Ldc_I4, j);
                        ilOfMethod.Emit(OpCodes.Ldarg, j + 1);
                        ilOfMethod.Emit(OpCodes.Stelem_Ref);
                    }


                    ilOfMethod.Emit(OpCodes.Ldarg_0);
                    ilOfMethod.Emit(OpCodes.Ldfld, fieldInterceptor);

                    //拦截方法参数赋值
                    if (inheritMode)
                    {
                        //继承传递代理类本身
                        ilOfMethod.Emit(OpCodes.Ldarg_0);
                    }
                    else
                    {
                        //接口传递实现类
                        ilOfMethod.Emit(OpCodes.Ldarg_0);
                        ilOfMethod.Emit(OpCodes.Ldfld, fieldBeProxy);
                    }

                    ilOfMethod.Emit(OpCodes.Ldstr, method.Name);
                    ilOfMethod.Emit(OpCodes.Ldloc, parameters);
                    //调用拦截类中的Before方法
                    ilOfMethod.Emit(OpCodes.Callvirt, InterceptType.GetMethod("Before"));

                }


                // ---- call ----
                ////定义实现类局部变量
                //var localimpobj = ilOfMethod.DeclareLocal(ImpType);
                ////new一个实现类的对象
                //ilOfMethod.Emit(OpCodes.Newobj, ImpType.GetConstructor(new Type[0]));
                ////局部变量赋值
                //ilOfMethod.Emit(OpCodes.Stloc, localimpobj);

                ////局部变量出栈，等待调用
                //ilOfMethod.Emit(OpCodes.Ldloc, localimpobj);

                //方法执行开始时间
                var localstart = ilOfMethod.DeclareLocal(typeof(DateTime));
                ilOfMethod.Emit(OpCodes.Call, typeof(DateTime).GetMethod("get_Now"));
                ilOfMethod.Emit(OpCodes.Stloc, localstart);



                if (inheritMode)
                {
                    //继承方法调用父类的方法
                    ilOfMethod.Emit(OpCodes.Ldarg_0);
                    for (var j = 0; j < methodParameterTypes.Length; j++)
                    {
                        ilOfMethod.Emit(OpCodes.Ldarg, j + 1);
                    }
                    ilOfMethod.Emit(OpCodes.Call, ImpType.GetMethod(method.Name));
                }
                else
                {
                    //接口方法调用实现类的方法
                    ilOfMethod.Emit(OpCodes.Ldarg_0);
                    ilOfMethod.Emit(OpCodes.Ldfld, fieldBeProxy);
                    for (var j = 0; j < methodParameterTypes.Length; j++)
                    {
                        ilOfMethod.Emit(OpCodes.Ldarg, j + 1);
                    }
                    //调用方法
                    ilOfMethod.Emit(OpCodes.Callvirt, InterfaceType.GetMethod(method.Name));
                }



                var localend = ilOfMethod.DeclareLocal(typeof(DateTime));
                ilOfMethod.Emit(OpCodes.Call, typeof(DateTime).GetMethod("get_Now"));
                ilOfMethod.Emit(OpCodes.Stloc, localend);

                // ---- after ----
                if (IsMethodIntercept)
                {
                    if (method.ReturnType == typeof(void))
                    {
                        ilOfMethod.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        ilOfMethod.Emit(OpCodes.Box, method.ReturnType);
                    }

                    ilOfMethod.Emit(OpCodes.Stloc, methodresult);

                    ilOfMethod.Emit(OpCodes.Ldarg_0);
                    ilOfMethod.Emit(OpCodes.Ldfld, fieldInterceptor);


                    //拦截方法参数赋值
                    if (inheritMode)
                    {
                        //继承传递代理类本身
                        ilOfMethod.Emit(OpCodes.Ldarg_0);
                    }
                    else
                    {
                        //接口传递实现类
                        ilOfMethod.Emit(OpCodes.Ldarg_0);
                        ilOfMethod.Emit(OpCodes.Ldfld, fieldBeProxy);
                    }

                    ilOfMethod.Emit(OpCodes.Ldstr, method.Name);
                    ilOfMethod.Emit(OpCodes.Ldloc, methodresult);
                    ilOfMethod.Emit(OpCodes.Ldloc, localstart);
                    ilOfMethod.Emit(OpCodes.Ldloc, localend);
                    ilOfMethod.Emit(OpCodes.Callvirt, InterceptType.GetMethod("After"));
                }

#if false

                if (IsTransitinal)
                {
                    //调用TransitionManage.DoCommit
                    ilOfMethod.Emit(OpCodes.Pop);
                    ilOfMethod.Emit(OpCodes.Call, typeof(TransitionManage).GetMethod("DoCommit"));
                    ilOfMethod.Emit(OpCodes.Ldarg_0);


                    //开始catch模块
                    ilOfMethod.BeginCatchBlock(typeof(Exception));
                    //调用TransitionManage.DoRollBack
                    Transitional transition = (Transitional)method.GetCustomAttribute(typeof(Transitional));
                    if (transition.AutoRollBack)
                    {
                        ilOfMethod.Emit(OpCodes.Pop);
                        ilOfMethod.Emit(OpCodes.Call, typeof(TransitionManage).GetMethod("DoRollBack"));
                        //throw异常
                        ilOfMethod.Emit(OpCodes.Rethrow);
                    }
                    else
                    {
                        //throw异常
                        ilOfMethod.Emit(OpCodes.Pop);
                        ilOfMethod.Emit(OpCodes.Rethrow);
                    }


                    //结束异常块
                    ilOfMethod.EndExceptionBlock();
                }
#endif

                //调用完After方法后,将实现类的方法返回值的临时变量再次压栈用作代理方法的返回
                ilOfMethod.Emit(OpCodes.Ldloc, methodresult);

                // pop the stack if return void
                if (method.ReturnType == typeof(void))
                {
                    ilOfMethod.Emit(OpCodes.Pop);
                }
                else
                {
                    if (method.ReturnType.IsValueType)
                    {
                        ilOfMethod.Emit(OpCodes.Unbox_Any, method.ReturnType);
                    }
                    else
                    {
                        ilOfMethod.Emit(OpCodes.Castclass, method.ReturnType);
                    }
                }

                // complete
                ilOfMethod.Emit(OpCodes.Ret);
            }
        }
    }
}
