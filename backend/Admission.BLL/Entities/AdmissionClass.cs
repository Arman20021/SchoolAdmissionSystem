using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admission.BLL.Entities
{
    public class AdmissionClass
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int TotalSeats { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
