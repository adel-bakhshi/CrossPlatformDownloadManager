#!/bin/bash

# Check if the correct number of arguments are provided
if [ $# -lt 2 ]; then
    echo "Error: Missing arguments!"
    echo "Usage: $0 <runtime> <version>"
    echo "Example: $0 osx-x64 v1.0.0"
    exit 1
fi

# Assign arguments
RUNTIME=$1
VERSION=$2

# App configuration
APP_NAME="CrossPlatformDownloadManager.DesktopApp"
APP_DISPLAY_NAME="Cross platform Download Manager (CDM)"
BUNDLE_NAME="Cross-platform.Download.Manager"
EXECUTABLE_NAME="CrossPlatformDownloadManager.DesktopApp"
BUNDLE_ID="adel.bakhshi.cdm"
BUNDLE_VERSION=$VERSION
ICON_NAME="$EXECUTABLE_NAME.icns"

# Create .app bundle structure
echo "Creating .app bundle structure..."
APP_DIR="./Deploy/macos/app/out/$RUNTIME/$BUNDLE_NAME.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"

# Store root directory
ROOT_DIR="$(pwd)"

# Clean previous build
echo "Cleaning previous build..."
rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR"
mkdir -p "$RESOURCES_DIR"

# Build the .NET app
echo "Building .NET app for $RUNTIME..."
dotnet publish "./CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:Version=$VERSION \
  -p:FileVersion=$VERSION \
  -p:AssemblyVersion=$VERSION \
  --runtime $RUNTIME \
  --output "$MACOS_DIR"

# Check if publish was successful
if [ $? -ne 0 ]; then
  echo "Error: .NET publish failed!"
  exit 1
fi

# Copy Info.plist template
cp "./Deploy/macos/app/Info.plist" "$CONTENTS_DIR/Info.plist"

# Replace placeholders (macOS compatible sed)
sed -i '' "s/__APP_NAME__/$APP_NAME/g" "$CONTENTS_DIR/Info.plist"
sed -i '' "s/__APP_DISPLAY_NAME__/$APP_DISPLAY_NAME/g" "$CONTENTS_DIR/Info.plist"
sed -i '' "s/__BUNDLE_ID__/$BUNDLE_ID/g" "$CONTENTS_DIR/Info.plist"
sed -i '' "s/__VERSION__/$VERSION/g" "$CONTENTS_DIR/Info.plist"
sed -i '' "s/__EXECUTABLE_NAME__/$EXECUTABLE_NAME/g" "$CONTENTS_DIR/Info.plist"
sed -i '' "s/__ICON_NAME__/$ICON_NAME/g" "$CONTENTS_DIR/Info.plist"
cat "$CONTENTS_DIR/Info.plist"

# Copy icon (if exists)
ICON_SOURCE="./Deploy/cdm-logo.icns"
if [ -f "$ICON_SOURCE" ]; then
    echo "Copying app icon..."
    cp "$ICON_SOURCE" "$RESOURCES_DIR/$ICON_NAME"
else
    echo "Warning: $ICON_NAME.icns not found. Using default icon."
fi

# Set executable permissions
echo "Setting file permissions..."
chmod +x "$MACOS_DIR/$APP_NAME"
chmod -R a+r "$APP_DIR"

# Create output directory for final .app
mkdir -p "./Deploy/bin"

# Create a zip file from the .app bundle
echo "Creating zip file from .app bundle..."
cd "$(dirname "$APP_DIR")"
zip -r "$BUNDLE_NAME.$VERSION.$RUNTIME.app.zip" "$BUNDLE_NAME.app"

# Check if zip was successful
if [ $? -ne 0 ]; then
  echo "Error: Failed to create zip file!"
  exit 1
fi

# Move the zip file to the output directory
mv "$BUNDLE_NAME.$VERSION.$RUNTIME.app.zip" "$ROOT_DIR/Deploy/bin/"

# Check if move was successful
if [ $? -ne 0 ]; then
  echo "Error: Failed to move zip to Deploy/bin!"
  exit 1
fi

# Verify final file exists
cd "$ROOT_DIR"
if [ -f "./Deploy/bin/$BUNDLE_NAME.$VERSION.$RUNTIME.app.zip" ]; then
    echo "Success: $BUNDLE_NAME.$VERSION.$RUNTIME.app.zip created at /Deploy/bin/"
    echo "File size: $(du -h "./Deploy/bin/$BUNDLE_NAME.$VERSION.$RUNTIME.app.zip" | cut -f1)"
else
    echo "Error: Final zip file not found!"
    exit 1
fi