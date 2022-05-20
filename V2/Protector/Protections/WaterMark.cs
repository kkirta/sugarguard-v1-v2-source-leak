using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SugarGuard.Protector.Class;

namespace SugarGuard.Protector.Protections
{
    public class WaterMark
    {
        public WaterMark(SugarLib lib) => Main(lib);

        void Main(SugarLib lib)
        {
            lib.assembly.Name = "[Ϩ] Sugar Guard";
            lib.moduleDef.Name = "[ッ] Sugary";
        }
    }
}