namespace Akyildiz.Sevkiyat.Application.Common.Interfaces;

public interface IRequireRoles
{
    IReadOnlyList<string> AllowedRoles { get; }
}
