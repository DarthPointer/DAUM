using System;
using System.Collections.Generic;

namespace daum
{
    abstract class ReadMapOperation : Operation
    {
        int currentElementNumber;

        protected abstract string MapElementName { get; }
        protected abstract int EnumerationStart { get; }
        protected abstract int EnumerationIncrement { get; }

        protected void ReportElementContents(string message)
        {
            Console.WriteLine("  " + message);
        }

        protected abstract void PrepareForEnumeration();

        protected abstract bool HasNext();

        protected abstract void ReadNext(bool useJson, int nextIndex);

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            // TO DO: If JSON in args, use JSON

            ReadMap(useJSON: false);

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }

        protected void ReadMap(bool useJSON)
        {
            currentElementNumber = EnumerationStart;
            PrepareForEnumeration();

            while (HasNext())
            {
                if (useJSON)
                {

                }
                else
                {
                    Console.WriteLine("--------------------");
                    Console.WriteLine($"{MapElementName} {currentElementNumber}:");
                }
                ReadNext(useJSON, currentElementNumber);

                currentElementNumber += EnumerationIncrement;
            }
        }
    }
}
