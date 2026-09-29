namespace Acorda.Models;

public class AppSettings
{
    public Tema Tema { get; set; } = Tema.Escuro;
    public bool IniciarComWindows { get; set; } = true;
    public TamanhoJanela Tamanho { get; set; } = TamanhoJanela.Pequena;
}
