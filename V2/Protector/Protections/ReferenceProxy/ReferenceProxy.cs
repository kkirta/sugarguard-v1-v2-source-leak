using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System.Collections.Generic;
using System.Linq;

namespace SugarGuard.Protector.Protections.ReferenceProxy
{
    public class ReferenceProxy
    {
        public ReferenceProxy(SugarLib lib) => Main(lib);

        static bool canObfuscate(MethodDef methodDef)
        {
            if (!methodDef.HasBody)
                return false;
            if (!methodDef.Body.HasInstructions)
                return false;
            if (methodDef.DeclaringType.IsGlobalModuleType)
                return false;

            return true;
        }

        List<MethodDef> usedMethods = new List<MethodDef>();

        void Main(SugarLib lib)
        {
            Helper rPHelper = new Helper();
            fixProxy(lib.moduleDef);

            static void fixProxy(ModuleDef moduleDef)
            {
                AssemblyResolver assemblyResolver = new AssemblyResolver();
                ModuleContext moduleContext = new ModuleContext(assemblyResolver);
                assemblyResolver.DefaultModuleContext = moduleContext;
                assemblyResolver.EnableTypeDefCache = true;
                List<AssemblyRef> list = moduleDef.GetAssemblyRefs().ToList<AssemblyRef>();
                moduleDef.Context = moduleContext;

                foreach (AssemblyRef assemblyRef in list)
                {
                    bool flag3 = assemblyRef == null;
                    if (!flag3)
                    {
                        AssemblyDef assemblyDef = assemblyResolver.Resolve(assemblyRef.FullName, moduleDef);
                        bool flag4 = assemblyDef == null;
                        if (!flag4)
                            ((AssemblyResolver)moduleDef.Context.AssemblyResolver).AddToCache(assemblyDef);
                    }
                }
            }

            foreach (TypeDef type in lib.moduleDef.Types.ToArray())
            {
                foreach (MethodDef method in type.Methods.ToArray())
                {
                    if (usedMethods.Contains(method)) 
                        continue;

                    if (canObfuscate(method))
                    {
                        foreach (Instruction instruction in method.Body.Instructions.ToArray())
                        {
                            if (instruction.OpCode == OpCodes.Newobj)
                            {
                                IMethodDefOrRef methodDefOrRef = instruction.Operand as IMethodDefOrRef;

                                if (methodDefOrRef.IsMethodSpec) 
                                    continue;

                                if (methodDefOrRef == null) 
                                    continue;

                                MethodDef methodDef = rPHelper.GenerateMethod(methodDefOrRef, method);

                                if (methodDef == null) 
                                    continue;

                                method.DeclaringType.Methods.Add(methodDef);
                                usedMethods.Add(methodDef);
                                instruction.OpCode = OpCodes.Call;
                                instruction.Operand = methodDef;
                                usedMethods.Add(methodDef);
                            }
                            else if (instruction.OpCode == OpCodes.Stfld)
                            {
                                FieldDef targetField = instruction.Operand as FieldDef;

                                if (targetField == null) 
                                    continue;

                                CilBody body = new CilBody();

                                body.Instructions.Add(OpCodes.Nop.ToInstruction());
                                body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                                body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
                                body.Instructions.Add(OpCodes.Stfld.ToInstruction(targetField));
                                body.Instructions.Add(OpCodes.Ret.ToInstruction());

                                var sig = MethodSig.CreateInstance(lib.moduleDef.CorLibTypes.Void, targetField.FieldSig.GetFieldType());
                                sig.HasThis = true;

                                MethodDefUser methodDefUser = new MethodDefUser(Renamer.InvisibleName, sig)
                                {
                                    Body = body,
                                    IsHideBySig = true
                                };

                                usedMethods.Add(methodDefUser);
                                method.DeclaringType.Methods.Add(methodDefUser);
                                instruction.Operand = methodDefUser;
                                instruction.OpCode = OpCodes.Call;
                            }
                            else if (instruction.OpCode == OpCodes.Ldfld)
                            {
                                FieldDef targetField = instruction.Operand as FieldDef;
                                if (targetField == null) continue;
                                MethodDef newmethod = rPHelper.GenerateMethod(targetField, method);
                                instruction.OpCode = OpCodes.Call;
                                instruction.Operand = newmethod;
                                usedMethods.Add(newmethod);
                            }
                            else if (instruction.OpCode == OpCodes.Call)
                            {
                                if (instruction.Operand is MemberRef)
                                {
                                    MemberRef methodReference = (MemberRef)instruction.Operand;
                                    if (!methodReference.FullName.Contains("Collections.Generic") && !methodReference.Name.Contains("ToString") && !methodReference.FullName.Contains("Thread::Start"))
                                    {
                                        MethodDef methodDef = rPHelper.GenerateMethod(type, methodReference, methodReference.HasThis, methodReference.FullName.StartsWith("System.Void"));
                                        if (methodDef != null)
                                        {
                                            usedMethods.Add(methodDef);
                                            type.Methods.Add(methodDef);
                                            instruction.Operand = methodDef;
                                            methodDef.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
