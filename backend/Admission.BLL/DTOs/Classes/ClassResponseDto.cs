using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admission.BLL.DTOs.Classes
{
    public class ClassResponseDto
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int TotalSeats { get; set; }

        public int HeldSeats { get; set; }

        public int ConfirmedSeats { get; set; }

        public int AvailableSeats { get; set; }
    }
}
