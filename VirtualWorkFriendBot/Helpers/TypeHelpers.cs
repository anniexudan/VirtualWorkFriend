using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace VirtualWorkFriendBot.Helpers
{
    public class TypeHelpers
    {        
        public static T Construct<T>(Type[] paramTypes, object[] paramValues)
        {
            Type t = typeof(T);

            ConstructorInfo ci = t.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, paramTypes, null);

            return (T)ci.Invoke(paramValues);
        }
        public static List<T> MakeList<T>(T itemOftype)
        {
            List<T> newList = new List<T>();
            return newList;
        }
    }
}
