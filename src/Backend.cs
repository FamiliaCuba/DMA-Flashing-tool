using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DmaTool
{
    // Rutas a las herramientas externas, relativas al .exe (carpeta runtime\).
    internal static class AppPaths
    {
        public static string BaseDir
        {
            get
            {
                string p = System.Reflection.Assembly.GetEntryAssembly() != null
                    ? System.Reflection.Assembly.GetEntryAssembly().Location
                    : Process.GetCurrentProcess().MainModule.FileName;
                return Path.GetDirectoryName(Path.GetFullPath(p));
            }
        }

        private static string _runtime;

        // Sidecar junto al .exe si existe; si no, lo extraido del propio ejecutable.
        public static string Runtime
        {
            get
            {
                if (_runtime != null) return _runtime;
                string side = Path.Combine(BaseDir, "runtime");
                if (File.Exists(Path.Combine(side, "openFPGALoader\\openFPGALoader.exe")))
                    _runtime = side;
                else
                    _runtime = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "FamiliaCuba", "runtime");
                return _runtime;
            }
        }
        public static string DataDir
        {
            get
            {
                string d = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FamiliaCuba");
                try { if (!Directory.Exists(d)) Directory.CreateDirectory(d); }
                catch { }
                return d;
            }
        }

        public static string OpenFpgaLoaderExe { get { return Path.Combine(Runtime, "openFPGALoader\\openFPGALoader.exe"); } }
        public static string DriversDir { get { return Path.Combine(Runtime, "drivers"); } }
        public static string ZadigExe { get { return Path.Combine(DriversDir, "rs232\\zadig-2.9.exe"); } }
        public static string BackupsDir { get { return Path.Combine(Runtime, "backups"); } }
    }

    // Lanza procesos y devuelve stdout/stderr linea a linea por callback.
    internal static class ProcRunner
    {
        public static Process Current;

        public static void KillCurrent()
        {
            try
            {
                Process c = Current;
                if (c != null && !c.HasExited) c.Kill();
            }
            catch { }
        }

        public static int Run(string exe, string args, string workingDir, Action<string> onLine)
        {
            if (!File.Exists(exe))
            {
                onLine(Loc.T("err.noexe"));
                onLine("  " + exe);
                return -1;
            }

            ProcessStartInfo psi = new ProcessStartInfo(exe, args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.Default;
            psi.StandardErrorEncoding = Encoding.Default;
            if (!string.IsNullOrEmpty(workingDir)) psi.WorkingDirectory = workingDir;

            Process p = new Process();
            p.StartInfo = psi;
            p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) onLine(e.Data); };
            p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) onLine(e.Data); };

            try
            {
                p.Start();
                Current = p;
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                return p.ExitCode;
            }
            catch (Exception ex)
            {
                onLine("ERROR al ejecutar: " + ex.Message);
                return -1;
            }
            finally { Current = null; }
        }
    }

    // Flasheo y deteccion JTAG mediante openFPGALoader (open source, distribuido aparte).
    internal static class OpenFpga
    {
        public static bool IsAvailable() { return File.Exists(AppPaths.OpenFpgaLoaderExe); }

        public static int RunOfl(string args, Action<string> onLine)
        {
            onLine("> openFPGALoader " + args);
            onLine("");
            return ProcRunner.Run(AppPaths.OpenFpgaLoaderExe, args,
                Path.GetDirectoryName(AppPaths.OpenFpgaLoaderExe), onLine);
        }

        // Escribe un .bin en la flash SPI. cable: "ch347_jtag" o "ft2232". part: "xc7a75t"...
        public static int Flash(string binPath, string cable, string fpgaPart, Action<string> onLine)
        {
            if (!File.Exists(binPath))
            {
                onLine(Loc.T("err.nobin") + binPath);
                return -1;
            }
            return RunOfl("-c " + cable + " --fpga-part " + fpgaPart + " -f --verify \"" + binPath + "\"", onLine);
        }

        public static int DumpFlash(string destPath, string cable, string fpgaPart, long fileSize, Action<string> onLine)
        {
            if (fileSize < 1024) fileSize = 16777216;
            string dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return RunOfl("-c " + cable + " --fpga-part " + fpgaPart +
                " --dump-flash --file-size " + fileSize.ToString(CultureInfo.InvariantCulture) +
                " \"" + destPath + "\"", onLine);
        }

        // Backup SPI (si falla, sigue) + grabar + verificar.
        public static int FlashWithBackup(string binPath, string cable, string fpgaPart, Action<string> onLine)
        {
            if (!File.Exists(binPath))
            {
                onLine(Loc.T("err.nobin") + binPath);
                return -1;
            }
            try
            {
                if (!Directory.Exists(AppPaths.BackupsDir)) Directory.CreateDirectory(AppPaths.BackupsDir);
                string bak = Path.Combine(AppPaths.BackupsDir,
                    "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".bin");
                long sz = new FileInfo(binPath).Length;
                if (sz < 1024 * 1024) sz = 16777216;
                onLine(Loc.T("flash.backup"));
                int d = DumpFlash(bak, cable, fpgaPart, sz, onLine);
                if (d == 0 && File.Exists(bak))
                    onLine(Loc.T("flash.backup.ok") + bak);
                else
                    onLine(Loc.T("flash.backup.fail"));
            }
            catch (Exception ex)
            {
                onLine(Loc.T("flash.backup.fail") + " " + ex.Message);
            }
            onLine("");
            onLine(Loc.T("flash.verify"));
            return Flash(binPath, cable, fpgaPart, onLine);
        }

        public static int Detect(string cable, Action<string> onLine)
        {
            return RunOfl("-c " + cable + " --detect", onLine);
        }

        // Prueba CH347 y luego FTDI. Devuelve el cable o "".
        public static string AutoDetect(Action<string> onLine)
        {
            if (!IsAvailable())
            {
                onLine(Loc.T("err.noexe"));
                onLine("  " + AppPaths.OpenFpgaLoaderExe);
                return "";
            }
            string[] cables = new string[] { "ch347_jtag", "ft2232" };
            for (int i = 0; i < cables.Length; i++)
            {
                onLine(Loc.T("flash.detecting") + " " + cables[i] + "...");
                int rc = Detect(cables[i], onLine);
                if (rc == 0)
                {
                    onLine(Loc.T("flash.found") + cables[i]);
                    return cables[i];
                }
            }
            onLine(Loc.T("flash.notfound"));
            return "";
        }

        // IDCODE + DNA unico del die (openFPGALoader --detect y --read-dna).
        public static FpgaDna ReadIdentity(string preferredCable, Action<string> onLine)
        {
            List<string> acc = new List<string>();
            Action<string> cap = delegate(string line)
            {
                acc.Add(line);
                onLine(line);
            };
            string cable = preferredCable;
            int rc = Detect(cable, cap);
            if (rc != 0)
            {
                cable = AutoDetect(cap);
            }
            FpgaDna d;
            if (string.IsNullOrEmpty(cable))
            {
                d = ParseIdentity(acc);
                return d;
            }
            cap("");
            cap(Loc.T("dna.reading"));
            RunOfl("-c " + cable + " --read-dna", cap);
            d = ParseIdentity(acc);
            d.Cable = cable;
            return d;
        }

        private static FpgaDna ParseIdentity(List<string> lines)
        {
            FpgaDna d = new FpgaDna();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line == null) continue;
                string t = line.Trim();
                if (t.Length == 0) continue;
                if (t[0] == '>') continue;
                string low = t.ToLowerInvariant();
                if (low.IndexOf("idcode") >= 0)
                {
                    string hx = ExtractHexToken(t);
                    if (hx == null && i + 1 < lines.Count) hx = ExtractHexToken(lines[i + 1]);
                    if (hx != null) d.Idcode = hx;
                }
                else if (low.IndexOf("model") >= 0)
                {
                    int c = t.LastIndexOf(':');
                    if (c >= 0 && c + 1 < t.Length) d.Model = t.Substring(c + 1).Trim();
                }
                else if (low.IndexOf("dna") >= 0)
                {
                    string hx = ExtractHexToken(t);
                    if (hx != null) d.Dna = hx;
                }
            }
            d.Ok = !string.IsNullOrEmpty(d.Dna) || !string.IsNullOrEmpty(d.Idcode);
            return d;
        }

        private static string ExtractHexToken(string s)
        {
            if (s == null) return null;
            int bestStart = -1, bestLen = 0;
            int i = 0;
            while (i < s.Length)
            {
                if (i + 1 < s.Length && s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
                {
                    int j = i + 2;
                    while (j < s.Length && IsHexChar(s[j])) j++;
                    int n = j - (i + 2);
                    if (n > bestLen) { bestStart = i + 2; bestLen = n; }
                    i = j;
                    continue;
                }
                if (IsHexChar(s[i]))
                {
                    int j = i + 1;
                    while (j < s.Length && IsHexChar(s[j])) j++;
                    int n = j - i;
                    if (n >= 8 && n > bestLen) { bestStart = i; bestLen = n; }
                    i = j;
                    continue;
                }
                i++;
            }
            if (bestLen < 4) return null;
            return "0x" + s.Substring(bestStart, bestLen).ToUpperInvariant();
        }

        private static bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }

    internal sealed class FpgaDna
    {
        public bool Ok;
        public string Dna;
        public string Idcode;
        public string Model;
        public string Cable;

        public string Primary()
        {
            if (!string.IsNullOrEmpty(Dna)) return Dna;
            if (!string.IsNullOrEmpty(Idcode)) return Idcode;
            return "";
        }

        public string Summary()
        {
            string s = "";
            if (!string.IsNullOrEmpty(Idcode)) s += "IDCODE " + Idcode;
            if (!string.IsNullOrEmpty(Model))
            {
                if (s.Length > 0) s += "   \u00b7   ";
                s += Model;
            }
            if (!string.IsNullOrEmpty(Cable))
            {
                if (s.Length > 0) s += "   \u00b7   ";
                s += Cable;
            }
            return s;
        }

        public string CopyBlock()
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Dna)) sb.AppendLine("DNA: " + Dna);
            if (!string.IsNullOrEmpty(Idcode)) sb.AppendLine("IDCODE: " + Idcode);
            if (!string.IsNullOrEmpty(Model)) sb.AppendLine("FPGA: " + Model);
            if (!string.IsNullOrEmpty(Cable)) sb.AppendLine("Cable: " + Cable);
            return sb.ToString().Trim();
        }
    }

    // Estado de conexion FPGA/DMA (LeechCore).
    internal sealed class DmaInfo
    {
        public bool FpgaOk;
        public bool DmaReadOk;
        public ulong FpgaId;
        public ulong FwMajor;
        public ulong FwMinor;
        public ulong DeviceId;
        public ulong AddrMax;
        public ulong MaxRx;
        public string Error;
    }

    // Muestra en vivo del speed test (el hilo de lectura actualiza, la UI lee).
    internal sealed class SpeedSnap
    {
        public bool Running;
        public bool Connected;
        public double Mbps;
        public double ReadsPerSec;
        public long TotalBytes;
        public long TotalReads;
        public long Failed;
        public ulong Address;
        public uint Chunk;
    }

    // Lectura de info del dispositivo y speed test de DMA via LeechCore (leechcore.dll).
    // P/Invoke conforme a leechcore.h (version 0xc0fd0002).
    internal static class Leech
    {
        private const uint LC_CONFIG_VERSION = 0xc0fd0002;
        private const ulong LC_OPT_CORE_ADDR_MAX = 0x1000000800000000;
        private const ulong LC_OPT_FPGA_MAX_SIZE_RX = 0x0300000300000000;
        private const ulong LC_OPT_FPGA_DELAY_READ = 0x0300000800000000;
        private const ulong LC_OPT_FPGA_RETRY_ON_ERROR = 0x0300000900000000;
        private const ulong LC_OPT_FPGA_DEVICE_ID = 0x0300008000000000;
        private const ulong LC_OPT_FPGA_FPGA_ID = 0x0300008100000000;
        private const ulong LC_OPT_FPGA_VERSION_MAJOR = 0x0300008200000000;
        private const ulong LC_OPT_FPGA_VERSION_MINOR = 0x0300008300000000;
        private const ulong LC_OPT_FPGA_ALGO_TINY = 0x0300008400000000;
        private const ulong LC_OPT_FPGA_ALGO_SYNCHRONOUS = 0x0300008500000000;

        public static readonly SpeedSnap Live = new SpeedSnap();
        private static readonly object LiveLock = new object();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct LC_CONFIG
        {
            public uint dwVersion;
            public uint dwPrintfVerbosity;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDevice;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szRemote;
            public IntPtr pfn_printf_opt;
            public ulong paMax;
            public int fVolatile;
            public int fWritable;
            public int fRemote;
            public int fRemoteDisableCompress;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDeviceName;
        }

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LcCreate(ref LC_CONFIG pLcCreateConfig);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void LcClose(IntPtr hLC);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LcRead(IntPtr hLC, ulong pa, uint cb, byte[] pb);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LcGetOption(IntPtr hLC, ulong fOption, out ulong pqwValue);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LcSetOption(IntPtr hLC, ulong fOption, ulong qwValue);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LcAllocScatter1(uint cMEMs, out IntPtr pppMEMs);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void LcReadScatter(IntPtr hLC, uint cMEMs, IntPtr ppMEMs);

        [DllImport("leechcore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void LcMemFree(IntPtr pv);

        private static void Log(Action<string> onLine, string s)
        {
            if (onLine != null) onLine(s);
        }

        private static IntPtr OpenRaw(string device)
        {
            LC_CONFIG cfg = new LC_CONFIG();
            cfg.dwVersion = LC_CONFIG_VERSION;
            cfg.dwPrintfVerbosity = 0;
            cfg.szDevice = device;
            cfg.szRemote = "";
            cfg.szDeviceName = "";
            cfg.pfn_printf_opt = IntPtr.Zero;
            return LcCreate(ref cfg);
        }

        private static IntPtr Open(out string err)
        {
            err = null;
            try
            {
                IntPtr h = OpenRaw("fpga://algo=0");
                if (h == IntPtr.Zero) h = OpenRaw("fpga");
                if (h == IntPtr.Zero)
                {
                    err = Loc.T("err.leech.open");
                    return IntPtr.Zero;
                }
                LcSetOption(h, LC_OPT_FPGA_ALGO_TINY, 0);
                LcSetOption(h, LC_OPT_FPGA_ALGO_SYNCHRONOUS, 0);
                LcSetOption(h, LC_OPT_FPGA_DELAY_READ, 0);
                LcSetOption(h, LC_OPT_FPGA_MAX_SIZE_RX, 0x001c0000);
                return h;
            }
            catch (DllNotFoundException)
            {
                err = Loc.T("err.leech.dll");
                return IntPtr.Zero;
            }
        }

        private static void FillInfo(IntPtr h, DmaInfo inf)
        {
            inf.FpgaOk = true;
            ulong v;
            if (LcGetOption(h, LC_OPT_FPGA_VERSION_MAJOR, out v)) inf.FwMajor = v;
            if (LcGetOption(h, LC_OPT_FPGA_VERSION_MINOR, out v)) inf.FwMinor = v;
            if (LcGetOption(h, LC_OPT_FPGA_FPGA_ID, out v)) inf.FpgaId = v;
            if (LcGetOption(h, LC_OPT_FPGA_DEVICE_ID, out v)) inf.DeviceId = v;
            if (LcGetOption(h, LC_OPT_CORE_ADDR_MAX, out v)) inf.AddrMax = v;
            if (LcGetOption(h, LC_OPT_FPGA_MAX_SIZE_RX, out v)) inf.MaxRx = v;
        }

        private static ulong FindPage(IntPtr h)
        {
            byte[] page = new byte[0x1000];
            ulong[] addrs = new ulong[] { 0x100000UL, 0x1000UL, 0x2000000UL, 0x10000000UL, 0x40000000UL, 0x80000000UL };
            for (int i = 0; i < addrs.Length; i++)
            {
                if (LcRead(h, addrs[i], 0x1000, page)) return addrs[i];
            }
            return 0;
        }

        private static uint AllocScatter(out IntPtr pp, out IntPtr[] mems)
        {
            uint[] sizes = new uint[] { 1024, 768, 512, 256 };
            pp = IntPtr.Zero;
            mems = null;
            for (int i = 0; i < sizes.Length; i++)
            {
                IntPtr p;
                if (!LcAllocScatter1(sizes[i], out p) || p == IntPtr.Zero) continue;
                uint n = sizes[i];
                IntPtr[] m = new IntPtr[n];
                int k;
                for (k = 0; k < (int)n; k++) m[k] = Marshal.ReadIntPtr(p, k * IntPtr.Size);
                pp = p;
                mems = m;
                return n;
            }
            return 0;
        }

        public static SpeedSnap CopyLive()
        {
            lock (LiveLock)
            {
                SpeedSnap s = new SpeedSnap();
                s.Running = Live.Running;
                s.Connected = Live.Connected;
                s.Mbps = Live.Mbps;
                s.ReadsPerSec = Live.ReadsPerSec;
                s.TotalBytes = Live.TotalBytes;
                s.TotalReads = Live.TotalReads;
                s.Failed = Live.Failed;
                s.Address = Live.Address;
                s.Chunk = Live.Chunk;
                return s;
            }
        }

        private static void ResetLive()
        {
            lock (LiveLock)
            {
                Live.Running = false;
                Live.Connected = false;
                Live.Mbps = 0;
                Live.ReadsPerSec = 0;
                Live.TotalBytes = 0;
                Live.TotalReads = 0;
                Live.Failed = 0;
                Live.Address = 0;
                Live.Chunk = 0;
            }
        }

        // Abre el FPGA, lee IDs y comprueba si hay lectura DMA del objetivo.
        public static DmaInfo Probe(Action<string> onLine)
        {
            DmaInfo inf = new DmaInfo();
            IntPtr h = IntPtr.Zero;
            try
            {
                Log(onLine, Loc.T("leech.info"));
                string err;
                h = Open(out err);
                if (h == IntPtr.Zero)
                {
                    inf.Error = err;
                    Log(onLine, err);
                    return inf;
                }
                FillInfo(h, inf);
                Log(onLine, Loc.T("leech.fw") + inf.FwMajor + "." + inf.FwMinor);
                Log(onLine, Loc.T("leech.fid") + inf.FpgaId.ToString("X"));
                Log(onLine, Loc.T("leech.did") + inf.DeviceId.ToString("X"));
                byte[] page = new byte[0x1000];
                ulong[] addrs = new ulong[] { 0x100000UL, 0x1000UL, 0x2000000UL, 0x10000000UL };
                for (int i = 0; i < addrs.Length; i++)
                {
                    if (LcRead(h, addrs[i], 0x1000, page))
                    {
                        inf.DmaReadOk = true;
                        break;
                    }
                }
                if (!inf.DmaReadOk) Log(onLine, Loc.T("leech.zero"));
                else Log(onLine, Loc.T("dma.readok"));
                return inf;
            }
            catch (DllNotFoundException)
            {
                inf.Error = Loc.T("err.leech.dll");
                Log(onLine, inf.Error);
                return inf;
            }
            catch (Exception ex)
            {
                inf.Error = ex.Message;
                Log(onLine, "ERROR LeechCore: " + ex.Message);
                return inf;
            }
            finally { if (h != IntPtr.Zero) try { LcClose(h); } catch { } }
        }

        public static void ReadInfo(Action<string> onLine)
        {
            Probe(onLine);
        }

        // Lectura continua scatter 4 KB (como pcileech probe), hasta que keepGoing sea false.
        public static void BenchmarkLive(Action<string> onLine, Func<bool> keepGoing)
        {
            ResetLive();
            IntPtr h = IntPtr.Zero;
            IntPtr scatter = IntPtr.Zero;
            try
            {
                Log(onLine, Loc.T("leech.bench"));
                string err;
                h = Open(out err);
                if (h == IntPtr.Zero)
                {
                    Log(onLine, err);
                    return;
                }
                LcSetOption(h, LC_OPT_FPGA_RETRY_ON_ERROR, 0);
                ulong pa = FindPage(h);
                if (pa == 0)
                {
                    Log(onLine, Loc.T("leech.zero"));
                    lock (LiveLock) { Live.Connected = true; }
                    return;
                }
                IntPtr[] mems;
                uint n = AllocScatter(out scatter, out mems);
                if (n == 0 || mems == null)
                {
                    Log(onLine, Loc.T("err.leech.dll"));
                    return;
                }
                uint chunk = n * 0x1000;
                ulong pa0 = pa;
                ulong paMax = pa0 + 0x10000000UL;
                Log(onLine, Loc.T("leech.chunk") + (chunk / 1024) + " KB scatter (" + n + " paginas) @ 0x" + pa.ToString("X"));
                lock (LiveLock)
                {
                    Live.Connected = true;
                    Live.Running = true;
                    Live.Address = pa;
                    Live.Chunk = chunk;
                }
                Stopwatch win = Stopwatch.StartNew();
                long bytesWin = 0, readsWin = 0;
                long bytesAll = 0, readsAll = 0, failAll = 0;
                while (keepGoing())
                {
                    int i;
                    for (i = 0; i < (int)n; i++)
                    {
                        Marshal.WriteInt64(mems[i], 8, (long)(pa + (ulong)((uint)i * 0x1000)));
                        Marshal.WriteInt32(mems[i], 4, 0);
                    }
                    LcReadScatter(h, n, scatter);
                    int ok = 0, fail = 0;
                    for (i = 0; i < (int)n; i++)
                    {
                        if (Marshal.ReadInt32(mems[i], 4) != 0) ok++;
                        else fail++;
                    }
                    bytesWin += (long)ok * 0x1000;
                    readsWin += ok;
                    bytesAll += (long)ok * 0x1000;
                    readsAll += ok;
                    failAll += fail;
                    pa += chunk;
                    if (pa >= paMax) pa = pa0;
                    if (win.ElapsedMilliseconds >= 120)
                    {
                        double sec = win.Elapsed.TotalSeconds;
                        if (sec < 0.001) sec = 0.001;
                        lock (LiveLock)
                        {
                            Live.Mbps = (bytesWin / (1024.0 * 1024.0)) / sec;
                            Live.ReadsPerSec = readsWin / sec;
                            Live.TotalBytes = bytesAll;
                            Live.TotalReads = readsAll;
                            Live.Failed = failAll;
                            Live.Running = true;
                            Live.Connected = true;
                            Live.Address = pa;
                        }
                        bytesWin = 0;
                        readsWin = 0;
                        win.Restart();
                    }
                }
                lock (LiveLock) { Live.Running = false; }
                Log(onLine, Loc.T("leech.read") + (bytesAll / (1024 * 1024)) + " MB");
            }
            catch (DllNotFoundException)
            {
                Log(onLine, Loc.T("err.leech.dll"));
            }
            catch (Exception ex)
            {
                Log(onLine, "ERROR LeechCore: " + ex.Message);
            }
            finally
            {
                lock (LiveLock) { Live.Running = false; }
                if (scatter != IntPtr.Zero) try { LcMemFree(scatter); } catch { }
                if (h != IntPtr.Zero) try { LcClose(h); } catch { }
            }
        }
    }

    // Instalacion de drivers empaquetados en runtime\drivers.
    internal static class Drivers
    {
        public static bool HasZadig() { return File.Exists(AppPaths.ZadigExe); }

        public static bool HasPackage(string subfolder)
        {
            string dir = Path.Combine(AppPaths.DriversDir, subfolder);
            if (!Directory.Exists(dir)) return false;
            return FirstInf(dir) != null || FirstSetup(dir) != null;
        }

        public static string FirstInf(string dir)
        {
            if (!Directory.Exists(dir)) return null;
            string[] top = Directory.GetFiles(dir, "*.inf");
            if (top.Length > 0) return top[0];
            string[] infs = Directory.GetFiles(dir, "*.inf", SearchOption.AllDirectories);
            return infs.Length > 0 ? infs[0] : null;
        }

        public static string FirstSetup(string dir)
        {
            if (!Directory.Exists(dir)) return null;
            string[] names = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);
            for (int i = 0; i < names.Length; i++)
            {
                string n = Path.GetFileName(names[i]).ToLowerInvariant();
                if (n.IndexOf("zadig") >= 0) continue;
                return names[i];
            }
            return null;
        }

        public static int InstallFromFolder(string subfolder, Action<string> onLine)
        {
            string dir = Path.Combine(AppPaths.DriversDir, subfolder);
            if (!Directory.Exists(dir))
            {
                onLine(Loc.T("drv.nodir") + dir);
                return -1;
            }
            string inf = FirstInf(dir);
            if (inf != null) return InstallInf(inf, onLine);
            string setup = FirstSetup(dir);
            if (setup != null)
            {
                onLine(Loc.T("ok.setup") + setup);
                try
                {
                    Process.Start(setup);
                    return 0;
                }
                catch (Exception ex)
                {
                    onLine("ERROR: " + ex.Message);
                    return -1;
                }
            }
            onLine(Loc.T("drv.noinf") + dir);
            onLine(Loc.T("ok.zadig"));
            if (File.Exists(AppPaths.ZadigExe))
            {
                try { Process.Start(AppPaths.ZadigExe); return 0; }
                catch (Exception ex) { onLine("ERROR: " + ex.Message); return -1; }
            }
            return -1;
        }

        public static int InstallInf(string infPath, Action<string> onLine)
        {
            if (!File.Exists(infPath))
            {
                onLine(Loc.T("err.inf") + infPath);
                return -1;
            }
            string args = "/add-driver \"" + infPath + "\" /install";
            onLine("> pnputil " + args);
            onLine("");
            return ProcRunner.Run(Path.Combine(Environment.SystemDirectory, "pnputil.exe"), args, null, onLine);
        }

        public static bool LaunchZadig(Action<string> onLine)
        {
            if (!File.Exists(AppPaths.ZadigExe))
            {
                onLine(Loc.T("err.zadig"));
                return false;
            }
            try
            {
                Process.Start(AppPaths.ZadigExe);
                onLine(Loc.T("ok.zadig"));
                return true;
            }
            catch (Exception ex)
            {
                onLine("ERROR: " + ex.Message);
                return false;
            }
        }
    }
}
