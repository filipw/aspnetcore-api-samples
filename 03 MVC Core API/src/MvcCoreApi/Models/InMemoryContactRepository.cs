using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MvcCoreApi.Models
{
    public class InMemoryContactRepository : IContactRepository
    {
        private readonly List<Contact> _contacts = new List<Contact>
        {
            new Contact { ContactId = 1, Name = "Filip W", Address = "107 Atlantic Avenue", City = "Toronto", State = "ON", Zip = "M6K 1Y2", Email = "filip.wojcieszyn@climaxmedia.com", Twitter = "filip_woj" },
            new Contact { ContactId = 2, Name = "Josh Donaldson", Address = "1 Blue Jays Way", City = "Toronto", State = "ON", Zip = "M5V 1J1", Email = "joshd@bluejays.com", Twitter = "BringerOfRain20" },
            new Contact { ContactId = 3, Name = "Aaron Sanchez", Address = "1 Blue Jays Way", City = "Toronto", State = "ON", Zip = "M5V 1J1", Email = "aarons@bluejays.com", Twitter = "A_Sanch41" },
            new Contact { ContactId = 4, Name = "Jose Bautista", Address = "1 Blue Jays Way", City = "Toronto", State = "ON", Zip = "M5V 1J1", Email = "joseb@bluejays.com", Twitter = "JoeyBats19" },
            new Contact { ContactId = 5, Name = "Edwin Encarnacion", Address = "1 Blue Jays Way", City = "Toronto", State = "ON", Zip = "M5V 1J1", Email = "edwine@bluejays.com", Twitter = "Encadwin" },
        };

        public Task<IEnumerable<Contact>> GetAll()
        {
            return Task.FromResult(_contacts.AsEnumerable());
        }

        public Task<Contact> Get(int id)
        {
            return Task.FromResult(_contacts.FirstOrDefault(x => x.ContactId == id));
        }

        public Task<int> Add(Contact contact)
        {
            var newId = (_contacts.LastOrDefault()?.ContactId ?? 0) + 1;
            contact.ContactId = newId;
            _contacts.Add(contact);
            return Task.FromResult(newId);
        }

        public async Task Update(Contact updatedContact)
        {
            var contact = await Get(updatedContact.ContactId).ConfigureAwait(false);
            if (contact == null)
            {
                throw new InvalidOperationException(string.Format("Contact with id '{0}' does not exists", updatedContact.ContactId));
            }

            contact.Address = updatedContact.Address;
            contact.City = updatedContact.City;
            contact.Email = updatedContact.Email;
            contact.Name = updatedContact.Name;
            contact.State = updatedContact.State;
            contact.Twitter = updatedContact.Twitter;
            contact.Zip = updatedContact.Zip;
        }

        public async Task Delete(int id)
        {
            var contact = await Get(id).ConfigureAwait(false);
            if (contact == null)
            {
                throw new InvalidOperationException(string.Format("Contact with id '{0}' does not exists", id));
            }

            _contacts.Remove(contact);
        }
    }
}