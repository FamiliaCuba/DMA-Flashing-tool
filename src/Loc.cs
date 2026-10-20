using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DmaTool
{
    // Textos por idioma. C# 5: sin interpolacion ni inicializadores modernos.
    internal static class Loc
    {
        public static string Lang = "es";
        private static Dictionary<string, Dictionary<string, string>> _all;
        private static readonly string[] _codes = {
            "es", "en", "de", "fr", "it", "pt", "nl", "pl", "ru", "tr", "zh", "ja", "ko"
        };
        private static readonly string[] _labels = {
            "ES", "EN", "DE", "FR", "IT", "PT", "NL", "PL", "RU", "TR", "ZH", "JA", "KO"
        };

        public static string[] Codes { get { return _codes; } }
        public static string[] Labels { get { return _labels; } }

        public static string LabelOf(string code)
        {
            int i;
            for (i = 0; i < _codes.Length; i++)
                if (_codes[i] == code) return _labels[i];
            return "EN";
        }

        public static bool Known(string code)
        {
            if (code == null) return false;
            int i;
            for (i = 0; i < _codes.Length; i++)
                if (_codes[i] == code) return true;
            return false;
        }

        public static void Init()
        {
            EnsureDicts();
            Lang = DetectOsLang();
            try
            {
                string p = Path.Combine(AppPaths.DataDir, "lang.txt");
                if (File.Exists(p))
                {
                    string v = File.ReadAllText(p).Trim().ToLowerInvariant();
                    if (Known(v)) Lang = v;
                }
            }
            catch { }
        }

        private static string DetectOsLang()
        {
            try
            {
                CultureInfo ui = CultureInfo.CurrentUICulture;
                if (ui == null) ui = CultureInfo.InstalledUICulture;
                string two = ui.TwoLetterISOLanguageName;
                if (two != null) two = two.ToLowerInvariant();
                if (Known(two)) return two;
            }
            catch { }
            return "en";
        }

        public static void Set(string lang)
        {
            Lang = Known(lang) ? lang : "en";
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "lang.txt"), Lang); }
            catch { }
        }

        public static string T(string key)
        {
            EnsureDicts();
            string v = Pick(Lang, key);
            if (v != null) return v;
            v = Pick("en", key);
            if (v != null) return v;
            v = Pick("es", key);
            if (v != null) return v;
            return key;
        }

        private static string Pick(string lang, string key)
        {
            Dictionary<string, string> d;
            string v;
            if (_all.TryGetValue(lang, out d) && d.TryGetValue(key, out v)) return v;
            return null;
        }

        private static void EnsureDicts()
        {
            if (_all != null) return;
            _all = new Dictionary<string, Dictionary<string, string>>();
            int i;
            for (i = 0; i < _codes.Length; i++)
                _all[_codes[i]] = new Dictionary<string, string>();
            Add("nav.menu", "MENU", "MENU");
            Add("nav.home", "Inicio", "Home");
            Add("nav.home.sub", "Guia y Discord", "Guide & Discord");
            Add("nav.drivers", "Drivers", "Drivers");
            Add("nav.drivers.sub", "CH347 / RS232", "CH347 / RS232");
            Add("nav.flash", "Flash", "Flash");
            Add("nav.flash.sub", "Escribir firmware", "Write firmware");
            Add("nav.dna", "DNA", "DNA");
            Add("nav.dna.sub", "Info dispositivo", "Device info");
            Add("nav.speed", "Speed Test", "Speed Test");
            Add("nav.speed.sub", "Velocidad DMA", "DMA speed");
            Add("nav.about", "Acerca de", "About");
            Add("nav.about.sub", "Version e info", "Version & info");
            Add("help.title", "\u2753  \u00bfNecesitas ayuda?", "\u2753  Need help?");
            Add("help.body", "Entra al Discord para soporte, consejos y actualizaciones.",
                "Join Discord for support, tips and updates.");
            Add("help.join", "Unirme al Discord", "Join Discord");
            Add("app.name", "FM Flashing Tool", "FM Flashing Tool");
            Add("header.tagline", "Firmware DMA premium",
                "Premium DMA Firmware");
            Add("footer.tagline", "  \u2022  FM Flashing Tool",
                "  \u2022  FM Flashing Tool");
            Add("brand.sub", "FM Flashing Tool", "FM Flashing Tool");

            Add("home.eyebrow", "Primeros pasos", "Getting Started");
            Add("home.title", "FM Flashing Tool", "FM Flashing Tool");
            Add("home.body",
                "Flasher de firmware DMA con lectura de info y test de velocidad. Sigue la guia y abre cada apartado desde el menu de la izquierda.",
                "DMA firmware flasher with device info and speed test. Follow the guide and open each section from the left menu.");
            Add("home.start", "Empezar", "Get started");
            Add("home.c1.title", "Instalar Driver", "Install Driver");
            Add("home.c1.body", "Abre Drivers. Elige CH347 o RS232 e instala. Acepta UAC si aparece.",
                "Open Drivers. Pick CH347 or RS232 and install. Accept UAC if asked.");
            Add("home.c2.title", "Flashear Firmware", "Flash Firmware");
            Add("home.c2.body", "Elige modelo FPGA, selecciona el .bin y pulsa Iniciar flasheo.",
                "Pick the FPGA model, select the .bin and press Start flashing.");
            Add("home.c3.title", "DNA / Info", "DNA / Info");
            Add("home.c3.body", "Detecta el IDCODE y lee la info del dispositivo (LeechCore).",
                "Detect the IDCODE and read device info (LeechCore).");
            Add("home.c4.title", "Speed Test", "Speed Test");
            Add("home.c4.body", "Mide el ancho de banda de lectura DMA via LeechCore.",
                "Measure DMA read bandwidth via LeechCore.");
            Add("home.open", "Abrir pagina  \u2192", "Open page  \u2192");

            Add("flash.title", "Flash", "Flash");
            Add("flash.sub", "Elige modelo, firmware .bin y flashea la SPI con openFPGALoader.",
                "Pick model, firmware .bin and flash SPI with openFPGALoader.");
            Add("flash.port", "Tipo de puerto", "Port Type");
            Add("flash.model", "Modelo FPGA", "FPGA Model");
            Add("flash.file", "Archivo destino", "Target File");
            Add("flash.pick", "Seleccionar archivo", "Choose file");
            Add("flash.start", "Iniciar flasheo", "Start flashing");
            Add("flash.cancel", "Cancelar", "Cancel");
            Add("flash.status", "Estado del flasheo", "Flashing Status");
            Add("flash.progress", "Progreso", "Progress");
            Add("flash.output", "Salida de comandos", "Command Output");
            Add("flash.clear", "Limpiar salida", "Clear output");
            Add("flash.ready", "Listo para flashear", "Ready to flash");
            Add("flash.running", "Flasheando... no desconectes la tarjeta.", "Flashing... do not unplug the card.");
            Add("flash.done", "FLASH COMPLETADO CORRECTAMENTE. Firmware verificado.",
                "FLASH COMPLETED SUCCESSFULLY. Firmware verified.");
            Add("flash.done.hint", "Ya puedes desconectar la tarjeta.",
                "You can unplug the card now.");
            Add("flash.err", "FALLO EL FLASHEO (rc=", "FLASH FAILED (rc=");
            Add("flash.wait.hint", "Normal que tarde 1-3 minutos. No cierres la app ni desconectes el USB.",
                "This often takes 1-3 minutes. Do not close the app or unplug USB.");
            Add("flash.wait.backup", "Paso 1/2: copia de seguridad...", "Step 1/2: backing up...");
            Add("flash.wait.write", "Paso 2/2: escribiendo y verificando...", "Step 2/2: writing and verifying...");
            Add("flash.wait.tick", "> Sigue trabajando... ", "> Still working... ");
            Add("flash.cancelled", "Cancelado", "Cancelled");
            Add("flash.needfile", "Selecciona primero un archivo .bin valido.",
                "Select a valid .bin file first.");
            Add("flash.placeholder", "Selecciona un archivo .bin...", "Select a .bin file...");
            Add("flash.user.cancel", "> Cancelado por el usuario.", "> Cancelled by user.");
            Add("flash.filter", "Bitstream (*.bin;*.bit;*.mcs)|*.bin;*.bit;*.mcs|Todos (*.*)|*.*",
                "Bitstream (*.bin;*.bit;*.mcs)|*.bin;*.bit;*.mcs|All (*.*)|*.*");
            Add("flash.detect", "Detectar cable", "Detect cable");
            Add("flash.detecting", "> Buscando programador", "> Looking for programmer");
            Add("flash.found", "> Cable detectado: ", "> Cable found: ");
            Add("flash.notfound", "> No se detecto CH347 ni FTDI. Elige el puerto a mano e instala el driver.",
                "> Neither CH347 nor FTDI found. Pick the port manually and install the driver.");
            Add("flash.backup", "> Haciendo copia de seguridad de la SPI (puede tardar)...",
                "> Backing up SPI flash (this may take a while)...");
            Add("flash.backup.ok", "> Backup guardado: ", "> Backup saved: ");
            Add("flash.backup.fail", "> No se pudo hacer backup. Se continua con el flasheo.",
                "> Backup failed. Continuing with flash.");
            Add("flash.verify", "> Grabando y verificando...", "> Writing and verifying...");
            Add("flash.hash", "Archivo: ", "File: ");

            Add("dna.title", "DNA / Info", "DNA / Info");
            Add("dna.sub", "Lee el DNA unico del FPGA y el IDCODE. Se calcula solo al abrir esta pagina.",
                "Reads the unique FPGA DNA and IDCODE. Calculated automatically when you open this page.");
            Add("dna.cable", "Programador", "Programmer");
            Add("dna.value", "DNA", "DNA");
            Add("dna.read", "Leer DNA", "Read DNA");
            Add("dna.copy", "Copiar", "Copy");
            Add("dna.copied", "Copiado", "Copied");
            Add("dna.waiting", "Leyendo DNA...", "Reading DNA...");
            Add("dna.reading", "> Leyendo DNA del FPGA...", "> Reading FPGA DNA...");
            Add("dna.none", "No se pudo leer el DNA. Conecta el programador JTAG a la tarjeta.",
                "Could not read DNA. Connect the JTAG programmer to the card.");
            Add("dna.nocopy", "Nada que copiar todavia.", "Nothing to copy yet.");
            Add("dna.info", "Info DMA (LeechCore)", "DMA info (LeechCore)");
            Add("dna.output", "Salida de comandos", "Command Output");

            Add("drv.title", "Drivers", "Drivers");
            Add("drv.sub", "Los instaladores van incluidos. Requiere administrador.",
                "Installers are bundled. Administrator required.");
            Add("drv.ch.title", "CH347 / WCH (JTAG)", "CH347 / WCH (JTAG)");
            Add("drv.ch.body", "Instala el driver WCH del CH347 o usa Zadig (WinUSB) para flashear.",
                "Install the WCH CH347 driver or use Zadig (WinUSB) to flash.");
            Add("drv.ch.btn", "Instalar CH347", "Install CH347");
            Add("drv.ftdi.title", "RS232 / FTDI", "RS232 / FTDI");
            Add("drv.ftdi.body", "Instala el driver FTDI (ftdibus) para variantes FT2232/RS232.",
                "Install the FTDI (ftdibus) driver for FT2232/RS232 boards.");
            Add("drv.ftdi.btn", "Instalar FTDI", "Install FTDI");
            Add("drv.zadig", "Abrir Zadig (WinUSB)", "Open Zadig (WinUSB)");
            Add("drv.output", "Salida de comandos", "Command Output");
            Add("drv.ready", "Pulsa Instalar y espera el resultado aqui.",
                "Press Install and wait for the result here.");
            Add("drv.success", "INSTALADO CORRECTAMENTE. El driver ya esta en Windows.",
                "INSTALLED SUCCESSFULLY. The driver is now in Windows.");
            Add("drv.fail", "FALLO LA INSTALACION. Revisa el detalle o prueba Zadig.",
                "INSTALL FAILED. Check the details or try Zadig.");
            Add("drv.status", "Estado del paquete interno", "Bundled package status");
            Add("drv.ok", "incluido", "bundled");
            Add("drv.missing", "no encontrado", "not found");
            Add("drv.nodir", "No existe la carpeta de driver: ", "Driver folder missing: ");
            Add("drv.noinf", "No se encontro ningun .inf ni instalador en ",
                "No .inf or installer found in ");

            Add("spd.title", "Speed Test", "Speed Test");
            Add("spd.sub", "Conexion DMA, MB/s y reads/s en tiempo real (LeechCore / FT601).",
                "DMA link, MB/s and reads/s live (LeechCore / FT601).");
            Add("spd.result", "Velocidad en vivo", "Live speed");
            Add("spd.run", "Iniciar test en vivo", "Start live test");
            Add("spd.stop", "Detener", "Stop");
            Add("spd.check", "Comprobar", "Check");
            Add("spd.mbps", "MB/s", "MB/s");
            Add("spd.reads", "Reads / s", "Reads / s");
            Add("spd.total", "Leido", "Read");
            Add("spd.output", "Salida de comandos", "Command Output");
            Add("spd.hint.off", "Conecta el USB del FT601 al PC atacante y la tarjeta al PCIe del objetivo.",
                "Plug FT601 USB into the attacker PC and the card into the target PCIe slot.");
            Add("spd.hint.on", "DMA listo. Pulsa Iniciar para ver MB/s y reads/s en vivo.",
                "DMA ready. Press Start to see live MB/s and reads/s.");
            Add("spd.hint.fpga", "FPGA USB OK, pero no hay lectura. La tarjeta tiene que estar en el PC objetivo.",
                "FPGA USB OK, but no memory read. The card must be in the target PC.");
            Add("spd.hint.run", "Midiendo en vivo... no desconectes el USB.",
                "Live measuring... do not unplug USB.");
            Add("spd.fail", "Lecturas fallidas: ", "Failed reads: ");
            Add("dma.checking", "DMA  ·  comprobando", "DMA  ·  checking");
            Add("dma.on", "DMA  ·  conectado", "DMA  ·  connected");
            Add("dma.off", "DMA  ·  no conectado", "DMA  ·  not connected");
            Add("dma.fpgaonly", "FPGA USB  ·  sin lectura", "FPGA USB  ·  no read");
            Add("dma.readok", "  Lectura DMA del objetivo: OK", "  Target DMA read: OK");

            Add("about.title", "Acerca de", "About");
            Add("about.sub", "Familia Cuba \u00b7 Artix-7 DMA",
                "Familia Cuba \u00b7 Artix-7 DMA");
            Add("about.heading", "FM Flashing Tool", "FM Flashing Tool");
            Add("about.ver", "v1.0  \u00b7  MIT", "v1.0  \u00b7  MIT");
            Add("about.body",
                "Flashea firmware DMA, instala drivers, lee la info del dispositivo y mide la velocidad.",
                "Flash DMA firmware, install drivers, read device info and measure speed.");
            Add("about.inc.title", "Incluido en el .exe", "Bundled in the .exe");
            Add("about.inc.body", "openFPGALoader, LeechCore, FTD3XX, drivers CH347/FTDI y Zadig. No hace falta ninguna carpeta extra.",
                "openFPGALoader, LeechCore, FTD3XX, CH347/FTDI drivers and Zadig. No extra folder needed.");
            Add("about.lic.title", "Licencia", "License");
            Add("about.lic.body", "Codigo propio MIT. openFPGALoader y LeechCore van con sus propias licencias.",
                "Original code MIT. openFPGALoader and LeechCore keep their own licenses.");
            Add("about.join", "Unirme al Discord", "Join Discord");

            Add("err.noexe", "ERROR: no se encontro el ejecutable (deberia ir dentro de la app):",
                "ERROR: executable not found (it should be bundled in the app):");
            Add("err.payload.missing", "ERROR: el paquete interno (openFPGALoader/drivers/LeechCore) no esta en el .exe. Recompila con pack.ps1 / build.bat.",
                "ERROR: internal package (openFPGALoader/drivers/LeechCore) is missing from the .exe. Rebuild with pack.ps1 / build.bat.");
            Add("err.nobin", "ERROR: el archivo .bin no existe: ", "ERROR: .bin file does not exist: ");
            Add("err.leech.open", "ERROR: LeechCore no pudo abrir el dispositivo FPGA (conecta la tarjeta y comprueba FTD3XX).",
                "ERROR: LeechCore could not open the FPGA device (connect the card and check FTD3XX).");
            Add("err.leech.dll", "ERROR: no se pudo cargar leechcore.dll / FTD3XX.dll (paquete interno).",
                "ERROR: could not load leechcore.dll / FTD3XX.dll (bundled package).");
            Add("err.inf", "ERROR: no se encontro el .inf: ", "ERROR: .inf not found: ");
            Add("err.zadig", "ERROR: falta Zadig en el paquete interno.",
                "ERROR: Zadig missing from the bundled package.");
            Add("ok.zadig", "> Zadig lanzado. Selecciona la interfaz e instala WinUSB.",
                "> Zadig launched. Select the interface and install WinUSB.");
            Add("ok.setup", "> Lanzando instalador: ", "> Launching installer: ");
            Add("leech.info", "> Leyendo info del dispositivo (LeechCore)...",
                "> Reading device info (LeechCore)...");
            Add("leech.fw", "  Firmware FPGA: v", "  FPGA firmware: v");
            Add("leech.fid", "  FPGA ID: 0x", "  FPGA ID: 0x");
            Add("leech.did", "  Device ID (bus:dev.fn): 0x", "  Device ID (bus:dev.fn): 0x");
            Add("leech.bench", "> Speed test DMA en vivo (LeechCore, algo=0, bloques grandes)...",
                "> Live DMA speed test (LeechCore, algo=0, large chunks)...");
            Add("leech.chunk", "> Tamano de bloque: ", "> Chunk size: ");
            Add("leech.read", "  Leidos ", "  Read ");
            Add("leech.zero", "  (0 MB: la tarjeta debe estar conectada a un PC objetivo para leer su memoria)",
                "  (0 MB: the card must be connected to a target PC to read its memory)");
            LangPack.Fill(_all);
        }

        private static void Add(string key, string es, string en)
        {
            _all["es"][key] = es;
            _all["en"][key] = en;
        }
    }
}
