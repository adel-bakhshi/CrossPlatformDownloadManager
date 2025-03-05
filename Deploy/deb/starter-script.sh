#!/bin/bash
# use exec to not have the wrapper script staying as a separate process
# "$@" to pass command line arguments to the app
exec /usr/lib/cross-platform-download-manager/CrossPlatformDownloadManager.DesktopApp
