using FrionGraet;

namespace FastIOC.Builder
{
    public class RegisterEntity
    {

        public Type RegistType { set; get; }
        public Type RealType { set; get; }
        public String Name { set; get; }
        public bool IsEnableIntercept { set; get; }
        public bool IsInterceptAllMethod { set; get; }
        public Type InterceptType { set; get; }
        public Object EntityInstance { set; get; }

        public RegisterEntity(Type RegistType)
        {
            this.RegistType = RegistType;
            this.RealType = RegistType;
            this.Name = RegistType.Name;
            this.IsEnableIntercept = false;
            this.IsInterceptAllMethod = false;
        }

        public RegisterEntity As<T>()
        {
            this.RealType = typeof(T);
            this.Name = RealType.Name;
            return this;
        }

        public RegisterEntity Named<T>(string Name)
        {
            this.RealType = typeof(T);
            this.Name = Name;
            return this;
        }

        public RegisterEntity AsImplementedInterfaces()
        {
            Type[] BaseinterfaceList = this.RegistType.GetInterfaces();
            if (BaseinterfaceList.Count()>0)
	        {
                this.RealType = BaseinterfaceList[0];
                this.Name = RealType.Name;
            }
            else
            {
                throw new Exception("");
            }
            return this;
        }

        public RegisterEntity EnableIntercept()
        {
            this.IsEnableIntercept = true;
            this.IsInterceptAllMethod = true;
            return this;
        }

        public RegisterEntity InterceptedBy<T>() where T : IIntercept
        {
            this.InterceptType = typeof(T);
            return this;
        }

        public RegisterEntity UseAttributeIntercept()
        {
            this.IsInterceptAllMethod = false;
            return this;
        }
    }
}
