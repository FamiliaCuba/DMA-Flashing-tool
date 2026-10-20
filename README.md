# Familia Cuba — DMA Flashing Tool

Utilidad de Windows para tarjetas **PCILeech FPGA (Artix-7)**. Interfaz oscura
estilo "RED DMA". Flashea firmware, instala drivers, lee la info del dispositivo
y mide la velocidad de DMA.

> Soporte y comunidad: **https://discord.gg/WecrFkBGGx**

## Funciones

- **Flash** — Escribe un `.bin` en la flash SPI con `openFPGALoader` (cable
  **CH347** o **FTDI**, y modelo **35T / 75T / 100T**).
- **DNA / Info** — Detecta el **IDCODE** (JTAG) y lee la info del dispositivo
  (versión de firmware, FPGA ID) vía **LeechCore**.
- **Speed Test** — Mide el ancho de banda de lectura DMA (FT601) vía LeechCore.
- **Drivers** — Instala CH347/WCH y FTDI (`pnputil`) o abre **Zadig**.

## Requisitos

- Windows 10/11 x64, ejecutar como **administrador**.
- Las herramientas externas en `runtime/` (ver `runtime/README.txt`):
  `openFPGALoader`, `leechcore.dll`, `FTD3XX.dll`, drivers y Zadig.

## Compilar

No necesita Visual Studio ni .NET SDK: la GUI es **WPF por código** y se compila
con el `csc.exe` de .NET Framework 4.x incluido en Windows.

```
build.bat
```

Genera `FamiliaCuba.exe`.

## Licencia

Código propio bajo **MIT** (ver `LICENSE`). Las herramientas de terceros que usa
por debajo se distribuyen aparte bajo sus propias licencias
(ver `THIRD_PARTY_NOTICES.md`). Este proyecto **no** reutiliza código de la
herramienta de sercam (PolyForm Noncommercial); solo comparte el enfoque de
apoyarse en las mismas utilidades open-source (openFPGALoader / LeechCore).
