namespace FastIOC.Util
{
    public class CompareUtil
    {
        public static bool IsAssignableFrom(Type @Type, Type @BaseType)
        {
            bool Flag = false;
            if (!@BaseType.IsGenericType)
            {
                Flag = @BaseType.IsAssignableFrom(@Type);

            }
            else
            {
                Type[] Interfaces = @Type.GetInterfaces();
                foreach (Type item in Interfaces)
                {
                    if (item.IsGenericType)
                    {
                        var t = item.GetGenericTypeDefinition();
                        if (t == @BaseType)
                        {
                            Flag = true;
                            break;
                        }
                    }
                }
            }

            return Flag;
        }
    }
}
