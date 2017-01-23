using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcCoreApiWithDocs.Models;

namespace MvcCoreApiWithDocs.Controllers
{
    [Route("[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactRepository _repository;

        public ContactsController(IContactRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Fetch all available contacts
        /// </summary>
        /// <response code="200">Contacts returned (could be an empty array)</response>
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _repository.GetAll());
        }

        /// <summary>
        /// Fetch a single contact by its ID
        /// </summary>
        /// <param name="id">ID of the contact (integer)</param>
        /// <response code="200">A contact resource</response>
        /// <response code="404">No contact with a given ID exists</response>
        [HttpGet("{id}", Name = "GetContactById")]
        public async Task<ActionResult> Get(int id)
        {
            var contact = await _repository.Get(id);
            if (contact == null)
            {
                return NotFound();
            }

            return Ok(contact);
        }

        /// <summary>
        /// Create a new contact
        /// </summary>
        /// <param name="contact">Contact to create.</param>
        /// <response code="201">Contact created successfully</response>
        /// <response code="400">The payload of the request was invalid.</response>
        [HttpPost("")]
        [Authorize("WritePolicy")]
        public async Task<IActionResult> Post([FromBody]Contact contact)
        {
            if (ModelState.IsValid)
            {
                var newId = await _repository.Add(contact);
                return CreatedAtRoute("GetContactById", new { id = newId }, contact);
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Update an existing contact. Note - all of the properties will be overwritten.
        /// </summary>
        /// <param name="id">ID of the contact (integer)</param>
        /// <param name="contact">Updated contact.</param>
        /// <response code="204">Contact updated correctly</response>
        /// <response code="400">The payload of the request was invalid.</response>
        [HttpPut("{id}")]
        [Authorize("WritePolicy")]
        public async Task<IActionResult> Put(int id, [FromBody]Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.ContactId = id;
                await _repository.Update(contact);
                return NoContent();
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Remove an existing contact by its ID
        /// </summary>
        /// <param name="id">ID of the contact (integer)</param>
        /// <response code="204">Contact successfully removed</response>
        /// <response code="404">No contact with a given ID exists</response>
        [HttpDelete("{id}")]
        [Authorize("WritePolicy")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _repository.Get(id);
            if (deleted == null)
            {
                return NotFound();
            }

            await _repository.Delete(id);
            return NoContent();
        }
    }
}
