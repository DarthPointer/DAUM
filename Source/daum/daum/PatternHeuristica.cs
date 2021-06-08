using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    static class PatternHeuristica
    {
        public enum HeuristicaStatus
        {
            Failure = 0,
            NonCriticalFailure = 1,
            Success = 2
        }

        public static List<string> AssumedStructPattern(Program.RunData runData, ReadingContext readingContext, out HeuristicaStatus heuristicaStatus)
        {
            Int32 assumedEndOfStructOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;
            Int32 assumedSize = readingContext.collectionElementCount > 0 ?
                readingContext.declaredSize/readingContext.collectionElementCount :
                readingContext.collectionElementCount;

            try
            {
                string assumedLastName = ExportParsingMachine.FullNameString(runData.uexp, assumedEndOfStructOffset - 8);

                if (assumedLastName == ExportReadOperation.endOfStructConfigName)
                {
                    heuristicaStatus = HeuristicaStatus.Success;
                    return new List<string>() { "NTPL" };
                }
            }
            catch(Exception)
            {
                
            }

            heuristicaStatus = HeuristicaStatus.Failure;
            return new List<string>();
        }
    }
}
