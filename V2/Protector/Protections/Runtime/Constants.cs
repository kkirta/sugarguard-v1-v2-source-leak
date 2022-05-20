using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SugarGuard.Protector.Protections.Runtime
{
    public static class ConstantsRuntime
    {
        public static int[] tempField;
        public static string Decrypt(string s, int key, RuntimeMethodHandle runtimeMethodHandle)
        {
            StackTrace stackTrace = new StackTrace();
            MethodBase method = ((stackTrace != null) ? stackTrace.GetFrame(1) : null).GetMethod();
            if (method != null)
            {
                if (method.Name == "InvokeMethod")
                    goto end;
                byte[] bytearray = new byte[] { 0, 0, 0, 0, };
                if (method.MethodHandle == runtimeMethodHandle || method.Name == ".cctor")
                {
                    bytearray = MethodBase.GetMethodFromHandle(runtimeMethodHandle).GetMethodBody().GetILAsByteArray();
                }
                bool same = runtimeMethodHandle.Value == method.MethodHandle.Value || method.Name == ".cctor";
                int solved = bytearray.Length - (Convert.ToInt32(same) * 6);
                int metadataToken = (int)bytearray[solved++] | (int)bytearray[solved++] << 8 | (int)bytearray[solved++] << 16 | (int)bytearray[solved++] << 24;
                int sum = Convert.ToInt32(method.Module.ResolveString(metadataToken));
                key += sum;
                var array = tempField;
                for (int i = 0; i < array.Length; i++)
                {
                    key += array[i];
                }
                var charray = new char[s.Length];
                for (int j = 0; j < s.Length; j++)
                {
                    charray[j] = (char)(s[j] ^ key);
                }
                return new string(charray);
            }
        end:
            return method.Name;
        }

        //public static string Decrypt(string s, int key)
        //{
        //    var assembly = Assembly.GetExecutingAssembly();
        //    if (assembly == Assembly.GetCallingAssembly())
        //    {
        //        var module = assembly.ManifestModule;
        //        var field = module.ResolveField(777);
        //        var array = (int[])field.GetValue(null);
        //        for (int i = 0; i < array.Length; i++)
        //            key += array[i];
        //        var charray = new char[s.Length];
        //        for (int j = 0; j < s.Length; j++)
        //        {
        //            charray[j] = (char)(s[j] ^ key);
        //        }
        //        return new string(charray);
        //    }
        //    return assembly.FullName;
        //}
    }
}
