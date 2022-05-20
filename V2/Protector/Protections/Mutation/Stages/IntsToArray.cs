using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsToArray
    {
        public MethodDef method { get; set; }
        public IntsToArray(MethodDef method)
        {
            this.method = method;
        }

        public void Execute(int i, int start)
        {
            Local local = new Local(method.Module.ImportAsTypeSig(typeof(int[])));
            method.Body.Variables.Add(local);
            int valor = method.Body.Instructions[i].GetLdcI4Value();
            method.Body.Instructions[i].OpCode = OpCodes.Ldloc_S;
            method.Body.Instructions[i].Operand = local;
            method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4_0.ToInstruction());
            method.Body.Instructions.Insert(i + 2, OpCodes.Ldelem_I4.ToInstruction());
            method.Body.Instructions.Insert(start, OpCodes.Ldc_I4_1.ToInstruction());
            method.Body.Instructions.Insert(++start, OpCodes.Newarr.ToInstruction(method.Module.CorLibTypes.Int32));
            method.Body.Instructions.Insert(++start, OpCodes.Dup.ToInstruction());
            method.Body.Instructions.Insert(++start, OpCodes.Ldc_I4_0.ToInstruction());
            method.Body.Instructions.Insert(++start, OpCodes.Ldc_I4.ToInstruction(valor));
            method.Body.Instructions.Insert(++start, OpCodes.Stelem_I4.ToInstruction());
            method.Body.Instructions.Insert(++start, OpCodes.Stloc_S.ToInstruction(local));

        }
    }
}
