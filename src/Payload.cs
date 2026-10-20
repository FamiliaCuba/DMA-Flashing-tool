using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace DmaTool
{
    // Empaqueta openFPGALoader, LeechCore, FTD3XX y drivers DENTRO del .exe
    // y los extrae a %LocalAppData%\FamiliaCuba\runtime si no hay carpeta sidecar.
    internal static class Payload
    {
        private const string ResourceName = "FC.payload";

        public static void Ensure()
        {
            string sideOfl = Path.Combine(AppPaths.BaseDir, "runtime\\openFPGALoader\\openFPGALoader.exe");
            if (File.Exists(sideOfl)) return;

            Assembly asm = Assembly.GetExecutingAssembly();
            Stream res = asm.GetManifestResourceStream(ResourceName);
            if (res == null)
                throw new InvalidOperationException(Loc.T("err.payload.missing"));

            byte[] blob;
            using (res)
            {
                MemoryStream ms = new MemoryStream();
                byte[] buf = new byte[8192];
                int n;
                while ((n = res.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
                blob = ms.ToArray();
            }

            if (blob.Length < 16) throw new InvalidOperationException(Loc.T("err.payload.missing"));

            string dest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FamiliaCuba", "runtime");
            string stampPath = Path.Combine(dest, ".payload.ver");
            string stampVal = blob.Length.ToString();
            string ofl = Path.Combine(dest, "openFPGALoader\\openFPGALoader.exe");
            try
            {
                if (File.Exists(ofl) && File.Exists(stampPath) && File.ReadAllText(stampPath).Trim() == stampVal)
                    return;
            }
            catch { }

            Directory.CreateDirectory(dest);
            Unpack(blob, dest);
            File.WriteAllText(stampPath, stampVal);
        }

        private static void Unpack(byte[] blob, string dest)
        {
            string destFull = Path.GetFullPath(dest);
            MemoryStream ms = new MemoryStream(blob, false);
            BinaryReader br = new BinaryReader(ms, Encoding.UTF8);
            byte[] mag = br.ReadBytes(4);
            if (mag.Length != 4 || mag[0] != 0x46 || mag[1] != 0x43 || mag[2] != 0x30 || mag[3] != 0x31)
                throw new InvalidOperationException(Loc.T("err.payload.missing"));
            br.ReadInt32();
            int count = br.ReadInt32();
            if (count < 1 || count > 400) throw new InvalidOperationException(Loc.T("err.payload.missing"));

            for (int i = 0; i < count; i++)
            {
                int nameLen = br.ReadInt32();
                if (nameLen < 1 || nameLen > 512) throw new InvalidOperationException(Loc.T("err.payload.missing"));
                string rel = Encoding.UTF8.GetString(br.ReadBytes(nameLen));
                int dataLen = br.ReadInt32();
                if (dataLen < 0 || dataLen > 80 * 1024 * 1024) throw new InvalidOperationException(Loc.T("err.payload.missing"));
                byte[] data = br.ReadBytes(dataLen);
                if (rel.IndexOf("..") >= 0) continue;
                rel = rel.Replace('/', Path.DirectorySeparatorChar);
                string path = Path.GetFullPath(Path.Combine(dest, rel));
                string prefix = destFull.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? destFull : destFull + Path.DirectorySeparatorChar;
                if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, data);
            }
        }
    }
}
