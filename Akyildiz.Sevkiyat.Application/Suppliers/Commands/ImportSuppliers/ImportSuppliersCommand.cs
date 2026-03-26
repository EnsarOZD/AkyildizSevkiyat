using Akyildiz.Sevkiyat.Application.Interfaces;
using Akyildiz.Sevkiyat.Domain.Entities;
using ExcelDataReader;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;
using System.Text;

namespace Akyildiz.Sevkiyat.Application.Suppliers.Commands.ImportSuppliers
{
    public record ImportSuppliersCommand(Stream FileStream) : IRequest<int>;

    public class ImportSuppliersCommandHandler : IRequestHandler<ImportSuppliersCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public ImportSuppliersCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(ImportSuppliersCommand request, CancellationToken cancellationToken)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var reader = ExcelReaderFactory.CreateReader(request.FileStream);
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            var dataTable = result.Tables[0];
            int importedCount = 0;

            var existingSuppliers = await _context.Suppliers.ToListAsync(cancellationToken);

            foreach (DataRow row in dataTable.Rows)
            {
                var name = row[0]?.ToString()?.Trim();
                var code = row.ItemArray.Length > 1 ? row[1]?.ToString()?.Trim() : null;

                if (string.IsNullOrWhiteSpace(name)) continue;

                // Simple matching logic
                var existing = existingSuppliers.FirstOrDefault(x => 
                    (!string.IsNullOrEmpty(code) && x.SupplierCode == code) || 
                    x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Update if needed
                    bool changed = false;
                    if (!string.IsNullOrEmpty(code) && existing.SupplierCode != code)
                    {
                        existing.SupplierCode = code;
                        changed = true;
                    }
                    if (existing.Name != name)
                    {
                        existing.Name = name;
                        changed = true;
                    }
                    if(changed) _context.Suppliers.Update(existing);
                }
                else
                {
                    var supplier = new Supplier
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        SupplierCode = code,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Suppliers.Add(supplier);
                    importedCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return importedCount;
        }
    }
}
