﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Gridsum.DataflowEx.AutoCompletion
{
    public class RingMonitor
    {
        private readonly Dataflow m_host;
        private readonly IRingNode[] m_ringNodes;
        private readonly Lazy<string> m_lazyDisplayName;
        private IHeartbeatNode m_hb;
        private readonly ILogger<RingMonitor> _logger;
        public RingMonitor(Dataflow host, IRingNode[] ringNodes, ILogger<RingMonitor> logger = null)
        {
            _logger = logger;
            if (ringNodes.Length == 0)
            {
                throw new ArgumentException("The child ring contains nothing");
            }

            m_hb = ringNodes.OfType<IHeartbeatNode>().FirstOrDefault();
            if (m_hb == null)
            {
                throw new ArgumentException("A ring must contain at least one IHeartbeatNode");
            }

            var nonChild = ringNodes.FirstOrDefault(r => { return !host.IsMyChild(r); });
            if (nonChild != null)
            {
                throw new ArgumentException("The child ring contains a non-child: " + nonChild.FullName);
            }

            this.m_host = host;
            this.m_ringNodes = ringNodes;

            m_lazyDisplayName = new Lazy<string>(this.GetDisplayName);
        }

        private string GetDisplayName()
        {
            string ringString = string.Join("=>", m_ringNodes.Select(n => n.Name));
            return string.Format("({0})", ringString);
        }

        public string DisplayName
        {
            get
            {
                return m_lazyDisplayName.Value;
            }
        }
       
        public async Task StartMonitoring(Task preTask)
        {
            _logger?.LogInformation("{0} A ring is set up: {1}", m_host.FullName, DisplayName);
          
            await preTask.ConfigureAwait(false);
            _logger?.LogInformation("{0} Ring pretask done. Starting check loop for {1}", m_host.FullName, DisplayName);
          

            while (true)
            {
                await Task.Delay(m_host.m_dataflowOptions.MonitorInterval).ConfigureAwait(false);

                bool empty = false;
                
                if (m_hb.NoHeartbeatDuring(() => { empty = this.IsRingEmpty(); }) && empty)
                {
                    _logger.LogDebug("{0} 1st level empty ring check passed for {1}", m_host.FullName, this.DisplayName);

                    bool noHeartbeat = await m_hb.NoHeartbeatDuring(
                        async () =>
                            {
                                //trigger batch
                                foreach (var batchedFlow in m_ringNodes.OfType<IBatchedDataflow>())
                                {
                                    batchedFlow.TriggerBatch();
                                }

                                await Task.Delay(m_host.m_dataflowOptions.MonitorInterval).ConfigureAwait(false);

                                empty = this.IsRingEmpty();
                            }).ConfigureAwait(false);

                    if (noHeartbeat && empty)
                    {
                        _logger.LogDebug("{0} 2nd level empty ring check passed for {1}", m_host.FullName, this.DisplayName);

                        m_hb.Complete(); //start the completion domino :)
                        break;
                    }
                    else
                    {
                        //batched flow dumped some hidden elements to the flow, go back to the loop
                        _logger.LogDebug("{0} New elements entered the ring by batched flows ({1}). Will fall back to 1st level empty ring check.", m_host.FullName, this.DisplayName);
                    }
                }
            }

            _logger.LogInformation("{0} Ring completion detected! Completion triggered on heartbeat node: {1}", m_host.FullName, DisplayName);
        }

        private bool IsRingEmpty()
        {
            var hb = m_ringNodes.OfType<IHeartbeatNode>().First();

            if (m_ringNodes.All(r => !r.IsBusy))
            {
                long before = hb.ProcessedItemCount;
                int buffered = m_ringNodes.Sum(r => r.BufferedCount) + m_ringNodes[0].BufferedCount;
                long after = hb.ProcessedItemCount;

                if (buffered == 0 && before == after)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
