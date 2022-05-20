using System.Collections.Generic;
using SugarGuard.Protector.Class;
using SugarGuard.Protector.Protections;
using SugarGuard.Protector.Protections.Constants;
using SugarGuard.Protector.Protections.ControlFlow;
using SugarGuard.Protector.Protections.ReferenceProxy;
using SugarGuard.Protector.Protections.Mutation;

namespace SugarGuard.Protector
{
    public class Sugar
    {
        public List<Enums.Protections> protections = new List<Enums.Protections>();

        SugarLib lib { get; set; }

        public Sugar(string filePath)
        {
            lib = new SugarLib(filePath);
        }

        public void Protect()
        {
            foreach (Enums.Protections protection in protections)
            {
                if (protection == Enums.Protections.CallConvertion)
                    new CallConvertion(lib);

                if (protection == Enums.Protections.Constants)
                    new Constants(lib);

                if (protection == Enums.Protections.VM)
                    //new Protections.Virtualization.Virtualzation(lib);

                if (protection == Enums.Protections.ReferenceProxy)
                    new ReferenceProxy(lib);

                if (protection == Enums.Protections.ControlFlow)
                    new ControlFlow(lib);

                if (protection == Enums.Protections.InvalidOpcodes)
                    new InvalidOpcodes(lib);

                if (protection == Enums.Protections.AntiDump)
                    new AntiDump(lib);

                if (protection == Enums.Protections.AntiDebug)
                    new AntiDebug(lib);

                if (protection == Enums.Protections.Mutation)
                    new Mutation(lib);
            }

            foreach (Enums.Protections protection in protections)
            {
                if (protection == Enums.Protections.PosConstants)
                    new PosConstants(lib);

                if (protection == Enums.Protections.Renamer)
                    new Renamer(lib);

                if (protection == Enums.Protections.FakeAttributes)
                    new FakeAttributes(lib);

                if (protection == Enums.Protections.WaterMark)
                    new WaterMark(lib);

                //if (protection == Enums.Protections.ReferenceOverload)
                //    new ReferenceOverload(lib);
            }
        }

        public void Save()
        {
            lib.buildASM(Enums.saveMode.x86);
        }
    }
}