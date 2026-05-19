using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Akyildiz.Sevkiyat.Application.ExternalEmailContacts.Commands
{
    public record CreateExternalEmailContactCommand(string Name, string Email, string? Note) : IRequest<int>;

    public class CreateExternalEmailContactHandler : IRequestHandler<CreateExternalEmailContactCommand, int>
    {
        private readonly IApplicationDbContext _context;
        public CreateExternalEmailContactHandler(IApplicationDbContext context) => _context = context;

        public async Task<int> Handle(CreateExternalEmailContactCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                throw new DomainException("Geçerli bir e-posta adresi giriniz.");

            var contact = new ExternalEmailContact
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Note = request.Note?.Trim(),
                IsActive = true
            };
            _context.ExternalEmailContacts.Add(contact);
            await _context.SaveChangesAsync(cancellationToken);
            return contact.Id;
        }
    }

    public record UpdateExternalEmailContactCommand(int Id, string Name, string Email, string? Note, bool IsActive) : IRequest;

    public class UpdateExternalEmailContactHandler : IRequestHandler<UpdateExternalEmailContactCommand>
    {
        private readonly IApplicationDbContext _context;
        public UpdateExternalEmailContactHandler(IApplicationDbContext context) => _context = context;

        public async Task Handle(UpdateExternalEmailContactCommand request, CancellationToken cancellationToken)
        {
            var contact = await _context.ExternalEmailContacts.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException("Kişi bulunamadı.");
            contact.Name = request.Name.Trim();
            contact.Email = request.Email.Trim();
            contact.Note = request.Note?.Trim();
            contact.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public record DeleteExternalEmailContactCommand(int Id) : IRequest;

    public class DeleteExternalEmailContactHandler : IRequestHandler<DeleteExternalEmailContactCommand>
    {
        private readonly IApplicationDbContext _context;
        public DeleteExternalEmailContactHandler(IApplicationDbContext context) => _context = context;

        public async Task Handle(DeleteExternalEmailContactCommand request, CancellationToken cancellationToken)
        {
            var contact = await _context.ExternalEmailContacts.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException("Kişi bulunamadı.");
            _context.ExternalEmailContacts.Remove(contact);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
