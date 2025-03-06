%define debug_package %{nil}

Name: cross-platform-download-manager
Version: 0.3.0
Release: 1
Summary: Cross-platform download manager with resume support

License: AGPLv3
Source0: %{name}-%{version}.tar.gz
BuildArch: x86_64
Requires: lib64lttng-ust0

%description
CDM is a user-friendly tool for efficient file downloading across platforms, offering queue management and reliable performance. Released under AGPL.

%prep
%setup -q

%install
# Create directories
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/lib/cross-platform-download-manager
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/16x16/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/32x32/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/48x48/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/64x64/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/128x128/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/512x512/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/1024x1024/apps
mkdir -p %{buildroot}/usr/share/icons/hicolor/scalable/apps
mkdir -p %{buildroot}/usr/share/pixmaps

# Starter script
cp ./assets/cross-platform-download-manager %{buildroot}/usr/bin/
chmod +x %{buildroot}/usr/bin/cross-platform-download-manager # set executable permissions to starter script

# Desktop shortcut
cp ./assets/cross-platform-download-manager.desktop %{buildroot}/usr/share/applications/

# Desktop icon
# A 1024px x 1024px PNG, like VS Code uses for its icon
cp ./assets/icon-1024.png %{buildroot}/usr/share/pixmaps/cross-platform-download-manager.png

# Hicolor icons
cp ./assets/icon-16.png %{buildroot}/usr/share/icons/hicolor/16x16/apps/cross-platform-download-manager.png
cp ./assets/icon-32.png %{buildroot}/usr/share/icons/hicolor/32x32/apps/cross-platform-download-manager.png
cp ./assets/icon-48.png %{buildroot}/usr/share/icons/hicolor/48x48/apps/cross-platform-download-manager.png
cp ./assets/icon-64.png %{buildroot}/usr/share/icons/hicolor/64x64/apps/cross-platform-download-manager.png
cp ./assets/icon-128.png %{buildroot}/usr/share/icons/hicolor/128x128/apps/cross-platform-download-manager.png
cp ./assets/icon-256.png %{buildroot}/usr/share/icons/hicolor/256x256/apps/cross-platform-download-manager.png
cp ./assets/icon-512.png %{buildroot}/usr/share/icons/hicolor/512x512/apps/cross-platform-download-manager.png
cp ./assets/icon-1024.png %{buildroot}/usr/share/icons/hicolor/1024x1024/apps/cross-platform-download-manager.png
cp ./assets/icon-scalable.svg %{buildroot}/usr/share/icons/hicolor/scalable/apps/cross-platform-download-manager.svg

# Remove assets directory
rm -rf ./assets/

# Copy binaries
cp -r . %{buildroot}/usr/lib/cross-platform-download-manager/

# Change permissions
chmod -R a+rwX %{buildroot}/usr/lib/cross-platform-download-manager/
chmod +x %{buildroot}/usr/lib/cross-platform-download-manager/CrossPlatformDownloadManager.DesktopApp

%files
/usr/bin/cross-platform-download-manager
/usr/lib/cross-platform-download-manager/*
/usr/share/applications/cross-platform-download-manager.desktop
/usr/share/pixmaps/cross-platform-download-manager.png
/usr/share/icons/*

%changelog
* Wed Mar 05 2025 Your Name <your.email@example.com> - 1.0-1
- Initial RPM package