using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections.Mutation.Stages
{
    public class IntsToInitializeArray
    {
        MethodDef method { get; set; }
        public IntsToInitializeArray(MethodDef method)
        {
            this.method = method;
        }

        public void Execute()
        {
            if (method.Name != "button1_Click") return;
            ITypeDefOrRef cu = method.Module.ImportAsTypeSig(typeof(System.ValueType)).ToTypeDefOrRef();
            TypeDef satan = new TypeDefUser("'__StaticArrayInitTypeSize=24'", cu);
            satan.Attributes |= TypeAttributes.Sealed | TypeAttributes.ExplicitLayout;
            satan.ClassLayout = new ClassLayoutUser(1, 24);
            method.Module.Types.Add(satan);
            
            FieldDef fieldizinha = new FieldDefUser("'$$method0x6000003-1'", new FieldSig(satan.ToTypeSig()), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.HasFieldRVA| FieldAttributes.Public)
            {
                InitialValue = new byte[] { 01, 00, 00, 00, 02, 00, 00, 00, 03, 00, 00, 00, 04, 00, 00, 00, 05, 00, 00, 00, 06, 00, 00, 00 }
            };
            method.Module.GlobalType.Fields.Add(fieldizinha);

            int i = 0;
            Local pacto = new Local(method.Module.ImportAsTypeSig(typeof(int[])));
            method.Body.Variables.Add(pacto);
            method.Body.Instructions.Insert(i, OpCodes.Ldc_I4_6.ToInstruction());
            method.Body.Instructions.Insert(++i, OpCodes.Newarr.ToInstruction(method.Module.CorLibTypes.Int32));
            method.Body.Instructions.Insert(++i, OpCodes.Dup.ToInstruction());
            method.Body.Instructions.Insert(++i, OpCodes.Ldtoken.ToInstruction(fieldizinha));
            method.Body.Instructions.Insert(++i, OpCodes.Call.ToInstruction(method.Module.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new Type[] { typeof(Array), typeof(RuntimeFieldHandle) }))));
            method.Body.Instructions.Insert(++i, OpCodes.Stloc_S.ToInstruction(pacto));
            method.Body.UpdateInstructionOffsets();
            method.Body.OptimizeBranches();
            method.Body.OptimizeMacros();

            //for (int i = 0; i < method.Body.Instructions.Count; i++)
            //{
            //    if (method.Body.Instructions[i].IsLdcI4())
            //    {

            //    }
            //}
        }
    }
}
