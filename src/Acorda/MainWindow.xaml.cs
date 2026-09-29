using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

    private const int DuracaoAnimacaoMs = 180;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        using (var icone = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!))
        {
            Icon = Imaging.CreateBitmapSourceFromHIcon(icone!.Handle, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        AplicarTamanho(((App)Application.Current).Settings.Tamanho);
        AncorarNoDesktop();
        CarregarLista();
        CarregarEstatistica();

        // Reforça a posição no fundo do Z-order periodicamente: nenhuma outra janela
        // deveria conseguir "roubar" o fundo da pilha permanentemente, mas isso garante.
        _watchdogAncoragem = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _watchdogAncoragem.Tick += (_, _) =>
        {
            if (!ShowInTaskbar && DesktopPinner.JanelaValida(Hwnd))
                DesktopPinner.ReforcarFundo(Hwnd);
        };
        _watchdogAncoragem.Start();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (PermitirFechamento) return;
        e.Cancel = true;
        AnimarFechar(AncorarNoDesktop);
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal && Opacity < 1)
            AnimarAbrir();
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
        AnimarAbrir();
    }

    // Anima fade + encolhimento e só executa a mudança de estado real ao terminar
    // (WPF não anima minimizar/restaurar nativamente em janelas AllowsTransparency=True).
    private void AnimarFechar(Action aoTerminar)
    {
        var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        var duracao = TimeSpan.FromMilliseconds(DuracaoAnimacaoMs);

        var sb = new Storyboard();
        sb.Children.Add(CriarAnimacao(this, OpacityProperty, 1, 0, duracao, easing));
        sb.Children.Add(CriarAnimacao(EscalaJanela, ScaleTransform.ScaleXProperty, 1, 0.92, duracao, easing));
        sb.Children.Add(CriarAnimacao(EscalaJanela, ScaleTransform.ScaleYProperty, 1, 0.92, duracao, easing));

        sb.Completed += (_, _) => aoTerminar();
        sb.Begin();
    }

    private void AnimarAbrir()
    {
        Opacity = 0;
        EscalaJanela.ScaleX = EscalaJanela.ScaleY = 0.92;

        var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var duracao = TimeSpan.FromMilliseconds(DuracaoAnimacaoMs);

        var sb = new Storyboard();
        sb.Children.Add(CriarAnimacao(this, OpacityProperty, 0, 1, duracao, easing));
        sb.Children.Add(CriarAnimacao(EscalaJanela, ScaleTransform.ScaleXProperty, 0.92, 1, duracao, easing));
        sb.Children.Add(CriarAnimacao(EscalaJanela, ScaleTransform.ScaleYProperty, 0.92, 1, duracao, easing));
        sb.Begin();
    }

    private static DoubleAnimation CriarAnimacao(DependencyObject alvo, DependencyProperty propriedade,
        double de, double para, TimeSpan duracao, IEasingFunction easing)
    {
        var anim = new DoubleAnimation(de, para, duracao) { EasingFunction = easing };
        Storyboard.SetTarget(anim, alvo);
        Storyboard.SetTargetProperty(anim, new PropertyPath(propriedade));
        return anim;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch (InvalidOperationException) { /* botão já solto */ }
    }

    private void Minimizar_Click(object sender, RoutedEventArgs e)
    {
        if (ShowInTaskbar)
            AnimarFechar(() => WindowState = WindowState.Minimized);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();

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
            AlturaBarra = 4 + kv.Value / (double)max * 18
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

    private bool _painelInputAberto;

    private void AbrirPainelInput()
    {
        _painelInputAberto = true;
        PainelInput.Visibility = Visibility.Visible;
        BtnAgendar.Visibility = Visibility.Visible;
        BtnAdicionar.Content = "✓";
        TxtNovaTarefa.Focus();
    }

    private void FecharPainelInput()
    {
        _painelInputAberto = false;
        PainelInput.Visibility = Visibility.Collapsed;
        BtnAgendar.Visibility = Visibility.Collapsed;
        BtnAdicionar.Content = "+";
        TxtNovaTarefa.Clear();
        _dataAgendadaPendente = null;
        BtnAgendar.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
        BtnAgendar.ToolTip = "Agendar para outro dia";
    }

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

    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        if (!_painelInputAberto)
        {
            AbrirPainelInput();
            return;
        }

        if (!TentarAdicionarTarefa())
            FecharPainelInput();
    }

    private void TxtNovaTarefa_TextChanged(object sender, TextChangedEventArgs e)
    {
        TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtNovaTarefa.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TxtNovaTarefa_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TentarAdicionarTarefa();
        else if (e.Key == Key.Escape) FecharPainelInput();
    }

    // Retorna true se uma tarefa foi de fato adicionada.
    private bool TentarAdicionarTarefa()
    {
        var texto = TxtNovaTarefa.Text.Trim();
        if (string.IsNullOrEmpty(texto)) return false;

        if (_dataAgendadaPendente.HasValue)
            Tarefas.AdicionarAgendada(texto, _dataAgendadaPendente.Value);
        else
            Tarefas.AdicionarFixa(texto);

        FecharPainelInput();
        CarregarLista();
        CarregarEstatistica();
        return true;
    }

    // ---- Tema ----

    private void BtnConfig_Click(object sender, RoutedEventArgs e)
    {
        AtualizarVisualToggle(((App)Application.Current).Settings.IniciarComWindows);
        PopupTemas.IsOpen = true;
    }

    private void AtualizarVisualToggle(bool ligado)
    {
        BolinhaToggle.HorizontalAlignment = ligado ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        BolinhaToggle.Margin = ligado ? new Thickness(0, 0, 1, 0) : new Thickness(1, 0, 0, 0);
        TrilhoToggle.Background = ligado ? (System.Windows.Media.Brush)FindResource("AccentBrush") : System.Windows.Media.Brushes.Transparent;
        BolinhaToggle.Background = ligado
            ? (System.Windows.Media.Brush)FindResource("WindowBackgroundBrush")
            : (System.Windows.Media.Brush)FindResource("MutedForegroundBrush");
    }

    private void ToggleIniciar_Click(object sender, MouseButtonEventArgs e)
    {
        var app = (App)Application.Current;
        var ligado = !app.Settings.IniciarComWindows;

        app.Settings.IniciarComWindows = ligado;
        app.Storage.SalvarSettings(app.Settings);
        Services.StartupService.Sincronizar(ligado);
        AtualizarVisualToggle(ligado);
    }

    private void TemaEscuro_Click(object sender, RoutedEventArgs e) => AplicarTema(Tema.Escuro);
    private void TemaClaro_Click(object sender, RoutedEventArgs e) => AplicarTema(Tema.Claro);
    private void TemaNostalgia_Click(object sender, RoutedEventArgs e) => AplicarTema(Tema.Nostalgia);

    private void AplicarTema(Tema tema)
    {
        ((App)Application.Current).AplicarTema(tema);
        PopupTemas.IsOpen = false;
    }

    // ---- Tamanho da janela ----

    private void TamanhoPequena_Click(object sender, RoutedEventArgs e) => AplicarTamanho(TamanhoJanela.Pequena);
    private void TamanhoMedia_Click(object sender, RoutedEventArgs e) => AplicarTamanho(TamanhoJanela.Media);
    private void TamanhoGrande_Click(object sender, RoutedEventArgs e) => AplicarTamanho(TamanhoJanela.Grande);

    private void AplicarTamanho(TamanhoJanela tamanho)
    {
        (Width, Height) = tamanho switch
        {
            TamanhoJanela.Media => (300d, 560d),
            TamanhoJanela.Grande => (340d, 680d),
            _ => (250d, 460d)
        };

        var app = (App)Application.Current;
        app.AplicarTamanhoFonte(tamanho);
        app.Settings.Tamanho = tamanho;
        app.Storage.SalvarSettings(app.Settings);
        PopupTemas.IsOpen = false;
    }

    // ---- Dashboard semanal ----

    private void Estatistica_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        new DashboardSemanalWindow(Tarefas) { Owner = this }.ShowDialog();
    }
}
