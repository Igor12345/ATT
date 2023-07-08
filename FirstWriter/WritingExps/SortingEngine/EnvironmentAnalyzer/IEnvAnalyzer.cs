using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SortingEngine.RuntimeConfiguration;

namespace SortingEngine.EnvironmentAnalyzer
{
   internal interface IEnvAnalyzer
   {
      IConfig SuggestConfig();
   }
}
