using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SugarGuard.Protector.Class.MutationHelper;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class LocalsToCustomLocal
    {
        private MethodDef method { get; set; }
        public LocalsToCustomLocal(MethodDef method)
        {
            this.method = method;
        }

        public void Execute(ref int i)
        {
            switch (rnd.Next(0, 2))
            {
                case 0: PointerLocal(ref i); break;
                case 1: RefLocal(ref i); break;
            }
        }

        public void RefLocal(ref int i)
        {
            Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
            method.Body.Variables.Add(local);
            int defaultvalue = method.Body.Instructions[i].GetLdcI4Value();
            method.Body.Instructions[i].OpCode = OpCodes.Ldloc_S;
            method.Body.Instructions[i].Operand = local;
            method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(rnd.Next(100, 200)));
            method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
            method.Body.Instructions.Insert(2, OpCodes.Ldloca_S.ToInstruction(local));
            method.Body.Instructions.Insert(3, OpCodes.Mkrefany.ToInstruction(method.Module.CorLibTypes.Int32));
            method.Body.Instructions.Insert(4, OpCodes.Refanyval.ToInstruction(method.Module.CorLibTypes.Int32));
            method.Body.Instructions.Insert(5, OpCodes.Ldc_I4.ToInstruction(defaultvalue));
            method.Body.Instructions.Insert(6, OpCodes.Stind_I4.ToInstruction());
            i += 7;
        }

        static Random rnd = new Random();

        public void PointerLocal(ref int i)
        {
            int defaultvalue = method.Body.Instructions[i].GetLdcI4Value();
            Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
            Local locali = new Local(method.Module.ImportAsTypeSig(typeof(int*)));
            method.Body.Variables.Add(local);
            method.Body.Variables.Add(locali);
            method.Body.Instructions[i] = OpCodes.Ldloc_S.ToInstruction(local);
            method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(rnd.Next()));
            method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
            method.Body.Instructions.Insert(2, OpCodes.Ldloca_S.ToInstruction(local));
            method.Body.Instructions.Insert(3, OpCodes.Conv_U.ToInstruction());
            method.Body.Instructions.Insert(4, OpCodes.Stloc_S.ToInstruction(locali));
            method.Body.Instructions.Insert(5, OpCodes.Ldloc_S.ToInstruction(locali));
            method.Body.Instructions.Insert(6, OpCodes.Ldc_I4.ToInstruction(defaultvalue));
            method.Body.Instructions.Insert(7, OpCodes.Stind_I4.ToInstruction());
            i += 8;
        }
    }
}
