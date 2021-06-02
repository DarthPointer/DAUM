You MUST have Offseter tool 1.3 or higher downloaded for all currently available benefits from automation!

How to use:
0.0) Rename ExampleConfig.json to Config.json if you don't have Config.json yet. If you do,
check if Example contains any new fileds introduced you might want to add in Config.
0.1) Open Config.json and change offSetterPath value to absolute filename of OffSetter.exe.
0.2) Set your DRG Parser installation path to use parse command if you want automation levels grow insane.
0.3) In case you have configged parser path, you can also set a flag to run re-parsing automatically
after each change you made by DAUM. How far are we from getting a DIY IDE?

Interactive mode:
1) Drag'n'drop a .uasset to edit OR launch with the only argument (.uasset filepath).
2) Input commands.

Single command mode:
1) Launch with first argument being the .uasset's filepath and following arguments being command to execute.

5head mode:
1) Make a .bat calling DAUM with one command to execute each time combined with calling OffSetter to make most of your
changes done automatically. See example exe.bat. It is to be put to your M1K WPN files and launched to swap T1.Damage
and T5.FastReload mods.

Edited file is saved under name of the original one. .offset and .daum and otherfiles are backups created after each command changing anything.

Commands:
Uasset map commands are: [block] [operation] [operation params]

Block: -i for Import Map, -n for Name Map, -edef for export map.
Operation: -a to add at the end of map, -r to replace record, -edef does not support -r as of now.

Whenever you have options to specify something in different ways:
-i stands for by index. If you don't use -i, then the tool assumes you give it a name string to search for.
-e in case of import/export choice: without -e name string is used to search in imports, -e switches to search in exports.
If you input index via -i, you don't need to choose between imports and exports as .uasset uses positive for exports and
negative for imports. WARNING: for export names, both via index and string, you also must add name augmentation int after
the name. Import-via-namestring does not support name augmentation.
-s stands for skip. Used in replacement (-r) instead of value to keep the original one.

Name Map:
	-a: The only param is name you want to add.
	-r: The first param specifies which name you are replacing (via name string or -i index), the second is new string.

Import Map:
	-a: ClassPackage(name) ClassName(name) Outer(import) ObjectName(name). Names can be either string or -i index.
	Import is either ObjectName of another import or IMPORT'S -i index.
	-r: Same as -a, but you have extra name in the beginning - target's ObjectName string or IMPORT's index. You can -s
	skip 4 following values using -s instead of new values.

Export Map:
	-a: Class(import/export) SuperIndex(int) Template(i/e) Outer(i/e) Name(string/index) NameAugmentation(int) Flags(int)
	and 15 more 32-bit (4-byte) ints you are supposed to take starting from relative offset of 44 in Export Definition
	you use as a source of working values.
	-r: Not implemented.

Examples:

-n -r name1 name2				Replaces name1 with name2.

-i -a -i 10 className -i -4 objName		Creates import with "10th" (11th) name of ClassPackage, className, outer
						import having index -4 and objName.

-i -a [-i 10] className [-i -4] objName		Invalid syntax, just added brackets to make the command more understandable.

-i -a 11thName className 4thImportObjName objName
						Does the same thing but uses search for name string for all values.

-edef -a StatChangeStatusEffectItem 0 Default__StatChangeStatusEffectItem -e Default__STE_Revolver_Neurotoxin_C 0 StatChangeStatusEffectItem 2 41 0 0 0 0 0 0 0 0 1 0 18 1 1 2 1

						Are you alive? This one replicates StatChangeStatusEffectItem_1
						of revolver's STE with different name augmentation
						(copy is named StatChangeStatusEffectItem_2)

-edef -a [StatChangeStatusEffectItem] [0] [Default__StatChangeStatusEffectItem] [-e Default__STE_Revolver_Neurotoxin_C 0]
[StatChangeStatusEffectItem 2] [41] {0 0 0 0 0 0 0 0 1 0 18 1 1 2 1}

[StatChangeStatusEffectItem] and [Default__StatChangeStatusEffectItem] are on import/export positions. Neither -e nor -i
flags, so these are imports' ObjectName strings.

[-e Default__STE_Revolver_Neurotoxin_C 0] is another import/export, this time it is an export. Remember export name must
have augmentation after it!

Parser Funcionality:

-eread [export]

export is eiter -i index or name with aug (like Upgradable 0). DO NOT SKIP THE AUG EVEN IF IT IS ZERO!

Auxiliary commands:
nullconfig: Creates NullConfig.json with all fields you must/can fill in case you broke your config.

parse: Call DRG Parser to parse current file (if its path is configged, exception otherwise).

-o: Passes current file name to OffSetter with all the arguments you add further.

exit: Exit.