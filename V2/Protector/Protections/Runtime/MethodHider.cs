using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Runtime
{
    internal static class MethodHider
    {
        public static void MethodHiderInj(object[] parameters, int token)
        {
            parameters.Reverse();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Assembly assemblycalling = Assembly.GetCallingAssembly();
            if (assembly == assemblycalling)
            {
                var module = assembly.ManifestModule;
                MethodInfo method = (MethodInfo)module.ResolveMethod(token);
                method.Invoke(null, parameters);
            }
        }
    }
}
