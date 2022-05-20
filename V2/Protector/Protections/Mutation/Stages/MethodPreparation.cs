using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class MethodPreparation
    {
        private MethodDef Method { get; set; }
        static Random rnd = new Random();
        
        public MethodPreparation(MethodDef method)
        {
            Method = method;
        }

        public void Execute()
        {
            if (hasInts())
            {
                Ints();
            }
        }

        private bool hasInts()
        {
            for (int i = 0; i < Method.Body.Instructions.Count; i++)
                if (Method.Body.Instructions[i].IsLdcI4())
                    return true;
            return false;
        }

        private void Ints()
        {
            for (int i = 0; i < Method.Body.Instructions.Count; i++)
            {
                if (Method.Body.Instructions[i].IsLdcI4() && MutationHelper.CanObfuscate(Method.Body.Instructions, i))
                {
                    switch (rnd.Next(0, 10))
                    {
                        case 1: ConvToField(ref i); break;
                        case 2: ConvToLocal(ref i); break;
                        case 3: ConvToFieldModule(ref i); break;
                        default: break;
                    }
                }
            }
        }
        private void ConvToField(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            FieldDef field = new FieldDefUser(Renamer.InvisibleName, new FieldSig(Method.Module.CorLibTypes.Int32), dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
            Method.DeclaringType.Fields.Add(field);
            Method.Body.Instructions.Insert(0, Instruction.CreateLdcI4(value));
            Method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, field));
            i += 2;
            Method.Body.Instructions[i].OpCode = OpCodes.Ldsfld;
            Method.Body.Instructions[i].Operand = field;
        }

        private void ConvToFieldModule(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            MethodDef cctor = Method.Module.GlobalType.FindOrCreateStaticConstructor();
            FieldDef field = new FieldDefUser(Renamer.InvisibleName, new FieldSig(Method.Module.CorLibTypes.Int32), dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
            Method.Module.GlobalType.Fields.Add(field);
            cctor.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(rnd.Next()));
            cctor.Body.Instructions.Insert(1, OpCodes.Stsfld.ToInstruction(field));
            Method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(value));
            Method.Body.Instructions.Insert(1, OpCodes.Stsfld.ToInstruction(field));
            i += 2;
            Method.Body.Instructions[i].OpCode = OpCodes.Ldsfld;
            Method.Body.Instructions[i].Operand = field;
        }

        private void ConvToLocal(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            Local local = new Local(Method.Module.ImportAsTypeSig(typeof(int)));
            Method.Body.Variables.Add(local);
            Method.Body.Instructions[i].OpCode = OpCodes.Ldloc_S;
            Method.Body.Instructions[i].Operand = local;
            Method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(value));
            Method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
            i += 2;
        }
    }
}
