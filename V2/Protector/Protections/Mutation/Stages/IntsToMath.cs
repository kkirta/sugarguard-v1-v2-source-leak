using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsToMath
    {
        private MethodDef Method { get; set; }
        private static Random rnd = new Random();
        public IntsToMath(MethodDef method)
        {
            Method = method;
        }

        public void Execute(ref int i)
        {
            switch (rnd.Next(0, 7))
            {
                case 0: Neg(ref i); break;
                case 1: Not(ref i); break;
                case 2: Shr(ref i); break;
                case 3: Shl(ref i); break;
                case 4: Or(ref i); break;
                case 5: Rem(ref i); break;
                case 6: ConditionalMath(ref i); break;
                case 7: Add(ref i); break;
                case 8: Sub(ref i); break;
                case 9: Xor(ref i); break;
            }
        }

        private void Add(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int total = value - random;
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = total;
            Method.Body.Instructions.Insert(++i, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(++i, OpCodes.Add.ToInstruction());
        }

        private void Xor(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int total = value ^ random;
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = total;
            Method.Body.Instructions.Insert(++i, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(++i, OpCodes.Xor.ToInstruction());
        }

        private void Sub(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int total = value + random;
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = total;
            Method.Body.Instructions.Insert(++i, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(++i, OpCodes.Sub.ToInstruction());
        }

        private void Neg(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int nr = -random;
            Calculator calculator = new Calculator(value, nr);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = calculator.getResult();
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(i + 2, OpCodes.Neg.ToInstruction());
            Method.Body.Instructions.Insert(i + 3, calculator.getOpCode().ToInstruction());
            i += 3;
        }

        private void Rem(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int random2 = rnd.Next(10000, 50000);
            int nr = random2 % random;
            Calculator calculator = new Calculator(value, nr);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = calculator.getResult();
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(random2));
            Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(i + 3, OpCodes.Rem.ToInstruction());
            Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
            i += 4;
        }
        private void Not(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int nr = ~random;
            Calculator calculator = new Calculator(value, nr);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = calculator.getResult();
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(i + 2, OpCodes.Not.ToInstruction());
            Method.Body.Instructions.Insert(i + 3, calculator.getOpCode().ToInstruction());
            i += 3;
        }

        private void Shl(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int random2 = rnd.Next(10000, 50000);
            int nr = random2 << random;
            Calculator calculator = new Calculator(value, nr);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = calculator.getResult();
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(random2));
            Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(i + 3, OpCodes.Shl.ToInstruction());
            Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
            i += 4;
        }

        private void Or(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int random2 = rnd.Next(10000, 50000);
            int nr = random2 | random;
            Calculator calculator = new Calculator(value, nr);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = calculator.getResult();
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(random2));
            Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(i + 3, OpCodes.Or.ToInstruction());
            Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
            i += 4;
        }

        private void Shr(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            int random = rnd.Next(10000, 50000);
            int random2 = rnd.Next(10000, 50000);
            int nr = random2 >> random;
            Calculator calculator = new Calculator(value, nr);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = calculator.getResult();
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(random2));
            Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(random));
            Method.Body.Instructions.Insert(i + 3, OpCodes.Shr.ToInstruction());
            Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
            i += 4;
        }

        private void ConditionalMath(ref int i)
        {
            Instruction instr = Method.Body.Instructions[i];
            Local int_lcl = new Local(Method.Module.ImportAsTypeSig(typeof(int)));

            int Real_Number = instr.GetLdcI4Value();
            int randomvalue1 = rnd.Next();
            int randomvalue2 = rnd.Next();
            int l2;
            int l;
            if (randomvalue1 > randomvalue2)
            {
                l = Real_Number;
                l2 = Real_Number + Real_Number / 3;
            }
            else
            {
                l2 = Real_Number;
                l = Real_Number + Real_Number / 3;
            }

            Method.Body.Variables.Add(int_lcl);
            instr.OpCode = OpCodes.Ldc_I4;
            instr.Operand = randomvalue2;
            Method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Ldc_I4, randomvalue1));
            Method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Nop));//BGT.S
            Method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, l));
            Method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Nop));//BR.S
            Method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Ldc_I4, l2));
            Method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Stloc, int_lcl));
            Method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Ldloc, int_lcl));

            Method.Body.Instructions[i + 2].OpCode = OpCodes.Bgt_S;
            Method.Body.Instructions[i + 2].Operand = Method.Body.Instructions[i + 5];
            Method.Body.Instructions[i + 4].OpCode = OpCodes.Br_S;
            Method.Body.Instructions[i + 4].Operand = Method.Body.Instructions[i + 6];
            i += 7;
        }
    }
}
