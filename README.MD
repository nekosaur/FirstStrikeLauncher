# FirstStrikeLauncher

FirstStrikeLauncher was previously used by the Battlefield 2142 modification First Strike (http://www.fsmod.com). It includes the ability to incrementally update game data files using an unzip/zip process. The code has not been touched in probably 2-3 years. Code in general is super messy. Sorry about that.

## Dependencies
- Ionic.Zip
- NAppUpdate.Framework
- SerializedDictionary  (included in project) (pretty sure I found it somewhere on the internet, credit due whomever wrote it)

## Parts
- FirstStrikeLauncher is the GUI
- ModUpdater is a command line tool for actually updating the mod
- ModPacker is a command line tool for packing an unpacked mod folder (might not be finished and/or work)
- SerializedDictionary is used for storing GUI configuration file

## TODO
- I might have used a custom compiled version of NAppUpdate cause I seem to recall it causing a lot of problems. Replace it ASAP with a better library
- Contains a lot of code to deal with installations of the old launcher, can be thrown out
- I remember having problems sorting out UAC errors for a few people, not sure if they're still there
