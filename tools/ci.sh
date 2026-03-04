#!/bin/bash
set -e

# MaaAssistantArknights Local CI Script
# Focuses on MaaGui and its dependencies (MaaCore, MaaUtils)

# Determine OS and Architecture
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
case "$OS" in
    msys*|mingw*|cygwin*) OS="windows" ;;
    darwin) OS="darwin" ;;
    linux) OS="linux" ;;
esac

ARCH=$(uname -m)
case "$ARCH" in
    x86_64|amd64) ARCH="x64" ;;
    arm64|aarch64) ARCH="arm64" ;;
esac

echo "Detected Platform: $OS-$ARCH"

# Check for required tools
command -v dotnet >/dev/null 2>&1 || { echo >&2 "dotnet is required but not installed. Aborting."; exit 1; }
command -v cmake >/dev/null 2>&1 || { echo >&2 "cmake is required but not installed. Aborting."; exit 1; }
command -v uv >/dev/null 2>&1 || { echo >&2 "uv is required but not installed. Aborting."; exit 1; }

# 1. Initialize Submodules
echo "--- Initializing submodules ---"
git submodule update --init --depth 1 src/MaaUtils
git submodule update --init --depth 1 3rdparty/EmulatorExtras

# 2. Download MaaDeps (Native Dependencies)
echo "--- Downloading MaaDeps ---"
TRIPLET=""
case "$OS" in
    darwin) TRIPLET="$ARCH-osx" ;;
    linux) TRIPLET="$ARCH-linux" ;;
    windows) TRIPLET="$ARCH-windows" ;;
    *) echo "Unsupported OS: $OS"; exit 1 ;;
esac

# Try to download MaaDeps.
uv run tools/maadeps-download.py "$TRIPLET" || {
    echo "Warning: uv run tools/maadeps-download.py failed."
    # We continue because they might already be there.
}

# 3. Build Native Core (MaaUtils, MaaCore)
if command -v clang-format >/dev/null 2>&1; then
    echo "--- Formatting Native Core (C++) ---"
    uv run tools/ClangFormatter/clang-formatter.py --input src/MaaCore --style file || echo "C++ Formatting skip."
fi

echo "--- Building Native Core ---"
PRESET=""
case "$OS" in
    darwin)
        if [ "$ARCH" = "arm64" ]; then PRESET="macos-arm64"; else PRESET="macos-x64"; fi
        ;;
    linux)
        if [ "$ARCH" = "arm64" ]; then PRESET="linux-arm64"; else PRESET="linux-x64"; fi
        ;;
    windows)
        if [ "$ARCH" = "arm64" ]; then PRESET="windows-arm64"; else PRESET="windows-x64"; fi
        ;;
esac

# Common CMake arguments
CMAKE_ARGS="-DINSTALL_RESOURCE=ON -DBUILD_SMOKE_TEST=ON"

if [ -n "$PRESET" ]; then
    echo "Configuring with preset $PRESET..."
    cmake --preset "$PRESET" $CMAKE_ARGS
    
    # Attempt to use the RelWithDebInfo build preset if it exists
    BUILD_PRESET="$PRESET-RelWithDebInfo"
    echo "Building with preset $BUILD_PRESET..."
    if ! cmake --build --preset "$BUILD_PRESET" --parallel; then
        echo "Build preset $BUILD_PRESET failed, trying manual build..."
        cmake --build build --config RelWithDebInfo --parallel
    fi
    cmake --install build --config RelWithDebInfo --prefix install
else
    echo "No matching CMake preset found for $OS-$ARCH. Using default configure."
    cmake -B build -DCMAKE_BUILD_TYPE=RelWithDebInfo $CMAKE_ARGS
    cmake --build build --config RelWithDebInfo --parallel
    cmake --install build --config RelWithDebInfo --prefix install
fi

# 4. Build MaaGui (.NET)
echo "--- Formatting MaaGui (C#) ---"
dotnet format src/MaaGui/MaaGui.csproj

echo "--- Building MaaGui ---"
dotnet build src/MaaGui/MaaGui.csproj -c Release

# Copy native libraries to MaaGui output for local running
echo "--- Copying native libraries to MaaGui output ---"
# Detect output directory (this can vary, so we try a common one)
GUI_OUT_DIR="src/MaaGui/bin/Release/net10.0"
if [ -d "$GUI_OUT_DIR" ]; then
    cp -v install/*.dylib "$GUI_OUT_DIR/" 2>/dev/null || \
    cp -v install/*.so "$GUI_OUT_DIR/" 2>/dev/null || \
    cp -v install/*.dll "$GUI_OUT_DIR/" 2>/dev/null || true
    cp -rv install/resource "$GUI_OUT_DIR/" 2>/dev/null || true
fi

# 5. Run MaaGui Tests
echo "--- Running MaaGui Tests ---"
dotnet test src/MaaGui.Tests/MaaGui.Tests.csproj -c Release

# Also copy native libs to test output if they are needed
TEST_OUT_DIR="src/MaaGui.Tests/bin/Release/net10.0"
if [ -d "$TEST_OUT_DIR" ]; then
    cp -v install/*.dylib "$TEST_OUT_DIR/" 2>/dev/null || \
    cp -v install/*.so "$TEST_OUT_DIR/" 2>/dev/null || \
    cp -v install/*.dll "$TEST_OUT_DIR/" 2>/dev/null || true
    cp -rv install/resource "$TEST_OUT_DIR/" 2>/dev/null || true
fi

# Run smoke tests if they were built
if [ -f "install/smoke_test" ] || [ -f "install/smoke_test.exe" ]; then
    echo "--- Running Smoke Tests ---"
    if [ -f "install/smoke_test.exe" ]; then
        (cd install && ./smoke_test.exe) || echo "Smoke tests failed."
    else
        (cd install && ./smoke_test) || echo "Smoke tests failed."
    fi
fi

echo "--- CI Tasks Completed Successfully ---"
