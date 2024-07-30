using FastIOC.Annotation;
using FastIOC.Desgin;
using FastIOC.Proxy;
using FastIOC.Util;
using FrionGraet;
using System.ComponentModel;
using System.Reflection;

namespace FastIOC.Builder
{
    public class ContainerBuilder
    {
        private static ContainerBuilder Container { set; get; }

        private static object SingleLock = new object(); //锁同步
        private Dictionary<Type, RegisterEntity> RegistDic { set; get; }
        private Dictionary<Type, List<RegisterEntity>> RegistResultDic { set; get; }
        private Dictionary<Type, Object> SingleInstanceDic { set; get; }
        private List<MutiRegisterEntity> MutiRegisterEntityList { set; get; }

        public static ContainerBuilder GetInstance()
        {
            if (Container == null)
            {
                lock (SingleLock)
                {
                    if (Container == null)
                    {
                        Container = new ContainerBuilder();
                    }
                }
            }
            return Container;

        }

        public ContainerBuilder()
        {
            RegistDic = new Dictionary<Type, RegisterEntity>();
            RegistResultDic = new Dictionary<Type, List<RegisterEntity>>();
            MutiRegisterEntityList = new List<MutiRegisterEntity>();
            SingleInstanceDic = new Dictionary<Type, object>();
        }
        public RegisterEntity RegisterType<T>()
        {
            RegisterEntity RE = new RegisterEntity(typeof(T));
            if (!RegistDic.Keys.Contains(RE.RegistType))
            {
                RegistDic.Add(typeof(T), RE);
            }
            else
            {
                throw new Exception("");
            }

            return RE;
        }

        public MutiRegisterEntity RegisterAssemblyTypes(Assembly[] AssemblyList, Type BaseType, bool IsAttribute = false)
        {
            MutiRegisterEntity MutilEntity = new MutiRegisterEntity(AssemblyList, BaseType, IsAttribute);
            MutiRegisterEntityList.Add(MutilEntity);
            return MutilEntity;
        }

        public T Resolve<T>()
        {
            List<RegisterEntity> ValueList = RegistResultDic[typeof(T)];
            return (T)GetInstance(ValueList[ValueList.Count - 1]);
        }

        public T ResolveNamed<T>(string Name)
        {
            List<RegisterEntity> ValueList = RegistResultDic[typeof(T)].Where(a => a.Name == Name).ToList<RegisterEntity>();
            return (T)GetInstance(ValueList[ValueList.Count - 1]);
        }

        public Object Resolve(Type @Type, CotainerEnum.TypeEqual TypeEqual = CotainerEnum.TypeEqual.Ref)
        {
            List<RegisterEntity> ValueList = null;
            if (TypeEqual == CotainerEnum.TypeEqual.Ref)
            {
                ValueList = RegistResultDic[@Type];
            }
            else
            {
                foreach (var KeyPair in RegistResultDic)
                {
                    if (KeyPair.Key.FullName == @Type.FullName)
                    {
                        ValueList = KeyPair.Value;
                        break;
                    }
                }
            }

            return GetInstance(ValueList[ValueList.Count - 1]);
        }


        public Object ResolveAsClassProxyModel(Type @Type)
        {

            return GetInstance(new RegisterEntity(@Type), null, true);
        }


        /// <summary>
        /// 根据类型和泛型类型，获取泛型对象实例
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="GenericTypeArguments"></param>
        /// <param name="TypeEqual"></param>
        /// <returns></returns>

        public Object ResolveGeneric(Type @Type, Type[] GenericTypeArguments, CotainerEnum.TypeEqual TypeEqual = CotainerEnum.TypeEqual.Ref)
        {
            List<RegisterEntity> ValueList = null;
            if (TypeEqual == CotainerEnum.TypeEqual.Ref)
            {
                ValueList = RegistResultDic[@Type];
            }
            else
            {
                foreach (var KeyPair in RegistResultDic)
                {
                    if (KeyPair.Key.FullName == @Type.FullName)
                    {
                        ValueList = KeyPair.Value;
                        break;
                    }
                }
            }

            return GetInstance(ValueList[ValueList.Count - 1], GenericTypeArguments);
        }

