using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Abstracts
{
    public abstract class IntegrationEvent
    {
        public Guid Id { get;  set; }
        public DateTime CreationDate { get;  set; }

        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }
    }
}
