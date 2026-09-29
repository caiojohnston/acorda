using System;
using System.Threading;
using System.Windows;
using Acorda.Models;
using Acorda.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace Acorda;

public partial class App : Application
{
    private static Mutex? _mutexInstanciaUnica;
    private TaskbarIcon? _trayIcon;

    public StorageService Storage { get; private set; } = null!;
    public TarefaService Tarefas { get; private set; } = null!;
    public AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutexInstanciaUnica = new Mutex(true, "Acorda_App_SingleInstance", out bool criouNovo);
        if (!criouNovo)
        {
            MessageBox.Show("Acorda já está em execução (veja a bandeja do Windows).", "Acorda",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Storage = new StorageService();
        Tarefas = new TarefaService(Storage);
        Settings = Storage.CarregarSettings();
        StartupService.Sincronizar(Settings.IniciarComWindows);

        AplicarTema(Settings.Tema);
        ConfigurarTrayIcon();

        var janela = new MainWindow();
        MainWindow = janela;
        janela.Show();
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

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutexInstanciaUnica?.ReleaseMutex();
        base.OnExit(e);
    }
}
