using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nictoarch.ServiceLink.Operator
{
    internal sealed class Settings
    {
        private Values m_values = new Values();

        internal int BatchEventDelayMs => this.m_values.batchEventDelayMs;
        internal int MaxBatchSize => this.m_values.maxMatchSize;
        internal int OperationTimeoutMs => this.m_values.operationTimeoutMs;
        internal double RequestExpirationSeconds => this.m_values.requestExpirationSeconds;

        private sealed class Values
        {
            public int batchEventDelayMs { get; set; } = 200;
            public int maxMatchSize { get; set; } = 1000;

            public int operationTimeoutMs { get; set; } = 500;
            public double requestExpirationSeconds { get; set; } = 5.0;
        }
    }

}
