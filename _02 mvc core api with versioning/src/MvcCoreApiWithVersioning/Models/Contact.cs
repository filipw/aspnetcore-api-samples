using System.ComponentModel.DataAnnotations;

namespace MvcCoreApi.Models
{
    public abstract class CommonContact
    {
        public int ContactId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string Email { get; set; }

        public string Twitter { get; set; }
    }

    public class Contact : CommonContact
    {
        public string State { get; set; }

        public string Zip { get; set; }
    }

    public class ContactV2 : CommonContact
    {
        public ContactV2(Contact c)
        {
            ContactId = c.ContactId;
            Name = c.Name;
            Address = c.Address;
            City = c.City;
            Email = c.Email;
            Twitter = c.Twitter;
        }

        public int CumulusNumber { get; set; }
    }
}