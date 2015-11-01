# FirstStrikeLauncher

## Dependencies
- Ionic.Zip
- NAppUpdate.Framework
- SerializedDictionary  (included in project) (pretty sure I found it somewhere on the internet, credit due whomever wrote it)

## Parts
- FirstStrikeLauncher is the GUI
- ModUpdater is a separate command line tool for actually updating a mod
- ModPacker is a separate command line tool for packing an unpacked mod folder (might not be finished and or work)
- SerializedDictionary is used when storing GUI configuration file

## TODO
- I might have used a custom compiled version of NAppUpdate cause I seem to recall it causing a lot of problems. Replace it ASAP with a better library
- Contains a lot of code to deal with the old launcher, can be thrown out
- I remember having problems sorting out UAC errors for a few people, not sure if they're still there
- Code in general is super messy. Sorry about that