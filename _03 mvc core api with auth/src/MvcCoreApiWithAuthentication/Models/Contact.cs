using System.ComponentModel.DataAnnotations;

namespace MvcCoreApiWithAuthentication.Models
{
    public class Contact
    {
        public int ContactId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string Email { get; set; }

        public string Twitter { get; set; }
    }
}