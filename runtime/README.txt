RUNTIME - herramientas externas
================================

Familia Cuba invoca estas herramientas por debajo. No se incluyen en el
repositorio (cada una tiene su propia licencia). Coloca los archivos con
ESTA estructura, junto al FamiliaCuba.exe:

runtime\
  leechcore.dll / FTD3XX.dll      <- YA INCLUIDOS (info + speed test)
  openFPGALoader\
    openFPGALoader.exe            <- YA INCLUIDO (flasheo, backup, verify, detect)
    (libusb-1.0.dll, libftdi1.dll, libusbK.dll ...)   <- dlls que trae su release
    data\
      spiOverJtag_xc7a35t.bit.gz
      spiOverJtag_xc7a75t.bit.gz
      spiOverJtag_xc7a100t.bit.gz
  leechcore.dll                   <- info del dispositivo + speed test
  FTD3XX.dll                      <- driver D3XX del FT601 (USB3)
  drivers\
    wch\    (CH347/CH341: *.inf, *.cat, *.sys, SETUP.EXE)
    ftdi\   (ftdibus: *.inf, *.cat, *.sys, Setup.exe)
    rs232\  zadig-2.9.exe   <- YA INCLUIDO (WinUSB para CH347/FTDI JTAG)

De donde descargarlas:
  - openFPGALoader:  https://github.com/trabucayre/openFPGALoader/releases
  - LeechCore:       https://github.com/ufrisk/LeechCore/releases  (leechcore.dll)
  - FTD3XX.dll:      driver D3XX de FTDI  (https://ftdichip.com/drivers/d3xx-drivers/)
  - Zadig:           https://zadig.akeo.ie/
  - Drivers WCH:     https://www.wch-ic.com/  (CH347)

Notas:
  - El .exe pide permisos de administrador (necesario para flashear/instalar drivers).
  - Para el speed test, la tarjeta debe estar conectada a un PC objetivo
    (LeechCore lee la memoria del objetivo por DMA).
