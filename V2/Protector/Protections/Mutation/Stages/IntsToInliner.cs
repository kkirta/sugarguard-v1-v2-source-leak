using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsToInliner
    {
        public MethodDef method { get; set; }
        public IntsToInliner(MethodDef method)
        {
            this.method = method;
        }
        static Random rnd = new Random();
        public void Execute(int i, int start)
        {
            Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
            method.Body.Variables.Add(local);
            int valor = method.Body.Instructions[i].GetLdcI4Value();
            method.Body.Instructions[i].OpCode = OpCodes.Ldloc;
            method.Body.Instructions[i].Operand = local;
            Instruction end = OpCodes.Nop.ToInstruction();
            method.Body.Instructions.Insert(start, Instruction.CreateLdcI4(rnd.Next(100, 500)));
            method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Stloc, local));
            method.Body.Instructions.Insert(++start, Instruction.CreateLdcI4(rnd.Next(1000, 5000)));
            method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Ldloc, local));
            method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Ceq));
            method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Brtrue, end));
            method.Body.Instructions.Insert(++start, Instruction.CreateLdcI4(valor));
            method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Stloc, local));
            method.Body.Instructions.Insert(++start, end);

        }
    }
}
