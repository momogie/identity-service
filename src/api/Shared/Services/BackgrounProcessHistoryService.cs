using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services;

public class BackgrounProcessHistoryService
{
    public IDictionary<long, IDictionary<string, ProcessHistory>> Histories { get;set; } = new Dictionary<long, IDictionary<string, ProcessHistory>>();

}

public class ProcessHistory
{
    public string Id { get; set; }

    public string Name { get; set; }

    public int StepCount { get; set; } = 1;

    public int CurrentStep { get; set; } = 1;

    public string CurrentStepDescription { get; set; }

    public int Processed { get; set; }

    public int Total { get; set; }

    public double Percentage 
    { 
        get 
        {
            if (CurrentStep < 1 || CurrentStep > StepCount)
                return 0;

            if (Total == 0)
                return 0;

            // Bobot per step
            double stepWeight = 100.0 / StepCount;

            // Persentase step yang sudah selesai sepenuhnya
            double progressStepSelesai = (CurrentStep - 1) * stepWeight;

            // Persentase dalam step saat ini
            double progressDalamStep = ((double)Processed / Total) * stepWeight;

            return progressStepSelesai + progressDalamStep;
        }
    }

    public DateTime StartedAt { get; set; }
}

public class ProcessHistoryItem
{
    public string Id { get; set; }
    public string ProcessId { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
