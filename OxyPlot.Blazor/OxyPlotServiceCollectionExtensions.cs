using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OxyPlotServiceCollectionExtensions
    {
        /// <summary>
        /// Adds required JS support module for OxyPlot.Blazor
        /// </summary>
        /// <param name="serviceDescriptors"></param>
        /// <returns></returns>
        public static IServiceCollection AddOxyPlotBlazor(this IServiceCollection serviceDescriptors)
        {
            ArgumentNullException.ThrowIfNull(serviceDescriptors);
            serviceDescriptors.AddScoped<OxyPlot.Blazor.OxyPlotJsInterop>();
            return serviceDescriptors;
        }
    }
}
