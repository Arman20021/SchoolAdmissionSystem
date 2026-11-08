using Admission.BLL.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admission.BLL.Interfaces.Repositories
{
    public interface IClassRepository
    {
        Task AddAsync(
            AdmissionClass admissionClass,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<AdmissionClass>> GetAllAsync(
            CancellationToken cancellationToken = default
        );

        Task<AdmissionClass?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default
        );
    }
}
