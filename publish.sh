
cd Bindings/OpenTok.Net.iOS/bin/Release/
IOS=$(ls -1 | grep 'nupkg' | awk '{print $1}');
IOScount=$(ls -1 | grep 'nupkg' | awk '{print $1}' | wc -l | awk '{print $1}')
cd ../../../..
cd Bindings/OpenTok.Net.Android/bin/Release/
ANDROID=$(ls -1 | grep 'nupkg' | awk '{print $1}');
ANDROIDcount=$(ls -1 | grep 'nupkg' | awk '{print $1}' | wc -l | awk '{print $1}')
cd ../../../..

cd NugetPackages
GLOBAL=$(ls -1 | grep 'nupkg' | awk '{print $1}');
GLOBALcount=$(ls -1 | grep 'nupkg' | awk '{print $1}' | wc -l | awk '{print $1}')
cd ..

echo "found for iOS:"
echo "$IOS"
for i in $(seq "$IOScount")
do
currentNuget=$(echo "$IOS" | sed -n ''$i' p');
dotnet nuget push Bindings/OpenTok.Net.iOS/bin/Release/$currentNuget --api-key $1 --source https://api.nuget.org/v3/index.json --timeout 3000000
done


echo "found for Android:"
echo "$ANDROID"
for i in $(seq "$ANDROIDcount")
do
currentNuget=$(echo "$ANDROID" | sed -n ''$i' p');
dotnet nuget push Bindings/OpenTok.Net.Android/bin/Release/$currentNuget --api-key $1 --source https://api.nuget.org/v3/index.json --timeout 3000000
done

echo "found for global:"
echo "$GLOBAL"
for i in $(seq "$GLOBALcount")
do
currentNuget=$(echo "$GLOBAL" | sed -n ''$i' p');
dotnet nuget push NugetPackages/$currentNuget --api-key $1 --source https://api.nuget.org/v3/index.json --timeout 3000000
done