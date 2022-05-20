using dnlib.DotNet;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using dnlib.DotNet.Emit;
using Microsoft.VisualBasic;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
using System.Reflection;
using System.IO;
using SugarGuard.Protector.Class.Constants;
using dnlib.IO;
using dnlib.PE;

namespace SugarGuard.Protector.Protections.Constants
{
    public class Constants
    {
        public Constants(SugarLib lib)
        {
            encodedMethods = new List<EncodedMethod>();
            Main(lib);
        }
        public static List<EncodedMethod> encodedMethods;
        public static Random rnd = new Random();
        void Main(SugarLib lib)
        {
            var module = lib.moduleDef;
            var array = GenerateArray();
            string name = Renamer.InvisibleName;
            var field = new FieldDefUser(name, new FieldSig(module.ImportAsTypeSig(typeof(int[]))), dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
            module.GlobalType.Fields.Add(field);
            InjectArray(array, lib.ctor, field);
            var decryptor = InjectDecryptor(lib);
            decryptor = ModifyDecryptor(decryptor, field);

            foreach (TypeDef type in module.Types)
            {
                if (type.IsGlobalModuleType) continue;
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody || !method.Body.HasInstructions) continue;
                    if (!hasStrings(method)) continue;
                  
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            var local = new Local(method.Module.ImportAsTypeSig(typeof(string)));
                            method.Body.Variables.Add(local);
                            string operand = method.Body.Instructions[i].Operand.ToString();
                         
                            method.Body.Instructions[i].OpCode = OpCodes.Ldloc;
                            method.Body.Instructions[i].Operand = local;
                            method.Body.Instructions.Insert(0, OpCodes.Ldstr.ToInstruction(operand));
                            method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
                            i += 2;
                        }
                    }
                    int num = rnd.Next(100, 500);
                    encodedMethods.Add(new EncodedMethod(method, num));

                    var ctor = module.GlobalType.FindOrCreateStaticConstructor();

                    Local hlocal = new Local(module.ImportAsTypeSig(typeof(RuntimeMethodHandle)));
                    ctor.Body.Variables.Add(hlocal);
                    ctor.Body.Instructions.Insert(0, OpCodes.Ldtoken.ToInstruction(method));
                    ctor.Body.Instructions.Insert(1, OpCodes.Stloc.ToInstruction(hlocal));


                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {

                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            var s = method.Body.Instructions[i].Operand.ToString();
                            var realkey = rnd.Next(10, 50);
                            string encoded = EncodeString(s, realkey + num, array);

                            var fieldstatic = new FieldDefUser(Renamer.GenerateName(), new FieldSig(module.ImportAsTypeSig(typeof(string))), dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
                            module.GlobalType.Fields.Add(fieldstatic);

                           

                            var e = ctor.Body.Instructions.Count - 1;
                            ctor.Body.Instructions.Insert(e, OpCodes.Ldstr.ToInstruction(encoded));
                            ctor.Body.Instructions.Insert(++e, OpCodes.Ldc_I4.ToInstruction(realkey));
                            ctor.Body.Instructions.Insert(++e, OpCodes.Ldloc.ToInstruction(hlocal));
                            ctor.Body.Instructions.Insert(++e, OpCodes.Call.ToInstruction(decryptor));
                            ctor.Body.Instructions.Insert(++e, OpCodes.Stsfld.ToInstruction(fieldstatic));

                            method.Body.Instructions[i] = OpCodes.Ldsfld.ToInstruction(fieldstatic);

                            method.Body.SimplifyBranches();
                            method.Body.OptimizeBranches();
                        }
                    }
                }
            }

        }

        string EncodeString(string s, int realkey, int[] array)
        {
            for (int i = 0; i < array.Length; i++)
                realkey += array[i];
            var charray = new char[s.Length];
            for (int j = 0; j < s.Length; j++)
            {
                charray[j] = (char)(s[j] ^ realkey);
            }
            return new string(charray);
        }


        int[] GenerateArray()
        {
            var array = new int[rnd.Next(10, 50)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = rnd.Next(100, 500);
            }
            return array;
        }

        void InjectArray(int[] array, MethodDef cctor, FieldDef arrayField)
        {
            if (rnd.Next(0, 3) == 1)
            {
                List<Instruction> toInject = new List<Instruction>
                {
                    OpCodes.Ldc_I4.ToInstruction(array.Length),
                    OpCodes.Newarr.ToInstruction(cctor.Module.CorLibTypes.Int32),
                    OpCodes.Stsfld.ToInstruction(arrayField)
                };
                for (int i = 0; i < array.Length; i++)
                {
                    toInject.Add(OpCodes.Ldsfld.ToInstruction(arrayField));
                    toInject.Add(OpCodes.Ldc_I4.ToInstruction(i));
                    toInject.Add(OpCodes.Ldc_I4.ToInstruction(array[i]));
                    toInject.Add(OpCodes.Stelem_I4.ToInstruction());
                    toInject.Add(OpCodes.Nop.ToInstruction());
                }
                for (int j = 0; j < toInject.Count; j++)
                    cctor.Body.Instructions.Insert(j, toInject[j]);
            }
            else
            {
                List<Instruction> toInject = new List<Instruction>
                {
                    OpCodes.Ldc_I4.ToInstruction(array.Length),
                    OpCodes.Newarr.ToInstruction(cctor.Module.CorLibTypes.Int32),
                    OpCodes.Dup.ToInstruction()
                };
                for (int i = 0; i < array.Length; i++)
                {
                    toInject.Add(OpCodes.Ldc_I4.ToInstruction(i));
                    toInject.Add(OpCodes.Ldc_I4.ToInstruction(array[i]));
                    toInject.Add(OpCodes.Stelem_I4.ToInstruction());
                    if (i != array.Length - 1)
                    {
                        toInject.Add(OpCodes.Dup.ToInstruction());
                    }
                }
                toInject.Add(OpCodes.Stsfld.ToInstruction(arrayField));
                for (int j = 0; j < toInject.Count; j++)
                    cctor.Body.Instructions.Insert(j, toInject[j]);
            }

        }

        MethodDef InjectDecryptor(SugarLib lib)
        {
            ModuleDefMD typeModule = ModuleDefMD.Load(typeof(Runtime.ConstantsRuntime).Module);
            TypeDef typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(Runtime.ConstantsRuntime).MetadataToken));
            IEnumerable<IDnlibDef> members = InjectHelper.Inject(typeDef, lib.globalType, lib.moduleDef);
            var dec = (MethodDef)members.Single(method => method.Name == "Decrypt");

            foreach (var mem in members)
                mem.Name = GenerateName();

            return dec;
        }

        public static string GenerateName()
        {
            const string chars = "あいうえおかきくけこがぎぐげごさしすせそざじずぜアイウエオクザタツワルムパリピンプペヲポ";
            return new string(Enumerable.Repeat(chars, 10).Select(s => s[rnd.Next(s.Length)]).ToArray());
        }

        MethodDef ModifyDecryptor(MethodDef dec, FieldDef field)
        {
            for (int j = 0; j < dec.Body.Instructions.Count; j++)
            {
                if (dec.Body.Instructions[j].OpCode == OpCodes.Ldsfld)
                {
                    dec.Body.Instructions[j].OpCode = OpCodes.Ldsfld;
                    dec.Body.Instructions[j].Operand = field;
                }
            }
            return dec;
        }


        bool hasStrings(MethodDef method)
        {
            foreach (var instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldstr)
                    return true;
            }
            return false;
        }
    }
}