using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.InfluxDB.Console.Console
{
    public class SampleOptions
    {
        public const string Sample = "Sample";
        public int Number { get; }
        public SampleOptions(int number)
        {
            Number = number;
        }

        public SampleOptions()
        {
            Number = 10000;
        }
    }
}
