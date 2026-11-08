using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admission.BLL.DTOs.Classes
{
    public class CreateClassDto
    {
        public string Name { get; set; } = string.Empty;

        public int TotalSeats { get; set; }
    }
}
