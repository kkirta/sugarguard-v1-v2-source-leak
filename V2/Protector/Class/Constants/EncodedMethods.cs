using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Class.Constants
{
    public class EncodedMethod
    {
        public MethodDef eMethod { get; private set; }
        public int eNum { get; private set; }
        public EncodedMethod(MethodDef method, int num)
        {
            eMethod = method;
            eNum = num;
        }
    }
}
