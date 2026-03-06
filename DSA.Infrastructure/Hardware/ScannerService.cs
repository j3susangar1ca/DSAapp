namespace DSA.Infrastructure.Hardware;

using System;
using System.Threading.Tasks;
using DSA.Application.Interfaces;

public sealed class ScannerService : IScannerService
{
    public bool IsDeviceBusy { get; private set; }

    public async Task<byte[]> CaptureAsync(Guid documentoId)
    {
        if (IsDeviceBusy)
        {
            throw new InvalidOperationException("Hardware TWAIN/WIA ocupado por otro subproceso.");
        }

        IsDeviceBusy = true;

        try
        {
            return await Task.Run(() =>
            {
                Task.Delay(2500).Wait(); 
                return [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]; 
            });
        }
        finally
        {
            IsDeviceBusy = false;
        }
    }
}