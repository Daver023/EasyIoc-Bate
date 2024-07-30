using FastIOC.Util;
using FrionGraet;
using System.Reflection;

namespace FastIOC.Builder
{
    public class MutiRegisterEntity
    {
        public List<RegisterEntity> RegisterEntityList { set; get; }
        public Type BaseType { set; get; }
        public MutiRegisterEntity(Assembly[] AssemblyList, Type BaseType, bool IsAttribute = false)
        {
            RegisterEntityList = new List<RegisterEntity>();
            this.BaseType = BaseType;
            foreach (Assembly assItem in AssemblyList)
            {
                Type[] typelist = null;
                if (IsAttribute)
                {
                    typelist = assItem.GetTypes().Where(a => a.GetCustomAttribute(BaseType, true) != null).ToArray();
                }
                else
                {
                    typelist = assItem.GetTypes().Where(a => CompareUtil.IsAssignableFrom(a, BaseType) && a != BaseType).ToArray();
                }
                foreach (Type typeItem in typelist)
                {
                    RegisterEntity RE = new RegisterEntity(typeItem);
                    RegisterEntityList.Add(RE);
                }
            }

        }

        public MutiRegisterEntity AsImplementedInterfaces()
        {
            foreach (RegisterEntity item in RegisterEntityList)
            {
                item.AsImplementedInterfaces();
            }
            return this;
        }

        public MutiRegisterEntity EnableInterfaceInterceptors()
        {
            foreach (RegisterEntity item in RegisterEntityList)
            {
                item.EnableIntercept();
            }
            return this;
        }

        public MutiRegisterEntity InterceptedBy<T>() where T : IIntercept
        {
            foreach (RegisterEntity item in RegisterEntityList)
            {
                item.InterceptedBy<T>();
            }
            return this;
        }

        public MutiRegisterEntity UseAttributeIntercept()
        {
            foreach (RegisterEntity item in RegisterEntityList)
            {
                item.UseAttributeIntercept();
            }
            return this;
        }



    }
}
