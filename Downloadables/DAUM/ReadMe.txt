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

Script execution mode:
1) Make a text file containing DAUM commands and launch `...daum.exe -s [scirpt file]` or open a with ExecuteScript.bat.
	Drag'n'drop into the bat/`...bat [script file]`/set the bat to be default program to open an extension you choose for DAUM scripts and open it.

	Script mode loads DAUM with no file chosen so the script should start with -f [uasset file] before any other ops working with ue files.
	PreloadPatterns in the beginning of scripts is strongly recommended for performance reasons.

## Commands

Edited file is saved under name of the original one. .offset and .daum and other files are backups created after each command changing anything.

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

Uexp editing:

-echange operation.

-echange -r: Overwrites values. Format: -echange -r [export] [path] [new_value]
	Export is either
		name string + aug integer
		OR
		-i export object index
	Path is a set of '/'-separated names and indices, corelates with relevant pattern contents.
	New value is a string for new value. TO DO: describe tricky and non-conventional input formats.

	Additional args (add right after the mandatory part):
		-r: Report steps of search for target path, may be useful for debugging.
		-nullstr: DAUM gets string values from users without null terminator, but all
			strings stored in the files have it. Thus empty string in DAUM will turn
			into a single-char string with null-terminator. Use -nullstr if you need
			the string to become completely empty. New value will be ignored but
			CANT BE OMITED!
		-utf16: Store string as 16-bit based unicode. 8-bit is default, no arg for it.

	Path elements:
		If you need to get inside a property with name X, then use its name.
		If you need to change primitive type value, use TypeValuePattern/X, where X is the "index" of relevant type entry for current "context".
		If you need to get into array, use Array/X, X is the "index" of relevant array entry. X is 0 in most cases which are plain ArrayProperty.
		Use index to specify array element. Array/0/0 means 1st element of 1st array (see above).
	Path Example: 
		ExternalStruct/ArrayPropName/Array/0/0/ElementStructPropName/Float32/0
		This points us into a struct property with name "ExternalStruct", its "ArrayPropName" ArrayProperty, 1st element, "ElementStructPropName"
		property inside the element, to the first Float32 we see.
	
	SPECIAL CASE: Due to TextProperty being weird shit and having to direct support, you need to use a special path scheme to change its contents.
		The way to the property itself is regular. But then you have to define type and relative offset of its contents to change in path.
		...[path to text property]/TextProperty/I/X/Type/0.
		I is index of TP body to change, for plain TextProperty it is 0.
		X is relative offset from body start to data you change.
		Type is name of pattern element for your data.
		0 does not make much sense here but is not omitted in order for the used workaround to work.

-echange -a: Adds properties or collection elements. Format: -echange -a [export] [path] [generation params].

	Path works the same way as for -r. If you want to add a property, make it point to a struct ("" to
	add in the export itself). For array extension, point path to array (.../Array/X). New element is 
	added at the end.

	Generation Params:
		Data needed to understand what you want to add. It must be a sequence of parameters, put inside
		quotes if there are few, separated with space. Example: "IsPercentage BoolProperty". If you need
		one a parameter contain spaces, use \" combinations to quote it. "blah-blah \"param with spaces\"".

	Params for new property:
		Property name and property type. Each is either name string or -i [index].
		
		If the property is an Array, you need extra param - element type. If it is a struct - extra
		param for struct type. Array of structs will be "Name ArrayProperty StructProperty StructType".

	Params for new array element:
		You don't need any params for array extension but params cant be omitted, use "".

	SPECIAL CASE:
		TextProperty. Ma favorite. Needs extra param of raw bytes. Effectively inside \" quotes
		because it should be a space-separated string "of hexadecimal bytes", just the way they
		get copied to the clipboard from HxD.
	

Parser Funcionality:

-eread [export]

export is eiter -i index or name with aug (like Upgradable 0). DO NOT SKIP THE AUG EVEN IF IT IS ZERO!

Auxiliary commands:
nullConfig: Creates NullConfig.json with all fields you must/can fill in case you broke your config.

ONLY AS APPLICATION LAUNCH PARAMS: -s [Script File Name]:
Executes all strings of given file as interactive mode commands.

-f [Uasset File Name]: Loads given file as a new active file, uexp is changed automatically. Basically a replacement for exit and launch for the new file.

PreloadPatterns: Load all pattern files contents to boost performance, relevant for script execution mode. Don't use in interactive mode for your own convinience.
You will have to execute it again to apply changes in pattern files in case you executed it at least once in the current run of the app.

parse: Call DRG Parser to parse current file (if its path is configged, exception otherwise).

-o: Passes current file name to OffSetter with all the arguments you add further.

exit: Exit.

## Config
	offsetterPath: Absolute path of OffSetter.exe, pay attention to doubled '//'.
	drgParserPath: Absolute path of DRG Parser's exe, pay attention to doubled '//'.
	
	autoParseAfterSuccess: If true, DRG Parser is called to reparse files in case of a successful run of a command that changes the files.

	enablePatternReadingHeuristica: If true, heuristic assumptions will be applied in case of pattern lack. Powerful, yet has a minimal chance of causing 
		exceptions.