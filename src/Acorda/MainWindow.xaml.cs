using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Acorda.Models;
using Acorda.Native;
using Acorda.Services;
using Acorda.ViewModels;

namespace Acorda;

public partial class MainWindow : Window
{
    private TarefaService Tarefas => ((App)Application.Current).Tarefas;

    private DateTime? _dataAgendadaPendente;
    private DispatcherTimer? _watchdogAncoragem;
    public bool PermitirFechamento { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        using (var icone = Native.IconFactory.CriarIconePlaceholder())
        {
            Icon = Imaging.CreateBitmapSourceFromHIcon(icone.Handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        AncorarNoDesktop();
        CarregarLista();
        CarregarEstatistica();

        _watchdogAncoragem = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _watchdogAncoragem.Tick += (_, _) =>
        {
            if (!ShowInTaskbar && !DesktopPinner.ParentAindaValido())
                AncorarNoDesktop();
        };
        _watchdogAncoragem.Start();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (PermitirFechamento) return;
        e.Cancel = true;
        AncorarNoDesktop();
    }

    private IntPtr Hwnd => new WindowInteropHelper(this).Handle;

    private void AncorarNoDesktop()
    {
        ShowInTaskbar = false;
        DesktopPinner.Ancorar(Hwnd);
    }

    public void AbrirEmPrimeiroPlano()
    {
        DesktopPinner.Desancorar(Hwnd);
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Show();
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch (InvalidOperationException) { /* botão já solto */ }
    }

    private void Minimizar_Click(object sender, RoutedEventArgs e)
    {
        if (ShowInTaskbar)
            WindowState = WindowState.Minimized;
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => AncorarNoDesktop();

    // ---- Lista de tarefas ----

    private void CarregarLista()
    {
        var visiveis = Tarefas.ListarVisiveisHoje();
        ListaTarefas.ItemsSource = visiveis
            .Select(t => new TarefaItemVM(t, Tarefas.EstaConcluidaHoje(t)))
            .ToList();
    }

    private void CarregarEstatistica()
    {
        var stats = Tarefas.EstatisticaSemanal();
        var max = Math.Max(stats.Values.DefaultIfEmpty(0).Max(), 1);

        ListaEstatistica.ItemsSource = stats.Select(kv => new StatDiaVM
        {
            AlturaBarra = 4 + kv.Value / (double)max * 18,
            Tooltip = $"{kv.Key:dd/MM}: {kv.Value} concluída(s)"
        }).ToList();
    }

    private static TarefaItemVM? ObterVM(object sender) =>
        (sender as FrameworkElement)?.DataContext as TarefaItemVM;

    private void Circulo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var vm = ObterVM(sender);
        if (vm is null) return;

        Tarefas.AlternarConclusao(vm.Id);
        vm.Concluida = !vm.Concluida;
        CarregarEstatistica();
    }

    private void Excluir_Click(object sender, RoutedEventArgs e)
    {
        var vm = ObterVM(sender);
        if (vm is null) return;

        Tarefas.Excluir(vm.Id);
        CarregarLista();
        CarregarEstatistica();
    }

    private void AdiarAmanha_Click(object sender, RoutedEventArgs e)
    {
        var vm = ObterVM(sender);
        if (vm is null) return;

        Tarefas.Adiar(vm.Id, DateTime.Today.AddDays(1));
        CarregarLista();
    }

    private void AdiarEscolherData_Click(object sender, RoutedEventArgs e)
    {
        var vm = ObterVM(sender);
        if (vm is null) return;

        var dlg = new EscolherDataWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.DataSelecionada.HasValue)
        {
            Tarefas.Adiar(vm.Id, dlg.DataSelecionada.Value);
            CarregarLista();
        }
    }

    // ---- Adicionar tarefa ----

    private void BtnAgendar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new EscolherDataWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.DataSelecionada.HasValue)
        {
            _dataAgendadaPendente = dlg.DataSelecionada.Value;
            BtnAgendar.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            BtnAgendar.ToolTip = $"Agendada para {_dataAgendadaPendente:dd/MM}";
        }
    }

    private void AdicionarTarefa_Click(object sender, RoutedEventArgs e) => AdicionarTarefa();

    private void TxtNovaTarefa_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AdicionarTarefa();
    }

    private void AdicionarTarefa()
    {
        var texto = TxtNovaTarefa.Text.Trim();
        if (string.IsNullOrEmpty(texto)) return;

        if (_dataAgendadaPendente.HasValue)
        {
            Tarefas.AdicionarAgendada(texto, _dataAgendadaPendente.Value);
            _dataAgendadaPendente = null;
            BtnAgendar.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
            BtnAgendar.ToolTip = "Agendar para outro dia";
        }
        else
        {
            Tarefas.AdicionarFixa(texto);
        }

        TxtNovaTarefa.Clear();
        CarregarLista();
        CarregarEstatistica();
    }

    // ---- Tema ----

    private void BtnConfig_Click(object sender, RoutedEventArgs e)
    {
        ChkIniciarComWindows.IsChecked = ((App)Application.Current).Settings.IniciarComWindows;
        PopupTemas.IsOpen = true;
    }

    private void ChkIniciarComWindows_Changed(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        app.Settings.IniciarComWindows = ChkIniciarComWindows.IsChecked == true;
        app.Storage.SalvarSettings(app.Settings);
        Services.StartupService.Sincronizar(app.Settings.IniciarComWindows);
    }

    private void TemaEscuro_Click(object sender, RoutedEventArgs e) => AplicarTema(Tema.Escuro);
    private void TemaClaro_Click(object sender, RoutedEventArgs e) => AplicarTema(Tema.Claro);
    private void TemaNostalgia_Click(object sender, RoutedEventArgs e) => AplicarTema(Tema.Nostalgia);

    private void AplicarTema(Tema tema)
    {
        ((App)Application.Current).AplicarTema(tema);
        PopupTemas.IsOpen = false;
    }
}
