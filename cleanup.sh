#!/bin/sh

rm -rf Downloads
rm -rf Bindings/OpenTok.Net.iOS/bin
rm -rf Bindings/OpenTok.Net.iOS/obj
rm -rf Bindings/OpenTok.Net.Android/bin
rm -rf Bindings/OpenTok.Net.Android/obj

if [ -z "$1" ]
then
echo "for cleaning lib, add this parameter"
else
rm -rf Bindings/OpenTok.Net.iOS/lib
rm -rf Bindings/OpenTok.Net.Android/lib
fi



