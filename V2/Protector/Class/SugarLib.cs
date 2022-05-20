using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using SugarGuard.Protector.Enums;

namespace SugarGuard.Protector.Class
{
    public class SugarLib
    {
        public string filePath { get; private set; }
        public AssemblyDef assembly { get; private set; }
        public ModuleDef moduleDef { get; private set; }
        public TypeDef globalType { get; private set; }
        public MethodDef ctor { get; private set; }
        public NativeModuleWriterOptions nativeModuleWriterOptions { get; private set; }
        public ModuleWriterOptions moduleWriterOptions { get; private set; }
        public bool noThrowInstance { get; set; }


        public SugarLib(string file)
        {
            filePath = file;

            assembly = AssemblyDef.Load(file);
            moduleDef = assembly.ManifestModule;
            globalType = assembly.ManifestModule.GlobalType;
            ctor = assembly.ManifestModule.GlobalType.FindOrCreateStaticConstructor();
            //CurrentListener.OnWriterEvent += (sender, e) => Listener();
            noThrowInstance = false;

            nativeModuleWriterOptions = new NativeModuleWriterOptions(moduleDef as ModuleDefMD, true)
            {
                MetadataLogger = DummyLogger.NoThrowInstance
            };

            moduleWriterOptions = new ModuleWriterOptions(moduleDef as ModuleDef)
            {
                MetadataLogger = DummyLogger.NoThrowInstance
            };
        }

        void Listener() { }

        string NewName()
        {
            return Path.GetDirectoryName(filePath) + "//" + Path.GetFileNameWithoutExtension(filePath) + "_" + "Sugary" + Path.GetExtension(filePath);
        }

        public void buildASM(saveMode mode)
        {
            if (mode == saveMode.Normal)
            {
                moduleWriterOptions.MetadataOptions.Flags = MetadataFlags.AlwaysCreateStringsHeap | MetadataFlags.AlwaysCreateBlobHeap | MetadataFlags.AlwaysCreateGuidHeap | MetadataFlags.AlwaysCreateUSHeap;
                moduleDef.Write(NewName(), moduleWriterOptions);
            }
            else if (mode == saveMode.x86)
            {
                nativeModuleWriterOptions.MetadataOptions.Flags = MetadataFlags.AlwaysCreateStringsHeap | MetadataFlags.AlwaysCreateBlobHeap | MetadataFlags.AlwaysCreateGuidHeap | MetadataFlags.AlwaysCreateUSHeap;
                (moduleDef as ModuleDefMD).NativeWrite(NewName(), nativeModuleWriterOptions);
            }
        }
    }
}