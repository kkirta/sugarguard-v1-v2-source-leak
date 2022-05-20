using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsConversions
    {
        private MethodDef Method { get; set; }
        private static Random rnd = new Random();
        public IntsConversions(MethodDef method)
        {
            Method = method;
        }

        public void Execute()
        {
            //Local local = null;
            //if (HasEmptyTypes())
            //{
            //    local = new Local(Method.Module.ImportAsTypeSig(typeof(int)));
            //    Method.Body.Variables.Add(local);
            //    Method.Body.Instructions.Insert(0, OpCodes.Ldsfld.ToInstruction(Method.Module.Import(typeof(Type).GetField("EmptyTypes"))));
            //    Method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Ldlen));
            //    Method.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Stloc_S, local));
            //}
            for (int i =0; i < Method.Body.Instructions.Count; i++)
            {
                if (Method.Body.Instructions[i].IsLdcI4())
                {
                    int value = Method.Body.Instructions[i].GetLdcI4Value();
                    //if (value < 1)
                    //{
                    //    Method.Body.Instructions[i] = OpCodes.Ldsfld.ToInstruction(Method.Module.Import(typeof(Type).GetField("EmptyTypes")));
                    //    Method.Body.Instructions.Insert(++i, OpCodes.Ldlen.ToInstruction());
                    //    continue;
                    //}
                    if (value < 10 && value > 0)
                    {
                        ConvToStrLen(ref i);
                        continue;
                    }
                    ConvToIntPtr(ref i);
                    //switch (rnd.Next(0, 3))
                    //{
                    //    case 0: ConvToIntPtr(ref i); break;
                    //    default: break;
                    //}
                }
            }
        }

        private bool HasEmptyTypes()
        {
            for (int i = 0; i < Method.Body.Instructions.Count; i++)
            {
                if (Method.Body.Instructions[i].IsLdcI4())
                    if (Method.Body.Instructions[i].GetLdcI4Value() < 1)
                        return true;
            }
            return false;
        }

        private void ConvToIntPtr(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            Method.Body.Instructions[i].Operand = value;
            Method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(Method.Module.Import(typeof(IntPtr).GetMethod("op_Explicit", new Type[1] { typeof(int) }))));
            Method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Conv_Ovf_I4));
            i += 2;
        }

        private void ConvToStrLen(ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            Method.Body.Instructions[i].OpCode = OpCodes.Ldstr;
            Method.Body.Instructions[i].Operand = RandomString(value);
            Method.Body.Instructions.Insert(i + 1, OpCodes.Ldlen.ToInstruction());
            i += 1;
        }

        private string RandomString(int len)
        {
            string chars = "abcdefghijklmnopqrstuvwxyz1234567890";
            string ret = "";
            for (int i = 0; i < len; i++)
                ret += chars[rnd.Next(0, chars.Length)];
            return ret;
        }


    }
}
