#!/bin/bash

# Trojan Plus App - Android Build and Install Script

# 1. Build the Android project
echo "Building Android project..."
dotnet publish TrojanPlusApp.Android/TrojanPlusApp.Android.csproj -f net10.0-android -c Release

# 2. Check for connected devices
DEVICES=$(adb devices | grep -v "List" | grep "device")

if [ -z "$DEVICES" ]; then
    echo "No devices connected via adb. Please connect a device or start an emulator."
    exit 1
fi

# 3. Get the first device ID
DEVICE_ID=$(echo "$DEVICES" | head -n 1 | awk '{print $1}')
echo "Installing to device: $DEVICE_ID"

# 4. Install the APK
APK_PATH="TrojanPlusApp.Android/bin/Release/net10.0-android/publish/com.trojan_plus.android-Signed.apk"

if [ -f "$APK_PATH" ]; then
    adb -s "$DEVICE_ID" install -r "$APK_PATH"
else
    echo "Error: APK file not found at $APK_PATH"
    exit 1
fi

echo "Done!"
