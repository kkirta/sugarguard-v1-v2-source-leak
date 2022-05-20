
using System.IO;
using System.Reflection;
using System.Text;

namespace SugarGuard.Runtime
{
    public static class StringEncryptionRuntime
    {
        public static byte[] b;
        public static void Initialize(string name)
        {
            var asm = Assembly.GetCallingAssembly();
            var stream = asm.GetManifestResourceStream(name);
            var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            b = memStream.ToArray();
        }
        public static string Decrypt(int id)
        {
            var l = (b[id++] | b[id++] << 8 | b[id++] << 16 | b[id++] << 24);
            return string.Intern(Encoding.UTF8.GetString(b, id, l));
        }
    }
}
