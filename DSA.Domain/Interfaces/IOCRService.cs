namespace DSA.Domain.Interfaces;

using System.Threading.Tasks;

public interface IOCRService
{
    Task<string> ExtractTextAsync(byte[] content);
}
