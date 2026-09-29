using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Acorda.Models;
using Acorda.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace Acorda;

public partial class App : Application
{
    private const string NomePipeAtivacao = "Acorda_Ativar";

    private static Mutex? _mutexInstanciaUnica;
    private TaskbarIcon? _trayIcon;
    private bool _encerrando;

    public StorageService Storage { get; private set; } = null!;
    public TarefaService Tarefas { get; private set; } = null!;
    public AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutexInstanciaUnica = new Mutex(true, "Acorda_App_SingleInstance", out bool criouNovo);
        if (!criouNovo)
        {
            // Já tem uma instância rodando (provavelmente ancorada e invisível) — manda ela
            // se abrir em vez de só mostrar um aviso que pode passar despercebido atrás de outra janela.
            SinalizarInstanciaExistente();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Storage = new StorageService();
        Tarefas = new TarefaService(Storage);
        Settings = Storage.CarregarSettings();
        StartupService.Sincronizar(Settings.IniciarComWindows);

        AplicarTema(Settings.Tema);
        AplicarTamanhoFonte(Settings.Tamanho);
        ConfigurarTrayIcon();
        IniciarServidorAtivacao();

        var janela = new MainWindow();
        MainWindow = janela;
        janela.Show();
    }

    // Escuta pedidos de ativação vindos de uma segunda instância (ver SinalizarInstanciaExistente).
    private void IniciarServidorAtivacao()
    {
        Task.Run(async () =>
        {
            while (!_encerrando)
            {
                try
                {
                    using var server = new NamedPipeServerStream(NomePipeAtivacao, PipeDirection.In);
                    await server.WaitForConnectionAsync();
                    Dispatcher.Invoke(AbrirJanelaPrincipal);
                }
                catch
                {
                    break;
                }
            }
        });
    }

    private static void SinalizarInstanciaExistente()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", NomePipeAtivacao, PipeDirection.Out);
            client.Connect(500);
            client.WriteByte(1);
        }
        catch
        {
            // instância existente não respondeu a tempo — avisa do jeito antigo, como último recurso
            MessageBox.Show("Acorda já está em execução (veja a bandeja do Windows).", "Acorda",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ConfigurarTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            ToolTipText = "Acorda"
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var itemAbrir = new System.Windows.Controls.MenuItem { Header = "Abrir" };
        itemAbrir.Click += (_, _) => AbrirJanelaPrincipal();
        menu.Items.Add(itemAbrir);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var itemSair = new System.Windows.Controls.MenuItem { Header = "Sair" };
        itemSair.Click += (_, _) =>
        {
            if (MainWindow is MainWindow janela) janela.PermitirFechamento = true;
            Shutdown();
        };
        menu.Items.Add(itemSair);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => AbrirJanelaPrincipal();
    }

    private void AbrirJanelaPrincipal()
    {
        if (MainWindow is MainWindow janela)
        {
            janela.AbrirEmPrimeiroPlano();
        }
    }

    public void AplicarTema(Tema tema)
    {
        var arquivo = tema switch
        {
            Tema.Claro => "Themes/Light.xaml",
            Tema.Nostalgia => "Themes/Nostalgia.xaml",
            _ => "Themes/Dark.xaml"
        };

        var novoDicionario = new ResourceDictionary { Source = new Uri(arquivo, UriKind.Relative) };
        Resources.MergedDictionaries[0] = novoDicionario;

        Settings.Tema = tema;
        Storage.SalvarSettings(Settings);
    }

    public void AplicarTamanhoFonte(TamanhoJanela tamanho)
    {
        var (normal, pequena) = tamanho switch
        {
            TamanhoJanela.Media => (15d, 11d),
            TamanhoJanela.Grande => (17d, 12d),
            _ => (13d, 10d)
        };

        Resources["FontSizeNormal"] = normal;
        Resources["FontSizeSmall"] = pequena;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _encerrando = true;
        _trayIcon?.Dispose();
        _mutexInstanciaUnica?.ReleaseMutex();
        base.OnExit(e);
    }
}