        public List<Type> GetRegistType(Type @Type)
        {
            List<Type> result = new List<Type>();
            foreach (var item in RegistDic.Keys)
            {
                if (CompareUtil.IsAssignableFrom(item, @Type))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public bool Contains(Type @Type)
        {
            return RegistResultDic.ContainsKey(@Type);
        }

        private Object GetInstance(RegisterEntity Entity, Type[] GenericTypeArguments = null, bool HandleAsClassProxy = false)
        {
            Object obj = null;
            if (Entity.IsEnableIntercept)
            {
                if (HandleAsClassProxy)
                {
                    obj = DynamictProxy.CreateProxyObject(Entity.RegistType, Entity.RegistType, Entity.InterceptType, true, Entity.IsInterceptAllMethod, true);
                }
                else
                {
                    bool IsExtend = Entity.RealType == Entity.RegistType;
                    obj = DynamictProxy.CreateProxyObject(Entity.RealType, Entity.RegistType, Entity.InterceptType, IsExtend, Entity.IsInterceptAllMethod, true);
                }

            }
            else
            {
                if (Entity.RegistType.IsGenericType && GenericTypeArguments != null)
                {
                    //这里的泛型对象实例化只做到第一级，如果泛型class中还有嵌套泛型自动泛型注入属性，不保证实例化成功
                    var GenericType = Entity.RegistType.MakeGenericType(GenericTypeArguments);
                    obj = Activator.CreateInstance(GenericType);
                }
                else
                {
                    var constructors = Entity.RegistType.GetConstructors();
                    obj = constructors[0].Invoke(new Object[] { });
                }
            }
            //这里使用单例模式将实例化Instance存储,提前暴露未进行后续设置的对象实例
            if (!SingleInstanceDic.ContainsKey(Entity.RealType))
            {
                SingleInstanceDic.Add(Entity.RealType, obj);
            }

            //如果这个class标记了Component，且有标记了AutoWired的Field，进行自动注入
            if (Entity.RealType.GetCustomAttribute(typeof(FastIOC.Annotation.Component), true) != null)
            {
                //这里要使用GetRuntimeFields，此方法返回在指定类型上定义的所有字段，包括继承，非公共，实例和静态字段。
                List<FieldInfo> FiledList = new List<FieldInfo>();
                if (Entity.RegistType == Entity.RealType)
                {
                    FiledList = Entity.RegistType.GetRuntimeFields().ToList();
                }
                foreach (FieldInfo Field in FiledList)
                {
                    if (Field.GetCustomAttribute(typeof(AutoWired), true) != null)
                    {
                        Type FieldType = Field.FieldType;
                        if (Contains(FieldType))
                        {
                            //判断单例存储中是否包含，如果有，取出赋值，这里可以防止循环依赖导致的死循环
                            if (Entity.RegistType.IsGenericType)
                            {
                                //泛型对象设置Field
                                var GenericType = Entity.RegistType.MakeGenericType(GenericTypeArguments);
                                if (SingleInstanceDic.ContainsKey(FieldType))
                                {
                                    GenericType.InvokeMember(Field.Name, BindingFlags.SetField, null, obj, new object[] { SingleInstanceDic[FieldType] });
                                }
                                else
                                {
                                    GenericType.InvokeMember(Field.Name, BindingFlags.SetField, null, obj, new object[] { Resolve(FieldType) });
                                }

                            }
                            else
                            {
                                if (SingleInstanceDic.ContainsKey(FieldType))
                                {
                                    Field.SetValue(obj, SingleInstanceDic[FieldType]);
                                }
                                else
                                {
                                    Field.SetValue(obj, Resolve(FieldType));
                                }
                            }
                        }
                    }
                }
            }
            return obj;

        }

        public ContainerBuilder Build(CotainerEnum.BuidlModel Model = CotainerEnum.BuidlModel.NoRepeat)
        {

            //combine RegistDic MutiRegisterEntityList
            foreach (MutiRegisterEntity mutiitem in MutiRegisterEntityList)
            {
                List<RegisterEntity> list = mutiitem.RegisterEntityList;
                foreach (RegisterEntity item in list)
                {
                    if (!RegistDic.Keys.Contains(item.RegistType))
                    {
                        RegistDic.Add(item.RegistType, item);
                    }
                    else
                    {
                        if (Model == CotainerEnum.BuidlModel.NoRepeat)
                        {
                            throw new Exception("");
                        }
                        else
                        {
                            RegistDic[item.RegistType] = item;
                        }

                    }

                }
            }
            RegisterEntity[] ValueList = new RegisterEntity[RegistDic.Values.Count];
            RegistDic.Values.CopyTo(ValueList, 0);
            List<RegisterEntity> ValueConvertList = ValueList.ToList<RegisterEntity>();
            foreach (IGrouping<Type, RegisterEntity> group in ValueConvertList.GroupBy(a => a.RealType))
            {
                List<RegisterEntity> grouplist = new List<RegisterEntity>();
                foreach (RegisterEntity entity in group)
                {
                    grouplist.Add(entity);
                }
                if (!RegistResultDic.ContainsKey(group.Key))
                {
                    RegistResultDic.Add(group.Key, grouplist);
                }
                else
                {
                    RegistResultDic[group.Key] = grouplist;
                }

            }
            return this;
        }
    }
}
