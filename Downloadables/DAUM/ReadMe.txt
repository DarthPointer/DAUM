This is an early test version! Weird shit can happen!

You MUST have Offseter tool 1.1 or higher downloaded for actual benefits from automation!

Only one mode supported: d'n'drop a .uasset to initiate "interactive" mode. With only one operation.
-n -a [name_to_add_to_name_map].

How to use:
0.0) Open Config.json and change offSetterPath value to absolute filename of OffSetter.exe.
0.1) Set your DRG Parser installation path to use parse command if you want automation levels grow insane.
0.2) In case you have configged parser path, you can also set a flag to run re-parsing automatically
after each change you made by DAUM.

Interactive mode:
1) Drag'n'drop a .uasset to edit OR launch with the only argument (.uasset filepath).
2) Input commands.

Single command mode:
1) Launch with first argument being the .uasset's filepath and following arguments being command to execute.

5head mode:
1) Make a .bat calling DAUM with one command to execute each time combined with calling OffSetter to make most of your
changes done automatically. See example exe.bat. It is to be put to your M1K WPN files and launched to swap T1.Damage
and T5.FastReload mods.

Edited file is saved under name of the original one. .offset and .daum files are backups created after each command.

Commands:
[block] [operation] [operation params]

Block: -i for import map, -n for name map.
Operation: -a to add at the end of map, -r to replace record.

When replacing: Your next param determines which record to replace. It can be either name (parsed as Name in name map or
as ObjectName in import map) or -i and next param is index. For import map index is taken according to object codes:
starts with -1 and decrements for each next import record.

After specifying replacement target or add operation, you should give params for new/replacement record. For name record
you only need the name string. 
For import record, you should specify ClassPackage, ClassName, OuterIndex, Name. For import replacement, you can use -s keys 
to skip and leave according value unchanged. Each value can be either given as an index (-i [index], mind reversed indexes of
impot map) or as a name string. Name and ObjectName values are used to associate with actual indices put in the record
for name map and import map accordingly.