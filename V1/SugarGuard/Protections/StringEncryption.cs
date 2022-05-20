
using SugarGuard.Core;
using SugarGuard.Helpers.Injection;

using System.IO;
using System.Text;

using dnlib.DotNet;
using dnlib.DotNet.Emit;


namespace SugarGuard.Protections
{
    public class StringEncryption : Protection
    {
        public override string Name => "String Encryption";
        public override void Execute(Context context)
        {
            var module = context.Module;
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            var inj = new Injector(module, typeof(Runtime.StringEncryptionRuntime));
            var initCall = inj.FindMember("Initialize") as MethodDef;
            var decryptCall = inj.FindMember("Decrypt") as MethodDef;

            var cctorBody = context.Cctor.Body.Instructions;

            writer.Write(Utils.RandomByteArr(Utils.RandomSmallInt32()));

            foreach (var type in module.GetTypes())
            {
                if (type.IsGlobalModuleType)
                    continue;
                if (type.Namespace == "Costura")
                    continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody || !method.Body.HasInstructions)
                        continue;

                    method.Body.SimplifyMacros(method.Parameters);
                    method.Body.SimplifyBranches();

                    var instrs = method.Body.Instructions;

                    for (int i = 0; i < instrs.Count; i++)
                    {
                        if (instrs[i].OpCode == OpCodes.Ldstr)
                        {
                            var field = Utils.CreateField(new FieldSig(module.CorLibTypes.String));
                            module.GlobalType.Fields.Add(field);

                            var operand = instrs[i].Operand.ToString();
                            var id = (int)writer.BaseStream.Position;
                            var len = operand.Length;
                            var bytes = Encoding.UTF8.GetBytes(operand);

                            writer.Write(len);
                            writer.Write(bytes);
                            writer.Write(Utils.RandomByteArr(Utils.RandomSmallInt32()));

                            instrs[i].OpCode = OpCodes.Ldsfld;
                            instrs[i].Operand = field;

                            cctorBody.Insert(0, OpCodes.Ldc_I4.ToInstruction(id));
                            cctorBody.Insert(1, OpCodes.Call.ToInstruction(decryptCall));
                            cctorBody.Insert(2, OpCodes.Stsfld.ToInstruction(field));
                        }
                    }
                }
            }

            var name = Utils.GenerateString();
            var res = new EmbeddedResource(name, stream.ToArray(), ManifestResourceAttributes.Private);

            module.Resources.Add(res);

            var cctor = context.Module.GlobalType.FindOrCreateStaticConstructor();
            cctorBody = cctor.Body.Instructions;
            cctorBody.Insert(0, OpCodes.Ldstr.ToInstruction(name));
            cctorBody.Insert(1, OpCodes.Call.ToInstruction(initCall));

            inj.Rename();
        }
    }
}
