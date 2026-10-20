# FM Flashing Tool

Windows desktop application for PCILeech FPGA cards based on Xilinx Artix-7. It is a single workstation for the tasks that usually sit in separate command-line tools: writing SPI firmware, installing the programmer drivers, reading device identity, and measuring DMA read speed.

The interface is original. Firmware and device access go through well-known external utilities, invoked as separate programs and libraries. Their source is not part of this repository.

Community and support: https://discord.gg/WecrFkBGGx

## What it covers

- **Flash.** Writes a `.bin` to SPI with openFPGALoader, after an optional backup, and verifies the result. Supports CH347 and FTDI programmers and the 35T, 75T, and 100T parts.
- **Drivers.** Installs the bundled CH347/WCH and FTDI packages, or opens Zadig when WinUSB is required.
- **DNA / Info.** Reads the FPGA IDCODE and the unique device DNA over JTAG, and can copy the result. Device details from LeechCore are shown alongside.
- **Speed test.** Reports whether DMA is connected and measures live read throughput over FT601 with LeechCore.
- **Language.** The UI follows the Windows display language. Thirteen languages are included. Any other system language opens in English, and the choice can be changed from the window.

## Requirements

- Windows 10 or 11, 64-bit.
- Administrator rights. Flashing and driver installation need them.
- The external tools listed in `runtime/README.txt`, placed next to the executable. They are not stored in this repository.

## Build

No Visual Studio or .NET SDK is required. The interface is WPF written entirely in C# and is compiled with the `csc.exe` shipped with .NET Framework 4.x.

```
build.bat
```

The script produces `FamiliaCuba.exe`.

## License

Original code is MIT. See `LICENSE`.

openFPGALoader, LeechCore, FTDI libraries, Zadig, and the WCH drivers are separate works. This project calls them. It does not vendor their source. Their terms stay with each project. See `THIRD_PARTY_NOTICES.md`.
