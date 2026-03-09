PROJECT     = CodingTimeTrackerForSteam
PUBLISH_DIR = bin/Release/net8.0-windows/win-x64/publish
ISS_FILE    = Resources/InnoConfig.iss
ISCC        = "C:/Program Files (x86)/Inno Setup 6/ISCC.exe"

.PHONY: all build installer clean

all: build

build:
	dotnet publish -c Release -r win-x64 --self-contained true

installer: build
	$(ISCC) $(ISS_FILE)

clean:
	dotnet clean
	rm -rf bin obj
