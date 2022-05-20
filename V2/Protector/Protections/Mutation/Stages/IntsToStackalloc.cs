using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SugarGuard.Protector.Class.MutationHelper;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    class IntsToStackalloc
    {
        private MethodDef method { get; set; }
        private Local local;
        private List<Instruction> toInject = new List<Instruction>();
        private int offset = 0;
        public IntsToStackalloc(MethodDef method)
        {
            this.method = method;
            Create();
        }

        public void Create()
        {
            local = new Local(method.Module.Import(typeof(int*)).ToTypeSig());
            method.Body.Variables.Add(local);
            toInject.Add(OpCodes.Ldc_I4.ToInstruction(4));
            toInject.Add(OpCodes.Conv_U.ToInstruction());
            toInject.Add(OpCodes.Localloc.ToInstruction());
        }
        public void Execute(ref int i)
        {
            int valor = method.Body.Instructions[i].GetLdcI4Value();
            toInject.Add(OpCodes.Dup.ToInstruction());
            toInject.Add(OpCodes.Ldc_I4.ToInstruction(offset * 4));
            toInject.Add(OpCodes.Add.ToInstruction());
            toInject.Add(OpCodes.Ldc_I4.ToInstruction(valor));
            toInject.Add(OpCodes.Stind_I4.ToInstruction());
            method.Body.Instructions[i] = OpCodes.Ldloc_S.ToInstruction(local);
            method.Body.Instructions.Insert(++i, OpCodes.Ldc_I4.ToInstruction(offset++ * 4));
            method.Body.Instructions.Insert(++i, OpCodes.Add.ToInstruction());
            method.Body.Instructions.Insert(++i, OpCodes.Ldind_I4.ToInstruction());
        }

        public void Inject()
        {
            int index = offset * 4;
            if (index != 0)
            {
                toInject[0] = OpCodes.Ldc_I4.ToInstruction(index);
                if (toInject.Last().OpCode == OpCodes.Dup)
                {
                    toInject.RemoveAt(toInject.Count - 1);
                }
                toInject.Add(OpCodes.Stloc_S.ToInstruction(local));
                for (int i = 0; i < toInject.Count; i++)
                {
                    method.Body.Instructions.Insert(i, toInject[i]);
                }
            }
        }
    }
}