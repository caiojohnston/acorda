using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using Acorda.Services;
using Acorda.ViewModels;

namespace Acorda;

public partial class DashboardSemanalWindow : Window
{
    public DashboardSemanalWindow(TarefaService tarefas)
    {
        InitializeComponent();

        var stats = tarefas.EstatisticaSemanal();
        var max = Math.Max(stats.Values.DefaultIfEmpty(0).Max(), 1);
        var cultura = new CultureInfo("pt-BR");

        ListaDias.ItemsSource = stats.Select(kv => new DiaSemanaVM
        {
            Quantidade = kv.Value,
            AlturaBarra = 6 + kv.Value / (double)max * 90,
            DiaAbrev = cultura.DateTimeFormat.GetAbbreviatedDayName(kv.Key.DayOfWeek).TrimEnd('.')
        }).ToList();

        var total = stats.Values.Sum();
        var media = stats.Values.Average();

        TxtTotalSemana.Text = $"{total} tarefa(s) concluída(s)";
        TxtMediaDia.Text = $"média de {media:0.0} por dia";
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
