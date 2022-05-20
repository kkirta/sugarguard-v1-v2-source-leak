using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System.Linq;

namespace SugarGuard.Protector.Protections
{
    public class AntiDump
    {
        public AntiDump(SugarLib lib) => Main(lib);

        void Main(SugarLib lib)
        {
            var module = lib.moduleDef;
            var typeModule = ModuleDefMD.Load(typeof(Runtime.AntiDump).Module);
            var typeDefs = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(Runtime.AntiDump).MetadataToken));
            var members = InjectHelper.Inject(typeDefs, module.GlobalType, module);

            var init = (MethodDef)members.Single(method => method.Name == "AntiDumpInj");
            MethodDef cctor = module.GlobalType.FindStaticConstructor();

            cctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(init));
        }
    }
}