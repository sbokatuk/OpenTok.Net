
cd Bindings/MAUI.Opentok.iOS/bin/Release/
IOS=$(ls -1 | grep 'nupkg' | awk '{print $1}');
IOScount=$(ls -1 | grep 'nupkg' | awk '{print $1}' | wc -l | awk '{print $1}')
cd ../../../..
cd Bindings/MAUI.Opentok.Android/bin/Release/
ANDROID=$(ls -1 | grep 'nupkg' | awk '{print $1}');
ANDROIDcount=$(ls -1 | grep 'nupkg' | awk '{print $1}' | wc -l | awk '{print $1}')
cd ../../../..

echo "found for iOS:"
echo "$IOS"
for i in $(seq "$IOScount")
do
currentNuget=$(echo "$IOS" | sed -n ''$i' p');
dotnet nuget push Bindings/MAUI.Opentok.iOS/bin/Release/$currentNuget --api-key $1 --source https://api.nuget.org/v3/index.json --timeout 3000000
done


echo "found for Android:"
echo "$ANDROID"
for i in $(seq "$ANDROIDcount")
do
currentNuget=$(echo "$ANDROID" | sed -n ''$i' p');
dotnet nuget push Bindings/MAUI.Opentok.Android/bin/Release/$currentNuget --api-key $1 --source https://api.nuget.org/v3/index.json --timeout 3000000
done