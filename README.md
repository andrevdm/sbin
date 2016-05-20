vbin
====

VBin - run .NET applications from a virtual provider such as MongoDB / SQL

Available on NuGet as VBin

Command Line
==========

vbin.exe AppToRun.exe param1 param2
vbin.exe -v=1 AppToRun.exe param1 param2
vbin.exe --cfg -v=1 "--MongoDB.Server=mongodb://localhost:27017" "--VBinBootStrapperType=VBin.MongoBootstrapper,vbin" "--VBinDatabase=AppVbin" -- AppToRun.exe param1 param2
