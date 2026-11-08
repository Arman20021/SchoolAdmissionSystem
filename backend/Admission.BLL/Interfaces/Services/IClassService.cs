using Admission.BLL.DTOs.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admission.BLL.Interfaces.Services
{
    public interface IClassService
    {
        Task<ClassResponseDto> CreateAsync(
    CreateClassDto request,
    CancellationToken cancellationToken = default
);

        Task<IReadOnlyList<ClassResponseDto>> GetAllAsync(
            CancellationToken cancellationToken = default
        );

        Task<ClassResponseDto?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default
        );
    }
}
