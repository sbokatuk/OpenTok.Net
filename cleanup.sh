#!/bin/sh

rm -rf Downloads
rm -rf Bindings/MAUI.Opentok.iOS/bin
rm -rf Bindings/MAUI.Opentok.iOS/obj
rm -rf Bindings/MAUI.Opentok.Android/bin
rm -rf Bindings/MAUI.Opentok.Android/obj

if [ -z "$1" ]
then
echo "for cleaning lib, add this parameter"
else
rm -rf Bindings/MAUI.Opentok.iOS/lib
rm -rf Bindings/MAUI.Opentok.Android/lib
fi



