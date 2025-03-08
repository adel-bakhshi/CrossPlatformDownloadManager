#!/bin/bash
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/lib/cross-platform-download-manager/CrossPlatformDownloadManager.DesktopApp"