using System.Collections.Generic;
using System.Threading.Tasks;

namespace MvcCoreApi.Models
{
    public interface IContactRepository
    {
        Task<IEnumerable<Contact>> GetAll();

        Task<Contact> Get(int id);

        Task<int> Add(Contact contact);

        Task Update(Contact updatedContact);

        Task Delete(int id);
    }
}
