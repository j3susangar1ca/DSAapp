namespace DSA.Domain.Interfaces;

using System.Threading.Tasks;

public interface IIAService
{
    Task<string> AnalyzeTextAsync(string text);
}
