using Microsoft.Win32;

namespace Acorda.Services;

public static class StartupService
{
    private const string ChaveRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string NomeValor = "Acorda";

    public static void Sincronizar(bool deveIniciarComWindows)
    {
        using var chave = Registry.CurrentUser.OpenSubKey(ChaveRun, writable: true);
        if (chave is null) return;

        if (deveIniciarComWindows)
        {
            var caminhoExe = System.Environment.ProcessPath;
            if (!string.IsNullOrEmpty(caminhoExe))
                chave.SetValue(NomeValor, $"\"{caminhoExe}\"");
        }
        else
        {
            chave.DeleteValue(NomeValor, throwOnMissingValue: false);
        }
    }
}
