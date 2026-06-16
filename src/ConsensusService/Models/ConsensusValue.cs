using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsensusService.Models
{
    public class ConsensusValue
    {
        public long Id { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public int ParticipatingSensors { get; set; }
    }
}
