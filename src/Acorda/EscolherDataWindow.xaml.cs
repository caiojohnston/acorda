using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Acorda.ViewModels;

namespace Acorda;

public partial class EscolherDataWindow : Window
{
    private static readonly CultureInfo Cultura = new("pt-BR");
    private readonly DateTime _hoje = DateTime.Today;
    private DateTime _mesAtual;
    private CalendarioDiaVM? _diaSelecionado;

    public DateTime? DataSelecionada { get; private set; }

    public EscolherDataWindow()
    {
        InitializeComponent();
        _mesAtual = new DateTime(_hoje.Year, _hoje.Month, 1);
        GerarGrade();
    }

    private void GerarGrade()
    {
        var offset = (int)_mesAtual.DayOfWeek;
        var inicioGrade = _mesAtual.AddDays(-offset);
        var dataSelecionadaAnterior = _diaSelecionado?.Data;

        var celulas = new List<CalendarioDiaVM>();
        for (int i = 0; i < 42; i++)
        {
            var data = inicioGrade.AddDays(i);
            var celula = new CalendarioDiaVM(data, data.Month != _mesAtual.Month, _hoje);
            if (dataSelecionadaAnterior.HasValue && data.Date == dataSelecionadaAnterior.Value.Date)
                celula.Selecionado = true;
            celulas.Add(celula);
        }

        if (dataSelecionadaAnterior is null)
        {
            var hojeCelula = celulas.First(c => c.EhHoje);
            hojeCelula.Selecionado = true;
        }

        _diaSelecionado = celulas.FirstOrDefault(c => c.Selecionado);

        GradeDias.ItemsSource = celulas;

        var nomeMes = Cultura.DateTimeFormat.GetMonthName(_mesAtual.Month);
        TxtMesAno.Text = char.ToUpper(nomeMes[0], Cultura) + nomeMes[1..] + " " + _mesAtual.Year;
    }

    private void MesAnterior_Click(object sender, RoutedEventArgs e)
    {
        _mesAtual = _mesAtual.AddMonths(-1);
        GerarGrade();
    }

    private void ProximoMes_Click(object sender, RoutedEventArgs e)
    {
        _mesAtual = _mesAtual.AddMonths(1);
        GerarGrade();
    }

    private void Dia_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not CalendarioDiaVM vm || !vm.Selecionavel) return;

        if (_diaSelecionado is not null) _diaSelecionado.Selecionado = false;
        vm.Selecionado = true;
        _diaSelecionado = vm;
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        DataSelecionada = _diaSelecionado?.Data ?? _hoje;
        DialogResult = true;
    }
}
