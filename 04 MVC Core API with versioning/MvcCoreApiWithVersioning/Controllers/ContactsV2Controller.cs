using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MvcCoreApi.Models;

namespace MvcCoreApi.Controllers
{
    [Route("contacts")]
    [ApiVersion("2")]
    public class ContactsV2Controller : ControllerBase
    {
        private readonly IContactRepository _repository;

        public ContactsV2Controller(IContactRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{id}", Name = "GetContactById")]
        public async Task<ActionResult> Get(int id)
        {
            var contact = await _repository.Get(id);
            if (contact == null)
            {
                return NotFound();
            }

            return Ok(new ContactV2(contact)
            {
                CumulusNumber = 12345678
            });
        }
    }
}
