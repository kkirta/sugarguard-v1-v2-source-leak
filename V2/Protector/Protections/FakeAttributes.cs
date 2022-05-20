using dnlib.DotNet;
using SugarGuard.Protector.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SugarGuard.Protector.Protections
{
    public class FakeAttributes
    {
        public FakeAttributes(SugarLib lib) => Main(lib);

        void Main(SugarLib lib)
        {
            var module = lib.moduleDef;
            List<string> Foolnames = new List<string>()
                    {
                        "Borland.Vcl.Types",
                        "Borland.Eco.Interfaces",
                        "();\u0009",
                        "BabelObfuscatorAttribute",
                        "ZYXDNGuarder",
                        "DotfuscatorAttribute",
                        "YanoAttribute",
                        "Reactor",
                        "EMyPID_8234_",
                        "ObfuscatedByAgileDotNetAttribute",
                        "ObfuscatedByGoliath",
                        "CheckRuntime",
                        "ObfuscatedByCliSecureAttribute",
                        "____KILL",
                        "CodeWallTrialVersion",
                        "Sixxpack",
                        "Microsoft.VisualBasic",
                        "nsnet",
                        "ConfusedByAttribute",
                        "Macrobject.Obfuscator",
                        "Protected_By_Attribute'00'NETSpider.Attribute",
                        "CryptoObfuscator.ProtectedWithCryptoObfuscatorAttribute",
                        "Xenocode.Client.Attributes.AssemblyAttributes.ProcessedByXenocode",
                        "NineRays.Obfuscator.Evaluation",
                        "SecureTeam.Attributes.ObfuscatedByAgileDotNetAttribute",
                        "SmartAssembly.Attributes.PoweredByAttribute",
                        "Sugary",
                        "Form",
                        "Program",
                    };

            foreach (string foolName in Foolnames)
            {
                TypeDef type = new TypeDefUser("SugarGuard.Attributes", foolName, module.Import(typeof(Attribute)));
                type.Attributes = TypeAttributes.NotPublic;
                lib.moduleDef.Types.Add(type);
            }

            TypeDef targetType = lib.moduleDef.Types[new Random().Next(0, module.Types.Count)];
        }
    }
}
