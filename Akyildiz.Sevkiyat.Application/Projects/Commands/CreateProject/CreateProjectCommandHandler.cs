using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using MediatR;

namespace Akyildiz.Sevkiyat.Application.Projects.Commands.CreateProject
{
    public class CreateProjectCommandHandler
        : IRequestHandler<CreateProjectCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public CreateProjectCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var project = new Project
            {
                Code = request.IssProjectCode,
                Name = request.Name,
                Region = request.Region,
                IsActive = true
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);

            return project.Id;
        }
    }
}
