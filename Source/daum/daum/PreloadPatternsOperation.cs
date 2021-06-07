using System;
using System.Collections.Generic;
using System.IO;

namespace daum
{
    class PreloadPatternsOperation : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            doneSomething = true;
            useStandardBackup = false;

            IEnumerable<string> propertyPatternFiles = Directory.EnumerateFiles(Program.runData.toolDir + Program.PatternFolders.property);
            IEnumerable<string> bodyPatternFiles = Directory.EnumerateFiles(Program.runData.toolDir + Program.PatternFolders.body);
            IEnumerable<string> structPatternFiles = Directory.EnumerateFiles(Program.runData.toolDir + Program.PatternFolders.structure);

            Program.runData.preloadedPatterns.Clear();

            foreach (string file in propertyPatternFiles)
            {
                string patternKey = Program.PatternFolders.property + '/' + file.Substring(file.LastIndexOf('\\') + 1);
                Program.runData.preloadedPatterns[patternKey] = Program.ParseCommandString(File.ReadAllText(file));
            }

            foreach (string file in bodyPatternFiles)
            {
                string patternKey = Program.PatternFolders.body + '/' + file.Substring(file.LastIndexOf('\\') + 1);
                Program.runData.preloadedPatterns[patternKey] = Program.ParseCommandString(File.ReadAllText(file));
            }

            foreach (string file in structPatternFiles)
            {
                string patternKey = Program.PatternFolders.structure + '/' + file.Substring(file.LastIndexOf('\\') + 1);
                Program.runData.preloadedPatterns[patternKey] = Program.ParseCommandString(File.ReadAllText(file));
            }

            Program.runData.patternsArePreloaded = true;

            return "";
        }
    }
}
