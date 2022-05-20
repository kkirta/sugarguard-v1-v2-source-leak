using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;
using System.Linq;

namespace SugarGuard.Protector.Protections
{
    public class AntiDebug
    {
        public AntiDebug(SugarLib lib) => Main(lib);

        void Main(SugarLib lib)
        {
            var module = lib.moduleDef;
            var typeModule = ModuleDefMD.Load(typeof(Runtime.AntiDebug).Module);
            var typeDefs = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(Runtime.AntiDebug).MetadataToken));
            var members = InjectHelper.Inject(typeDefs, module.EntryPoint.DeclaringType, module);

            var init = (MethodDef)members.Single(method => method.Name == "Initialize");
            var entrypoint = module.EntryPoint;

            entrypoint.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(init));
        }
    }
}
