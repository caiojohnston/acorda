using System;
using System.IO;
using System.Text.Json;
using Acorda.Models;

namespace Acorda.Services;

public class StorageService
{
    private static readonly string PastaApp = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Acorda");

    private static readonly string ArquivoDados = Path.Combine(PastaApp, "data.json");
    private static readonly string ArquivoSettings = Path.Combine(PastaApp, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public StorageService()
    {
        Directory.CreateDirectory(PastaApp);
    }

    public AppData CarregarDados()
    {
        if (!File.Exists(ArquivoDados))
            return new AppData();

        try
        {
            var json = File.ReadAllText(ArquivoDados);
            return JsonSerializer.Deserialize<AppData>(json, JsonOpts) ?? new AppData();
        }
        catch (JsonException)
        {
            return new AppData();
        }
    }

    public void SalvarDados(AppData dados)
    {
        var json = JsonSerializer.Serialize(dados, JsonOpts);
        File.WriteAllText(ArquivoDados, json);
    }

    public AppSettings CarregarSettings()
    {
        if (!File.Exists(ArquivoSettings))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(ArquivoSettings);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void SalvarSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        File.WriteAllText(ArquivoSettings, json);
    }
}
