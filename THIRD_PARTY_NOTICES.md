# Third-party notices

The code in `src/` is original and licensed under MIT. See `LICENSE`.

To flash, identify a board, and measure DMA speed, the application starts external tools and loads external libraries. Those components are not included in this repository. Download them and place them under `runtime/` as described in `runtime/README.txt`. Each one keeps its own license. Nothing in this repository is a copy of their source code.

| Component | Role in this tool | Upstream |
|---|---|---|
| openFPGALoader | SPI flash, verify, backup, and JTAG IDCODE | [trabucayre/openFPGALoader](https://github.com/trabucayre/openFPGALoader) |
| `spiOverJtag_xc7a*.bit` | SPI-over-JTAG proxy for each Artix-7 part | Shipped with openFPGALoader |
| LeechCore (`leechcore.dll`) | Device info and FT601 DMA read benchmark | [ufrisk/LeechCore](https://github.com/ufrisk/LeechCore) |
| FTD3XX | USB3 access to the FT601 link | FTDI D3XX driver |
| Zadig | WinUSB install for the JTAG interface | [pbatard/libwdi](https://github.com/pbatard/libwdi), GPLv3 |
| WCH CH347 driver | JTAG programmer driver | WCH |
| FTDI ftdibus driver | FT2232 / RS232 programmer driver | FTDI |

If you redistribute a package that contains any of these files, keep that component’s license and copyright notices with it.
