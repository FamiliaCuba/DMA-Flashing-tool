External tools
==============

FM Flashing Tool calls these programs and libraries. They are not part of
this repository. Each one has its own license. Place the files next to
FamiliaCuba.exe using this layout:

runtime\
  leechcore.dll
  FTD3XX.dll
  openFPGALoader\
    openFPGALoader.exe
    libusb-1.0.dll, libftdi1.dll, and the other DLLs from its release
    spiOverJtag_xc7a35t.bit.gz
    spiOverJtag_xc7a75t.bit.gz
    spiOverJtag_xc7a100t.bit.gz
  drivers\
    wch\     CH347 package (inf, cat, sys, setup)
    ftdi\    ftdibus package (inf, cat, sys)
    rs232\   zadig-2.9.exe

Download
--------
  openFPGALoader   https://github.com/trabucayre/openFPGALoader/releases
  LeechCore        https://github.com/ufrisk/LeechCore/releases
  FTD3XX.dll       https://ftdichip.com/drivers/d3xx-drivers/
  Zadig            https://zadig.akeo.ie/
  WCH CH347        https://www.wch-ic.com/

Notes
-----
  The executable requests administrator rights. Flashing and driver setup
  need them.
  The speed test reads memory from the target PC. The card must be seated
  in that machine, with the FT601 USB link attached to the machine running
  this tool.
