using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsReplacer
    {
        public MethodDef Method { get; set; }
        public IntsReplacer(MethodDef method)
        {
            Method = method;
        }

        public void Execute()
        {
            var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
            var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
            var methodDefUser = new MethodDefUser(Renamer.InvisibleName, MethodSig.CreateStatic(Method.Module.CorLibTypes.Int32, Method.Module.CorLibTypes.Double), methImplFlags, methFlags)
            {
                Body = new CilBody()
            };
            methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(Method.Module.Import(typeof(Math).GetMethod("Sqrt", new Type[1] { typeof(double) }))));
            methodDefUser.Body.Instructions.Add(OpCodes.Conv_I4.ToInstruction());
            methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            Method.Module.GlobalType.Methods.Add(methodDefUser);
            for (int i = 0; i < Method.Body.Instructions.Count; i++)
            {
                if (Method.Body.Instructions[i].IsLdcI4())
                {
                    MathReplacer(methodDefUser, ref i);
                }
            }
        }

        public void MathReplacer(MethodDef method, ref int i)
        {
            int value = Method.Body.Instructions[i].GetLdcI4Value();
            double n = (double)value * value;
            Method.Body.Instructions[i].OpCode = OpCodes.Ldc_R8;
            Method.Body.Instructions[i].Operand = n;
            Method.Body.Instructions.Insert(++i, OpCodes.Call.ToInstruction(method));
            i += 1;

        }
    }
}
