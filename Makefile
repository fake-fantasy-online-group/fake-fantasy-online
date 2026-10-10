UNITY_EDITOR_BIN ?= Unity
BUILD_PATH ?= $(CURDIR)/Build/fake-fantasy-online.app
WINDOWS_BUILD_PATH ?= $(CURDIR)/Build/Windows/fake-fantasy-online.exe

.PHONY: build-macos build-windows

build-macos:
	@command -v "$(UNITY_EDITOR_BIN)" >/dev/null 2>&1 || { echo "Unity 6000.4.9f1 editor not found: $(UNITY_EDITOR_BIN)"; exit 1; }
	@FAKE_FANTASY_ONLINE_BUILD_PATH="$(BUILD_PATH)" "$(UNITY_EDITOR_BIN)" -batchmode -quit \
		-buildTarget OSXUniversal \
		-projectPath "$(CURDIR)" \
		-executeMethod FakeFantasyOnline.Editor.ClientBuild.BuildMacOS \
		-logFile -

build-windows:
	@command -v "$(UNITY_EDITOR_BIN)" >/dev/null 2>&1 || { echo "Unity 6000.4.9f1 editor not found: $(UNITY_EDITOR_BIN)"; exit 1; }
	@FAKE_FANTASY_ONLINE_WINDOWS_BUILD_PATH="$(WINDOWS_BUILD_PATH)" "$(UNITY_EDITOR_BIN)" -batchmode -quit \
		-buildTarget Win64 \
		-projectPath "$(CURDIR)" \
		-executeMethod FakeFantasyOnline.Editor.ClientBuild.BuildWindows64 \
		-logFile -
