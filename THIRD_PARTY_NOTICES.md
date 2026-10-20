# Third-Party Notices

El código propio de **Familia Cuba** (carpeta `src/`) está bajo licencia **MIT**
(ver `LICENSE`). Para funcionar, la herramienta invoca por debajo varias
herramientas de terceros que **no forman parte de este repositorio** y se
distribuyen bajo sus propias licencias. Debes descargarlas y colocarlas en
`runtime/` (ver `runtime/README.txt`).

| Componente | Uso | Proyecto / Licencia |
|---|---|---|
| **openFPGALoader** | Flasheo del bitstream y detección JTAG (IDCODE) | trabucayre/openFPGALoader — ver su licencia |
| bitstreams `spiOverJtag_xc7a*.bit` | Proxy SPI-over-JTAG por modelo de FPGA | distribuidos con openFPGALoader |
| **LeechCore** (`leechcore.dll`) | Info del dispositivo y speed test DMA (FT601) | ufrisk/LeechCore — ver su licencia |
| **FTD3XX.dll** | Driver D3XX del FT601 (USB3) | FTDI — licencia FTDI |
| **Zadig** (`zadig-2.9.exe`) | Instalación de drivers WinUSB | pbatard/libwdi — GPLv3 |
| Drivers **WCH CH347/CH341** | Driver JTAG/UART | WCH — licencia WCH |
| Driver **FTDI ftdibus** | Driver serie/JTAG FTDI | FTDI — licencia FTDI |

> Nota legal: estas herramientas se usan como ejecutables/DLL externos (no se
> enlaza ni se copia su código fuente en este proyecto). Cada una conserva su
> propia licencia. Al redistribuir un paquete que las incluya, respeta los
> términos de cada componente e incluye sus avisos de licencia.

Este proyecto **no** reutiliza código de `sercanarga/fpga-dma-multi-tool`
(licencia PolyForm Noncommercial, incompatible con MIT); solo comparte el
enfoque de apoyarse en las mismas herramientas open-source anteriores.
