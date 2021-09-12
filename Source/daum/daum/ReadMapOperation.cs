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

        protected abstract void ReadNext();

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            ReadMap();

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }

        protected void ReadMap()
        {
            currentElementNumber = EnumerationStart;
            PrepareForEnumeration();

            while (HasNext())
            {
                Console.WriteLine("--------------------");
                Console.WriteLine($"{MapElementName} {currentElementNumber}:");
                ReadNext();

                currentElementNumber += EnumerationIncrement;
            }
        }
    }
}
