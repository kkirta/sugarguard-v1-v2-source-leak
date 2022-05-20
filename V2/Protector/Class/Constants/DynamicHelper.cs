using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using ExceptionHandler = dnlib.DotNet.Emit.ExceptionHandler;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
using OperandType = dnlib.DotNet.Emit.OperandType;

namespace SugarGuard.Protector.Class.Constants
{
    class DynamicHelper
    {
        public DynamicContext Context { get; set; }
        private MethodDef[] methodDef { get; set; }
        private MethodDef Method { get; set; }
        private CilBody Body { get; set; }
        public DynamicHelper(params MethodDef[] methodDef)
        {
            this.methodDef = methodDef;
        }
        /// <summary>
        /// Convert all instructions to OpCodes and invoke with <see cref="DynamicMethod"/>
        /// </summary>
        /// <param name="context"></param>
        public void Execute(SugarLib lib)
        {
            Context = new Inject().Execute(lib, lib.moduleDef); 
            foreach (var method in methodDef)
            {
                Method = method;

                Serialize();
                InitializeLocals();
                InitializeBranches();
                InitializeExceptionHandlers();

                var instructions = Body.Instructions;
                foreach (var instruction in method.Body.Instructions)
                {
                    TryTransform(instruction);

                    var parameters = new List<Type>();
                    parameters.Add(typeof(System.Reflection.Emit.OpCode));
                    string name = "Emit";
                    var reflectionOpCode = OpCodeToRef(instruction.OpCode.Name.Replace(".", "_"));
                    instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                    instructions.Add(OpCodes.Ldsfld.ToInstruction(reflectionOpCode));

                    switch (instruction.OpCode.OperandType)
                    {
                        case OperandType.InlineBrTarget:
                            var lbl = Context.BranchRefs[instruction];
                            parameters.Add(typeof(Label));
                            instructions.Add(OpCodes.Ldloc.ToInstruction(lbl));
                            break;
                        case OperandType.InlineVar:
                            if (instruction.Operand is Local lcl)
                            {
                                var local = Context.LocalRefs[lcl];
                                parameters.Add(typeof(LocalBuilder));
                                instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                            }
                            else
                            {
                                parameters.Add(typeof(int));
                                instructions.Add(Instruction.CreateLdcI4(instruction.GetParameterIndex()));
                            }
                            break;
                        case OperandType.InlineI:
                            parameters.Add(typeof(int));
                            instructions.Add(OpCodes.Ldc_I4.ToInstruction((int)instruction.Operand));
                            break;
                        case OperandType.InlineR:
                            parameters.Add(typeof(double));
                            instructions.Add(OpCodes.Ldc_R8.ToInstruction((double)instruction.Operand));
                            break;
                        case OperandType.ShortInlineR:
                            parameters.Add(typeof(float));
                            instructions.Add(OpCodes.Ldc_R4.ToInstruction((float)instruction.Operand));
                            break;
                        case OperandType.InlineI8:
                            parameters.Add(typeof(long));
                            instructions.Add(OpCodes.Ldc_I8.ToInstruction((long)instruction.Operand));
                            break;
                        case OperandType.InlineString:
                            parameters.Add(typeof(string));
                            instructions.Add(OpCodes.Ldstr.ToInstruction((string)instruction.Operand));
                            break;
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                            parameters.Add(typeof(Type));
                            EmitTypeof((ITypeDefOrRef)instruction.Operand);
                            instructions.Add(OpCodes.Call.ToInstruction(Context.GetRefImport<IMethod>("GetTypeFromHandle")));
                            break;
                        case OperandType.InlineField:
                            parameters.Add(typeof(FieldInfo));
                            var mRef = (IField)instruction.Operand;
                            EmitTypeof(mRef.DeclaringType);
                            instructions.Add(OpCodes.Ldstr.ToInstruction(mRef.Name));
                            instructions.Add(OpCodes.Ldc_I4.ToInstruction(0x3D));
                            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("GetField")));
                            break;
                        case OperandType.InlineMethod:
                            bool addLdnull = false;
                            var m = (IMethod)instruction.Operand;
                            MethodDef get = null;
                            if (m.Name == ".ctor" && instruction.OpCode != OpCodes.Call)
                            {
                                parameters.Add(typeof(ConstructorInfo));
                                get = Inject.allMethods["GetConstructor"];
                                get = Context.GetRefImport<MethodDef>("GetConstructor");
                            }
                            else
                            {
                                if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                                {
                                    parameters.AddRange(new Type[] { typeof(MethodInfo), typeof(Type[]) });
                                    name = "EmitCall";
                                    addLdnull = true;
                                }
                                else
                                {
                                    parameters.Add(typeof(MethodInfo));
                                }
                                get = Inject.allMethods["GetMethod"];

                                //get = Context.GetRefImport<MethodDef>("GetMethod");
                                EmitTypeof(m.DeclaringType);
                            }
                            instructions.Add(OpCodes.Ldstr.ToInstruction(m.Name));
                            EmitTypeArray(m.MethodSig.Params);
                            instructions.Add(OpCodes.Call.ToInstruction(get));
                            if (addLdnull)
                                instructions.Add(OpCodes.Ldnull.ToInstruction());
                            break;

                    }
                    instructions.Add(Instruction.Create(OpCodes.Callvirt, method.Module.Import(typeof(ILGenerator).GetMethod(name, parameters.ToArray()))));
                }
                Deserialize();
            }
        }
        private void Serialize()
        {
            Method.Body.SimplifyMacros(Method.Parameters);
            Body = new CilBody();

            Context.ILGenerator = new Local(Context.GetTypeImport<TypeSig>("ILGenerator"));
            Body.Variables.Add(Context.ILGenerator);
            Context.MethodInfo = new Local(Context.GetTypeImport<TypeSig>("MethodInfo"));
            Body.Variables.Add(Context.MethodInfo);
            Context.DynamicMethod = new Local(Context.GetTypeImport<TypeSig>("DynamicMethod"));
            Body.Variables.Add(Context.DynamicMethod);

            var instructions = Body.Instructions;

            instructions.Add(OpCodes.Ldstr.ToInstruction("Virtual Machine")); // Name DynamicMethod
            instructions.Add(OpCodes.Ldc_I4.ToInstruction(0x16)); // Method Attributes
            instructions.Add(OpCodes.Ldc_I4_1.ToInstruction()); // CallingConvertion
            EmitTypeof(Method.ReturnType.ToTypeDefOrRef()); // Return type
            EmitTypeArray(GetSigs(Method.Parameters));

            EmitTypeof(Method.DeclaringType); // Owner Type
            instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());

            instructions.Add(OpCodes.Newobj.ToInstruction(Context.GetRefImport<MemberRef>(".ctor"))); // New instance DynamicMethod
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Call.ToInstruction(Inject.allMethods["GetCreatedMethodInfo"]));
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.MethodInfo)); // Branch: checking method in Dictionary
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Ldnull.ToInstruction());
            instructions.Add(OpCodes.Ceq.ToInstruction());
            Context.Branch = OpCodes.Brfalse.ToInstruction(Body.Instructions.Last());
            instructions.Add(Context.Branch);
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("GetILGenerator"))); // Get ILGenerator
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.ILGenerator));
        }
        private void TryTransform(Instruction instruction)
        {
            var instructions = Body.Instructions;
            if (Context.IsBranchTarget(instruction, out var label))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                instructions.Add(OpCodes.Ldloc.ToInstruction(label));
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("MarkLabel")));
            }

            if (Context.IsExceptionStart(instruction))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("BeginExceptionBlock")));
                instructions.Add(OpCodes.Pop.ToInstruction());
            }
            else if (Context.IsHandlerStart(instruction, out var beginMethod, out var ex))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                if (ex.HandlerType == ExceptionHandlerType.Catch)
                    EmitTypeof(ex.CatchType);
                instructions.Add(OpCodes.Callvirt.ToInstruction(beginMethod));
            }
            else if (Context.IsExceptionEnd(instruction))
            {
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("EndExceptionBlock")));
            }
        }
        private void InitializeExceptionHandlers()
        {
            if (!Method.Body.HasExceptionHandlers)
                return;

            foreach (var ex in Method.Body.ExceptionHandlers)
                Context.ExceptionHandlers.Add(ex);
        }
        private void InitializeLocals()
        {
            if (!Method.Body.HasVariables)
                return;

            var instructions = Body.Instructions;
            foreach (var local in Method.Body.Variables)
            {
                var localBuilder = new Local(Context.GetTypeImport<TypeSig>("LocalBuilder"));
                Body.Variables.Add(localBuilder);
                instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                EmitTypeof(local.Type.ToTypeDefOrRef());
                instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("DeclareLocal")));
                instructions.Add(OpCodes.Stloc.ToInstruction(localBuilder));
                Context.LocalRefs.Add(local, localBuilder);
            }
        }
        private void InitializeBranches()
        {
            var instructions = Body.Instructions;
            var refs = new Dictionary<Instruction, Local>();
            foreach (var instruction in Method.Body.Instructions.Where(x => x.OpCode.OperandType == OperandType.InlineBrTarget))
            {
                Local lcl = null;
                if (Context.IsBranchTarget((Instruction)instruction.Operand, out var label))
                    lcl = label;
                else
                {
                    lcl = new Local(Context.GetTypeImport<TypeSig>("Label"));
                    Body.Variables.Add(lcl);
                    instructions.Add(OpCodes.Ldloc.ToInstruction(Context.ILGenerator));
                    instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("DefineLabel")));
                    instructions.Add(OpCodes.Stloc.ToInstruction(lcl));
                }

                Context.BranchRefs.Add(instruction, lcl);
            }
        }
        private void Deserialize()
        {
            var instructions = Body.Instructions;
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("GetBaseDefinition")));
            instructions.Add(OpCodes.Stloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.DynamicMethod));
            instructions.Add(OpCodes.Ldloc.ToInstruction(Context.MethodInfo));
            instructions.Add(OpCodes.Call.ToInstruction(Inject.allMethods["SetMethodInfo"]));
            var branchTarget = OpCodes.Ldloc.ToInstruction(Context.MethodInfo);
            Context.Branch.Operand = branchTarget;
            instructions.Add(branchTarget);

            instructions.Add(OpCodes.Ldnull.ToInstruction());
            var count = Method.MethodSig.Params.Count;

            EmitArray(Context.GetTypeImport<TypeSig>("Object"), GetSigs(Method.Parameters),
            (index, s, instrs) =>
            {
                instrs.Add(OpCodes.Dup.ToInstruction());
                instrs.Add(OpCodes.Ldc_I4.ToInstruction(index));
                instrs.Add(OpCodes.Ldarg.ToInstruction(Method.Parameters.ElementAt(index)));
                instrs.Add(OpCodes.Box.ToInstruction(Method.Parameters[index].Type.ToTypeDefOrRef()));
                instrs.Add(OpCodes.Stelem_Ref.ToInstruction());
            }
            );

            instructions.Add(OpCodes.Callvirt.ToInstruction(Context.GetRefImport<MemberRef>("Invoke")));

            if (Method.HasReturnType)
                instructions.Add(OpCodes.Unbox_Any.ToInstruction(Method.ReturnType.ToTypeDefOrRef()));
            else
                instructions.Add(OpCodes.Pop.ToInstruction());


            instructions.Add(OpCodes.Ret.ToInstruction());

            Method.FreeMethodBody();
            Method.Body = Body;
            Body.OptimizeMacros();
        }
        private MemberRef OpCodeToRef(string opName)
        {
            return Context.Module.Import(typeof(System.Reflection.Emit.OpCodes).GetField(opName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static));
        }
        private void EmitArray(TypeSig type, IList<TypeSig> sigs, Action<int, IList<TypeSig>, IList<Instruction>> emit)
        {
            var instructions = Body.Instructions;
            var count = sigs.Count;
            instructions.Add(OpCodes.Ldc_I4.ToInstruction(count));
            instructions.Add(OpCodes.Newarr.ToInstruction(type.ToTypeDefOrRef()));
            for (int i = 0; i < sigs.Count; i++)
                emit(i, sigs, instructions);
        }
        private void EmitTypeof(ITypeDefOrRef type)
        {
            Body.Instructions.Add(OpCodes.Ldtoken.ToInstruction(type));
            Body.Instructions.Add(OpCodes.Call.ToInstruction(Context.GetRefImport<MemberRef>("GetTypeFromHandle")));
        }
        private void EmitTypeArray(IList<TypeSig> sigs)
        {
            EmitArray(Context.GetTypeImport<TypeSig>("Type"), sigs,
            (index, s, instrs) =>
            {
                instrs.Add(OpCodes.Dup.ToInstruction());
                instrs.Add(OpCodes.Ldc_I4.ToInstruction(index));
                EmitTypeof(s[index].ToTypeDefOrRef());
                instrs.Add(OpCodes.Stelem_Ref.ToInstruction());
            }
            );
        }
        private List<TypeSig> GetSigs(ParameterList list)
        {
            var sigs = new List<TypeSig>();
            foreach (var parameter in list)
                sigs.Add(parameter.Type);
            return sigs;
        }
    }
    class Inject
    {
        public static Dictionary<string, MethodDef> allMethods;
        public DynamicContext Execute(SugarLib context, ModuleDef moduleDef)
        {
            allMethods = new Dictionary<string, MethodDef>();
            
            var typeModule = ModuleDefMD.Load(typeof(DynamicMethods).Module);
            var typeDefs = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(DynamicMethods).MetadataToken));

            var members = InjectHelper.Inject(typeDefs, moduleDef.GlobalType, moduleDef);

            var ctx = new DynamicContext(moduleDef);
            foreach (var member in members)
            {
                if (member is MethodDef mDef)
                {
                    ctx.AddRefImport(mDef);
                    allMethods.Add(mDef.Name, mDef);
                }
            }
            ctx.Initialize();
            return ctx;
        }
    }
    public class DynamicContext : ImportContext
    {
        public Dictionary<Instruction, Local> BranchRefs;
        public Dictionary<Local, Local> LocalRefs;
        public List<ExceptionHandler> ExceptionHandlers;
        public Instruction Branch;
        public Local ILGenerator;
        public Local DynamicMethod;
        public Local MethodInfo;
        public DynamicContext(ModuleDef module) : base(module)
        {
            LocalRefs = new Dictionary<Local, Local>();
            BranchRefs = new Dictionary<Instruction, Local>();
            ExceptionHandlers = new List<ExceptionHandler>();
        }

        public override void Initialize()
        {
            AddTypeImport(Module.ImportAsTypeSig(typeof(ILGenerator)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(DynamicMethod)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(LocalBuilder)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(Type)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(MethodInfo)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(Label)));
            AddTypeImport(Module.ImportAsTypeSig(typeof(object)));
            AddRefImport(Module.Import(typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) })));
            AddRefImport(Module.Import(typeof(DynamicMethod).GetConstructor(new Type[] { typeof(string), typeof(System.Reflection.MethodAttributes), typeof(CallingConventions), typeof(Type), typeof(Type[]), typeof(Type), typeof(bool) })));
            AddRefImport(Module.Import(typeof(DynamicMethod).GetMethod("GetILGenerator", new Type[0])));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("DeclareLocal", new Type[] { typeof(Type) })));
            AddRefImport(Module.Import(typeof(Type).GetMethod("get_Module")));
            AddRefImport(Module.Import(typeof(MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) })));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("DefineLabel")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("MarkLabel", new Type[] { typeof(Label) })));
            AddRefImport(Module.Import(typeof(MethodInfo).GetMethod("GetBaseDefinition")));
            AddRefImport(Module.Import(typeof(Type).GetMethod("GetField", new Type[] { typeof(string), typeof(BindingFlags) })));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginExceptionBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginCatchBlock", new Type[] { typeof(Type) })));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginExceptFilterBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginFinallyBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("BeginFaultBlock")));
            AddRefImport(Module.Import(typeof(ILGenerator).GetMethod("EndExceptionBlock")));

        }
        public bool IsBranchTarget(Instruction instruction, out Local label)
        {
            label = BranchRefs.FirstOrDefault(x => ((Instruction)x.Key.Operand) == instruction).Value;

            return label != null;
        }

        public bool IsExceptionStart(Instruction instruction)
        {
            return ExceptionHandlers.FirstOrDefault(x => x.TryStart == instruction) != null;
        }


        public bool IsExceptionEnd(Instruction instruction)
        {
            return ExceptionHandlers.FirstOrDefault(x => x.HandlerEnd == instruction) != null;
        }

        public bool IsHandlerStart(Instruction instruction, out MemberRef beginMethod, out ExceptionHandler ex)
        {
            ex = ExceptionHandlers.FirstOrDefault(x => x.HandlerStart == instruction);
            beginMethod = null;
            if (ex == null)
                return false;

            string handlerName = (ex.HandlerType == ExceptionHandlerType.Filter) ? FormatName("ExceptFilter") : FormatName(ex.HandlerType.ToString());
            beginMethod = GetRefImport<MemberRef>(handlerName);
            return true;
        }


        private string FormatName(string handlerName)
        {
            return "Begin" + handlerName + "Block";
        }


    }
    public abstract class ImportContext
    {
        private List<IMemberRef> importedRefs = new List<IMemberRef>();
        private List<IType> importedTypes = new List<IType>();

        public ModuleDef Module { get; private set; }
        public ImportContext(ModuleDef module)
        {
            Module = module;
        }
        public abstract void Initialize();

        public void AddRefImport(IMemberRef mRef)
        {
            importedRefs.Add(mRef);
        }
        public void AddTypeImport(IType type)
        {
            importedTypes.Add(type);
        }
        public T GetRefImport<T>(string name)
        {
            return (T)importedRefs.FirstOrDefault(x => x.Name == name);
        }
        public T GetTypeImport<T>(string name)
        {
            return (T)importedTypes.FirstOrDefault(x => x.Name == name);
        }
    }
    internal static class DynamicMethods
    {
        private static Dictionary<string, MethodInfo> _methods;

        internal static MethodInfo GetCreatedMethodInfo(DynamicMethod method)
        {
            if (_methods == null)
                _methods = new Dictionary<string, MethodInfo>();
            if (_methods.ContainsKey(method.Name))
                return _methods[method.Name];
            return null;
        }

        internal static void SetMethodInfo(DynamicMethod method, MethodInfo methodInfo)
        {
            if (!_methods.ContainsKey(method.Name))
                _methods.Add(method.Name, methodInfo);
            else
                _methods[method.Name] = methodInfo;
        }

        internal static MethodInfo GetMethod(Type ownerType, string name, Type[] parameters)
        {
            var method = ownerType.GetMethod(name, parameters);
            if (method == null)
            {
                foreach (var m in ownerType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (m.Name == name && HasSameParamSig(parameters, m.GetParameters()))
                    {
                        method = m;
                        break;
                    }
                }
            }
            if (method.IsGenericMethod)
                return method.GetGenericMethodDefinition();

            return method;
        }

        internal static ConstructorInfo GetConstructor(Type ownerType, Type[] parameters)
        {
            ConstructorInfo constructor = ownerType.GetConstructor(parameters);
            if (constructor == null)
            {
                foreach (var c in ownerType.GetConstructors())
                {
                    if (HasSameParamSig(parameters, c.GetParameters()))
                    {
                        constructor = c;
                        break;
                    }
                }
            }
            return constructor;
        }

        private static bool HasSameParamSig(Type[] fParameters, ParameterInfo[] sParameters)
        {
            if (fParameters.Length != sParameters.Length)
                return false;

            for (int i = 0; i < fParameters.Length; i++)
            {
                if (fParameters[i] != sParameters[i].ParameterType)
                    return false;
            }
            return true;
        }
    }
}
