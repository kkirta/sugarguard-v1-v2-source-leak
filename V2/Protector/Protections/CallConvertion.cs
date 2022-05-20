using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SugarGuard.Protector.Protections
{
    public class CallConvertion
    {
        public CallConvertion(SugarLib lib) => Main(lib);

        void Main(SugarLib lib)
        {
            Local modulelocal = new Local(lib.ctor.Module.ImportAsTypeSig(typeof(System.Reflection.Module)));
            lib.ctor.Body.Variables.Add(modulelocal);

            FieldDef field = new FieldDefUser(Renamer.InvisibleName, new FieldSig(lib.moduleDef.ImportAsTypeSig(typeof(IntPtr[]))), FieldAttributes.Public | FieldAttributes.Static);
            lib.moduleDef.GlobalType.Fields.Add(field);

            var instructions = new List<Instruction>
            {
                OpCodes.Ldtoken.ToInstruction(lib.moduleDef.GlobalType),
                OpCodes.Call.ToInstruction(lib.moduleDef.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }))),
                OpCodes.Callvirt.ToInstruction(lib.moduleDef.Import(typeof(Type).GetMethod("get_Module"))),
                OpCodes.Stloc.ToInstruction(modulelocal),
                OpCodes.Ldc_I4.ToInstruction(666),
                OpCodes.Newarr.ToInstruction(lib.moduleDef.CorLibTypes.IntPtr),
                OpCodes.Stsfld.ToInstruction(field)
            };

            var tokens = new Dictionary<int, int>();

            int index = 0;

            foreach (TypeDef type in lib.moduleDef.Types.ToArray())
                foreach (MethodDef method in type.Methods.ToArray())
                {
                    if (method.IsConstructor) continue;
                    if (method.Body == null) continue;
                    if (method.HasBody && method.Body.HasInstructions && !method.IsConstructor && !method.DeclaringType.IsGlobalModuleType)
                    {
                        var instr = method.Body.Instructions;
                        for (int i = 0; i < instr.Count(); i++)
                        {
                            if ((instr[i].OpCode == OpCodes.Call || instr[i].OpCode == OpCodes.Callvirt) && (instr[i].Operand is MemberRef member))
                            {
                                if (member.HasThis)
                                    continue;

                                int token = member.MDToken.ToInt32();
                                if (!tokens.ContainsKey(token))
                                {
                                    instructions.Add(OpCodes.Ldsfld.ToInstruction(field));
                                    instructions.Add(OpCodes.Ldc_I4.ToInstruction(index));

                                    //Local locali = new Local(method.Module.CorLibTypes.IntPtr);
                                    //lib.ctor.Body.Variables.Add(locali);
                                    //instructions.Add(OpCodes.Ldloc.ToInstruction(modulelocal));
                                    //instructions.Add(OpCodes.Ldc_I4.ToInstruction(token));
                                    //instructions.Add(OpCodes.Call.ToInstruction(lib.moduleDef.Import(typeof(System.Reflection.Module).GetMethod("ResolveMethod", new Type[] { typeof(int) }))));
                                    //instructions.Add(OpCodes.Callvirt.ToInstruction(lib.moduleDef.Import(typeof(System.Reflection.MethodBase).GetMethod("get_MethodHandle"))));
                                    //instructions.Add(OpCodes.Stloc.ToInstruction(locali));
                                    //instructions.Add(OpCodes.Ldloca_S.ToInstruction(locali));
                                    //instructions.Add(OpCodes.Call.ToInstruction(lib.moduleDef.Import(typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", new Type[0]))));

                                    instructions.Add(OpCodes.Ldftn.ToInstruction(member));
                                    instructions.Add(OpCodes.Stelem_I.ToInstruction());
                                    instructions.Add(OpCodes.Nop.ToInstruction());
                                    instr[i].OpCode = OpCodes.Ldsfld;
                                    instr[i].Operand = field;
                                    instr.Insert(++i, Instruction.Create(OpCodes.Ldc_I4, index));
                                    instr.Insert(++i, Instruction.Create(OpCodes.Ldelem_I));
                                    instr.Insert(++i, Instruction.Create(OpCodes.Calli, member.MethodSig));
                                    tokens.Add(token, index);
                                    ++index;
                                }
                                else
                                {
                                    tokens.TryGetValue(token, out int ind);
                                    instr[i].OpCode = OpCodes.Ldsfld;
                                    instr[i].Operand = field;
                                    instr.Insert(++i, Instruction.Create(OpCodes.Ldc_I4, ind));
                                    instr.Insert(++i, Instruction.Create(OpCodes.Ldelem_I));
                                    instr.Insert(++i, Instruction.Create(OpCodes.Calli, member.MethodSig));
                                }
                            }
                        }
                    }
                }

            instructions[4].OpCode = OpCodes.Ldc_I4;
            instructions[4].Operand = index;
            for (int i = 0; i < instructions.Count; ++i)
            {
                lib.ctor.Body.Instructions.Insert(i, instructions[i]);
            }
        }
        public static bool IsDelegate(TypeDef type)
        {
            if (type.BaseType == null)
                return false;

            string fullName = type.BaseType.FullName;
            return fullName == "System.Delegate" || fullName == "System.MulticastDelegate";
        }
    }
}