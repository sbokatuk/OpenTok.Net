#!/bin/sh

rm -rf Downloads

mkdir Downloads
cd Downloads

git clone https://github.com/opentok/opentok-ios-sdk-samples

cd opentok-ios-sdk-samples/Basic-Video-Chat-XCFramework

pod install

pod update

cd ../../..

mv Downloads/opentok-ios-sdk-samples/Basic-Video-Chat-XCFramework/Pods/OTXCFramework/OpenTok.xcframework Bindings/iOS/MAUI.Opentok

Version=$(cat Downloads/opentok-ios-sdk-samples/Basic-Video-Chat-XCFramework/Podfile.lock | grep "OTXCFramework (=" | grep -Eo '([0-9]{1,}\.)+[0-9]{1,}');

sed -E -i "" "s/version = ([0-9]{1,}\.)+[0-9]{1,}/version = ${Version}/" Bindings/iOS/MAUI.Opentok/MAUI.Opentok.sln

sed -E -i "" "s/<ReleaseVersion>([0-9]{1,}\.)+[0-9]{1,}/<ReleaseVersion>${Version}/" Bindings/iOS/MAUI.Opentok/MAUI.Opentok.csproj
rm -rf Downloads

echo "Downloaded Opentok framework with version $Version"