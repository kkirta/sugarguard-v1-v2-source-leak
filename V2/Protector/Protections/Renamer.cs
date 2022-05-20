using SugarGuard.Protector.Class;
using System;
using System.Linq;
using dnlib.DotNet;

namespace SugarGuard.Protector.Protections
{
    public class Renamer
    {
        public Renamer(SugarLib lib) => Main(lib);

        public static string InvisibleName { get { return string.Format("<{0}>Sugar{1}", GenerateName(), "Guard" + ".ツ"); } }

        void Main(SugarLib lib)
        {
            string old_type = null;
            foreach (ModuleDef module in lib.assembly.Modules)
            {
                foreach (TypeDef type in module.Types)
                {
                    if (type.IsPublic)
                        old_type = type.Name;

                    if (CanRename(type))
                        //type.Name = InvisibleName;


                        foreach (MethodDef method in type.Methods)
                        {
                            if (CanRename(method))
                            {
                                TypeRef attrRef = lib.moduleDef.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
                                var ctorRef = new MemberRefUser(lib.moduleDef, ".ctor", MethodSig.CreateInstance(lib.moduleDef.Import(typeof(void)).ToTypeSig()), attrRef);
                                var attr = new CustomAttribute(ctorRef);
                                method.CustomAttributes.Add(attr);
                                method.Name = InvisibleName;
                            }

                            foreach (var param in method.Parameters)
                                param.Name = InvisibleName;
                        }
                    foreach (FieldDef field in type.Fields)
                        if (CanRename(field))
                            field.Name = InvisibleName;

                    foreach (EventDef eventf in type.Events)
                        if (CanRename(eventf))
                            eventf.Name = InvisibleName;
                    if (type.IsPublic)
                    {
                        foreach (Resource xxx in lib.moduleDef.Resources)
                        {
                            if (xxx.Name.Contains(old_type))
                                xxx.Name = xxx.Name.Replace(old_type, type.Name);
                        }
                    }
                }
            }
        }

        static Random rnd = new Random();

        public static string GenerateName()
        {
            const string chars = "あいうえおかきくけこがぎぐげごさしすせそざじずぜアイウエオクザタツワルムパリピンプペヲポ";
            return new string(Enumerable.Repeat(chars, 10).Select(s => s[rnd.Next(s.Length)]).ToArray());
        }

        #region CanRename
        static bool CanRename(TypeDef type)
        {
            if (type.IsGlobalModuleType)
                return false;
            try
            {
                if (type.Namespace.Contains("My"))
                    return false;
            }
            catch { }
            if (type.Interfaces.Count > 0)
                return false;
            if (type.IsSpecialName)
                return false;
            if (type.IsRuntimeSpecialName)
                return false;
            if (type.Name.Contains("Sugar"))
                return false;
            else
                return true;
        }

        static bool CanRename(MethodDef method)
        {
            if (method.IsConstructor)
                return false;
            if (method.DeclaringType.IsForwarder)
                return false;
            if (method.IsFamily)
                return false;
            if (method.IsConstructor || method.IsStaticConstructor)
                return false;
            if (method.IsRuntimeSpecialName)
                return false;
            if (method.DeclaringType.IsForwarder)
                return false;
            if (method.DeclaringType.IsGlobalModuleType)
                return false;
            if (method.Name.Contains("Sugar"))
                return false;
            else
                return true;
        }

        static bool CanRename(FieldDef field)
        {
            if (field.IsLiteral && field.DeclaringType.IsEnum)
                return false;
            if (field.DeclaringType.IsForwarder)
                return false;
            if (field.IsRuntimeSpecialName)
                return false;
            if (field.IsLiteral && field.DeclaringType.IsEnum)
                return false;
            if (field.Name.Contains("Sugar"))
                return false;
            else
                return true;
        }

        static bool CanRename(EventDef ev)
        {
            if (ev.DeclaringType.IsForwarder)
                return false;
            if (ev.IsRuntimeSpecialName)
                return false;
            else
                return true;
        }
        #endregion
    }
}
